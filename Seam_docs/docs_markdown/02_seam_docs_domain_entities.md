# Seam.Domain — Entities Katmanı

---

## 1. Bu Katman Ne İçin Var?

`Seam.Domain` kütüphanenin **kalbi**dir. Hiçbir dış pakete bağımlı değildir — ne Entity Framework bilir, ne MediatR, ne de başka bir şey. Sadece saf C# kodu içerir.

Buradaki tek amaç şu soruyu cevaplamaktır:

> "Sistemdeki bir varlık (entity) nasıl görünmeli? Hangi kurallara uymak zorunda?"

Bir kimlik kartı düşün. Her vatandaşın bir TC numarası vardır. Bazılarının doğum tarihi vardır. Bazıları iptal edilmiştir. Bu "olması gereken özellikler" listesi tam olarak bu katmanda tanımlanır — ama kartın fiziksel olarak nasıl basılacağı burada yazılmaz.

---

## 2. `IEntity<TId>` — Her Şeyin Başlangıcı

```csharp
namespace Seam.Domain.Entities;

public interface IEntity<TId>
    where TId : notnull
{
    TId Id { get; }
}
```

### Satır Satır Açıklama

**`namespace Seam.Domain.Entities;`**
Bu dosyanın "Seam projesinin, Domain katmanının, Entities klasörüne" ait olduğunu belirtir. Bir adres gibi düşün.

**`public interface IEntity<TId>`**
- `public` → Bu arayüz dışarıdan erişilebilir. Her yerden kullanılabilir.
- `interface` → Bu bir sözleşmedir. "Beni uygulayan her sınıf şunlara sahip olmak zorunda" der.
- `IEntity` → İsimlendirme kuralı: Interface'ler `I` harfiyle başlar. Böylece koda bakan biri "bu bir interface" diye hemen anlar.
- `<TId>` → Generic parametre. "Id'nin tipi ne olacak, onu kullanan kişi belirlesin" demek. Bir proje `Guid` kullanabilir, başka bir proje `int` kullanabilir. Kod tekrarı olmaz.

**`where TId : notnull`**
Bu bir **kısıt (constraint)**'tır. `TId` olarak `null` olabilen bir tip verilemez. Çünkü bir varlığın Id'si hiçbir zaman `null` olamaz — bu anlamsız olurdu. Derleme zamanında bu hatayı engeller.

**`TId Id { get; }`**
- `TId` → Id'nin tipi, generic parametre ile belirlenir.
- `Id` → Property adı. Her entity'nin bir `Id`'si olmak zorunda.
- `{ get; }` → Sadece okunabilir. Dışarıdan `entity.Id = ...` diye değiştirilemez. Bu kasıtlı — Id bir kez verilir, değişmez.

### Neden Yazdık?

`IEntity<TId>` olmadan şu soruyu soramazdık: "Bu nesnenin bir Id'si var mı?" Generic Repository'de `where TEntity : IEntity<TId>` diyerek "sadece Id'si olan nesnelerle çalış" garantisi veririz. Bunu yazmasaydık her repository her tür nesneyi kabul edebilirdi — tip güvenliği olmazdı.

### Kullanım Örneği

```csharp
// Guid kullanan bir entity
public class Kullanici : IEntity<Guid>
{
    public Guid Id { get; init; }
    public string Ad { get; set; }
}

// int kullanan bir entity
public class Kategori : IEntity<int>
{
    public int Id { get; init; }
    public string Isim { get; set; }
}
```

---

## 3. `IAuditable` — Kim, Ne Zaman Yaptı?

```csharp
namespace Seam.Domain.Entities;

public interface IAuditable
{
    DateTime CreatedAt { get; }
    string CreatedBy { get; }
    DateTime? UpdatedAt { get; }
    string? UpdatedBy { get; }
}
```

### Satır Satır Açıklama

**`DateTime CreatedAt { get; }`**
- `DateTime` → Tarih ve saat bilgisi tutan C# tipi.
- `CreatedAt` → "Oluşturulma zamanı" demek. Kayıt ilk kez veritabanına eklendiğinde bu alan dolar.
- `{ get; }` → Sadece okunabilir. Oluşturulma zamanı sonradan değiştirilemez.

