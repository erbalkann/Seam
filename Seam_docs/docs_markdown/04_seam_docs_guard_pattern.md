# Seam.Domain — Guard Pattern

---

## 1. Problem: Geçersiz Veri Sisteme Girmesin

Bir kullanıcı kayıt formu düşün. Kullanıcı şunları yapabilir:

- Adı boş bırakabilir
- Yaşa `-5` yazabilir
- E-postayı 500 karakter yapabilir

Bu veriler veritabanına ulaşmadan önce durdurulmalıdır. **Guard Pattern** tam olarak bunu yapar — kapıdaki güvenlik görevlisi gibi, geçersiz veriyi içeri sokmaz.

### Guard Olmadan Hayat

```csharp
public Result KullaniciOlustur(string ad, int yas)
{
    // Hiç kontrol yok — geçersiz veri veritabanına gider
    var kullanici = new Kullanici { Ad = ad, Yas = yas };
    repository.Ekle(kullanici);
    return Result.Success();
}
```

**Sorun:** `ad` boş string olabilir, `yas` -999 olabilir. Veritabanı kirlenebilir.

### Guard ile Hayat

```csharp
public Result KullaniciOlustur(string ad, int yas)
{
    var kontrol = Guard.Combine(
        Guard.NotNullOrWhiteSpace(ad, nameof(ad)),
        Guard.Between(yas, 0, 150, nameof(yas))
    );

    if (kontrol.IsFailure) return kontrol;

    var kullanici = new Kullanici { Ad = ad, Yas = yas };
    repository.Ekle(kullanici);
    return Result.Success();
}
```

**Sonuç:** Geçersiz veri handler'a ulaşmadan `Result.Failure` döner. Temiz, güvenli, okunabilir.

---

## 2. `Guard` Sınıfı — Genel Yapı

```csharp
namespace Seam.Domain.Guards;

using Seam.Domain.Results;

public static class Guard
{
    // ... metodlar
}
```

**`public static class Guard`**
- `static class` → Bu sınıftan nesne oluşturulamaz. `new Guard()` yazamazsın. Tüm metodlar doğrudan sınıf adıyla çağrılır: `Guard.NotNull(...)`.
- Neden static? Guard bir araç kutusudur. Her seferinde yeni bir Guard nesnesi oluşturmak anlamsız olurdu — tıpkı her hesap için yeni bir hesap makinesi satın almak gibi.

**`using Seam.Domain.Results;`**
Guard metodları `Result` döndürdüğü için `Results` namespace'ini içeri alır.

---

## 3. Null / Empty / WhiteSpace Kontrolleri

### `NotNull<T>` — Referans Tipler

```csharp
public static Result NotNull<T>(
    T? value,
    string propertyName,
    string? message = null)
    where T : class
{
    return value is null
        ? Result.Failure(Error.Validation(
            propertyName,
            message ?? $"{propertyName} cannot be null."))
        : Result.Success();
}
```

**`T? value`**
`T?` burada "null olabilir T tipi" demektir. `where T : class` kısıtı bu generic'in sadece referans tipler (class) için çalışacağını söyler.

**`string? message = null`**
`= null` varsayılan değer parametresi. `message` verilmezse otomatik olarak `null` olur ve aşağıda varsayılan mesaj üretilir. Yani metodu iki şekilde çağırabilirsin:

```csharp
Guard.NotNull(kullanici, nameof(kullanici));
// → "kullanici cannot be null."

Guard.NotNull(kullanici, nameof(kullanici), "Kullanıcı zorunludur.");
// → "Kullanıcı zorunludur."
```

**`where T : class`**
Generic kısıt. Bu metod sadece class tiplerle çalışır (string, nesne vb.). `int`, `bool` gibi value type'lar için aşağıdaki overload kullanılır.

**`value is null ? ... : ...`**
Üçlü operatör. "value null ise Failure döndür, değilse Success döndür."

**`message ?? $"{propertyName} cannot be null."`**
`??` null-coalescing operatörü. "message null ise sağdaki string'i kullan." `$"..."` ise string interpolation — `{propertyName}` yerine gerçek değer yazılır.

---

### `NotNull<T>` — Value Type'lar

```csharp
public static Result NotNull<T>(
    T? value,
    string propertyName,
    string? message = null)
    where T : struct
{
    return value is null
        ? Result.Failure(Error.Validation(
            propertyName,
            message ?? $"{propertyName} cannot be null."))
        : Result.Success();
}
```

**`where T : struct`**
`struct` value type demektir — `int`, `DateTime`, `Guid` gibi. `int?` (nullable int) null olabilir ama `int` olamaz. Bu overload nullable value type'lar için çalışır.

**Neden iki ayrı `NotNull` metodu?**
C#'ta `class` ve `struct` tipler farklı davranır. `T?` her ikisinde de "nullable" anlamına gelir ama derleyici bunları farklı ele alır. İki overload sayesinde her iki tipi de destekleriz.

---

### `NotNullOrEmpty` — String

