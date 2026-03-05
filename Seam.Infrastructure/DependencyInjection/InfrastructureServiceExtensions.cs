namespace Seam.Infrastructure.DependencyInjection;

using Microsoft.Extensions.DependencyInjection;
using Seam.Application.Persistence;
using Seam.Application.Persistence.Repositories;
using Seam.Domain.Mapping;
using Seam.Infrastructure.Mapping;
using Seam.Infrastructure.Persistence;
using Seam.Infrastructure.Persistence.Decorators;
using Seam.Infrastructure.Persistence.Repositories;

public static class InfrastructureServiceExtensions
{
    /// <summary>
    /// Infrastructure katmanının tüm servislerini DI container'a kaydeder.
    ///
    /// Kapsam:
    ///   1. Repository implementasyonları (EfReadRepository, EfWriteRepository)
    ///   2. AuditLogging decorator'ları (Scrutor ile sarılır)
    ///   3. UnitOfWork
    ///   4. AutoMapper + IMapper adapter
    /// </summary>
    public static IServiceCollection AddSeamInfrastructure(
        this IServiceCollection services)
    {
        services
            .AddSeamRepositories()
            .AddSeamUnitOfWork()
            .AddSeamMapping();

        return services;
    }

    // ── Repository Kayıtları ──────────────────────────────────

    /// <summary>
    /// EF Core repository implementasyonlarını ve
    /// AuditLogging decorator'larını Scrutor ile kaydeder.
    ///
    /// Kayıt sırası:
    ///   1. Concrete implementasyon (EfReadRepository vb.) register edilir.
    ///   2. Scrutor'ın Decorate metodu, ilgili interface'i decorator ile sarar.
    ///   3. DI container IReadRepository&lt;T,TId&gt; istediğinde
    ///      AuditLoggingReadRepositoryDecorator döner —
    ///      içinde EfReadRepository çalışır.
    /// </summary>
    public static IServiceCollection AddSeamRepositories(
        this IServiceCollection services)
    {
        // Concrete implementasyonlar — open generic kayıt
        services.AddScoped(
            typeof(IReadRepository<,>),
            typeof(EfReadRepository<,,>));

        services.AddScoped(
            typeof(IWriteRepository<,>),
            typeof(EfWriteRepository<,,>));

        // Scrutor decorator kayıtları
        services.Decorate(
            typeof(IReadRepository<,>),
            typeof(AuditLoggingReadRepositoryDecorator<,>));

        services.Decorate(
            typeof(IWriteRepository<,>),
            typeof(AuditLoggingWriteRepositoryDecorator<,>));

        return services;
    }

    // ── UnitOfWork Kaydı ─────────────────────────────────────

    /// <summary>
    /// IUnitOfWork implementasyonunu DI container'a kaydeder.
    /// </summary>
    public static IServiceCollection AddSeamUnitOfWork(
        this IServiceCollection services)
    {
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        return services;
    }

    // ── Mapping Kaydı ────────────────────────────────────────

    /// <summary>
    /// AutoMapper ve IMapper adapter'ını DI container'a kaydeder.
    /// Convention-based mapping ile aynı isimli property'ler
    /// otomatik eşleşir — ek konfigürasyon gerekmez.
    /// </summary>
    public static IServiceCollection AddSeamMapping(
        this IServiceCollection services)
    {
        // AutoMapper — convention-based, sıfır konfigürasyon
        services.AddAutoMapper(cfg => { });

        // Seam.Domain.Mapping.IMapper → AutoMapperAdapter
        services.AddScoped<IMapper, AutoMapperAdapter>();

        return services;
    }
}