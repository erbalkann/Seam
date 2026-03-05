namespace Seam.Domain.Entities;

/// <summary>
/// Oluşturma ve güncelleme izlenebilirliği sağlar.
/// Kim, ne zaman oluşturdu/güncelledi bilgisini tutar.
/// </summary>
public interface IAuditable
{
    /// <summary>Kaydın oluşturulduğu UTC zaman damgası.</summary>
    DateTime CreatedAt { get; }

    /// <summary>Kaydı oluşturan kullanıcının kimliği.</summary>
    string CreatedBy { get; }

    /// <summary>
    /// Kaydın son güncellendiği UTC zaman damgası.
    /// Hiç güncellenmemişse null olabilir.
    /// </summary>
    DateTime? UpdatedAt { get; }

    /// <summary>
    /// Kaydı en son güncelleyen kullanıcının kimliği.
    /// Hiç güncellenmemişse null olabilir.
    /// </summary>
    string? UpdatedBy { get; }
}