```csharp
public static Result NotNullOrEmpty(
    string? value,
    string propertyName,
    string? message = null)
{
    return string.IsNullOrEmpty(value)
        ? Result.Failure(Error.Validation(
            propertyName,
            message ?? $"{propertyName} cannot be null or empty."))
        : Result.Success();
}
```

**`string.IsNullOrEmpty(value)`**
C#'ın built-in metodu. `null` veya `""` (boş string) ise `true` döner.

**`NotNull` ile farkı ne?**
`NotNull` sadece `null` kontrolü yapar. `NotNullOrEmpty` hem `null` hem `""` (boş) kontrolü yapar. `" "` (sadece boşluk) geçer — bunun için `NotNullOrWhiteSpace` var.

---

### `NotNullOrWhiteSpace` — String (En Kapsamlı)

```csharp
public static Result NotNullOrWhiteSpace(
    string? value,
    string propertyName,
    string? message = null)
{
    return string.IsNullOrWhiteSpace(value)
        ? Result.Failure(Error.Validation(
            propertyName,
            message ?? $"{propertyName} cannot be null, empty, or whitespace."))
        : Result.Success();
}
```

**`string.IsNullOrWhiteSpace(value)`**
`null`, `""`, `" "`, `"\t"`, `"\n"` — hepsini yakalar. Kullanıcı form alanına sadece boşluk tuşuna basarsa bile hata verir.

---

### `NotNullOrEmpty` — Koleksiyonlar

```csharp
public static Result NotNullOrEmpty<T>(
    IEnumerable<T>? value,
    string propertyName,
    string? message = null)
{
    return value is null || !value.Any()
        ? Result.Failure(Error.Validation(
            propertyName,
            message ?? $"{propertyName} cannot be null or empty."))
        : Result.Success();
}
```

**`IEnumerable<T>`**
Liste, dizi, koleksiyon gibi her türlü sıralı veri kaynağı için ortak arayüz. `List<T>`, `T[]`, `HashSet<T>` hepsi `IEnumerable<T>`'dir.

**`!value.Any()`**
`Any()` koleksiyonda en az bir eleman var mı? `!Any()` → eleman yok demek. Null değilse ama boşsa da hata verir.

---

## 4. Range Kontrolleri

### `Min<T>` — Minimum Değer

```csharp
public static Result Min<T>(
    T value,
    T min,
    string propertyName,
    string? message = null)
    where T : IComparable<T>
{
    return value.CompareTo(min) < 0
        ? Result.Failure(Error.Validation(
            propertyName,
            message ?? $"{propertyName} must be greater than or equal to {min}."))
        : Result.Success();
}
```

**`where T : IComparable<T>`**
`IComparable<T>` "karşılaştırılabilir" demektir. `int`, `decimal`, `DateTime`, `DateOnly` hepsi bu arayüzü uygular. Tek bir metod tüm karşılaştırılabilir tipler için çalışır.

**`value.CompareTo(min) < 0`**
`CompareTo` karşılaştırma metodur:
- `< 0` → value, min'den küçük
- `= 0` → eşit
- `> 0` → value, min'den büyük

`< 0` ise hata — değer minimumun altında.

```csharp
Guard.Min(yas, 0, nameof(yas));           // int
Guard.Min(fiyat, 0.01m, nameof(fiyat));   // decimal
Guard.Min(baslangic, DateTime.Now, nameof(baslangic)); // DateTime
```

---

### `Max<T>` — Maksimum Değer

```csharp
public static Result Max<T>(
    T value,
    T max,
    string propertyName,
    string? message = null)
    where T : IComparable<T>
{
    return value.CompareTo(max) > 0
        ? Result.Failure(Error.Validation(
            propertyName,
            message ?? $"{propertyName} must be less than or equal to {max}."))
        : Result.Success();
}
```

`> 0` → value, max'tan büyük → hata.

---

### `Between<T>` — Aralık Kontrolü

```csharp
public static Result Between<T>(
    T value,
    T min,
    T max,
    string propertyName,
    string? message = null)
    where T : IComparable<T>
{
    return value.CompareTo(min) < 0 || value.CompareTo(max) > 0
        ? Result.Failure(Error.Validation(
            propertyName,
            message ?? $"{propertyName} must be between {min} and {max}."))
        : Result.Success();
}
```

**`||` operatörü** → "veya" anlamında. Minimumdan küçük **ya da** maksimumdan büyükse hata.

```csharp
Guard.Between(yas, 0, 150, nameof(yas));
Guard.Between(indirimOrani, 0m, 100m, nameof(indirimOrani));
```

---

### `MinLength` ve `MaxLength` — String Uzunluğu

```csharp
public static Result MinLength(
    string? value,
    int min,
    string propertyName,
    string? message = null)
{
    var length = value?.Length ?? 0;
    return length < min
        ? Result.Failure(Error.Validation(
            propertyName,
            message ?? $"{propertyName} must be at least {min} characters long."))
        : Result.Success();
}
```

