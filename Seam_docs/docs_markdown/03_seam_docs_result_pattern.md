# Seam.Domain — Result Pattern

---

## 1. Problem: Hatalar Nasıl Yönetilmeli?

Bir fonksiyon çalıştığında iki şey olabilir: **başarılı** ya da **başarısız**. Peki başarısız olduğunda bunu nasıl anlatırız?

### Yöntem 1 — Exception Fırlatmak (Geleneksel Yol)

```csharp
public Kullanici KullaniciBul(Guid id)
{
    var kullanici = veritabani.Bul(id);
    if (kullanici == null)
        throw new Exception("Kullanıcı bulunamadı!"); // ← exception fırlatıldı
    return kullanici;
}
```

**Sorun:** Exception'lar "beklenmedik durumlar" için tasarlanmıştır. "Kullanıcı bulunamadı" beklenmedik bir durum değil, olası bir iş kuralıdır. Exception kullanmak:
- Pahalıdır (performans kaybı)
- Kodun akışını anlamayı zorlaştırır
- Her yerde `try/catch` yazmak zorunlu hale gelir

### Yöntem 2 — `null` Döndürmek

```csharp
public Kullanici? KullaniciBul(Guid id)
{
    return veritabani.Bul(id); // bulunamazsa null döner
}
```

**Sorun:** `null` "neden bulunamadı?" sorusunu cevaplamaz. Bulunamadı mı? Yetkin yok mu? Silinmiş mi? Hepsi `null` döner — anlamsız.

### Yöntem 3 — Result Pattern (Seam'in Yolu)

```csharp
public Result<Kullanici> KullaniciBul(Guid id)
{
    var kullanici = veritabani.Bul(id);
    if (kullanici == null)
        return Result.Failure<Kullanici>(Error.NotFound("Kullanıcı bulunamadı."));
    return Result.Success(kullanici);
}
```

**Sonuç:** Hem "başarılı mı?" hem de "neden başarısız?" bilgisi tek bir nesnede taşınır. `null` yok, exception yok — temiz ve anlaşılır.

---

## 2. `ErrorType` — Hata Kategorileri

```csharp
namespace Seam.Domain.Results;

public enum ErrorType
{
    BadRequest,
    Unauthorized,
    Forbidden,
    NotFound,
    Conflict,
    Validation,
    InternalError
}
```

### `enum` Nedir?

`enum` sabit değerler kümesidir. `int` yerine anlamlı isimler kullanmamızı sağlar.

```csharp
// enum olmadan — anlamsız
int hataTipi = 4; // 4 ne demek?

// enum ile — anlamlı
ErrorType hataTipi = ErrorType.NotFound; // "kayıt bulunamadı" demek
```

### Her Değer Ne Anlama Gelir?

- **`BadRequest`** → Gelen istek hatalı veya eksik. "Yaş alanı sayı olmalı" gibi.
- **`Unauthorized`** → Kimlik doğrulama başarısız. "Giriş yapmadan erişemezsin."
- **`Forbidden`** → Giriş yapıldı ama yetki yok. "Bu sayfaya erişim izniniz yok."
- **`NotFound`** → İstenen kayıt bulunamadı. "ID: 5 olan ürün yok."
- **`Conflict`** → Çakışma var. "Bu e-posta adresi zaten kayıtlı."
- **`Validation`** → İş kuralı ihlali. "Fiyat 0'dan küçük olamaz."
- **`InternalError`** → Beklenmedik sistem hatası. "Sunucu hatası."

**Neden bu kategoriler?**
API katmanında her `ErrorType` bir HTTP durum koduna dönüştürülür:
```
NotFound      → 404
Unauthorized  → 401
Validation    → 422
InternalError → 500
```
Böylece API'den dönen HTTP kodu otomatik ve tutarlı olur.

---

## 3. `ValidationError` — Alan Bazlı Hata

```csharp
namespace Seam.Domain.Results;

public sealed record ValidationError(string PropertyName, string Message);
```

### Satır Satır Açıklama

**`sealed`** → Bu sınıftan türetilemez. Tasarım sabittir.

