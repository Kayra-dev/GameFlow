using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace GameFlow.Api.Extensions;

/// <summary>
/// ASP.NET Core 10'un yerleşik OpenAPI üreticisini yapılandırır.
/// Dokümantasyon arayüzü olarak Scalar kullanılır.
/// </summary>
public static class OpenApiExtensions
{
    public static IServiceCollection AddGameFlowOpenApi(this IServiceCollection services)
    {
        services.AddOpenApi("v1", options =>
        {
            options.AddDocumentTransformer((document, _, _) =>
            {
                document.Info = new OpenApiInfo
                {
                    Title = "GameFlow API",
                    Version = "v1",
                    Description = "Oyun geliştirme ekipleri için proje yönetim sistemi API'si."
                };

                document.Components ??= new OpenApiComponents();
                document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
                document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description = "JWT erişim tokenı."
                };

                return Task.CompletedTask;
            });

            // Yetki gerektiren uç noktalar dokümantasyonda kilitli görünsün.
            options.AddOperationTransformer((operation, context, _) =>
            {
                var requiresAuth = context.Description.ActionDescriptor.EndpointMetadata
                    .OfType<Microsoft.AspNetCore.Authorization.IAuthorizeData>()
                    .Any();

                if (requiresAuth)
                {
                    operation.Security =
                    [
                        new OpenApiSecurityRequirement
                        {
                            [new OpenApiSecuritySchemeReference("Bearer")] = []
                        }
                    ];
                }

                return Task.CompletedTask;
            });
        });

        return services;
    }
}
