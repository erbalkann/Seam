# Seam.Infrastructure — UnitOfWork

---

## 1. UnitOfWork Neden Var?

Daha önce `IUnitOfWork` sözleşmesini `Seam.Application` katmanında yazmıştık. "Transaction yönetimi böyle görünmeli" dedik. Ama bu sadece sözleşmeydi — gerçek iş yapılmıyordu.

`UnitOfWork` sınıfı bu sözleşmeyi **gerçek hayata taşır**. EF Core'un transaction API'sini kullanarak veritabanı işlemlerini atomik hale getirir.

---

## 2. Kodun Tamamı

```csharp
namespace Seam.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Seam.Application.Persistence;

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
```

---

## 3. Satır Satır Açıklama

### Sınıf Tanımı

```csharp
public sealed class UnitOfWork(DbContext context) : IUnitOfWork
```

**`sealed`** → Bu sınıftan türetilemez. UnitOfWork'ün davranışı sabittir, değiştirilmesi istenmez.

**`(DbContext context)`** → Primary constructor. `DbContext` base tipi inject ediliyor — tüketen uygulamanın `AppDbContext`'i bu tipe otomatik çözümlenir. `AppDbContext` doğrudan inject etsek Seam, tüketen uygulamanın DbContext'ine bağımlı hale gelirdi. `DbContext` base tipi kullanmak soyutlamayı korur.

**`: IUnitOfWork`** → `IUnitOfWork` sözleşmesini uygular. İçindeki tüm metodları yazmak zorundayız — yazmassak derleme hatası alırız.

---

### `_transaction` Alanı

```csharp
private IDbContextTransaction? _transaction;
```

**`IDbContextTransaction`** → EF Core'un transaction nesnesi. `BeginTransactionAsync` çağrılınca bu alana atanır, `Commit` veya `Rollback` sonrası `null`'a döner.

**`?`** → Nullable. Başlangıçta transaction yok — `null`'dır. Transaction açılınca dolar, kapanınca tekrar `null` olur.

**`private`** → Sadece bu sınıf içinden erişilebilir. Dışarıdan transaction nesnesine doğrudan erişim kapatılmıştır — kontrol tamamen `UnitOfWork`'te.

---

### `SaveChangesAsync`

```csharp
public async Task<int> SaveChangesAsync(
    CancellationToken cancellationToken = default)
    => await context.SaveChangesAsync(cancellationToken);
```

Change tracker'daki tüm değişiklikleri veritabanına yazar. `context.SaveChangesAsync()` EF Core'un kendi metodudur — biz sadece onu çağırıyoruz.

`Task<int>` → Kaç satırın etkilendiğini döner. 3 kayıt eklendiyse `3` döner.

Transaction içindeyse bu çağrı henüz kalıcı değildir — `CommitTransactionAsync` ile onaylanana kadar geri alınabilir.

---

### `BeginTransactionAsync`

```csharp
public async Task BeginTransactionAsync(
    CancellationToken cancellationToken = default)
{
    if (_transaction is not null)
        throw new InvalidOperationException(
            "Zaten aktif bir transaction mevcut.");

    _transaction = await context.Database
        .BeginTransactionAsync(cancellationToken);
}
```

**`if (_transaction is not null)`**
Zaten açık bir transaction varken tekrar `BeginTransaction` çağırmak programcı hatasıdır. İç içe transaction açmak EF Core'da desteklenmez — erken ve net hata vermek daha güvenlidir.

**`context.Database.BeginTransactionAsync(...)`**
EF Core'un veritabanı bağlantısında transaction başlatır. Dönen `IDbContextTransaction` nesnesi `_transaction` alanına atanır — `Commit` veya `Rollback` için saklanır.

---

### `CommitTransactionAsync`

```csharp
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
```

**`if (_transaction is null)`**
Transaction açılmadan commit çağırmak programcı hatasıdır. Guard kontrolü ile erken hata verilir.

