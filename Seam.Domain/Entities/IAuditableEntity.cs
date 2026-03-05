namespace Seam.Domain.Entities;

/// <summary>
/// IEntity + IAuditable kombinasyonu.
/// Hem kimlik hem de denetim izlenebilirliği gerektiren
/// entity'ler için kullanışlı birleşik sözleşme.
/// </summary>
/// <typeparam name="TId">Primary key tipi</typeparam>
public interface IAuditableEntity<TId> : IEntity<TId>, IAuditable
    where TId : notnull;