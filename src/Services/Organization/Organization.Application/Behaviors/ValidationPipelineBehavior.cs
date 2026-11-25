using System.Reflection;
using BuildingBlocks.Contracts.Shared;
using FluentValidation;
using MediatR;

namespace Organization.Application.Behaviors
{
    public sealed class ValidationPipelineBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : notnull
    {
        private readonly IEnumerable<IValidator<TRequest>> _validators;

        public ValidationPipelineBehavior(IEnumerable<IValidator<TRequest>> validators)
        {
            _validators = validators;
        }

        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            var errors = new List<Error>();
            foreach (var validator in _validators)
            {
                var validation = await validator.ValidateAsync(request, cancellationToken);
                errors.AddRange(validation.Errors.Select(error => new Error(error.PropertyName, error.ErrorMessage)));
            }

            if (errors.Count == 0)
            {
                return await next();
            }

            if (typeof(TResponse) == typeof(Result))
            {
                return (TResponse)(object)ValidationResult.WithErrors(errors.ToArray());
            }

            var resultType = typeof(ValidationResult<>).MakeGenericType(typeof(TResponse).GetGenericArguments()[0]);
            return (TResponse)resultType.GetMethod("WithErrors", BindingFlags.Public | BindingFlags.Static)!
                .Invoke(null, new object[] { errors.ToArray() })!;
        }
    }
}
