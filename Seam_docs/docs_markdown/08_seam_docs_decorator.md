# Seam.Infrastructure — Decorator Pattern & AuditLogging

---

## 1. Decorator Pattern Nedir?

Bir nesnenin davranışını, o nesneyi değiştirmeden genişletmek istediğimizde **Decorator Pattern** kullanırız.

Bir kahve düşün. Sade kahve var. Üzerine süt eklemek istiyorsun — ama kahveyi değiştirmiyorsun, etrafına süt ekliyorsun. Sonra üzerine şeker eklemek istiyorsun — yine kahveyi değiştirmiyorsun, bir katman daha ekliyorsun.

```
Sade Kahve
    └── + Süt Dekoratörü
            └── + Şeker Dekoratörü
```

Her katman bir öncekini **sarar (wrap eder)**. Dışarıdan bakınca hepsi aynı arayüze sahip — "bir kahve" — ama içeride katmanlar var.

Yazılımda:

```
IReadRepository (arayüz)
    └── EfReadRepository (gerçek implementasyon)
            └── AuditLoggingReadRepositoryDecorator (onu sarar)
```

Dışarıdan `IReadRepository` istiyorsun, `AuditLoggingReadRepositoryDecorator` geliyor. İçinde `EfReadRepository` çalışıyor. Sen farkını görmüyorsun — aynı arayüz.

**Bunu yazmasaydık ne olurdu?**
Her repository metoduna ayrı ayrı log kodu yazmak gerekirdi. 10 entity, her birinde 5 metod = 50 yerde log kodu. Bir değişiklik istersen 50 yeri değiştirirsin.

---

## 2. `AuditLoggingReadRepositoryDecorator`

```csharp
namespace Seam.Infrastructure.Persistence.Decorators;

using System.Linq.Expressions;
using Serilog;
using Seam.Application.Persistence.Repositories;
using Seam.Domain.Entities;

public sealed class AuditLoggingReadRepositoryDecorator<TEntity, TId>(
    IReadRepository<TEntity, TId> inner,
    ILogger logger)
    : IReadRepository<TEntity, TId>
    where TEntity : class, IEntity<TId>
    where TId : notnull
{
    private readonly string _entityName = typeof(TEntity).Name;

    public async Task<TEntity?> GetByIdAsync(
        TId id,
        CancellationToken cancellationToken = default)
    {
        using var _ = BeginOperation(nameof(GetByIdAsync));
        var result = await inner.GetByIdAsync(id, cancellationToken);

        logger.Information(
            "[ReadRepository] {Operation} | Entity: {Entity} | Id: {Id} | Found: {Found}",
            nameof(GetByIdAsync), _entityName, id, result is not null);

        return result;
    }

    public async Task<IEnumerable<TEntity>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        using var _ = BeginOperation(nameof(GetAllAsync));
        var result = await inner.GetAllAsync(cancellationToken);
        var list = result.ToList();

        logger.Information(
            "[ReadRepository] {Operation} | Entity: {Entity} | Count: {Count}",
            nameof(GetAllAsync), _entityName, list.Count);

        return list;
    }

    public async Task<IEnumerable<TEntity>> FindAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        using var _ = BeginOperation(nameof(FindAsync));
        var result = await inner.FindAsync(predicate, cancellationToken);
        var list = result.ToList();

        logger.Information(
            "[ReadRepository] {Operation} | Entity: {Entity} | Count: {Count}",
            nameof(FindAsync), _entityName, list.Count);

        return list;
    }

    public async Task<TEntity?> FindSingleAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        using var _ = BeginOperation(nameof(FindSingleAsync));
        var result = await inner.FindSingleAsync(predicate, cancellationToken);

        logger.Information(
            "[ReadRepository] {Operation} | Entity: {Entity} | Found: {Found}",
            nameof(FindSingleAsync), _entityName, result is not null);

        return result;
    }

    public async Task<int> CountAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        using var _ = BeginOperation(nameof(CountAsync));
        var count = await inner.CountAsync(predicate, cancellationToken);

        logger.Information(
            "[ReadRepository] {Operation} | Entity: {Entity} | Count: {Count}",
            nameof(CountAsync), _entityName, count);

        return count;
    }

    private IDisposable BeginOperation(string operationName)
    {
        logger.Debug(
            "[ReadRepository] Starting {Operation} | Entity: {Entity}",
            operationName, _entityName);

        return Serilog.Context.LogContext.PushProperty(
            "Operation", operationName);
    }
}
```

