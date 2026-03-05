# Seam.Application — MediatR Pipeline Behaviors

---

## 1. MediatR Nedir?

Önce MediatR'ı anlamak gerekiyor. Bir uygulama düşün: Kullanıcı kayıt ol butonuna bastı. Ne olması gerekiyor?

1. Gelen veriyi doğrula
2. Kullanıcıyı veritabanına kaydet
3. Hoş geldin e-postası gönder
4. Log yaz
5. İşlemi transaction'a sar

Bütün bunları tek bir yerde yazmak karmaşıklık yaratır. **MediatR** bu işlemleri birbirinden ayırmayı sağlar. "Şu komutu çalıştır" dersin, MediatR doğru handler'ı bulur ve çalıştırır.

```
Controller → MediatR → Handler
              (arabulucu)
```

**Mediator (Arabulucu) Pattern:** İki nesne doğrudan haberleşmez, aralarında bir arabulucu vardır. Hava trafik kontrolü gibi — uçaklar birbirleriyle değil, kule ile konuşur.

---

## 2. Pipeline Behavior Nedir?

MediatR'da her istek (request) bir **pipeline** (boru hattı) üzerinden geçer. Pipeline'a davranışlar (behavior) ekleyebilirsin — her istek bu davranışlardan geçer.

```
İstek geldi
    │
    ▼
ExceptionHandlingBehavior   ← 1. katman (en dış)
    │
    ▼
LoggingBehavior             ← 2. katman
    │
    ▼
ValidationBehavior          ← 3. katman
    │
    ▼
TransactionBehavior         ← 4. katman
    │
    ▼
Handler                     ← asıl iş mantığı
    │
    ▼
(yanıt geri döner — aynı katmanlardan geçerek)
```

Bir soğan gibi düşün — her katman bir daha sar. İstek içe doğru gider, yanıt dışa doğru çıkar. Her behavior "öncesinde bir şey yap, sonrasında bir şey yap" diyebilir.

**Bunu yazmasaydık ne olurdu?**
Her handler'a ayrı ayrı "validasyon yap, log yaz, transaction aç" kodu yazman gerekirdi. 100 handler varsa 100 yerde aynı kod. Bir değişiklik yapmak istersen 100 yeri değiştirirsin.

---

## 3. `ICommand` ve `IQuery` — Marker Interface'ler

```csharp
namespace Seam.Application.Messaging;

using MediatR;
using Seam.Domain.Results;

public interface IQuery<TResponse> : IRequest<TResponse>
    where TResponse : IResult;

public interface ICommand : IRequest<Result>;

public interface ICommand<TResponse> : IRequest<TResponse>
    where TResponse : IResult;
```

### CQRS Nedir?

**CQRS (Command Query Responsibility Segregation)** — Komut ve Sorgu Sorumluluğu Ayrımı.

- **Query (Sorgu)** → Veri okur, hiçbir şeyi değiştirmez. "Kullanıcıları listele."
- **Command (Komut)** → Veri değiştirir, yan etkisi vardır. "Kullanıcı ekle."

Bu ayrım neden önemli? Transaction sadece command'larda gereklidir — query'lerde veritabanını değiştirmiyoruz, transaction açmak anlamsız.

**`: IRequest<TResponse>`**
MediatR'ın temel arayüzü. "Bu bir MediatR isteğidir ve `TResponse` tipinde yanıt döner" der.

**`where TResponse : IResult`**
Tüm yanıtların `IResult` olması zorunlu. Yani her komut ve sorgu `Result` veya `Result<T>` döner — hiçbir zaman ham veri veya exception fırlatmaz.

### Kullanım

```csharp
// Veri döndürmeyen komut
public sealed record KullaniciSilCommand(Guid Id) : ICommand;

// Veri döndüren komut
public sealed record KullaniciOlusturCommand(string Ad, string Email)
    : ICommand<Result<Guid>>;

// Sorgu
public sealed record KullaniciGetirQuery(Guid Id)
    : IQuery<Result<KullaniciDto>>;
```

---

