namespace Seam.Domain.Results;

/// <summary>
/// Tüm Result tiplerinin temel sözleşmesi.
/// Generic ve non-generic Result'lar bu interface üzerinden
/// polimorfik olarak ele alınabilir.
/// </summary>
public interface IResult
{
    bool IsSuccess { get; }
    bool IsFailure { get; }
    Error Error { get; }
}