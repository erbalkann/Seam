# Seam.Domain — IMapper & Seam.Infrastructure — AutoMapperAdapter

---

## 1. Mapping Nedir? Neden Gerekli?

Veritabanında sakladığımız nesneler (entity'ler) ile dışarıya gösterdiğimiz nesneler (DTO'lar) çoğu zaman farklıdır.

Bir kullanıcı tablosu düşün:

```csharp
// Veritabanındaki entity — tüm alanlar var
public class Kullanici : IFullAuditableEntity<Guid>
{
    public Guid Id { get; set; }
    public string Ad { get; set; }
    public string Email { get; set; }
    public string SifreHash { get; set; }   // ← dışarı verilmez!
    public string TcKimlik { get; set; }    // ← dışarı verilmez!
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; }
    // ...
}

// API'den dönen DTO — sadece gerekli alanlar
public sealed record KullaniciDto(Guid Id, string Ad, string Email) : IDto;
```

Entity'yi doğrudan API'den döndürsek şifre hash'i, TC kimlik numarası gibi hassas veriler dışarı sızar. DTO bu sızıntıyı engeller.

**Mapping** ise entity'den DTO'ya (veya DTO'dan entity'ye) dönüşüm işlemidir.

---

## 2. Mapping Olmadan Hayat

```csharp
// Her yerde elle dönüşüm — tekrar eden kod
var dto = new KullaniciDto(
    kullanici.Id,
    kullanici.Ad,
    kullanici.Email
);
```

10 entity, her birinde 3-4 metod = onlarca yerde aynı dönüşüm kodu. Bir alan eklendiğinde tüm bu yerleri bulmak ve güncellemek gerekir. Bir yer unutulursa bug oluşur.

**Mapping kütüphanesi** bu dönüşümü otomatik yapar — bir kez konfigüre et, her yerde kullan.

---

## 3. `IMapper` — Sözleşme

```csharp
namespace Seam.Domain.Mapping;

using Seam.Domain.Dtos;
using Seam.Domain.Entities;

public interface IMapper
{
    TDto ToDto<TEntity, TId, TDto>(TEntity entity)
        where TEntity : class, IEntity<TId>
        where TId : notnull
        where TDto : class, IDto;

    TEntity ToEntity<TDto, TEntity, TId>(TDto dto)
        where TDto : class, IDto
        where TEntity : class, IEntity<TId>
        where TId : notnull;

    IEnumerable<TDto> ToDtoList<TEntity, TId, TDto>(
        IEnumerable<TEntity> entities)
        where TEntity : class, IEntity<TId>
        where TId : notnull
        where TDto : class, IDto;

    IEnumerable<TEntity> ToEntityList<TDto, TEntity, TId>(
        IEnumerable<TDto> dtos)
        where TDto : class, IDto
        where TEntity : class, IEntity<TId>
        where TId : notnull;
}
```

### Neden `Seam.Domain` Katmanında?

`IMapper` bir sözleşmedir. Handler'lar bu sözleşmeye bağımlı olacak. Bağımlılık yönü `Application → Domain` olduğundan sözleşme Domain'de yaşamalı. Eğer `Seam.Infrastructure`'da yazsaydık Application katmanı Infrastructure'a bağımlı hale gelirdi — Clean Architecture ihlali.

---

### Satır Satır Açıklama

#### `ToDto` — Entity'den DTO'ya

```csharp
TDto ToDto<TEntity, TId, TDto>(TEntity entity)
    where TEntity : class, IEntity<TId>
    where TId : notnull
    where TDto : class, IDto;
```

**Üç generic parametre:**
- `TEntity` → Kaynak. Hangi entity dönüştürülecek? `Kullanici`, `Urun` vb.
- `TId` → Entity'nin Id tipi. `Guid`, `int` vb.
- `TDto` → Hedef. Hangi DTO'ya dönüştürülecek? `KullaniciDto`, `UrunDto` vb.

**`where TEntity : class, IEntity<TId>`**
İki kısıt birden:
- `class` → Referans tipi olmak zorunda.
- `IEntity<TId>` → Mutlaka bir `Id`'si olmak zorunda.

