using System.Reflection;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace GameFlow.Application;

/// <summary>Application katmanının servis kayıtları.</summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        services.AddAutoMapper(configuration => configuration.AddMaps(assembly));
        services.AddValidatorsFromAssembly(assembly, includeInternalTypes: true);

        // Feature servisleri: *Service ile biten arayüz/implementasyon çiftleri
        // tek tek elle kaydedilir (aşağıdaki modül kayıtları).
        ApplicationServiceRegistration.AddApplicationServices(services);

        return services;
    }
}
