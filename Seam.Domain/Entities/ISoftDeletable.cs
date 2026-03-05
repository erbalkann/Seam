namespace Seam.Domain.Entities;

/// <summary>
/// Fiziksel silme yerine mantıksal (soft) silmeyi temsil eder.
/// Bu interface'i implemente eden entity'ler veritabanından
/// silinmez; IsDeleted flag'i ile işaretlenir.
/// </summary>
public interface ISoftDeletable
{
    /// <summary>Kaydın silinip silinmediğini belirtir.</summary>
    bool IsDeleted { get; }

    /// <summary>
    /// Kaydın silindiği UTC zaman damgası.
    /// Silinmediyse null olabilir.
    /// </summary>
    DateTime? DeletedAt { get; }

    /// <summary>
    /// Kaydı silen kullanıcının kimliği.
    /// Silinmediyse null olabilir.
    /// </summary>
    string? DeletedBy { get; }
}