**`await _transaction.CommitAsync(...)`**
Transaction'ı onaylar. `SaveChangesAsync` ile yazılan tüm değişiklikler artık **kalıcıdır** — geri alınamaz.

**`await _transaction.DisposeAsync()`**
Transaction nesnesi kullanıldıktan sonra kaynakları serbest bırakılmalıdır. Veritabanı bağlantı kilitleri, bellek kaynakları temizlenir.

**`_transaction = null`**
Nesne dispose edildi — artık geçersiz. `null` atayarak "aktif transaction yok" durumuna döndürülür. Bir sonraki `BeginTransaction` temiz başlayabilir.

---

### `RollbackTransactionAsync`

```csharp
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
```

**`await _transaction.RollbackAsync(...)`**
Transaction'ı geri alır. `SaveChangesAsync` ile yazılan tüm değişiklikler **sanki hiç yapılmamış gibi** silinir. Veritabanı transaction başlamadan önceki haline döner.

`CommitAsync` ile aynı yapı — fark sadece `RollbackAsync` vs `CommitAsync`. Her ikisinde de `DisposeAsync` ve `null` ataması yapılır.

---

### `Dispose` ve `DisposeAsync`

```csharp
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
```

**Neden iki ayrı Dispose metodu var?**

`IUnitOfWork : IDisposable, IAsyncDisposable` ikisini birden miras aldığından her ikisini de yazmak zorundayız.

- `Dispose()` → `using` bloğu ile sync kullanım için.
- `DisposeAsync()` → `await using` bloğu ile async kullanım için.

**`_transaction?.Dispose()`**
`?.` null-conditional operatörü. `_transaction` null ise `Dispose()` çağrılmaz — NullReferenceException riski sıfırlanır. Transaction commit/rollback yapılmadan scope kapanırsa burası kaynakları temizler.

**`context.Dispose()`**
DbContext'i temizler — veritabanı bağlantısı kapatılır, bellek serbest bırakılır.

**`ValueTask` neden `Task` değil?**
`DisposeAsync` genellikle çok hızlı tamamlanır. `ValueTask` bu gibi kısa async operasyonlarda `Task`'a göre daha az bellek ayırır — performans optimizasyonu.

---

## 4. Transaction Yaşam Döngüsü

```
new UnitOfWork(context)
        │
        ▼
BeginTransactionAsync()    → _transaction dolar
        │
        ▼
repository.AddAsync(...)   → change tracker'a eklendi
repository.Update(...)     → change tracker'da işaretlendi
        │
        ▼
SaveChangesAsync()         → veritabanına yazıldı (henüz kalıcı değil)
        │
        ├── Başarılı → CommitTransactionAsync()  → kalıcı, _transaction = null
        │
        └── Hata    → RollbackTransactionAsync() → geri alındı, _transaction = null
        │
        ▼
DisposeAsync()             → context ve transaction kaynakları temizlendi
```

---

## 5. Tüketen Projede Kullanım

```csharp
// Program.cs
builder.Services.AddDbContext<AppDbContext>(...);

// DbContext base tip olarak da register edilmeli
// UnitOfWork DbContext base tipi bekliyor
builder.Services.AddScoped<DbContext>(sp =>
    sp.GetRequiredService<AppDbContext>());

// AddSeamInfrastructure zaten IUnitOfWork → UnitOfWork kaydeder
builder.Services.AddSeamInfrastructure();
```

```csharp
// Handler'da kullanım — TransactionBehavior otomatik yönetir
// Manuel kullanım gerekirse:
public class OrnekHandler(IUnitOfWork unitOfWork)
{
    public async Task<Result> Handle(CancellationToken ct)
    {
        await unitOfWork.BeginTransactionAsync(ct);
        try
        {
            // ... işlemler
            await unitOfWork.SaveChangesAsync(ct);
            await unitOfWork.CommitTransactionAsync(ct);
            return Result.Success();
        }
        catch
        {
            await unitOfWork.RollbackTransactionAsync(ct);
            return Result.Failure(Error.InternalError());
        }
    }
}
```
