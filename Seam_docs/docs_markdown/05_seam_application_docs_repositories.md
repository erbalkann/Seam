# Seam.Application — Repository & UnitOfWork Interfaces

---

## 1. Bu Katman Ne İçin Var?

`Seam.Application` katmanı **iş mantığının sözleşmelerini** tanımlar. Veritabanının nasıl çalıştığını bilmez — sadece "veritabanından şunları isteyebilirim" der.

Bir garson düşün. Mutfağa gider ve "2 numaralı masaya bir pizza lazım" der. Pizzanın odun fırınında mı, elektrikli fırında mı pişeceğini bilmez — bu mutfağın işidir. Garson sadece sipariş arayüzünü bilir.

```
Seam.Application  →  "Bana ID'ye göre kullanıcı getir"  (sözleşme)
Seam.Infrastructure →  EF Core ile veritabanına gider    (gerçek iş)
```

Bu ayrım sayesinde yarın EF Core yerine Dapper kullanmak istersen sadece Infrastructure katmanını değiştirirsin. Application katmanına dokunmazsın.

---

## 2. `IReadRepository<TEntity, TId>` — Okuma Sözleşmesi

```csharp
namespace Seam.Application.Persistence.Repositories;

using System.Linq.Expressions;
using Seam.Domain.Entities;

public interface IReadRepository<TEntity, TId>
    where TEntity : class, IEntity<TId>
    where TId : notnull
{
    Task<TEntity?> GetByIdAsync(TId id,
        CancellationToken cancellationToken = default);

    Task<IEnumerable<TEntity>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<IEnumerable<TEntity>> FindAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default);

    Task<TEntity?> FindSingleAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default);

    Task<int> CountAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default);
}
```

### Generic Kısıtlar

**`where TEntity : class, IEntity<TId>`**
İki kısıt aynı anda:
- `class` → TEntity bir referans tipi olmak zorunda (struct olamaz)
- `IEntity<TId>` → TEntity mutlaka bir `Id` özelliğine sahip olmak zorunda

Bu kısıt olmadan repository'ye `int` veya `string` gibi anlamsız tipler verilebilirdi.

**`where TId : notnull`**
Id tipi null olamaz — `Guid`, `int`, `string` olabilir ama `Guid?` olamaz.

---

### `Task<T>` Nedir? Neden Async?

```csharp
Task<TEntity?> GetByIdAsync(TId id, ...);
```

**`Task<T>`** → Asenkron (async) operasyonun temsilidir. Veritabanı sorgusu ağ üzerinden gider — bu süreçte program beklemek zorunda değil. Başka işler yapabilir.

Bir kargo şirketi düşün. Paketi verdin, "hazır olunca haber ver" dedin ve başka işinle ilgilendin. `Task` tam olarak budur — "ben bu işi yapacağım, bitince sana bildiririm."

**Neden metodun adı `GetByIdAsync`?**
C# konvansiyonu: async metodların adı `Async` ile biter. Koda bakan biri "bu metod asenkron çalışır, await ile çağrılmalı" diye hemen anlar.

---

### `CancellationToken` Nedir?

```csharp
Task<TEntity?> GetByIdAsync(TId id,
    CancellationToken cancellationToken = default);
```

Kullanıcı bir web sayfasını açtı, veritabanı sorgusu başladı — ama kullanıcı sayfayı kapattı. Sorgu hâlâ devam mı etsin? Hayır — kaynakları boşa harcamak gereksiz.

`CancellationToken` "işlemi iptal et" sinyalidir. `= default` varsayılan değeri — verilmezse iptal mekanizması olmadan çalışır.

---

### `Expression<Func<TEntity, bool>>` Nedir?

```csharp
Task<IEnumerable<TEntity>> FindAsync(
    Expression<Func<TEntity, bool>> predicate, ...);
```

Bu biraz karmaşık görünür — adım adım açıklayalım.

**`Func<TEntity, bool>`** → "TEntity alıp bool döndüren bir fonksiyon" demektir.
```csharp
Func<Kullanici, bool> aktifMi = k => k.IsActive == true;
```

