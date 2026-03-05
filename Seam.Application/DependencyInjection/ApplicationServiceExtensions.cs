namespace Seam.Application.DependencyInjection;

using System.Reflection;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Seam.Application.Behaviors;
using FluentValidation;

public static class ApplicationServiceExtensions
{
    /// <summary>
    /// Application katmanının servislerini DI container'a kaydeder.
    /// MediatR handler'ları, FluentValidation validator'ları ve
    /// pipeline behavior'ları bu metod ile register edilir.
    ///
    /// Behavior sırası (dıştan içe):
    ///   1. ExceptionHandlingBehavior  — tüm exception'ları yakalar
    ///   2. LoggingBehavior            — istek/yanıt loglar
    ///   3. ValidationBehavior         — FluentValidation çalıştırır
    ///   4. TransactionBehavior        — command'ları transaction'a sarar
    ///   5. Handler                    — asıl iş mantığı
    /// </summary>
    /// <param name="services">DI container.</param>
    /// <param name="assemblies">
    /// Handler ve validator'ların taranacağı assembly'ler.
    /// Örnek: Assembly.GetExecutingAssembly()
    /// </param>
    public static IServiceCollection AddSeamApplication(
        this IServiceCollection services,
        params Assembly[] assemblies)
    {
        // MediatR
        services.AddMediatR(cfg =>
        {
            foreach (var assembly in assemblies)
                cfg.RegisterServicesFromAssembly(assembly);
        });

        // FluentValidation — tüm validator'ları otomatik tarar
        foreach (var assembly in assemblies)
            services.AddValidatorsFromAssembly(assembly);

        // Pipeline Behaviors — kayıt sırası = çalışma sırası (dıştan içe)
        services.AddTransient(typeof(IPipelineBehavior<,>),
            typeof(ExceptionHandlingBehavior<,>));

        services.AddTransient(typeof(IPipelineBehavior<,>),
            typeof(LoggingBehavior<,>));

        services.AddTransient(typeof(IPipelineBehavior<,>),
            typeof(ValidationBehavior<,>));

        services.AddTransient(typeof(IPipelineBehavior<,>),
            typeof(TransactionBehavior<,>));

        return services;
    }
}