namespace Seam.Application.Behaviors;

using MediatR;
using Serilog;
using Seam.Domain.Results;

/// <summary>
/// Pipeline'ın en dış katmanı — tüm behavior'ların üstünde konumlanır.
/// Handler veya diğer behavior'lardan fırlayan beklenmedik exception'ları
/// yakalar ve Result.Failure(Error.InternalError) olarak döner.
/// Böylece exception hiçbir zaman üst katmanlara (API controller vb.) sızmaz.
/// </summary>
public sealed class ExceptionHandlingBehavior<TRequest, TResponse>(ILogger logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
    where TResponse : IResult
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        try
        {
            return await next(cancellationToken);
        }
        catch (Exception ex)
        {
            logger
                .ForContext("Request", request, destructureObjects: true)
                .Error(ex,
                    "Unhandled exception for {RequestType}",
                    typeof(TRequest).Name);

            var error = Error.InternalError(ex.Message);

            // TResponse'un Result veya Result<T> olduğu garantilidir.
            // Result.Failure<TResponse> ile tip güvenli dönüş sağlanır.
            return (TResponse)(object)Result.Failure(error);
        }
    }
}