**`Expression<Func<TEntity, bool>>`** → Bu fonksiyonun **ifade ağacı** (expression tree). Yani fonksiyonu çalıştırmak yerine, fonksiyonun **kodunu veri olarak** tutar. EF Core bu kodu okuyup SQL'e çevirir.

```csharp
// Bu lambda kodu SQL'e çevrilir:
// WHERE IsActive = 1 AND Yas > 18
repo.FindAsync(k => k.IsActive && k.Yas > 18);
```

Expression olmadan EF Core SQL üretemezdi — tüm veriyi çekip C#'ta filtrelemek zorunda kalırdı. Bu çok yavaş olurdu.

---

### Metodların Görevi

**`GetByIdAsync`** → Tek kayıt, ID ile. Bulamazsa `null` döner.
```csharp
var kullanici = await repo.GetByIdAsync(id);
if (kullanici is null) return Result.Failure(Error.NotFound("..."));
```

**`GetAllAsync`** → Tüm kayıtlar. Soft-delete filtreli (silinmişler gelmez).

**`FindAsync`** → Koşula uyan tüm kayıtlar.
```csharp
var aktifKullanicilar = await repo.FindAsync(k => k.IsActive);
```

**`FindSingleAsync`** → Koşula uyan tek kayıt. İkiden fazla sonuç varsa exception fırlatır — `SingleOrDefault` mantığı.
```csharp
var kullanici = await repo.FindSingleAsync(k => k.Email == email);
```

**`CountAsync`** → Koşula uyan kayıt sayısı.
```csharp
var aktifSayisi = await repo.CountAsync(k => k.IsActive);
```

---

## 3. `IWriteRepository<TEntity, TId>` — Yazma Sözleşmesi

```csharp
public interface IWriteRepository<TEntity, TId>
    where TEntity : class, IEntity<TId>
    where TId : notnull
{
    Task AddAsync(TEntity entity,
        CancellationToken cancellationToken = default);

    void Update(TEntity entity);
    void Delete(TEntity entity);
    void HardDelete(TEntity entity);
}
```

### Neden `Update`, `Delete`, `HardDelete` async değil?

```csharp
void Update(TEntity entity);   // async değil
void Delete(TEntity entity);   // async değil
```

EF Core'da `Update()` ve `Remove()` metodları **sadece change tracker'ı işaretler** — veritabanına hiçbir şey yazmaz. I/O (giriş/çıkış) işlemi olmadığı için async olmaları yanıltıcı olurdu.

Gerçek veritabanı yazması `SaveChangesAsync()` ile yapılır — o zaten async'tir.

```
AddAsync()    → veritabanına hazır listesine ekle (async — ID üretmek gerekebilir)
Update()      → "bu nesne değişti" diye işaretle (sync — sadece bellekte)
Delete()      → "bunu sil" diye işaretle (sync — sadece bellekte)
SaveChanges() → işaretlenenleri veritabanına yaz (async — gerçek I/O burada)
```

---

### `Delete` vs `HardDelete` Farkı

**`Delete`** → Soft delete. `IsDeleted = true` yapar. Kayıt hâlâ veritabanında durur.
**`HardDelete`** → Fiziksel silme. Kayıt veritabanından tamamen silinir, geri getirilemez.

```csharp
// Soft delete — kayıt silinmiş görünür ama veri kaybolmaz
repo.Delete(kullanici);

// Hard delete — kayıt tamamen yok edilir (dikkatli kullan!)
repo.HardDelete(eskiLog);
```

---

## 4. `IRepository<TEntity, TId>` — Birleşik Sözleşme

```csharp
public interface IRepository<TEntity, TId>
    : IReadRepository<TEntity, TId>, IWriteRepository<TEntity, TId>
    where TEntity : class, IEntity<TId>
    where TId : notnull;
```

Hem okuma hem yazma gerektiren senaryolar için kısa yol. Tek interface iki sözleşmeyi birden miras alır.

```csharp
// Ayrı ayrı inject etmek yerine
public class Handler(
    IReadRepository<Urun, Guid> readRepo,
    IWriteRepository<Urun, Guid> writeRepo) { }

// Birleşik inject
public class Handler(IRepository<Urun, Guid> repo) { }
```

