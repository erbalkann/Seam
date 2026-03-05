namespace Seam.Domain.Entities;

/// <summary>
/// IEntity + IAuditable + ISoftDeletable kombinasyonu.
/// Hem denetim izlenebilirliği hem de soft-delete desteği
/// gerektiren entity'ler için tam kapsamlı sözleşme.
/// Çoğu production entity bu interface'i implemente eder.
/// </summary>
/// <typeparam name="TId">Primary key tipi</typeparam>
public interface IFullAuditableEntity<TId> : IAuditableEntity<TId>, ISoftDeletable
    where TId : notnull;