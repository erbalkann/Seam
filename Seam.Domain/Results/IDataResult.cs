namespace Seam.Domain.Results;

/// <summary>
/// Veri taşıyan Result tiplerinin sözleşmesi.
/// </summary>
/// <typeparam name="TData">Taşınan verinin tipi.</typeparam>
public interface IDataResult<out TData> : IResult
{
    TData? Data { get; }
}