**`string CreatedBy { get; }`**
- `string` → Metin tipi.
- `CreatedBy` → "Kim oluşturdu?" Kullanıcının Id'si burada tutulur. `string` seçtik çünkü userId bazen `"abc123"` gibi metin, bazen `Guid` string'e çevrilmiş olabilir — esnek kalır.

**`DateTime? UpdatedAt { get; }`**
- `DateTime?` → Sonundaki `?` işareti "bu değer `null` olabilir" demektir. Neden? Çünkü yeni oluşturulmuş bir kayıt henüz güncellenmemiştir — `UpdatedAt` değeri yoktur, `null`'dur. `DateTime.MinValue` gibi sahte bir değer koymak yanlış olurdu.

**`string? UpdatedBy { get; }`**
- Aynı mantık: Kayıt hiç güncellenmemişse "kim güncelledi" bilgisi yoktur → `null`.

### Neden Yazdık?

Gerçek dünya uygulamalarında "Bu kaydı kim oluşturdu? Ne zaman değiştirildi?" soruları kritiktir. Banka, hastane, e-ticaret — hepsinde bu bilgiler gerekir. `IAuditable` bu bilgilerin **her zaman var olacağını** garanti eden sözleşmedir.

**Yazmasaydık ne olurdu?**
Her geliştirici bu alanları farklı isimlerle yazardı: `OlusturmaTarihi`, `CreationDate`, `created_at`... Tutarsızlık kaosa yol açardı. Merkezi sözleşme herkesi aynı standarda zorlar.

---

## 4. `ISoftDeletable` — Silmeden Silmek

```csharp
namespace Seam.Domain.Entities;

public interface ISoftDeletable
{
    bool IsDeleted { get; }
    DateTime? DeletedAt { get; }
    string? DeletedBy { get; }
}
```

### Satır Satır Açıklama

**`bool IsDeleted { get; }`**
- `bool` → Sadece `true` veya `false` alabilen tip.
- `IsDeleted` → "Silindi mi?" bayrağı. `true` ise kayıt "silinmiş" sayılır.

**`DateTime? DeletedAt { get; }`**
Kaydın silindiği zaman. Silinmemişse `null`.

**`string? DeletedBy { get; }`**
Kim sildi? Silinmemişse `null`.

### Soft Delete Nedir? Neden Fiziksel Silmiyoruz?

Düşün: Bir e-ticaret sitesinde kullanıcı siparişini iptal etti. Kaydı veritabanından fiziksel olarak silersen geçmiş raporlarda o sipariş görünmez. Muhasebe tutarsızlaşır. Kullanıcı "siparişim nerede?" diye sorarsa cevap veremezsin.

**Soft delete** ise kaydı silmez, sadece `IsDeleted = true` yazar. Kayıt hâlâ veritabanındadır ama normal sorgularda görünmez. İhtiyaç halinde geri getirilebilir.

**Fiziksel silme vs Soft delete:**
```
Fiziksel silme → Kayıt gitti, geri yok
Soft delete    → Kayıt var ama "görünmez" işaretlendi
```

**Yazmasaydık ne olurdu?**
Her geliştirici silme mantığını farklı yazardı. Bazıları fiziksel silerdi, bazıları `IsActive` alanı kullanırdı. Tutarsızlık her yerde.

---

## 5. Birleşik Interface'ler — Konfora Kavuşmak

Bazı entity'ler hem `IEntity` hem `IAuditable` hem de `ISoftDeletable` uygulamak zorundadır. Her seferinde üçünü de yazmak yerine bunları birleştiren kısa yollar tanımladık.

### `IAuditableEntity<TId>`

```csharp
namespace Seam.Domain.Entities;

public interface IAuditableEntity<TId> : IEntity<TId>, IAuditable
    where TId : notnull;
```

**`: IEntity<TId>, IAuditable`**
Bu interface hem `IEntity<TId>`'yi hem de `IAuditable`'ı **miras alır**. Yani `IAuditableEntity<TId>` uygulayan bir sınıf otomatik olarak her ikisini de uygulamak zorundadır.

