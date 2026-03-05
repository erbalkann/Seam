namespace Seam.Application.Persistence.Repositories;

using Seam.Domain.Entities;

/// <summary>
/// IReadRepository ve IWriteRepository'yi birleştiren sözleşme.
/// Hem okuma hem yazma gerektiren senaryolarda kullanılır.
/// Özellikle küçük aggregate'lerde ya da CQRS ayrımının
/// katı tutulmadığı modüllerde tercih edilebilir.
/// </summary>
/// <typeparam name="TEntity">IEntity'den türeyen entity tipi.</typeparam>
/// <typeparam name="TId">Entity'nin primary key tipi.</typeparam>
public interface IRepository<TEntity, TId>
    : IReadRepository<TEntity, TId>, IWriteRepository<TEntity, TId>
    where TEntity : class, IEntity<TId>
    where TId : notnull;