namespace Seam.Domain.Entities;

/// <summary>
/// Sistemdeki tüm entity'lerin temel sözleşmesi.
/// TId generic parametresi sayesinde her entity
/// kendi kimlik tipini (Guid, int, long, vb.) seçebilir.
/// </summary>
/// <typeparam name="TId">Primary key tipi</typeparam>
public interface IEntity<TId>
    where TId : notnull
{
    TId Id { get; }
}