Bu kısıt sayesinde yanlışlıkla entity olmayan bir nesneyi kaynak olarak vermek **derleme zamanında** engellenir.

**`where TDto : class, IDto`**
Hedef mutlaka `IDto` marker'ını taşımalı. Yanlışlıkla entity'yi hem kaynak hem hedef olarak vermek engellenir.

---

#### `ToEntity` — DTO'dan Entity'ye

```csharp
TEntity ToEntity<TDto, TEntity, TId>(TDto dto)
    where TDto : class, IDto
    where TEntity : class, IEntity<TId>
    where TId : notnull;
```

`ToDto`'nun tersi. Kullanıcıdan gelen form verisi (DTO) entity'ye dönüştürülür ve veritabanına kaydedilir.

**Generic parametre sırası neden farklı?**
`ToDto`'da önce `TEntity` var çünkü kaynak entity. `ToEntity`'de önce `TDto` var çünkü kaynak DTO. Okunabilirlik açısından "kaynak önce gelir" kuralı uygulandı.

---

#### `ToDtoList` — Entity Listesinden DTO Listesine

```csharp
IEnumerable<TDto> ToDtoList<TEntity, TId, TDto>(
    IEnumerable<TEntity> entities)
    where TEntity : class, IEntity<TId>
    where TId : notnull
    where TDto : class, IDto;
```

`ToDto`'nun liste versiyonu. Tek tek dönüştürmek yerine tüm koleksiyonu bir seferde dönüştürür.

**`IEnumerable<TEntity>`** → Liste, dizi, sorgu sonucu — her türlü koleksiyon kabul edilir.

---

#### `ToEntityList` — DTO Listesinden Entity Listesine

```csharp
IEnumerable<TEntity> ToEntityList<TDto, TEntity, TId>(
    IEnumerable<TDto> dtos)
    where TDto : class, IDto
    where TEntity : class, IEntity<TId>
    where TId : notnull;
```

`ToEntity`'nin liste versiyonu. Toplu yazma senaryolarında kullanılır — örneğin bir Excel'den toplu veri aktarımı.

---

## 4. `AutoMapperAdapter` — Implementasyon

```csharp
namespace Seam.Infrastructure.Mapping;

using Seam.Domain.Dtos;
using Seam.Domain.Entities;
using Seam.Domain.Mapping;

public sealed class AutoMapperAdapter(AutoMapper.IMapper autoMapper)
    : Seam.Domain.Mapping.IMapper
{
    public TDto ToDto<TEntity, TId, TDto>(TEntity entity)
        where TEntity : class, IEntity<TId>
        where TId : notnull
        where TDto : class, IDto
        => autoMapper.Map<TDto>(entity);

    public TEntity ToEntity<TDto, TEntity, TId>(TDto dto)
        where TDto : class, IDto
        where TEntity : class, IEntity<TId>
        where TId : notnull
        => autoMapper.Map<TEntity>(dto);

    public IEnumerable<TDto> ToDtoList<TEntity, TId, TDto>(
        IEnumerable<TEntity> entities)
        where TEntity : class, IEntity<TId>
        where TId : notnull
        where TDto : class, IDto
        => autoMapper.Map<IEnumerable<TDto>>(entities);

    public IEnumerable<TEntity> ToEntityList<TDto, TEntity, TId>(
        IEnumerable<TDto> dtos)
        where TDto : class, IDto
        where TEntity : class, IEntity<TId>
        where TId : notnull
        => autoMapper.Map<IEnumerable<TEntity>>(dtos);
}
```

### Satır Satır Açıklama

**`public sealed class AutoMapperAdapter(AutoMapper.IMapper autoMapper)`**
**`    : Seam.Domain.Mapping.IMapper`**

Burada dikkat edilmesi gereken önemli bir nokta var: **iki farklı `IMapper` aynı anda kullanılıyor.**

