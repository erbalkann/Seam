namespace Seam.Application.Persistence.Repositories;

using System.Linq.Expressions;
using Seam.Domain.Entities;

/// <summary>
/// Okuma operasyonları için soyut repository sözleşmesi.
/// CQRS pattern'inde Query tarafı bu interface'e bağımlıdır.
/// Sıfır yan etki — yalnızca veri okur, asla değiştirmez.
/// </summary>
/// <typeparam name="TEntity">IEntity'den türeyen entity tipi.</typeparam>
/// <typeparam name="TId">Entity'nin primary key tipi.</typeparam>
public interface IReadRepository<TEntity, TId>
    where TEntity : class, IEntity<TId>
    where TId : notnull
{
    /// <summary>
    /// Birincil anahtara göre entity getirir.
    /// Bulunamazsa null döner.
    /// </summary>
    Task<TEntity?> GetByIdAsync(TId id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Tüm entity'leri getirir.
    /// Soft-delete uygulayan entity'lerde silinmiş kayıtlar hariç tutulur.
    /// </summary>
    Task<IEnumerable<TEntity>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Verilen koşulu sağlayan entity'leri getirir.
    /// </summary>
    Task<IEnumerable<TEntity>> FindAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Verilen koşulu sağlayan tek bir entity getirir.
    /// Birden fazla sonuç varsa exception fırlatır.
    /// Bulunamazsa null döner.
    /// </summary>
    Task<TEntity?> FindSingleAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Verilen koşulu sağlayan entity sayısını döner.
    /// </summary>
    Task<int> CountAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default);
}