**`record`** → Value object. İki `ValidationError` aynı `PropertyName` ve `Message`'a sahipse eşittir. Normal sınıflarda `==` referansı karşılaştırır, `record`'da değeri karşılaştırır.

**`(string PropertyName, string Message)`** → Bu `record`'un kısa tanım söz dizimidir. C# otomatik olarak constructor ve property'leri oluşturur. Şu kodun kısaltmasıdır:

```csharp
public sealed record ValidationError
{
    public string PropertyName { get; init; }
    public string Message { get; init; }

    public ValidationError(string propertyName, string message)
    {
        PropertyName = propertyName;
        Message = message;
    }
}
```

### Neden Sadece Mesaj Değil de PropertyName de Var?

Kullanıcıya "Bir hata oluştu" demek yetersizdir. Hangi alanda hata var?

```csharp
// PropertyName olmadan — yetersiz
"Geçersiz değer"

// PropertyName ile — net ve kullanışlı
PropertyName: "Email"
Message: "Geçerli bir e-posta adresi giriniz."
```

Frontend bu bilgiyi kullanarak ilgili input alanının yanında hata mesajını gösterebilir.

---

## 4. `Error` — Hata Value Object'i

```csharp
namespace Seam.Domain.Results;

public sealed record Error
{
    public ErrorType Type { get; }
    public string Message { get; }
    public IReadOnlyList<ValidationError> ValidationErrors { get; }

    private Error(ErrorType type, string message,
        IReadOnlyList<ValidationError>? validationErrors = null)
    {
        Type = type;
        Message = message;
        ValidationErrors = validationErrors ?? [];
    }

    public static Error BadRequest(string message)
        => new(ErrorType.BadRequest, message);

    public static Error Unauthorized(string message = "Unauthorized access.")
        => new(ErrorType.Unauthorized, message);

    public static Error Forbidden(string message = "Access forbidden.")
        => new(ErrorType.Forbidden, message);

    public static Error NotFound(string message)
        => new(ErrorType.NotFound, message);

    public static Error Conflict(string message)
        => new(ErrorType.Conflict, message);

    public static Error Validation(IReadOnlyList<ValidationError> validationErrors)
        => new(ErrorType.Validation,
            "One or more validation errors occurred.", validationErrors);

    public static Error Validation(string propertyName, string message)
        => new(ErrorType.Validation,
            "One or more validation errors occurred.",
            [new ValidationError(propertyName, message)]);

    public static Error InternalError(string message = "An unexpected error occurred.")
        => new(ErrorType.InternalError, message);

    public static readonly Error None = new(ErrorType.InternalError, string.Empty);
}
```

### Satır Satır Açıklama

**`public sealed record Error`**
`Error` bir value object'tir. `record` kullandık çünkü aynı tip ve mesaja sahip iki hata eşit sayılmalıdır.

**`public ErrorType Type { get; }`**
Hatanın kategorisi. `{ get; }` ile sadece okunabilir — bir kez oluşturulur, değiştirilemez.

**`public IReadOnlyList<ValidationError> ValidationErrors { get; }`**
- `IReadOnlyList<T>` → Sadece okunabilen liste. Dışarıdan eleman eklenemez, silinemez.
- Sadece `Validation` tipindeki hatalarda dolu olur. Diğerlerinde boş liste `[]` döner.

**`private Error(...)`**
Constructor `private`! Yani `new Error(...)` diye dışarıdan nesne oluşturulamaz. Neden? Çünkü nesne oluşturmayı kontrol altına almak istiyoruz. Bunun yerine static factory metodları kullanılır.

**`validationErrors ?? []`**
`??` operatörü "null ise sağdakini kullan" demektir. `validationErrors` null gelirse boş liste `[]` atanır. Böylece `ValidationErrors` hiçbir zaman `null` olmaz — `NullReferenceException` riski sıfırlanır.

**Static Factory Metodlar**

```csharp
public static Error NotFound(string message)
    => new(ErrorType.NotFound, message);
```

