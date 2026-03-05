namespace Seam.Application.Persistence;

/// <summary>
/// Veritabanı işlemlerini bir iş birimi (transaction) altında
/// yöneten sözleşme. Repository'lerin biriktirdiği değişiklikleri
/// atomik olarak kalıcı hale getirir.
/// </summary>
public interface IUnitOfWork : IDisposable, IAsyncDisposable
{
    /// <summary>
    /// Change tracker'daki tüm değişiklikleri veritabanına yazar.
    /// Etkilenen satır sayısını döner.
    /// </summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Yeni bir veritabanı transaction'ı başlatır.
    /// </summary>
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Aktif transaction'ı onaylar ve kalıcı hale getirir.
    /// </summary>
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Aktif transaction'ı geri alır.
    /// Hata senaryolarında tüm değişiklikler iptal edilir.
    /// </summary>
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
}