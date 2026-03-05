namespace Seam.Application.Behaviors;

using System.Diagnostics;
using MediatR;
using Serilog;
using Seam.Domain.Results;

/// <summary>
/// Her MediatR isteğinin başlangıç, bitiş ve süresini loglar.
/// Başarısız sonuçlarda hata detaylarını Warning olarak yazar.
/// Serilog structured logging ile request verisi destructure edilir.
/// </summary>
public sealed class LoggingBehavior<TRequest, TResponse>(ILogger logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
    where TResponse : IResult
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var sw = Stopwatch.StartNew();

        logger
            .ForContext("Request", request, destructureObjects: true)
            .Information("Handling {RequestName}", requestName);

        try
        {
            var response = await next(cancellationToken);
            sw.Stop();

            if (response.IsFailure)
            {
                logger.Warning(
                    "Request {RequestName} failed in {ElapsedMs}ms — {ErrorType}: {ErrorMessage}",
                    requestName,
                    sw.ElapsedMilliseconds,
                    response.Error.Type,
                    response.Error.Message);
            }
            else
            {
                logger.Information(
                    "Request {RequestName} succeeded in {ElapsedMs}ms",
                    requestName,
                    sw.ElapsedMilliseconds);
            }

            return response;
        }
        catch (Exception ex)
        {
            sw.Stop();
            logger.Error(ex,
                "Request {RequestName} threw exception in {ElapsedMs}ms",
                requestName,
                sw.ElapsedMilliseconds);
            throw;
        }
    }
}