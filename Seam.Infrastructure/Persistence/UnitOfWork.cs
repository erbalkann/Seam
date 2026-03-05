namespace Seam.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Seam.Application.Persistence;

/// <summary>
/// IUnitOfWork'ün Entity Framework Core implementasyonu.
/// IDbContextTransaction ile transaction yönetimini
/// doğrudan EF Core API üzerinden sağlar.
/// </summary>
public sealed class UnitOfWork(DbContext context) : IUnitOfWork
{
    private IDbContextTransaction? _transaction;

    public async Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default)
        => await context.SaveChangesAsync(cancellationToken);

    public async Task BeginTransactionAsync(
        CancellationToken cancellationToken = default)
    {
        if (_transaction is not null)
            throw new InvalidOperationException(
                "Zaten aktif bir transaction mevcut.");

        _transaction = await context.Database
            .BeginTransactionAsync(cancellationToken);
    }

    public async Task CommitTransactionAsync(
        CancellationToken cancellationToken = default)
    {
        if (_transaction is null)
            throw new InvalidOperationException(
                "Commit edilecek aktif transaction bulunamadı.");

        await _transaction.CommitAsync(cancellationToken);
        await _transaction.DisposeAsync();
        _transaction = null;
    }

    public async Task RollbackTransactionAsync(
        CancellationToken cancellationToken = default)
    {
        if (_transaction is null)
            throw new InvalidOperationException(
                "Rollback edilecek aktif transaction bulunamadı.");

        await _transaction.RollbackAsync(cancellationToken);
        await _transaction.DisposeAsync();
        _transaction = null;
    }

    public void Dispose()
    {
        _transaction?.Dispose();
        context.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        if (_transaction is not null)
            await _transaction.DisposeAsync();

        await context.DisposeAsync();
    }
}