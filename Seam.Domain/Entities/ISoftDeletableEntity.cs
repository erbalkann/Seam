namespace Seam.Domain.Entities;

/// <summary>
/// IEntity + ISoftDeletable kombinasyonu.
/// Hem kimlik hem de soft-delete desteği gerektiren
/// entity'ler için kullanışlı birleşik sözleşme.
/// </summary>
/// <typeparam name="TId">Primary key tipi</typeparam>
public interface ISoftDeletableEntity<TId> : IEntity<TId>, ISoftDeletable
    where TId : notnull;