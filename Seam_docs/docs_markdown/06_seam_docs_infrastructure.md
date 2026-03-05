# Seam.Infrastructure — EF Core Repository Implementasyonları

---

## 1. Bu Katman Ne İçin Var?

`Seam.Application` katmanında sözleşmeler tanımladık — "şunları yapabilmeliyim" dedik. `Seam.Infrastructure` katmanı bu sözleşmeleri **gerçek hayata taşır**. Veritabanıyla konuşan, EF Core kullanan, SQL üreten kod burada yaşar.

Tekrar garson örneğine dönelim:
- `IReadRepository` → "Bana kullanıcı getir" (sipariş fişi)
- `EfReadRepository` → Mutfağa gider, EF Core ile SQL yazar, veriyi getirir (mutfak)

Tüketen uygulama sadece `IReadRepository`'yi bilir. İçinde EF Core mu, Dapper mı, başka bir şey mi var — umursamaz.

---

## 2. `EfReadRepository` — Okuma Implementasyonu

```csharp
namespace Seam.Infrastructure.Persistence.Repositories;

using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Seam.Application.Persistence.Repositories;
using Seam.Domain.Entities;

public class EfReadRepository<TEntity, TId, TContext>(TContext context)
    : IReadRepository<TEntity, TId>
    where TEntity : class, IEntity<TId>
    where TId : notnull
    where TContext : DbContext
{
    protected readonly DbSet<TEntity> DbSet = context.Set<TEntity>();

    public async Task<TEntity?> GetByIdAsync(
        TId id,
        CancellationToken cancellationToken = default)
        => await DbSet
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id.Equals(id), cancellationToken);

    public async Task<IEnumerable<TEntity>> GetAllAsync(
        CancellationToken cancellationToken = default)
        => await DbSet
            .AsNoTracking()
            .ToListAsync(cancellationToken);

    public async Task<IEnumerable<TEntity>> FindAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default)
        => await DbSet
            .AsNoTracking()
            .Where(predicate)
            .ToListAsync(cancellationToken);

    public async Task<TEntity?> FindSingleAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default)
        => await DbSet
            .AsNoTracking()
            .SingleOrDefaultAsync(predicate, cancellationToken);

    public async Task<int> CountAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default)
        => await DbSet
            .AsNoTracking()
            .CountAsync(predicate, cancellationToken);
}
```

### Satır Satır Açıklama

**`public class EfReadRepository<TEntity, TId, TContext>(TContext context)`**

Üç generic parametre var:
- `TEntity` → Hangi entity? (`Kullanici`, `Urun` vb.)
- `TId` → Id tipi nedir? (`Guid`, `int` vb.)
- `TContext` → Hangi DbContext? Tüketen uygulama kendi `AppDbContext`'ini verir.

`(TContext context)` → C# 12 ile gelen **primary constructor** söz dizimidir. Şunun kısaltmasıdır:

```csharp
private readonly TContext _context;

public EfReadRepository(TContext context)
{
    _context = context;
}
```

**`: IReadRepository<TEntity, TId>`**
Bu sınıf `IReadRepository` sözleşmesini **uygular (implement eder)**. Yani sözleşmedeki tüm metodları yazmak zorundadır — yazmasa derleme hatası alır.

**`where TContext : DbContext`**
`TContext` bir `DbContext` olmak zorunda. Böylece `context.Set<TEntity>()` gibi EF Core metodlarını güvenle kullanabiliriz.

---

### `protected readonly DbSet<TEntity> DbSet = context.Set<TEntity>();`

**`DbSet<TEntity>`** → EF Core'un veritabanı tablosunu temsil eden sınıfı. `DbSet<Kullanici>` diyorsak `Kullanicilar` tablosunu temsil eder.

**`context.Set<TEntity>()`** → DbContext'ten ilgili DbSet'i alır. Tüketen uygulama `public DbSet<Kullanici> Kullanicilar` diye tanımlamış olsa da biz generic olduğumuz için `Set<T>()` ile dinamik olarak alıyoruz.