## 4. `ExceptionHandlingBehavior` — Beklenmedik Hataları Yakala

```csharp
public sealed class ExceptionHandlingBehavior<TRequest, TResponse>(ILogger logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
    where TResponse : IResult
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        try
        {
            return await next(cancellationToken);
        }
        catch (Exception ex)
        {
            logger
                .ForContext("Request", request, destructureObjects: true)
                .Error(ex,
                    "Unhandled exception for {RequestType}",
                    typeof(TRequest).Name);

            var error = Error.InternalError(ex.Message);
            return (TResponse)(object)Result.Failure(error);
        }
    }
}
```

### Satır Satır Açıklama

**`IPipelineBehavior<TRequest, TResponse>`**
MediatR'ın pipeline behavior sözleşmesi. `Handle` metodunu yazmak zorundayız.

**`RequestHandlerDelegate<TResponse> next`**
`next` pipeline'daki bir sonraki adımdır. "Benden sonra ne varsa çalıştır" demek. `await next(cancellationToken)` diyerek bir sonraki behavior veya handler'ı çağırırız.

Bir ışık düğmesini hayal et. Düğmeye basınca önce ExceptionHandlingBehavior devreye girer, sonra `next()` ile bir sonraki davranışa geçer — tıpkı zincirdeki bir halka gibi.

**`try { return await next(...) } catch { ... }`**
Pipeline'daki herhangi bir yerde exception fırlatılırsa bu `catch` bloğu yakalar. Handler da dahil — her şey bu behavior'ın içinde çalışır.

**`logger.ForContext("Request", request, destructureObjects: true)`**
Serilog'un yapısal loglama özelliği. `request` nesnesini log'a detaylı olarak yazar. `destructureObjects: true` → nesnenin tüm property'lerini ayrı ayrı loglar.

**`return (TResponse)(object)Result.Failure(error)`**
Bu satır biraz karmaşık görünür. Neden `(TResponse)(object)` var?

`Result.Failure(error)` bir `Result` döndürür. `TResponse` ise `IResult` implement eden herhangi bir tip olabilir (`Result`, `Result<T>` vb.). C# derleyicisi bu dönüşümü doğrudan yapamaz — `object`'e cast edip sonra `TResponse`'a cast ederiz. Kötü görünür ama burada tip güvenliği `where TResponse : IResult` kısıtı sağlar.

---

## 5. `LoggingBehavior` — Her İsteği Logla

```csharp
public sealed class LoggingBehavior<TRequest, TResponse>(ILogger logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
    where TResponse : IResult
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var sw = Stopwatch.StartNew();

        logger
            .ForContext("Request", request, destructureObjects: true)
            .Information("Handling {RequestName}", requestName);

        try
        {
            var response = await next(cancellationToken);
            sw.Stop();

            if (response.IsFailure)
                logger.Warning(
                    "Request {RequestName} failed in {ElapsedMs}ms — {ErrorType}: {ErrorMessage}",
                    requestName,
                    sw.ElapsedMilliseconds,
                    response.Error.Type,
                    response.Error.Message);
            else
                logger.Information(
                    "Request {RequestName} succeeded in {ElapsedMs}ms",
                    requestName,
                    sw.ElapsedMilliseconds);

            return response;
        }
        catch (Exception ex)
        {
            sw.Stop();
            logger.Error(ex,
                "Request {RequestName} threw exception in {ElapsedMs}ms",
                requestName,
                sw.ElapsedMilliseconds);
            throw;
        }
    }
}
```

### Satır Satır Açıklama

**`typeof(TRequest).Name`**
`typeof()` bir tipin `Type` nesnesini döndürür. `.Name` property'si tipin adını string olarak verir. `typeof(KullaniciGetirQuery).Name` → `"KullaniciGetirQuery"`.

**`Stopwatch.StartNew()`**
`System.Diagnostics` namespace'indeki kronometre. `StartNew()` oluşturur ve hemen başlatır. `.ElapsedMilliseconds` → kaç milisaniye geçtiğini verir.

**`sw.Stop()`**
Kronometreyi durdurur. `next()` çağrısından sonra durduruluyor — isteğin toplam süresini ölçüyoruz.

