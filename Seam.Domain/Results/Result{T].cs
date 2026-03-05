namespace Seam.Domain.Results;

/// <summary>
/// Veri taşıyan işlem sonucunu temsil eder.
/// Başarı durumunda Data dolu, hata durumunda Error dolu gelir.
/// </summary>
/// <typeparam name="TData">Taşınan verinin tipi.</typeparam>
public sealed class Result<TData> : Result, IDataResult<TData>
{
    private readonly TData? _data;

    /// <summary>
    /// Başarı durumunda taşınan veri.
    /// Başarısız durumda erişmek InvalidOperationException fırlatır.
    /// </summary>
    public TData? Data => IsSuccess
        ? _data
        : throw new InvalidOperationException("Başarısız sonuçtan veri okunamaz.");

    internal Result(TData? data, bool isSuccess, Error error)
        : base(isSuccess, error)
    {
        _data = data;
    }
}