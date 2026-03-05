namespace Seam.Application.Messaging;

using MediatR;
using Seam.Domain.Results;

/// <summary>
/// Yan etkisi olan, veri döndürmeyen MediatR isteği.
/// TransactionBehavior bu marker'ı taşıyan request'leri sarar.
/// </summary>
public interface ICommand : IRequest<Result>;