namespace Seam.Domain.Results;

/// <summary>
/// Hata bilgisini taşıyan value object.
/// Result sınıfları içinde hata durumunu temsil eder.
/// Immutable ve karşılaştırılabilir (record) yapıdadır.
/// </summary>
public sealed record Error
{
    /// <summary>Hata kategorisi.</summary>
    public ErrorType Type { get; }

    /// <summary>Genel hata mesajı.</summary>
    public string Message { get; }

    /// <summary>
    /// Validasyon hatalarının listesi.
    /// Sadece ErrorType.Validation durumunda dolu olabilir.
    /// </summary>
    public IReadOnlyList<ValidationError> ValidationErrors { get; }

    private Error(ErrorType type, string message, IReadOnlyList<ValidationError>? validationErrors = null)
    {
        Type = type;
        Message = message;
        ValidationErrors = validationErrors ?? [];
    }

    // ── Static Factory Methods ────────────────────────────────

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
        => new(ErrorType.Validation, "One or more validation errors occurred.", validationErrors);

    public static Error Validation(string propertyName, string message)
        => new(ErrorType.Validation, "One or more validation errors occurred.",
            [new ValidationError(propertyName, message)]);

    public static Error InternalError(string message = "An unexpected error occurred.")
        => new(ErrorType.InternalError, message);

    // ── Önceden tanımlı yaygın hatalar ───────────────────────

    public static readonly Error None = new(ErrorType.InternalError, string.Empty);
}