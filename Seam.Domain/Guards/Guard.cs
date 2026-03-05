namespace Seam.Domain.Guards;

using Seam.Domain.Results;

/// <summary>
/// Giriş doğrulama ve iş kuralı koruma mekanizması.
/// Tüm guard metodları Result döner — exception fırlatmaz.
/// Kullanım amacına göre iki kategoriye ayrılır:
///   1. Null / Empty / WhiteSpace kontrolleri
///   2. Range kontrolleri (Min, Max, Between)
/// </summary>
public static class Guard
{
    // ════════════════════════════════════════════════════════
    // NULL / EMPTY / WHITESPACE KONTROLLERİ
    // ════════════════════════════════════════════════════════

    /// <summary>
    /// Değerin null olmadığını doğrular.
    /// </summary>
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

    /// <summary>
    /// Nullable value type'ın null olmadığını doğrular.
    /// </summary>
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

    /// <summary>
    /// String'in null veya boş olmadığını doğrular.
    /// </summary>
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

    /// <summary>
    /// String'in null, boş veya yalnızca boşluk karakterlerinden
    /// oluşmadığını doğrular.
    /// </summary>
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

    /// <summary>
    /// Koleksiyonun null olmadığını ve en az bir eleman içerdiğini doğrular.
    /// </summary>
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

    // ════════════════════════════════════════════════════════
    // RANGE KONTROLLERİ
    // ════════════════════════════════════════════════════════

    /// <summary>
    /// Değerin belirtilen minimum değerden küçük olmadığını doğrular.
    /// </summary>
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

    /// <summary>
    /// Değerin belirtilen maksimum değeri aşmadığını doğrular.
    /// </summary>
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

    /// <summary>
    /// Değerin belirtilen min ve max aralığında (dahil) olduğunu doğrular.
    /// </summary>
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

    /// <summary>
    /// String uzunluğunun belirtilen minimum değerden küçük olmadığını doğrular.
    /// </summary>
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

    /// <summary>
    /// String uzunluğunun belirtilen maksimum değeri aşmadığını doğrular.
    /// </summary>
    public static Result MaxLength(
        string? value,
        int max,
        string propertyName,
        string? message = null)
    {
        var length = value?.Length ?? 0;
        return length > max
            ? Result.Failure(Error.Validation(
                propertyName,
                message ?? $"{propertyName} must not exceed {max} characters."))
            : Result.Success();
    }

    // ════════════════════════════════════════════════════════
    // AGGREGATE — Birden fazla guard'ı tek seferde çalıştır
    // ════════════════════════════════════════════════════════

    /// <summary>
    /// Birden fazla guard sonucunu toplu olarak değerlendirir.
    /// Tüm başarısız sonuçların ValidationError'larını birleştirir.
    /// Tüm kontrollerden geçerse Success döner.
    /// </summary>
    /// <example>
    /// var result = Guard.Combine(
    ///     Guard.NotNullOrWhiteSpace(name, nameof(name)),
    ///     Guard.MinLength(name, 2, nameof(name)),
    ///     Guard.Between(age, 0, 150, nameof(age))
    /// );
    /// </example>
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
}