**`protected`** → Alt sınıflar bu alana erişebilir. `EfReadRepository`'den türeyen bir sınıf `DbSet`'i kullanabilir.

**`readonly`** → Bir kez atandıktan sonra değiştirilemez. Constructor'dan sonra `DbSet = başkaBirŞey` yazılamaz.

---

### `AsNoTracking()` — Neden Her Sorguda Var?

```csharp
await DbSet
    .AsNoTracking()
    .FirstOrDefaultAsync(...)
```

**Change Tracker Nedir?**
EF Core, getirdiği nesneleri **change tracker**'da takip eder. "Bu nesneyi getirdim, eğer değişirse veritabanına yazayım" der. Bu mekanizma yazma senaryolarında çok kullanışlıdır.

Ama okuma senaryolarında bu takip **gereksiz yük** oluşturur:
- Bellek kullanımı artar
- Her nesne için ek işlem yapılır
- Sorgu %20-40 daha yavaş olabilir

**`AsNoTracking()`** change tracker'ı devre dışı bırakır. "Bu nesneyi getir ama takip etme — sadece okuyacağız" der. `EfReadRepository` sadece okuma yaptığı için her sorguda `AsNoTracking()` kullanıyoruz.

---

### `FirstOrDefaultAsync` vs `SingleOrDefaultAsync`

**`FirstOrDefaultAsync`** → Koşulu sağlayan **ilk** kaydı döner. Birden fazla sonuç olsa bile şikayet etmez, ilkini alır.

**`SingleOrDefaultAsync`** → Koşulu sağlayan **tek** kaydı döner. Birden fazla sonuç varsa **exception fırlatır**.

```csharp
// GetByIdAsync — FirstOrDefault kullanır
// ID ile sorguluyoruz, ID unique olmalı ama biz garanti vermek zorunda değiliz
// İlk bulunanı al, yeter
.FirstOrDefaultAsync(e => e.Id.Equals(id), cancellationToken);

// FindSingleAsync — SingleOrDefault kullanır
// "Tek bir sonuç bekliyorum" diyoruz. İki sonuç gelirse hata — bu kasıtlı
.SingleOrDefaultAsync(predicate, cancellationToken);
```

**`e => e.Id.Equals(id)` neden `==` değil?**
Generic `TId` tipi `==` operatörünü desteklemeyebilir. `Equals()` her tipte güvenle çalışır.

---

## 3. `EfWriteRepository` — Yazma Implementasyonu

```csharp
public class EfWriteRepository<TEntity, TId, TContext>(TContext context)
    : IWriteRepository<TEntity, TId>
    where TEntity : class, IEntity<TId>
    where TId : notnull
    where TContext : DbContext
{
    protected readonly DbSet<TEntity> DbSet = context.Set<TEntity>();

    public async Task AddAsync(
        TEntity entity,
        CancellationToken cancellationToken = default)
        => await DbSet.AddAsync(entity, cancellationToken);

    public void Update(TEntity entity)
        => DbSet.Update(entity);

    public void Delete(TEntity entity)
        => DbSet.Remove(entity);

    public void HardDelete(TEntity entity)
    {
        context.Entry(entity).State = EntityState.Detached;
        DbSet.Attach(entity);
        DbSet.Remove(entity);
        context.Entry(entity).State = EntityState.Deleted;
    }
}
```

### `AddAsync` — Ekleme

```csharp
public async Task AddAsync(
    TEntity entity,
    CancellationToken cancellationToken = default)
    => await DbSet.AddAsync(entity, cancellationToken);
```

`DbSet.AddAsync()` → Entity'yi change tracker'a "Added" state ile ekler. `SaveChangesAsync()` çağrılana kadar veritabanına yazılmaz.

Neden async? `AddAsync`, özellikle `Guid` veya `HiLo` gibi veritabanı tarafında ID üretilen senaryolarda I/O yapabilir.

---

### `Update` — Güncelleme

```csharp
public void Update(TEntity entity)
    => DbSet.Update(entity);
```

`DbSet.Update()` → Entity'yi change tracker'a "Modified" state ile ekler. Tüm alanları günceller. Veritabanına yazma yok — sadece işaretleme.