`=>` tek satır metod gövdesi söz dizimidir. Şununla aynıdır:
```csharp
public static Error NotFound(string message)
{
    return new Error(ErrorType.NotFound, message);
}
```

Factory metodlar sayesinde:
```csharp
// Uzun yol — sıkıcı
var hata = new Error(ErrorType.NotFound, "Kullanıcı bulunamadı.");

// Kısa ve anlamlı yol
var hata = Error.NotFound("Kullanıcı bulunamadı.");
```

**`public static readonly Error None`**
Başarılı `Result` oluştururken "hata yok" anlamında kullanılan sentinel değer. `readonly` — değiştirilemez, tek bir örnek vardır.

---

## 5. `IResult` ve `IDataResult` — Temel Sözleşmeler

```csharp
public interface IResult
{
    bool IsSuccess { get; }
    bool IsFailure { get; }
    Error Error { get; }
}

public interface IDataResult<out TData> : IResult
{
    TData? Data { get; }
}
```

**`IResult`** → Her sonucun cevap vermek zorunda olduğu üç soru:
- Başarılı mı? (`IsSuccess`)
- Başarısız mı? (`IsFailure`)
- Hata neydi? (`Error`)

**`IDataResult<out TData>`**
- `out` anahtar kelimesi → `TData` sadece çıkış (output) olarak kullanılabilir, giriş olarak kullanılamaz. Bu **covariance** sağlar: `IDataResult<Kullanici>` bir `IDataResult<object>` değişkenine atanabilir.
- `: IResult` → `IResult`'ı miras alır. Yani `IDataResult` hem veri hem de sonuç bilgisi taşır.

**Neden bu interface'ler var?**
Decorator ve Pipeline'larda tüm sonuç tiplerini tek bir `IResult` üzerinden polimorfik işleyebiliriz:

```csharp
// Logging behavior — hangi Result tipi olduğunu bilmeden çalışır
if (response.IsFailure)
    logger.Warning(response.Error.Message);
```

---

## 6. `Result` ve `Result<T>` — Ana Sınıflar

```csharp
public class Result : IResult
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public Error Error { get; }

    protected Result(bool isSuccess, Error error)
    {
        if (isSuccess && error != Error.None)
            throw new InvalidOperationException(
                "Başarılı sonuç hata içeremez.");

        if (!isSuccess && error == Error.None)
            throw new InvalidOperationException(
                "Başarısız sonuç bir hata içermelidir.");

        IsSuccess = isSuccess;
        Error = error;
    }

    public static Result Success() => new(true, Error.None);
    public static Result Failure(Error error) => new(false, error);
    public static Result<TData> Success<TData>(TData data) => new(data, true, Error.None);
    public static Result<TData> Failure<TData>(Error error) => new(default, false, error);
}
```

### Satır Satır Açıklama

**`public bool IsFailure => !IsSuccess;`**
`=>` burada bir **expression body property**'dir. `IsFailure` ayrı bir alan tutmaz — her sorgulandığında `!IsSuccess` hesaplar. Veri tutarlılığını garanti eder: biri `true` ise diğeri her zaman `false`'tur.

**`protected Result(...)`**
Constructor `protected` — sadece bu sınıf ve alt sınıflar kullanabilir. Dışarıdan `new Result(...)` yapılamaz. Neden? Yanlış kombinasyonları önlemek için:

```csharp
// Bu saçmalık engellenir:
new Result(isSuccess: true, error: Error.NotFound("hata")); // YASAK
new Result(isSuccess: false, error: Error.None);             // YASAK
```

**Guard Kontrolleri**

```csharp
if (isSuccess && error != Error.None)
    throw new InvalidOperationException("Başarılı sonuç hata içeremez.");
```

Başarılı bir sonuçta hata olmamalı. Başarısız bir sonuçta hata olmalı. Bu kurallar constructor'da zorlanır — tutarsız nesne oluşturmak imkânsız hale gelir.

---

### `Result<TData>` — Veri Taşıyan Sonuç

