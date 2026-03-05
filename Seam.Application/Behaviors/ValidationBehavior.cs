namespace Seam.Application.Behaviors;

using FluentValidation;
using MediatR;
using Seam.Domain.Results;

/// <summary>
/// FluentValidation validator'larını otomatik çalıştırır.
/// DI container'da kayıtlı tüm IValidator&lt;TRequest&gt; implementasyonları
/// inject edilir ve paralel olarak çalıştırılır.
/// Validasyon hatası varsa handler çağrılmaz — Result.Failure döner.
/// Validator kayıtlı değilse behavior şeffaf olarak geçer.
/// </summary>
public sealed class ValidationBehavior<TRequest, TResponse>(
    IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
    where TResponse : IResult
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!validators.Any())
            return await next(cancellationToken);

        var context = new ValidationContext<TRequest>(request);

        var validationResults = await Task.WhenAll(
            validators.Select(v => v.ValidateAsync(context, cancellationToken)));

        var failures = validationResults
            .SelectMany(r => r.Errors)
            .Where(f => f is not null)
            .Select(f => new ValidationError(f.PropertyName, f.ErrorMessage))
            .ToList();

        if (failures.Count == 0)
            return await next(cancellationToken);

        var error = Error.Validation(failures);

        // TResponse Result veya Result<T> garantisi — tip güvenli dönüş.
        return (TResponse)(object)Result.Failure(error);
    }
}