namespace Seam.Infrastructure.Persistence.Decorators;

using System.Linq.Expressions;
using Serilog;
using Seam.Application.Persistence.Repositories;
using Seam.Domain.Entities;

/// <summary>
/// IReadRepository üzerine uygulanan AuditLogging decorator'ı.
/// Tüm okuma operasyonlarını — metod adı, entity tipi ve süre —
/// Serilog ile loglar. Asıl repository implementasyonunu sarar,
/// davranışını değiştirmez.
/// </summary>
public sealed class AuditLoggingReadRepositoryDecorator<TEntity, TId>(
    IReadRepository<TEntity, TId> inner,
    ILogger logger)
    : IReadRepository<TEntity, TId>
    where TEntity : class, IEntity<TId>
    where TId : notnull
{
    private readonly string _entityName = typeof(TEntity).Name;

    public async Task<TEntity?> GetByIdAsync(
        TId id,
        CancellationToken cancellationToken = default)
    {
        using var _ = BeginOperation(nameof(GetByIdAsync));
        var result = await inner.GetByIdAsync(id, cancellationToken);

        logger.Information(
            "[ReadRepository] {Operation} | Entity: {Entity} | Id: {Id} | Found: {Found}",
            nameof(GetByIdAsync), _entityName, id, result is not null);

        return result;
    }

    public async Task<IEnumerable<TEntity>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        using var _ = BeginOperation(nameof(GetAllAsync));
        var result = await inner.GetAllAsync(cancellationToken);
        var list = result.ToList();

        logger.Information(
            "[ReadRepository] {Operation} | Entity: {Entity} | Count: {Count}",
            nameof(GetAllAsync), _entityName, list.Count);

        return list;
    }

    public async Task<IEnumerable<TEntity>> FindAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        using var _ = BeginOperation(nameof(FindAsync));
        var result = await inner.FindAsync(predicate, cancellationToken);
        var list = result.ToList();

        logger.Information(
            "[ReadRepository] {Operation} | Entity: {Entity} | Count: {Count}",
            nameof(FindAsync), _entityName, list.Count);

        return list;
    }

    public async Task<TEntity?> FindSingleAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        using var _ = BeginOperation(nameof(FindSingleAsync));
        var result = await inner.FindSingleAsync(predicate, cancellationToken);

        logger.Information(
            "[ReadRepository] {Operation} | Entity: {Entity} | Found: {Found}",
            nameof(FindSingleAsync), _entityName, result is not null);

        return result;
    }

    public async Task<int> CountAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        using var _ = BeginOperation(nameof(CountAsync));
        var count = await inner.CountAsync(predicate, cancellationToken);

        logger.Information(
            "[ReadRepository] {Operation} | Entity: {Entity} | Count: {Count}",
            nameof(CountAsync), _entityName, count);

        return count;
    }

    // ── Yardımcı: operasyon başlangıcını loglar ───────────────
    private IDisposable BeginOperation(string operationName)
    {
        logger.Debug(
            "[ReadRepository] Starting {Operation} | Entity: {Entity}",
            operationName, _entityName);

        return Serilog.Context.LogContext.PushProperty("Operation", operationName);
    }
}