```csharp
public sealed class Result<TData> : Result, IDataResult<TData>
{
    private readonly TData? _data;

    public TData? Data => IsSuccess
        ? _data
        : throw new InvalidOperationException(
            "Başarısız sonuçtan veri okunamaz.");

    internal Result(TData? data, bool isSuccess, Error error)
        : base(isSuccess, error)
    {
        _data = data;
    }
}
```

**`: Result, IDataResult<TData>`**
`Result<TData>` hem `Result`'tan miras alır (tüm başarı/hata mantığı gelir) hem de `IDataResult<TData>`'yı uygular (veri taşıma sözleşmesi).

**`private readonly TData? _data`**
`readonly` → Bir kez atandıktan sonra değiştirilemez. `_` öneki C#'ta özel alan (private field) konvansiyonudur.

**`public TData? Data => IsSuccess ? _data : throw ...`**
`?:` üçlü operatör: "IsSuccess true ise `_data` döndür, false ise exception fırlat."

Neden exception? Başarısız sonuçtan veri okumaya çalışmak programcı hatasıdır. Sessizce `null` döndürmek hatayı gizler. Erken ve net hata vermek daha güvenlidir.

**`internal Result(...)`**
`internal` → Sadece aynı assembly (proje) içinden erişilebilir. Dışarıdan `new Result<T>(...)` yapılamaz. Sadece `Result.Success<T>(data)` factory metodu kullanılır.

---

## 7. Semantik Sınıflar — Okunabilirlik İçin

```csharp
public sealed class SuccessResult : Result
{
    public SuccessResult() : base(true, Error.None) { }
}

public sealed class ErrorResult : Result
{
    public ErrorResult(Error error) : base(false, error) { }
}

public sealed class SuccessDataResult<TData> : Result<TData>
{
    public SuccessDataResult(TData data) : base(data, true, Error.None) { }
}

public sealed class ErrorDataResult<TData> : Result<TData>
{
    public ErrorDataResult(Error error) : base(default, false, error) { }
}
```

**`: base(...)`** → Üst sınıfın constructor'ını çağırır. `SuccessResult` oluşturulduğunda otomatik olarak `Result(true, Error.None)` çağrılır.

**`default`** → Generic tip için varsayılan değer. `TData` bir `class` ise `null`, `int` ise `0`, `bool` ise `false` döner.

### Factory Metod vs Semantik Sınıf — Fark Ne?

```csharp
// Factory metod — kısa, zincirlenebilir
return Result.Success(kullaniciDto);

// Semantik sınıf — niyeti açık, okunabilir
return new SuccessDataResult<KullaniciDto>(kullaniciDto);
```

İkisi de aynı nesneyi üretir. Tercih geliştiriciye bırakılmıştır — ikisi de desteklenir.

---

## 8. Tüm Yapının Özeti

```
Result (başarı/hata + hata bilgisi)
├── Result<TData> (+ veri taşır)
│   ├── SuccessDataResult<TData>
│   └── ErrorDataResult<TData>
├── SuccessResult
└── ErrorResult
```

### Kullanım Senaryoları

```csharp
// 1. Başarılı — veri yok
return Result.Success();
return new SuccessResult();

// 2. Başarısız — veri yok
return Result.Failure(Error.NotFound("Kullanıcı bulunamadı."));
return new ErrorResult(Error.Conflict("E-posta zaten kayıtlı."));

// 3. Başarılı — veri var
return Result.Success(kullaniciDto);
return new SuccessDataResult<KullaniciDto>(kullaniciDto);

// 4. Başarısız — veri tipi belirtilmiş
return Result.Failure<KullaniciDto>(Error.Unauthorized());
return new ErrorDataResult<KullaniciDto>(Error.NotFound("Bulunamadı."));

// 5. Sonucu kontrol etmek
if (result.IsFailure)
{
    Console.WriteLine(result.Error.Type);    // NotFound
    Console.WriteLine(result.Error.Message); // "Kullanıcı bulunamadı."
}

// 6. Validasyon hatası
return Result.Failure(Error.Validation([
    new ValidationError("Email", "Geçerli e-posta giriniz."),
    new ValidationError("Ad", "Ad en az 2 karakter olmalı.")
]));
```
