using Identity.Contract.Abstraction.Shared;
using FluentValidation;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Identity.Application.Behaviors
{
    public class ValidationPipelineBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
     : IPipelineBehavior<TRequest, TResponse?>
     where TRequest : IRequest<TResponse?>
     where TResponse : Result
    {
        public async Task<TResponse?> Handle(TRequest request, RequestHandlerDelegate<TResponse?> next,
            CancellationToken cancellationToken)
        {
            if (!validators.Any())
            {
                return await next();
            }

            //IDictionary<string, string[]> errors = validators
            //    .Select(validator => validator.Validate(request))
            //    .SelectMany(validationResult => validationResult.Errors)
            //    .Where(validationFailure => validationFailure is not null)
            //    .GroupBy(failure => failure.PropertyName)
            //    .ToDictionary(
            //        group => group.Key, // Use the property name as the key
            //        group => group.Select(failure => failure.ErrorMessage).Distinct().ToArray() // Collect unique error messages for each property
            //    );

            Error[] errors = validators
                .Select(validator => validator.Validate(request))
                .SelectMany(validationResult => validationResult.Errors)
                .Where(validationFailure => validationFailure is not null)
                .Select(failure =>
                new Error(
                        failure.PropertyName,
                    failure.ErrorMessage
                ))
                .Distinct()
                .ToArray();

            if (errors.Any())
                return CreateValidationResult<TResponse>(errors);

            return await next();
        }

        //private static TResult? CreateValidationResult<TResult>(IDictionary<string, string[]> errors) where TResult : Result
        //{
        //    if (typeof(TResult) == typeof(Result))
        //    {
        //        return (ValidationResult.WithErrors(errors) as TResult);
        //    }

        //    object validationResult = typeof(ValidationResult<>)
        //        .GetGenericTypeDefinition()
        //        .MakeGenericType(typeof(TResult).GenericTypeArguments[0])
        //        .GetMethod(nameof(ValidationResult.WithErrors))!
        //        .Invoke(null, new object?[] { errors })!;

        //    return (TResult)validationResult;
        //}


        private static TResult? CreateValidationResult<TResult>(Error[] errors) where TResult : Result
        {
            if (typeof(TResult) == typeof(Result))
            {
                return (ValidationResult.WithErrors(errors) as TResult);
            }

            object validationResult = typeof(ValidationResult<>)
                .GetGenericTypeDefinition()
                .MakeGenericType(typeof(TResult).GenericTypeArguments[0])
                .GetMethod(nameof(ValidationResult.WithErrors))!
                .Invoke(null, new object?[] { errors })!;

            return (TResult)validationResult;
        }
    }
}