### Satır Satır Açıklama

**`public sealed class AuditLoggingReadRepositoryDecorator<TEntity, TId>(`**
**`    IReadRepository<TEntity, TId> inner,`**
**`    ILogger logger)`**

İki şey inject ediliyor:
- `inner` → Sarılacak gerçek repository. Adına "inner" dedik çünkü içteki nesne.
- `logger` → Serilog logger'ı.

Primary constructor söz dizimi kullanıldı — sınıfın başında parantez içinde parametreler.

**`: IReadRepository<TEntity, TId>`**
Decorator da **aynı arayüzü uygular**. Bu Decorator Pattern'ın özüdür. Dışarıdan bakınca dekoratör mü, gerçek implementasyon mu fark edilmez — ikisi de aynı arayüzde.

**`private readonly string _entityName = typeof(TEntity).Name;`**
Her metod çağrısında `typeof(TEntity).Name` yazmak yerine bir kez hesaplayıp saklıyoruz. `typeof(Kullanici).Name` → `"Kullanici"`. Log mesajlarında entity adını yazdırmak için kullanılır.

---

### `GetByIdAsync` Metodunu İnceleyelim

```csharp
public async Task<TEntity?> GetByIdAsync(
    TId id,
    CancellationToken cancellationToken = default)
{
    using var _ = BeginOperation(nameof(GetByIdAsync));
    var result = await inner.GetByIdAsync(id, cancellationToken);

    logger.Information(
        "[ReadRepository] {Operation} | Entity: {Entity} | Id: {Id} | Found: {Found}",
        nameof(GetByIdAsync), _entityName, id, result is not null);

    return result;
}
```

**`using var _ = BeginOperation(nameof(GetByIdAsync))`**

Üç şey var burada:

`nameof(GetByIdAsync)` → Metod adını string olarak verir: `"GetByIdAsync"`. Neden `"GetByIdAsync"` yazmak yerine `nameof` kullanıyoruz? Metod adı değişirse `nameof` otomatik güncellenir, elle yazılmış string güncellenmez.

`BeginOperation(...)` → `IDisposable` döndürür. `using` bloğuna girmeden operasyon başlangıcını loglar.

`using var _` → `_` isimsiz değişken konvansiyonu. "Bu değişkeni kullanmayacağım, sadece `using` için lazım" demek. `using` bloğu bitince `Dispose()` çağrılır — Serilog context temizlenir.

**`var result = await inner.GetByIdAsync(id, cancellationToken)`**
Gerçek işi `inner` yapıyor — `EfReadRepository.GetByIdAsync()` çağrılıyor. Decorator sadece etrafını sarıyor, kendisi işi yapmıyor.

**`result is not null`**
`result` null değilse `true`, null ise `false` döner. Log mesajına `Found: True` veya `Found: False` yazılır.

---

### `BeginOperation` — Yardımcı Metod

```csharp
private IDisposable BeginOperation(string operationName)
{
    logger.Debug(
        "[ReadRepository] Starting {Operation} | Entity: {Entity}",
        operationName, _entityName);

    return Serilog.Context.LogContext.PushProperty(
        "Operation", operationName);
}
```

**`Serilog.Context.LogContext.PushProperty("Operation", operationName)`**
Serilog'un **enrichment** özelliği. Bu çağrı sonrasında yazılan tüm log satırlarına otomatik `Operation` bilgisi eklenir. `using` bloğu bitince bu ek bilgi kaldırılır.

Bir not defterine "şu anda mutfaktayım" yazısı yapıştırmak gibi. O yapışkanlı not orada olduğu sürece yazdığın her şeye bu bağlam eklenir. `using` bitince not kalkar.