---

### `Delete` — Soft Delete

```csharp
public void Delete(TEntity entity)
    => DbSet.Remove(entity);
```

`DbSet.Remove()` → Entity'yi "Deleted" state ile işaretler. Ama dikkat — bu fiziksel silme gibi görünür! Soft delete işlemi nerede yapılıyor?

Cevap: `SeamDbContext`'in `SaveChangesAsync()` override'ında. Entity `ISoftDeletable` ise "Deleted" state'i yakalanır ve `IsDeleted = true`'ya çevrilir. Bu sayede `Delete()` metodu soft/hard delete mantığını bilmek zorunda kalmaz — sorumluluk ayrılmış olur.

---

### `HardDelete` — Fiziksel Silme

```csharp
public void HardDelete(TEntity entity)
{
    context.Entry(entity).State = EntityState.Detached;
    DbSet.Attach(entity);
    DbSet.Remove(entity);
    context.Entry(entity).State = EntityState.Deleted;
}
```

Bu metod daha karmaşık — neden?

**`context.Entry(entity).State = EntityState.Detached`**
Entity daha önce başka bir repository tarafından change tracker'a alınmış olabilir. Önce "takibi bırak" diyoruz — temiz bir başlangıç.

**`DbSet.Attach(entity)`**
Entity'yi tekrar change tracker'a ekliyoruz ama bu sefer "Unchanged" state ile.

**`DbSet.Remove(entity)`**
"Deleted" state'e alıyoruz.

**`context.Entry(entity).State = EntityState.Deleted`**
`SeamDbContext`'in soft-delete interceptor'ı `EntityState.Deleted`'ı görünce `ISoftDeletable` kontrolü yapıp `IsDeleted = true`'ya çevirir. Biz ise fiziksel silme istiyoruz — state'i tekrar `Deleted` olarak sabitleriz.

Peki interceptor yine müdahale etmez mi? `HardDelete` için interceptor'ı bypass eden özel bir işaret mekanizması eklenebilir — bu tüketen uygulamanın tercihine bırakılmıştır.

---

## 4. Tüketen Projede Kullanım

```csharp
// 1. Concrete repository tanımla
public sealed class KullaniciReadRepository(AppDbContext context)
    : EfReadRepository<Kullanici, Guid, AppDbContext>(context);

public sealed class KullaniciWriteRepository(AppDbContext context)
    : EfWriteRepository<Kullanici, Guid, AppDbContext>(context);

// 2. DI kaydı
services.AddScoped<IReadRepository<Kullanici, Guid>,
    KullaniciReadRepository>();
services.AddScoped<IWriteRepository<Kullanici, Guid>,
    KullaniciWriteRepository>();

// 3. Handler'da kullanım
public class KullaniciGetirHandler(
    IReadRepository<Kullanici, Guid> repository)
{
    public async Task<Result<KullaniciDto>> Handle(
        KullaniciGetirQuery query,
        CancellationToken ct)
    {
        var kullanici = await repository.GetByIdAsync(query.Id, ct);

        if (kullanici is null)
            return Result.Failure<KullaniciDto>(
                Error.NotFound("Kullanıcı bulunamadı."));

        return Result.Success(new KullaniciDto(kullanici.Id, kullanici.Ad));
    }
}
```

---

## 5. Büyük Resim — Veri Akışı

```
Handler
  │
  │  IReadRepository<Kullanici, Guid>.GetByIdAsync(id)
  ▼
EfReadRepository<Kullanici, Guid, AppDbContext>
  │
  │  DbSet<Kullanici>.AsNoTracking().FirstOrDefaultAsync(...)
  ▼
AppDbContext (EF Core)
  │
  │  SELECT * FROM Kullanicilar WHERE Id = @id AND IsDeleted = 0
  ▼
Veritabanı
```

Handler sadece `IReadRepository`'yi bilir. EF Core, `DbSet`, SQL — bunların hiçbirini görmez. Yarın Dapper kullanmak istersen sadece `EfReadRepository` yerine `DapperReadRepository` yazarsın. Handler'a dokunmazsın.