- `AutoMapper.IMapper` → AutoMapper kütüphanesinin kendi `IMapper`'ı. Constructor'da inject ediliyor.
- `Seam.Domain.Mapping.IMapper` → Bizim yazdığımız `IMapper`. Bu sınıfın implement ettiği arayüz.

İsimleri aynı ama namespace'leri farklı. C# hangi `IMapper`'ın hangisi olduğunu namespace üzerinden ayırt eder. Karışıklığı önlemek için `Seam.Domain.Mapping.IMapper` tam adıyla yazıldı.

---

**`=> autoMapper.Map<TDto>(entity)`**

Her metod tek satır — `=>` expression body söz dizimi. Gerçek işi AutoMapper yapıyor, biz sadece AutoMapper'ı çağırıyoruz.

`autoMapper.Map<TDto>(entity)` → "Bu entity'yi `TDto` tipine dönüştür" demek. AutoMapper aynı isimli property'leri otomatik eşleştirir:

```
Kullanici.Id    → KullaniciDto.Id    ✓ (aynı isim)
Kullanici.Ad    → KullaniciDto.Ad    ✓ (aynı isim)
Kullanici.Email → KullaniciDto.Email ✓ (aynı isim)
Kullanici.SifreHash → KullaniciDto'da yok → atlanır ✓
```

---

### Neden Adapter Pattern?

`AutoMapper.IMapper`'ı doğrudan handler'lara inject etmek yerine neden `AutoMapperAdapter` yazdık?

```csharp
// Kötü — AutoMapper doğrudan Application katmanına sızıyor
public class KullaniciGetirHandler(AutoMapper.IMapper mapper) { }

// İyi — sadece bizim IMapper sözleşmesi biliniyor
public class KullaniciGetirHandler(Seam.Domain.Mapping.IMapper mapper) { }
```

**Adapter Pattern** bir nesneyi başka bir arayüze uyumlu hale getirir. Elektrik prizi adaptörü gibi — Avrupa fişini Amerikan prizine bağlar, ikisi de değişmez.

Yarın AutoMapper yerine **Mapster** veya başka bir kütüphane kullanmak istersen:
- `AutoMapperAdapter` yerine `MapsterAdapter` yazarsın
- DI kaydını değiştirirsin
- Handler'lara, Application katmanına, Domain katmanına **hiç dokunmazsın**

---

## 5. DI Kaydı

```csharp
public static IServiceCollection AddSeamMapping(
    this IServiceCollection services)
{
    services.AddAutoMapper(cfg => { });
    services.AddScoped<Seam.Domain.Mapping.IMapper, AutoMapperAdapter>();
    return services;
}
```

**`services.AddAutoMapper(cfg => { })`**
AutoMapper'ı DI'a kaydeder. `cfg => { }` boş konfigürasyon — convention-based mapping aktif, ek ayar gerekmez.

**`services.AddScoped<Seam.Domain.Mapping.IMapper, AutoMapperAdapter>()`**
"`IMapper` istendiğinde `AutoMapperAdapter` ver" demek. `Scoped` — her HTTP isteği için bir instance oluşturulur.

---

## 6. Kullanım Örneği

```csharp
// Handler'da kullanım
public sealed class KullanicilariGetirHandler(
    IReadRepository<Kullanici, Guid> repository,
    IMapper mapper)
{
    public async Task<Result<IEnumerable<KullaniciDto>>> Handle(
        KullanicilariGetirQuery query,
        CancellationToken ct)
    {
        // Tüm kullanıcıları getir
        var kullanicilar = await repository.GetAllAsync(ct);

        // Entity listesini DTO listesine dönüştür
        var dtos = mapper.ToDtoList<Kullanici, Guid, KullaniciDto>(kullanicilar);

        return Result.Success(dtos);
    }
}

// Tekil dönüşüm
var dto = mapper.ToDto<Kullanici, Guid, KullaniciDto>(kullanici);

// DTO'dan Entity'ye
var entity = mapper.ToEntity<KullaniciDto, Kullanici, Guid>(dto);

// DTO listesinden Entity listesine
var entities = mapper.ToEntityList<KullaniciDto, Kullanici, Guid>(dtos);
```