**`IDisposable` döndürür**
`PushProperty` bir `IDisposable` döndürür. `using var _ = BeginOperation(...)` diyerek metod bitince otomatik `Dispose()` çağrılmasını sağlıyoruz. Bağlam temizlenir.

---

## 3. `AuditLoggingWriteRepositoryDecorator`

```csharp
public sealed class AuditLoggingWriteRepositoryDecorator<TEntity, TId>(
    IWriteRepository<TEntity, TId> inner,
    ILogger logger)
    : IWriteRepository<TEntity, TId>
    where TEntity : class, IEntity<TId>
    where TId : notnull
{
    private readonly string _entityName = typeof(TEntity).Name;

    public async Task AddAsync(
        TEntity entity,
        CancellationToken cancellationToken = default)
    {
        logger.Information(
            "[WriteRepository] {Operation} | Entity: {Entity} | Id: {Id}",
            nameof(AddAsync), _entityName, entity.Id);

        await inner.AddAsync(entity, cancellationToken);
    }

    public void Update(TEntity entity)
    {
        logger.Information(
            "[WriteRepository] {Operation} | Entity: {Entity} | Id: {Id}",
            nameof(Update), _entityName, entity.Id);

        inner.Update(entity);
    }

    public void Delete(TEntity entity)
    {
        logger.Information(
            "[WriteRepository] {Operation} | Entity: {Entity} | Id: {Id}",
            nameof(Delete), _entityName, entity.Id);

        inner.Delete(entity);
    }

    public void HardDelete(TEntity entity)
    {
        logger.Warning(
            "[WriteRepository] {Operation} | Entity: {Entity} | Id: {Id} — PHYSICAL DELETE",
            nameof(HardDelete), _entityName, entity.Id);

        inner.HardDelete(entity);
    }
}
```

### Write Decorator'ının Read'den Farkı

Write decorator'ında `BeginOperation` yok — yazma metodları daha basit, tek bir log satırı yeterli.

**`entity.Id`**
`IEntity<TId>` sözleşmesi sayesinde her entity'nin `Id` özelliği olduğu garantili. Generic olmasına rağmen `entity.Id` yazabiliyoruz — `where TEntity : IEntity<TId>` kısıtı bunu mümkün kılıyor.

**`HardDelete` neden `Warning`?**
Fiziksel silme geri alınamaz. `Warning` seviyesi monitoring araçlarında dikkat çeker. "Birisi bir kaydı tamamen sildi" bilgisi kritik olabilir — özellikle yanlışlıkla yapılmışsa.

---

## 4. Scrutor ile DI Kaydı

```csharp
public static IServiceCollection AddSeamRepositories(
    this IServiceCollection services)
{
    services.AddScoped(typeof(IReadRepository<,>),
        typeof(EfReadRepository<,,>));

    services.AddScoped(typeof(IWriteRepository<,>),
        typeof(EfWriteRepository<,,>));

    services.Decorate(
        typeof(IReadRepository<,>),
        typeof(AuditLoggingReadRepositoryDecorator<,>));

    services.Decorate(
        typeof(IWriteRepository<,>),
        typeof(AuditLoggingWriteRepositoryDecorator<,>));

    return services;
}
```

### Scrutor Nedir?

.NET'in varsayılan DI container'ı `Decorate` metodunu desteklemez. **Scrutor** bu eksikliği giderir — mevcut bir kaydı decorator ile sarmayı kolaylaştırır.

### `services.Decorate(...)` Nasıl Çalışır?

```csharp
// 1. Adım — Önce gerçek implementasyon kaydedildi:
services.AddScoped(typeof(IReadRepository<,>), typeof(EfReadRepository<,,>));

// 2. Adım — Scrutor bu kaydı alır ve decorator ile sarar:
services.Decorate(typeof(IReadRepository<,>),
    typeof(AuditLoggingReadRepositoryDecorator<,>));
```

Scrutor şunu yapar: "DI container'da `IReadRepository<,>` için `EfReadRepository<,,>` kayıtlı. Bunu `AuditLoggingReadRepositoryDecorator<,>` ile sar. Artık `IReadRepository<,>` istendiğinde `AuditLoggingReadRepositoryDecorator` ver — içine `EfReadRepository`'yi inject et."