**`response.IsFailure` kontrolü**
Başarısız sonuçlar `Warning` seviyesinde, başarılılar `Information` seviyesinde loglanır. Monitoring araçlarında başarısız istekleri kolayca filtreyebiliriz.

**`throw;`**
Exception'ı tekrar fırlatır. `throw ex;` yerine `throw;` kullanmak önemli — `throw;` orijinal stack trace'i korur, `throw ex;` sıfırlar.

---

## 6. `ValidationBehavior` — FluentValidation Entegrasyonu

```csharp
public sealed class ValidationBehavior<TRequest, TResponse>(
    IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
    where TResponse : IResult
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!validators.Any())
            return await next(cancellationToken);

        var context = new ValidationContext<TRequest>(request);

        var validationResults = await Task.WhenAll(
            validators.Select(v => v.ValidateAsync(context, cancellationToken)));

        var failures = validationResults
            .SelectMany(r => r.Errors)
            .Where(f => f is not null)
            .Select(f => new ValidationError(f.PropertyName, f.ErrorMessage))
            .ToList();

        if (failures.Count == 0)
            return await next(cancellationToken);

        var error = Error.Validation(failures);
        return (TResponse)(object)Result.Failure(error);
    }
}
```

### Satır Satır Açıklama

**`IEnumerable<IValidator<TRequest>> validators`**
DI container'dan tüm `IValidator<TRequest>` implementasyonları inject edilir. `KullaniciOlusturCommand` için `KullaniciOlusturCommandValidator` yazılmışsa otomatik bulunur. Hiç yazılmamışsa boş liste gelir.

**`if (!validators.Any()) return await next(cancellationToken);`**
Validator yoksa doğrulama atlanır, direkt handler'a geçilir. Her request için validator yazmak zorunlu değil.

**`new ValidationContext<TRequest>(request)`**
FluentValidation'ın doğrulama bağlamı. Validator'a "şu nesneyi doğrula" diye verilir.

**`await Task.WhenAll(...)`**
Tüm validator'ları **paralel** çalıştırır. 3 validator varsa sırayla değil, aynı anda çalışır. `Task.WhenAll` hepsinin bitmesini bekler.

**`.SelectMany(r => r.Errors)`**
Her validator'ın hata listesini alıp tek bir düz listeye birleştirir. Birden fazla validator varsa tüm hatalar toplanır.

**`.Select(f => new ValidationError(f.PropertyName, f.ErrorMessage))`**
FluentValidation'ın `ValidationFailure` tipini bizim `ValidationError` tipimize dönüştürür. Infrastructure bağımlılığı domain'e sızmaz.

---

## 7. `TransactionBehavior` — Otomatik Transaction

```csharp
public sealed class TransactionBehavior<TRequest, TResponse>(IUnitOfWork unitOfWork)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
    where TResponse : IResult
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var isCommand = request is ICommand || request is ICommand<TResponse>;

        if (!isCommand)
            return await next(cancellationToken);

        await unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            var response = await next(cancellationToken);

            if (response.IsFailure)
            {
                await unitOfWork.RollbackTransactionAsync(cancellationToken);
                return response;
            }

            await unitOfWork.SaveChangesAsync(cancellationToken);
            await unitOfWork.CommitTransactionAsync(cancellationToken);

            return response;
        }
        catch
        {
            await unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }
}
```

### Satır Satır Açıklama

**`var isCommand = request is ICommand || request is ICommand<TResponse>`**
`is` operatörü tip kontrolü yapar. `request` bir `ICommand` ya da `ICommand<TResponse>` mi? Değilse query'dir — transaction açmaya gerek yok.

**`if (!isCommand) return await next(cancellationToken);`**
Query ise bu behavior şeffaf geçer — transaction açmadan direkt handler'a gider.

**Transaction Akışı:**

```
BeginTransaction  →  Handler çalışır  →  Başarılı mı?
                                              │
                                    ┌─────────┴─────────┐
                                   Evet                 Hayır
                                    │                    │
                              SaveChanges           Rollback
                                    │
                               Commit
```

