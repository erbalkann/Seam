namespace Seam.Application.Persistence.Repositories;

using Seam.Domain.Entities;

/// <summary>
/// Yazma operasyonları için soyut repository sözleşmesi.
/// CQRS pattern'inde Command tarafı bu interface'e bağımlıdır.
/// Tüm değişiklikler UnitOfWork.SaveChangesAsync() ile kalıcı hale gelir.
/// </summary>
/// <typeparam name="TEntity">IEntity'den türeyen entity tipi.</typeparam>
/// <typeparam name="TId">Entity'nin primary key tipi.</typeparam>
public interface IWriteRepository<TEntity, TId>
    where TEntity : class, IEntity<TId>
    where TId : notnull
{
    /// <summary>
    /// Yeni entity'yi change tracker'a ekler.
    /// Kalıcı hale gelmesi için SaveChangesAsync() çağrılmalıdır.
    /// </summary>
    Task AddAsync(TEntity entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Mevcut entity'yi günceller.
    /// Kalıcı hale gelmesi için SaveChangesAsync() çağrılmalıdır.
    /// </summary>
    void Update(TEntity entity);

    /// <summary>
    /// ISoftDeletable entity'leri mantıksal olarak siler.
    /// ISoftDeletable değilse fiziksel silme yapar.
    /// Kalıcı hale gelmesi için SaveChangesAsync() çağrılmalıdır.
    /// </summary>
    void Delete(TEntity entity);

    /// <summary>
    /// Entity'yi veritabanından fiziksel olarak siler.
    /// Soft-delete uygulansa dahi kaydı tamamen kaldırır.
    /// Kalıcı hale gelmesi için SaveChangesAsync() çağrılmalıdır.
    /// </summary>
    void HardDelete(TEntity entity);
}