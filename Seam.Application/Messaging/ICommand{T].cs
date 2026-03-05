namespace Seam.Application.Messaging;

using MediatR;
using Seam.Domain.Results;

/// <summary>
/// Yan etkisi olan, veri döndüren MediatR isteği.
/// TransactionBehavior bu marker'ı taşıyan request'leri sarar.
/// </summary>
/// <typeparam name="TResponse">Komut sonuç tipi. Result&lt;T&gt; olmalıdır.</typeparam>
public interface ICommand<TResponse> : IRequest<TResponse>
    where TResponse : IResult;