**Neden handler `SaveChangesAsync` çağırmıyor?**
Handler sadece iş mantığını yazar: "Kullanıcı ekle, sipariş oluştur." `SaveChanges` çağırmak handler'ın sorumluluğu değil. `TransactionBehavior` bunu otomatik yapar. Handler temiz kalır.

---

## 8. `ApplicationServiceExtensions` — DI Kaydı

```csharp
public static class ApplicationServiceExtensions
{
    public static IServiceCollection AddSeamApplication(
        this IServiceCollection services,
        params Assembly[] assemblies)
    {
        services.AddMediatR(cfg =>
        {
            foreach (var assembly in assemblies)
                cfg.RegisterServicesFromAssembly(assembly);
        });

        foreach (var assembly in assemblies)
            services.AddValidatorsFromAssembly(assembly);

        services.AddTransient(typeof(IPipelineBehavior<,>),
            typeof(ExceptionHandlingBehavior<,>));

        services.AddTransient(typeof(IPipelineBehavior<,>),
            typeof(LoggingBehavior<,>));

        services.AddTransient(typeof(IPipelineBehavior<,>),
            typeof(ValidationBehavior<,>));

        services.AddTransient(typeof(IPipelineBehavior<,>),
            typeof(TransactionBehavior<,>));

        return services;
    }
}
```

### Satır Satır Açıklama

**`this IServiceCollection services`**
`this` anahtar kelimesi bu metodun bir **extension method** olduğunu gösterir. Yani `services.AddSeamApplication(...)` diye çağrılabilir — sanki `IServiceCollection`'ın kendi metoduymuş gibi.

**`params Assembly[] assemblies`**
Handler ve validator'ların bulunduğu assembly'ler. Tüketen uygulama kendi assembly'sini verir:
```csharp
builder.Services.AddSeamApplication(Assembly.GetExecutingAssembly());
```

**`cfg.RegisterServicesFromAssembly(assembly)`**
Assembly'deki tüm MediatR handler'larını otomatik bulup DI'a kaydeder. Her handler için ayrı kayıt yazmak gerekmez.

**`services.AddValidatorsFromAssembly(assembly)`**
Assembly'deki tüm FluentValidation validator'larını otomatik bulup DI'a kaydeder.

**`services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ExceptionHandlingBehavior<,>))`**
- `typeof(IPipelineBehavior<,>)` → Open generic tip. `<,>` iki generic parametresi olduğunu gösterir.
- `AddTransient` → Her istek için yeni bir instance oluşturur.
- Kayıt sırası = çalışma sırası. İlk kaydedilen en dışta çalışır.

**Neden `AddTransient`?**
Behavior'lar durum (state) tutmaz. Her istek için yeni bir instance oluşturmak güvenlidir ve hafıza sızıntısı riskini ortadan kaldırır.

---

## 9. Tam Akış — Örnek Senaryo

```
KullaniciOlusturCommand geldi
        │
        ▼
ExceptionHandlingBehavior.Handle() başladı
        │  try {
        ▼
LoggingBehavior.Handle() başladı
        │  Stopwatch başladı, "Handling KullaniciOlusturCommand" loglandı
        ▼
ValidationBehavior.Handle() başladı
        │  KullaniciOlusturCommandValidator çalıştı
        │  Ad boşsa → Result.Failure(Validation) döner, handler çağrılmaz
        │  Geçerliyse →
        ▼
TransactionBehavior.Handle() başladı
        │  ICommand olduğu için BeginTransaction açıldı
        ▼
KullaniciOlusturCommandHandler.Handle() çalıştı
        │  Kullanıcı oluşturuldu, repository.AddAsync() çağrıldı
        │  Result.Success(yeniId) döndü
        ▼
TransactionBehavior: IsSuccess → SaveChanges + Commit
        ▼
LoggingBehavior: "succeeded in 45ms" loglandı
        ▼
ExceptionHandlingBehavior: } // try bitti, exception yok
        ▼
Result<Guid> yanıtı controller'a döndü
```