namespace Seam.Infrastructure.Persistence.Repositories;

using Microsoft.EntityFrameworkCore;
using Seam.Application.Persistence.Repositories;
using Seam.Domain.Entities;

/// <summary>
/// IWriteRepository'nin Entity Framework Core implementasyonu.
/// </summary>
/// <typeparam name="TEntity">IEntity'den türeyen entity tipi.</typeparam>
/// <typeparam name="TId">Entity'nin primary key tipi.</typeparam>
public class EfWriteRepository<TEntity, TId, TContext>(TContext context)
    : IWriteRepository<TEntity, TId>
    where TEntity : class, IEntity<TId>
    where TId : notnull
    where TContext : DbContext
{
    protected readonly DbSet<TEntity> DbSet = context.Set<TEntity>();

    /// <inheritdoc />
    public async Task AddAsync(
        TEntity entity,
        CancellationToken cancellationToken = default)
        => await DbSet.AddAsync(entity, cancellationToken);

    /// <inheritdoc />
    public void Update(TEntity entity)
        => DbSet.Update(entity);

    /// <inheritdoc />
    public void Delete(TEntity entity)
        => DbSet.Remove(entity);

    /// <inheritdoc />
    public void HardDelete(TEntity entity)
        => DbSet.Remove(entity);
}