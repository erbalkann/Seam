namespace Seam.Infrastructure.Persistence.Repositories;

using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Seam.Application.Persistence.Repositories;
using Seam.Domain.Entities;

/// <summary>
/// IReadRepository'nin Entity Framework Core implementasyonu.
/// Tüm sorgular AsNoTracking ile çalışır.
/// </summary>
/// <typeparam name="TEntity">IEntity'den türeyen entity tipi.</typeparam>
/// <typeparam name="TId">Entity'nin primary key tipi.</typeparam>
public class EfReadRepository<TEntity, TId, TContext>(TContext context)
    : IReadRepository<TEntity, TId>
    where TEntity : class, IEntity<TId>
    where TId : notnull
    where TContext : DbContext
{
    protected readonly DbSet<TEntity> DbSet = context.Set<TEntity>();

    /// <inheritdoc />
    public async Task<TEntity?> GetByIdAsync(
        TId id,
        CancellationToken cancellationToken = default)
        => await DbSet
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id.Equals(id), cancellationToken);

    /// <inheritdoc />
    public async Task<IEnumerable<TEntity>> GetAllAsync(
        CancellationToken cancellationToken = default)
        => await DbSet
            .AsNoTracking()
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<IEnumerable<TEntity>> FindAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default)
        => await DbSet
            .AsNoTracking()
            .Where(predicate)
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<TEntity?> FindSingleAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default)
        => await DbSet
            .AsNoTracking()
            .SingleOrDefaultAsync(predicate, cancellationToken);

    /// <inheritdoc />
    public async Task<int> CountAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default)
        => await DbSet
            .AsNoTracking()
            .CountAsync(predicate, cancellationToken);
}