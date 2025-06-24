using DistributedSystem.Constract.Abstraction.Shared;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DistributedSystem.Presentation.Abstractions
{
    public abstract class ApiEndpoint
    {
        protected static IResult HandlerFailure(Result result) =>
            result switch
            {
                { IsSuccess: true } => throw new InvalidOperationException(),
                IValidationResult validationResult => Results.BadRequest(
                    CreateProblemDetail("Validation Error", StatusCodes.Status400BadRequest, result.Error,
                        validationResult.Errors)),

                _ => Results.BadRequest(
                    CreateProblemDetail("Bad Request", StatusCodes.Status400BadRequest, result.Error))
            };

        //private static ProblemDetails CreateProblemDetail(string title, int status, Error error, IDictionary<string, string[]>? errors = null)
        //=> new()
        //{
        //    Title = title,
        //    Type = error.Code,
        //    Detail = error.Message,
        //    Status = status,
        //    Extensions = { { nameof(errors), errors } }
        //};

        private static ProblemDetails CreateProblemDetail(string title, int status, Error error, Error[]? errors = null)
        => new()
        {
            Title = title,
            Type = error.Code,
            Detail = error.Message,
            Status = status,
            Extensions = { { nameof(errors), errors } }
        };
    }
}