**CQRS tercihi:** İdeal dünyada okuma ve yazma ayrılmalıdır. `IRepository` bu ayrımı esnetir — küçük projeler veya basit senaryolar için kullanılabilir.

---

## 5. `IUnitOfWork` — İş Birimi Sözleşmesi

```csharp
namespace Seam.Application.Persistence;

public interface IUnitOfWork : IDisposable, IAsyncDisposable
{
    Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default);

    Task BeginTransactionAsync(
        CancellationToken cancellationToken = default);

    Task CommitTransactionAsync(
        CancellationToken cancellationToken = default);

    Task RollbackTransactionAsync(
        CancellationToken cancellationToken = default);
}
```

### Unit of Work Nedir?

Birden fazla repository işlemini **tek bir atomik işlem** olarak ele alır.

Bir banka havalesi düşün:
1. A hesabından 1000 TL düş
2. B hesabına 1000 TL ekle

Eğer 1. adım başarılı, 2. adım başarısız olursa ne olur? A'dan para gitti ama B'ye ulaşmadı — felaket!

**Transaction** bu iki adımı birbirine bağlar: ya ikisi birden başarılı olur, ya ikisi birden geri alınır.

```csharp
await unitOfWork.BeginTransactionAsync();
try
{
    await hesapRepo.AddAsync(borc);      // 1. adım
    await hesapRepo.AddAsync(alacak);    // 2. adım
    await unitOfWork.SaveChangesAsync(); // veritabanına yaz
    await unitOfWork.CommitTransactionAsync(); // onayla
}
catch
{
    await unitOfWork.RollbackTransactionAsync(); // hata → geri al
}
```

---

### `IDisposable` ve `IAsyncDisposable` Nedir?

**`IDisposable`** → `Dispose()` metodunu zorunlu kılar. `using` bloğu ile kullanıldığında işlem bitince kaynaklar otomatik serbest bırakılır.

**`IAsyncDisposable`** → `DisposeAsync()` metodunu zorunlu kılar. `await using` ile async dispose yapılabilir.

```csharp
// Sync dispose
using var uow = serviceProvider.GetRequiredService<IUnitOfWork>();

// Async dispose
await using var uow = serviceProvider.GetRequiredService<IUnitOfWork>();
```

Transaction ve DbContext gibi kaynaklar veritabanı bağlantısı tutar. İşlem bitince bu bağlantının düzgünce kapatılması gerekir — `IDisposable` bunu garanti eder.

---

### `SaveChangesAsync` vs `CommitTransactionAsync` Farkı

Bu ikisi karıştırılabilir — farkları önemli:

**`SaveChangesAsync`** → Change tracker'daki değişiklikleri veritabanına yazar. Transaction içindeyse henüz kalıcı olmaz — transaction onaylanana kadar geri alınabilir.

**`CommitTransactionAsync`** → Transaction'ı onaylar. Artık değişiklikler kalıcıdır, geri alınamaz.

**`RollbackTransactionAsync`** → Transaction'ı geri alır. `SaveChangesAsync` ile yazılan her şey sanki hiç olmamış gibi silinir.

```
BeginTransaction  → "Not defterini aç, yazmaya başla"
SaveChanges       → "Not defterine yaz ama henüz teslim etme"
CommitTransaction → "Not defterini teslim et — artık kalıcı"
RollbackTransaction → "Not defterini yırt — hiçbir şey olmadı"
```

---

## 6. Seam.Application Katmanının Tam Resmi

```
Seam.Application/
├── Persistence/
│   ├── IUnitOfWork.cs
│   └── Repositories/
│       ├── IReadRepository.cs   → Okuma sözleşmesi
│       ├── IWriteRepository.cs  → Yazma sözleşmesi
│       └── IRepository.cs       → Birleşik sözleşme
├── Messaging/                   → (sonraki bölümde)
└── Behaviors/                   → (sonraki bölümde)
```

Bu katmanda **implementasyon yok** — sadece sözleşmeler var. EF Core bilgisi yok, SQL yok. Sadece "şunları yapabilmeliyim" diyor. Gerçek iş `Seam.Infrastructure`'da yapılır.
