namespace Seam.Application.Behaviors;

using MediatR;
using Seam.Application.Messaging;
using Seam.Application.Persistence;
using Seam.Domain.Results;

/// <summary>
/// Sadece ICommand ve ICommand&lt;T&gt; marker interface'ini taşıyan
/// request'leri UnitOfWork transaction ile sarar.
/// IQuery request'leri bu behavior tarafından işlenmez — şeffaf geçer.
///
/// Başarı  → SaveChangesAsync + CommitTransaction
/// Hata    → RollbackTransaction (SaveChanges çağrılmaz)
/// </summary>
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
        // Sadece command'lar transaction ile sarılır.
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