**`value?.Length ?? 0`**
- `value?.Length` → `value` null ise `null` döner, değilse uzunluğu döner. `?.` null-conditional operatörüdür.
- `?? 0` → null ise 0 kullan. Yani null string'in uzunluğu 0 kabul edilir.

**Neden `Between<string>` kullanmadık?**
`Between<string>` alfabe sıralamasıyla karşılaştırır — `"abc" < "abd"` gibi. Biz karakter sayısını karşılaştırmak istiyoruz. Bu yüzden string uzunluğu için özel metodlar yazdık.

---

## 5. `Combine` — Toplu Kontrol

```csharp
public static Result Combine(params Result[] results)
{
    var errors = results
        .Where(r => r.IsFailure)
        .SelectMany(r => r.Error.ValidationErrors)
        .ToList();

    return errors.Count > 0
        ? Result.Failure(Error.Validation(errors))
        : Result.Success();
}
```

### `params Result[]` Nedir?

`params` anahtar kelimesi "istediğin kadar parametre ver" demektir. Dizi oluşturmadan doğrudan virgülle ayırarak geçirebilirsin:

```csharp
// params olmadan
Guard.Combine(new Result[] { sonuc1, sonuc2, sonuc3 });

// params ile — çok daha temiz
Guard.Combine(sonuc1, sonuc2, sonuc3);
```

### LINQ Zinciri

```csharp
var errors = results
    .Where(r => r.IsFailure)        // sadece başarısız sonuçları al
    .SelectMany(r => r.Error.ValidationErrors) // her sonuçtaki hataları düzleştir
    .ToList();                       // listeye çevir
```

**`.Where(...)`** → Filtrele. Sadece `IsFailure == true` olanları geç.

**`.SelectMany(...)`** → Her `Result`'ın `ValidationErrors` listesini alıp tek bir düz listeye birleştir. Şöyle düşün:

```
Result1.ValidationErrors → [Hata1, Hata2]
Result2.ValidationErrors → [Hata3]
Result3.ValidationErrors → []

SelectMany sonucu → [Hata1, Hata2, Hata3]
```

**`.ToList()`** → LINQ sorgusu tembel (lazy) çalışır — `.ToList()` ile çalıştırır ve listeye dönüştürür.

### Neden Combine Var?

Combine olmadan her kontrolü ayrı ayrı yapmak zorunda kalırsın:

```csharp
// Combine olmadan — ilk hatada durur, diğer hatalar görülmez
var r1 = Guard.NotNullOrWhiteSpace(ad, nameof(ad));
if (r1.IsFailure) return r1;

var r2 = Guard.MinLength(ad, 2, nameof(ad));
if (r2.IsFailure) return r2;

var r3 = Guard.Between(yas, 0, 150, nameof(yas));
if (r3.IsFailure) return r3;
```

**Sorun:** Kullanıcı formu gönderdi, sadece "Ad boş olamaz" hatası gördü. Düzeltti, tekrar gönderdi, bu sefer "Yaş geçersiz" hatası gördü. Kullanıcı deneyimi berbat.

```csharp
// Combine ile — tüm hatalar tek seferde döner
var result = Guard.Combine(
    Guard.NotNullOrWhiteSpace(ad, nameof(ad)),
    Guard.MinLength(ad, 2, nameof(ad)),
    Guard.Between(yas, 0, 150, nameof(yas))
);

if (result.IsFailure) return result;
// result.Error.ValidationErrors → tüm hatalar bir arada
```

**Sonuç:** Kullanıcı tüm hataları tek seferde görür — çok daha iyi deneyim.

---

## 6. Gerçek Dünya Kullanım Örneği

```csharp
public Result<UrunDto> UrunOlustur(
    string ad,
    decimal fiyat,
    int stok,
    string? aciklama)
{
    // Tüm kontroller tek seferde
    var dogrulama = Guard.Combine(
        Guard.NotNullOrWhiteSpace(ad, nameof(ad)),
        Guard.MinLength(ad, 3, nameof(ad)),
        Guard.MaxLength(ad, 200, nameof(ad)),
        Guard.Min(fiyat, 0.01m, nameof(fiyat)),
        Guard.Between(stok, 0, 10_000, nameof(stok))
    );

    if (dogrulama.IsFailure)
        return Result.Failure<UrunDto>(dogrulama.Error);

    // Tüm kontroller geçti — iş mantığına devam
    var urun = new Urun { Ad = ad, Fiyat = fiyat, Stok = stok };
    repository.Ekle(urun);

    return Result.Success(new UrunDto(urun.Id, urun.Ad, urun.Fiyat));
}
```

Eğer `ad` boş ve `fiyat` negatifse dönen hata şöyle görünür:

```json
{
  "type": "Validation",
  "message": "One or more validation errors occurred.",
  "validationErrors": [
    { "propertyName": "ad", "message": "ad cannot be null, empty, or whitespace." },
    { "propertyName": "fiyat", "message": "fiyat must be greater than or equal to 0,01." }
  ]
}
```