**Yazmasaydak ne olurdu?**
```csharp
// Birleşik interface olmadan — her seferinde ikisini yazmak gerekir
public class Urun : IEntity<Guid>, IAuditable { ... }

// Birleşik interface ile — tek satır yeterli
public class Urun : IAuditableEntity<Guid> { ... }
```

---

### `ISoftDeletableEntity<TId>`

```csharp
public interface ISoftDeletableEntity<TId> : IEntity<TId>, ISoftDeletable
    where TId : notnull;
```

`IEntity` + `ISoftDeletable` birleşimi. Id'si olan ve soft-delete destekleyen entity'ler için kısa yol.

---

### `IFullAuditableEntity<TId>`

```csharp
public interface IFullAuditableEntity<TId> : IAuditableEntity<TId>, ISoftDeletable
    where TId : notnull;
```

**En kapsamlı sözleşme.** `IEntity` + `IAuditable` + `ISoftDeletable` üçünü birden içerir. Gerçek dünya projelerinde çoğu entity bu interface'i uygular.

```csharp
// Tek satırda üç sözleşmeyi birden uygular
public class Siparis : IFullAuditableEntity<Guid>
{
    public Guid Id { get; init; }

    // IAuditable alanları
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }

    // ISoftDeletable alanları
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }

    // İş alanları
    public decimal Tutar { get; set; }
}
```

---

## 6. `IDto` — Veri Transfer Nesneleri İçin Sözleşme

```csharp
namespace Seam.Domain.Dtos;

public interface IDto;
```

### Bu Ne Demek?

`IDto` hiçbir member (özellik veya metod) tanımlamayan bir **marker interface**'tir. Yani sadece bir etiket görevi görür.

**DTO (Data Transfer Object) Nedir?**
Veritabanındaki entity'leri doğrudan dışarıya vermek güvenli değildir. Örneğin `Kullanici` entity'sinde şifre hash'i, güvenlik bilgileri olabilir. Bunları API'den döndürmek istemezsin. Bunun yerine sadece göstermek istediğin alanları içeren bir **DTO** oluşturursun.

```csharp
// Entity — veritabanı yapısı
public class Kullanici : IFullAuditableEntity<Guid>
{
    public Guid Id { get; init; }
    public string Ad { get; set; }
    public string SifreHash { get; set; }  // Dışarı verilmez!
    // ... audit alanları
}

// DTO — dışarıya verilen yapı
public sealed record KullaniciDto(Guid Id, string Ad) : IDto;
```

**`IDto` marker'ı neden gerekli?**
Generic kısıtlarda `where TDto : IDto` diyerek "sadece DTO tipleri burada kullanılabilir" garantisi veririz. Yanlışlıkla bir entity ya da başka bir nesne geçirilmesini derleme zamanında önler.

---

## 7. Seam.Domain Katmanının Tam Resmi

```
Seam.Domain/
├── Entities/
│   ├── IEntity.cs               → Her entity'nin temel kimlik sözleşmesi
│   ├── IAuditable.cs            → Kim/ne zaman oluşturdu/güncelledi
│   ├── ISoftDeletable.cs        → Mantıksal silme sözleşmesi
│   ├── IAuditableEntity.cs      → IEntity + IAuditable birleşimi
│   ├── ISoftDeletableEntity.cs  → IEntity + ISoftDeletable birleşimi
│   └── IFullAuditableEntity.cs  → IEntity + IAuditable + ISoftDeletable birleşimi
├── Dtos/
│   └── IDto.cs                  → DTO marker sözleşmesi
├── Results/                     → (sonraki bölümde anlatılacak)
└── Guards/                      → (sonraki bölümde anlatılacak)
```

Bu katmanda **tek bir NuGet paketi bile yüklü değildir.** Saf C# — başka hiçbir şeye bağımlı değil. Bu sayede bu katman sonsuza kadar değişmeden kalabilir ve tüm diğer katmanlar ona güvenle bağlanabilir.
