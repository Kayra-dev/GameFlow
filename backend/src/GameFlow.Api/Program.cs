using GameFlow.Api.Extensions;
using GameFlow.Api.Filters;
using GameFlow.Api.Hubs;
using GameFlow.Api.Middleware;
using GameFlow.Api.Realtime;
using GameFlow.Api.Services;
using GameFlow.Application;
using GameFlow.Application.Common.Interfaces;
using GameFlow.Infrastructure;
using GameFlow.Infrastructure.Persistence.Seed;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.FileProviders;
using Scalar.AspNetCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// ---------- Günlükleme ----------
builder.Host.UseSerilog((context, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console());

// ---------- Katmanlar ----------
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// ---------- API ----------
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

builder.Services.AddControllers(options =>
{
    options.Filters.Add<ValidationFilter>();

    // Uç nokta adresleri küçük harfli kebab-case olur: /api/work-items
    options.Conventions.Add(new RouteTokenTransformerConvention(new KebabCaseRouteTransformer()));
});
builder.Services.AddProblemDetails();
builder.Services.AddSignalR(options =>
{
    // Bağlantı kopmalarını erken yakalamak için varsayılandan kısa aralıklar.
    options.KeepAliveInterval = TimeSpan.FromSeconds(10);
    options.ClientTimeoutInterval = TimeSpan.FromSeconds(30);
    options.EnableDetailedErrors = builder.Environment.IsDevelopment();
});

// Çevrimiçi durum izleyicisi tek örnek olmalı; bağlantı sayımı bellekte tutulur.
builder.Services.AddSingleton<PresenceTracker>();

// Infrastructure'daki etkisiz uygulamalar SignalR destekli olanlarla değiştirilir.
// Uygulama katmanı yalnızca arayüzleri tanıdığı için kodunda değişiklik gerekmez.
builder.Services.AddScoped<IRealtimeNotifier, SignalRRealtimeNotifier>();
builder.Services.AddScoped<IChatNotifier, SignalRChatNotifier>();
builder.Services.AddResponseCompression(options => options.EnableForHttps = true);

builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddGameFlowCors(builder.Configuration);
builder.Services.AddGameFlowOpenApi();
builder.Services.AddGameFlowRateLimiting(builder.Configuration);

// Ters vekil (Render/Railway) arkasında gerçek istemci IP'si ve şeması korunur.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

var app = builder.Build();

// Migration'lar ve zorunlu başlangıç kayıtları uygulanır.
await using (var scope = app.Services.CreateAsyncScope())
{
    var initializer = scope.ServiceProvider.GetRequiredService<DatabaseInitializer>();
    await initializer.InitializeAsync();
}

// Düz metin şifre modu açıkken bunun gözden kaçmaması gerekir.
if (builder.Configuration.GetValue<bool>("Security:StorePasswordsAsPlainText"))
{
    app.Logger.LogWarning(
        "GÜVENLİK UYARISI: Şifreler veritabanına DÜZ METİN olarak yazılıyor " +
        "(Security:StorePasswordsAsPlainText = true). Bu ayar yalnızca geliştirme " +
        "içindir ve yayına alınmadan önce kapatılmalıdır.");
}

app.UseForwardedHeaders();

// Sıra önemli: istek kaydı en dışta olmalı ki istisna middleware'inin ürettiği
// 401/403/404 yanıtları gerçek durum kodlarıyla loglanabilsin.
app.UseSerilogRequestLogging();
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseResponseCompression();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference("/docs", options => options.WithTitle("GameFlow API"));
}

app.UseCors(CorsExtensions.PolicyName);

// Dosyalar veritabanında tutuluyorsa disk üzerinde sunulacak bir şey yoktur;
// statik dosya katmanı yalnızca Local sağlayıcıda kurulur. Aksi halde
// /api/files yolunu gölgeler ve boş bir dizin için gereksiz disk erişimi yapar.
var storageProvider = builder.Configuration["FileStorage:Provider"];

if (!string.Equals(storageProvider, "Database", StringComparison.OrdinalIgnoreCase))
{
    // Yüklenen dosyalar statik olarak sunulur. Kök dizin dışına çıkılamaz ve
    // bilinmeyen uzantılar indirilmek üzere sunulur (tarayıcıda çalıştırılmaz).
    var uploadRoot = Path.GetFullPath(
        builder.Configuration["FileStorage:RootPath"] ?? "uploads",
        builder.Environment.ContentRootPath);

    Directory.CreateDirectory(uploadRoot);

    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(uploadRoot),
        RequestPath = builder.Configuration["FileStorage:PublicBasePath"] ?? "/uploads",
        ServeUnknownFileTypes = false,
        OnPrepareResponse = context =>
        {
            // Yüklenen içerik hiçbir koşulda site bağlamında çalıştırılmamalı.
            context.Context.Response.Headers.ContentDisposition = "inline";
            context.Context.Response.Headers["X-Content-Type-Options"] = "nosniff";
        }
    });
}
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers().RequireRateLimiting(RateLimitingExtensions.GlobalPolicy);

// Barındırma platformlarının servis sağlığını kontrol etmesi için.
// Hub'lar ayrı origin'deki (GitHub Pages) istemciden çağrıldığı için CORS
// politikası açıkça bağlanır.
app.MapHub<ChatHub>("/hubs/chat").RequireCors(CorsExtensions.PolicyName);
app.MapHub<PresenceHub>("/hubs/presence").RequireCors(CorsExtensions.PolicyName);

app.MapGet("/health", () => Results.Ok(new { status = "sağlıklı", time = DateTime.UtcNow }))
    .AllowAnonymous()
    .WithName("HealthCheck");

app.Run();
