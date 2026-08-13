using GameFlow.Application.Common.Interfaces;
using GameFlow.Domain.Entities;
using GameFlow.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace GameFlow.Infrastructure.Persistence.Seed;

/// <summary>
/// Uygulama açılışında bekleyen migration'ları uygular ve sistemin çalışması için
/// zorunlu olan kayıtları oluşturur: ilk yönetici hesabı ve lider sohbet odası.
/// Örnek/mock veri üretilmez.
/// </summary>
public class DatabaseInitializer(
    ApplicationDbContext context,
    IPasswordHasher passwordHasher,
    IConfiguration configuration,
    ILogger<DatabaseInitializer> logger)
{
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        var pending = await context.Database.GetPendingMigrationsAsync(cancellationToken);

        if (pending.Any())
        {
            logger.LogInformation("Bekleyen {Count} migration uygulanıyor.", pending.Count());
            await context.Database.MigrateAsync(cancellationToken);
        }

        await SeedAdminUserAsync(cancellationToken);
        await SeedLeadersChatRoomAsync(cancellationToken);
    }

    /// <summary>
    /// Sistemde hiç yönetici yoksa yapılandırmada verilen bilgilerle ilk yöneticiyi oluşturur.
    /// Kayıt ekranı bulunmadığı için bu hesap olmadan sisteme giriş yapılamaz.
    /// </summary>
    private async Task SeedAdminUserAsync(CancellationToken cancellationToken)
    {
        var adminExists = await context.Users
            .IgnoreQueryFilters()
            .AnyAsync(u => u.RoleId == (int)SystemRole.Admin, cancellationToken);

        if (adminExists)
        {
            return;
        }

        var email = configuration["Seed:AdminEmail"];
        var password = configuration["Seed:AdminPassword"];
        var fullName = configuration["Seed:AdminFullName"];

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            logger.LogWarning(
                "Yönetici hesabı oluşturulamadı: 'Seed:AdminEmail' ve 'Seed:AdminPassword' " +
                "değerleri tanımlanmalıdır.");
            return;
        }

        var admin = new User
        {
            FullName = string.IsNullOrWhiteSpace(fullName) ? "Sistem Yöneticisi" : fullName,
            Email = email.Trim().ToLowerInvariant(),
            PasswordHash = passwordHasher.Hash(password),
            RoleId = (int)SystemRole.Admin,
            JobTitle = "Stüdyo Yöneticisi",
            IsActive = true,
            // İlk giriş sonrası şifrenin değiştirilmesi beklenir.
            MustChangePassword = true
        };

        context.Users.Add(admin);
        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation("İlk yönetici hesabı oluşturuldu: {Email}", admin.Email);
    }

    /// <summary>Takım liderlerinin ortak kullandığı tek sohbet odasını oluşturur.</summary>
    private async Task SeedLeadersChatRoomAsync(CancellationToken cancellationToken)
    {
        var exists = await context.ChatRooms
            .AnyAsync(r => r.Type == ChatRoomType.Leaders, cancellationToken);

        if (exists)
        {
            return;
        }

        context.ChatRooms.Add(new ChatRoom
        {
            Type = ChatRoomType.Leaders,
            Name = "Lider Sohbeti",
            Description = "Yalnızca takım liderleri ve yöneticiler görebilir.",
            IsSystem = true
        });

        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Lider sohbet odası oluşturuldu.");
    }
}
