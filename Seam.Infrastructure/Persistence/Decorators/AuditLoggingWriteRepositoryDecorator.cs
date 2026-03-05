namespace Seam.Infrastructure.Persistence.Decorators;

using Serilog;
using Seam.Application.Persistence.Repositories;
using Seam.Domain.Entities;

/// <summary>
/// IWriteRepository üzerine uygulanan AuditLogging decorator'ı.
/// Tüm yazma operasyonlarını — metod adı, entity tipi ve entity Id'si —
/// Serilog ile loglar. Asıl repository implementasyonunu sarar,
/// davranışını değiştirmez.
/// </summary>
public sealed class AuditLoggingWriteRepositoryDecorator<TEntity, TId>(
    IWriteRepository<TEntity, TId> inner,
    ILogger logger)
    : IWriteRepository<TEntity, TId>
    where TEntity : class, IEntity<TId>
    where TId : notnull
{
    private readonly string _entityName = typeof(TEntity).Name;

    public async Task AddAsync(
        TEntity entity,
        CancellationToken cancellationToken = default)
    {
        logger.Information(
            "[WriteRepository] {Operation} | Entity: {Entity} | Id: {Id}",
            nameof(AddAsync), _entityName, entity.Id);

        await inner.AddAsync(entity, cancellationToken);
    }

    public void Update(TEntity entity)
    {
        logger.Information(
            "[WriteRepository] {Operation} | Entity: {Entity} | Id: {Id}",
            nameof(Update), _entityName, entity.Id);

        inner.Update(entity);
    }

    public void Delete(TEntity entity)
    {
        logger.Information(
            "[WriteRepository] {Operation} | Entity: {Entity} | Id: {Id}",
            nameof(Delete), _entityName, entity.Id);

        inner.Delete(entity);
    }

    public void HardDelete(TEntity entity)
    {
        logger.Warning(
            "[WriteRepository] {Operation} | Entity: {Entity} | Id: {Id} — PHYSICAL DELETE",
            nameof(HardDelete), _entityName, entity.Id);

        inner.HardDelete(entity);
    }
}