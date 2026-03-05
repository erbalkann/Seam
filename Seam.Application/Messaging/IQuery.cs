namespace Seam.Application.Messaging;

using MediatR;
using Seam.Domain.Results;

/// <summary>
/// Veri döndüren, yan etkisi olmayan MediatR isteği.
/// TransactionBehavior bu marker'ı taşıyan request'leri atlar.
/// </summary>
/// <typeparam name="TResponse">Sorgu sonuç tipi. Result veya Result&lt;T&gt; olmalıdır.</typeparam>
public interface IQuery<TResponse> : IRequest<TResponse>
    where TResponse : IResult;