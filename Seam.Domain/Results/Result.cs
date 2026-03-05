namespace Seam.Domain.Results;

/// <summary>
/// Veri taşımayan işlem sonucunu temsil eder.
/// Başarı ya da hata durumunu kapsüller.
/// Doğrudan instantiate edilemez; factory metodları kullanılır.
/// </summary>
public class Result : IResult
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public Error Error { get; }

    protected Result(bool isSuccess, Error error)
    {
        if (isSuccess && error != Error.None)
            throw new InvalidOperationException("Başarılı sonuç hata içeremez.");

        if (!isSuccess && error == Error.None)
            throw new InvalidOperationException("Başarısız sonuç bir hata içermelidir.");

        IsSuccess = isSuccess;
        Error = error;
    }

    // ── Static Factory Methods ────────────────────────────────

    /// <summary>Başarılı, veri taşımayan sonuç üretir.</summary>
    public static Result Success() => new(true, Error.None);

    /// <summary>Başarısız sonuç üretir.</summary>
    public static Result Failure(Error error) => new(false, error);

    /// <summary>Veri taşıyan başarılı sonuç üretir.</summary>
    public static Result<TData> Success<TData>(TData data) => new(data, true, Error.None);

    /// <summary>Veri taşıyan başarısız sonuç üretir.</summary>
    public static Result<TData> Failure<TData>(Error error) => new(default, false, error);
}