```
IReadRepository<Kullanici, Guid> istendi
    │
    ▼
AuditLoggingReadRepositoryDecorator<Kullanici, Guid>
    │  (inner olarak EfReadRepository inject edildi)
    ▼
EfReadRepository<Kullanici, Guid, AppDbContext>
    │
    ▼
Veritabanı
```

**Manuel yapılsaydı ne kadar karmaşık olurdu?**

```csharp
// Scrutor olmadan — her entity için ayrı ayrı yazmak gerekir
services.AddScoped<IReadRepository<Kullanici, Guid>>(sp =>
    new AuditLoggingReadRepositoryDecorator<Kullanici, Guid>(
        new EfReadRepository<Kullanici, Guid, AppDbContext>(
            sp.GetRequiredService<AppDbContext>()),
        sp.GetRequiredService<ILogger>()));

// 10 entity varsa bu kodu 10 kez yaz...
```

Scrutor ile tek satır, tüm entity'ler için geçerli.

---

## 5. Tüm Sistemin Birleşik Görünümü

Şimdi her şeyi bir araya getirelim. Bir kullanıcı API'ye istek attı:

```
HTTP POST /api/kullanicilar
        │
        ▼
Controller
        │  mediator.Send(new KullaniciOlusturCommand("Ali", "ali@mail.com"))
        ▼
MediatR Pipeline
        │
        ├─ ExceptionHandlingBehavior   → try/catch ile sarar
        ├─ LoggingBehavior             → "Handling KullaniciOlusturCommand" loglar
        ├─ ValidationBehavior          → KullaniciOlusturCommandValidator çalışır
        ├─ TransactionBehavior         → BeginTransaction açar
        │
        ▼
KullaniciOlusturCommandHandler
        │  Guard.Combine(...) → alan kontrolleri
        │  writeRepository.AddAsync(yeniKullanici)
        │       │
        │       ▼
        │  AuditLoggingWriteRepositoryDecorator
        │       │  "[WriteRepository] AddAsync | Entity: Kullanici | Id: ..." loglar
        │       │  inner.AddAsync(yeniKullanici) çağırır
        │       ▼
        │  EfWriteRepository
        │       │  DbSet.AddAsync(yeniKullanici) — change tracker'a ekler
        │       ▼
        │  (henüz veritabanına yazılmadı)
        │
        ▼
Handler Result.Success(yeniId) döndü
        │
        ▼
TransactionBehavior
        │  IsSuccess → SaveChangesAsync() → CommitTransactionAsync()
        │  SeamDbContext.SaveChanges:
        │      IAuditable → CreatedAt, CreatedBy dolduruldu
        │      (soft delete değil, fiziksel ekleme)
        ▼
LoggingBehavior: "succeeded in 32ms" loglandı
        ▼
ExceptionHandlingBehavior: exception yok, geçti
        ▼
Controller → HTTP 201 Created döndü
```

---

## 6. Özet — Her Katmanın Sorumluluğu

| Katman | Sorumluluk | Bağımlılık |
|--------|-----------|------------|
| `Seam.Domain` | Sözleşmeler, kurallar, değer nesneleri | Hiçbir şey |
| `Seam.Application` | İş mantığı arayüzleri, pipeline | Sadece Domain |
| `Seam.Infrastructure` | EF Core, veritabanı, dekoratörler | Domain + Application |

**Altın Kural:** Bağımlılıklar her zaman içe doğru akar. Domain kimseyi tanımaz. Application sadece Domain'i tanır. Infrastructure herkesi tanır ama kimse Infrastructure'ı tanımaz — sadece sözleşmelerini kullanır.

Bu sayede:
- EF Core'u Dapper ile değiştirmek istersen → Sadece Infrastructure değişir
- Yeni bir validation kuralı eklemek istersen → Sadece Application değişir  
- Yeni bir entity özelliği eklemek istersen → Domain'de interface güncellenir, diğerleri uyum sağlar
