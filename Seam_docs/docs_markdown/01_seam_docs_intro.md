# Seam Kütüphanesi — Sıfırdan Anlatım

---

## 1. Bu Kütüphane Ne İçin Yazıldı?

Bir yazılım projesi geliştirirken her seferinde aynı şeyleri tekrar tekrar yazmak zorunda kalırız:

- Veritabanına kayıt ekle, sil, güncelle, listele
- Kullanıcıdan gelen verileri doğrula
- Hata olursa bunu düzgünce yönet
- Kim ne zaman ne yaptı, kayıt altına al

Bu işlemleri her projede sıfırdan yazmak hem zaman kaybıdır hem de hata riskini artırır. **Seam**, bu tekrar eden işlemleri bir kez yazıp her projede kullanabilmek için geliştirilmiş bir **temel kütüphane (core library)**'dir.

Düşün ki LEGO parçaları üretiyorsun. Her seferinde yeni bir araba yaparken tekerlekleri sıfırdan icat etmiyorsun — hazır LEGO tekerleği kullanıyorsun. Seam de yazılım dünyasındaki o hazır LEGO parçalarıdır.

---

## 2. Proje Nasıl Organize Edildi? (Clean Architecture)

Seam tek bir dosya ya da tek bir proje değil. **Birden fazla katmandan** oluşuyor. Bu yaklaşıma **Clean Architecture (Temiz Mimari)** deniyor.

Neden tek proje yapmadık? Çünkü her şeyi tek bir yere koymak zamanla karmaşıklığa yol açar. Bir şeyi değiştirdiğinde başka şeyler de bozulur. Katmanlı yapıda ise her katmanın net bir görevi vardır ve birbirlerini minimum düzeyde etkiler.

```
Seam Çözümü (Solution)
│
├── Seam.Domain          → Temel kurallar ve sözleşmeler (hiçbir şeye bağımlı değil)
├── Seam.Application     → İş mantığı arayüzleri (sadece Domain'e bağımlı)
└── Seam.Infrastructure  → Gerçek implementasyonlar (Domain + Application'a bağımlı)
```

### Bağımlılık Yönü

```
Seam.Infrastructure
       ↓ bağımlı
Seam.Application
       ↓ bağımlı
Seam.Domain
       ↓ bağımlı
    HİÇBİR ŞEY
```

**Altın Kural:** Oklar yukarı doğru gidemez. Yani `Seam.Domain`, `Seam.Application`'ı tanımaz. `Seam.Application`, `Seam.Infrastructure`'ı tanımaz. Bu kural sayesinde katmanlar birbirinden bağımsız değiştirilebilir.

**Bunu yazmasaydık ne olurdu?**
Her şey birbirine bağlı olurdu. Veritabanını değiştirmek istediğinde iş mantığı kodunu da değiştirmek zorunda kalırdın. Bu "spagetti kod" denen kaosa yol açar.

---

## 3. C# Dilinin Temel Taşları (Kodları Anlamak İçin)

Kodları anlatmaya başlamadan önce C#'ta sıkça göreceğimiz birkaç kavramı açıklayalım.

### `namespace` Nedir?

```csharp
namespace Seam.Domain.Entities;
```

`namespace` bir klasör gibidir ama kod dünyasında. Aynı isimde iki sınıf olsa bile farklı namespace'lerde olduklarında çakışmaz. `Seam.Domain.Entities` dediğimizde "bu kod Seam projesinin, Domain katmanının, Entities klasörüne ait" diyoruz.

**Yazmasaydık ne olurdu?**
Farklı kütüphanelerde aynı isimde sınıflar olduğunda hangisini kullandığını bilemezdin. Her şey karışırdı.

---

### `interface` Nedir?

```csharp
public interface IEntity<TId>
{
    TId Id { get; }
}
```

`interface` bir **sözleşme**dir. "Bu sözleşmeyi imzalayan her sınıf, şu özelliklere/metodlara sahip olmak zorundadır" der. Ama nasıl çalışacağını söylemez — sadece ne yapması gerektiğini söyler.

Bir restoranın menüsü gibi düşün. Menüde "pizza var" yazar ama pizzanın nasıl yapılacağı yazmaz. Her şef kendi yöntemiyle pizza yapar ama sonuçta pizza çıkar.

**Yazmasaydık ne olurdu?**
Her sınıf istediği gibi davranırdı. "Bu nesnenin mutlaka bir `Id`'si vardır" diye garanti veremezdin.

---

### `generic` (`<T>`) Nedir?

```csharp
public interface IEntity<TId>
```

`<TId>` burada bir **generic parametre**dir. Yani "bu arayüzü kullanacak kişi, `TId`'nin ne olacağına kendisi karar versin" diyoruz.

Bir kap düşün: "İçine ne koyacağını sen belirle" yazan bir kap. `IEntity<Guid>` dersen Id tipi Guid olur. `IEntity<int>` dersen Id tipi int olur. Aynı kodu farklı tipler için tekrar yazmak zorunda kalmazsın.

---

### `record` Nedir?

```csharp
public sealed record ValidationError(string PropertyName, string Message);
```

`record` özel bir sınıf türüdür. Normal sınıflardan farkı:
- Otomatik olarak eşitlik karşılaştırması yapar (`==` operatörü)
- İçindeki veriler değişmez (immutable) olarak tasarlanır
- Kısa söz dizimiyle tanımlanabilir

Bir **pul** gibi düşün. İki pul aynı değere sahipse eşittir — hangi fiziksel nesne olduğu önemli değil.

---

### `sealed` Nedir?

```csharp
public sealed class ErrorResult : Result
```

`sealed` "bu sınıftan başka sınıf türetilemez" demektir. Kapıya kilit vurmak gibi. Tasarımı korur, istenmeyen kalıtımı engeller.

---

### `abstract` Nedir?

```csharp
public abstract class SeamDbContext : DbContext
```

`abstract` "bu sınıfı doğrudan kullanamazsın, önce türetmen gerekir" demektir. Bir **şablon** gibi. Mimarın çizdiği taslak plan gibi — evi inşa etmek için müteahhit o planı alıp somutlaştırır.

---

Bu temel kavramları öğrendikten sonra artık katmanlara geçebiliriz. İlk katman olan **Seam.Domain** ile devam edelim.
