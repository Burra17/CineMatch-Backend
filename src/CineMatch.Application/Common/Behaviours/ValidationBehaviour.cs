using ErrorOr;
using FluentValidation;
using MediatR;

namespace CineMatch.Application.Common.Behaviours;

// Runs FluentValidation against the request before the handler. Constrained to IErrorOr responses
// so failures can be returned as Error.Validation values instead of throwing exceptions.
public class ValidationBehaviour<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
    where TResponse : IErrorOr
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehaviour(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!_validators.Any())
        {
            return await next();
        }

        var context = new ValidationContext<TRequest>(request);

        var validationResults = await Task.WhenAll(
            _validators.Select(v => v.ValidateAsync(context, cancellationToken)));

        var errors = validationResults
            .Where(r => r.Errors.Count != 0)
            .SelectMany(r => r.Errors)
            .Select(failure => ErrorOr.Error.Validation(
                code: failure.PropertyName,
                description: failure.ErrorMessage))
            .Distinct()
            .ToList();

        if (errors.Count == 0)
        {
            return await next();
        }

        // ErrorOr<T> has an implicit conversion from List<Error>; the dynamic cast lets us return
        // validation errors through any concrete ErrorOr<T> response type without knowing T at compile time.
        return (TResponse)(dynamic)errors;
    }
}
