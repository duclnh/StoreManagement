using BuildingBlocks.Contracts.Shared;

namespace Organization.Api.Abstractions
{
    public abstract class ApiEndpoint
    {
        protected static IResult Failure(Result result)
        {
            if (result is IValidationResult validation)
            {
                return Results.ValidationProblem(validation.Errors.GroupBy(x => x.Code)
                    .ToDictionary(group => group.Key, group => group.Select(x => x.Message).ToArray()));
            }

            var status = result.Error.Code switch
            {
                "Organization.Forbidden" => 403,
                "Organization.NotFound" => 404,
                "Organization.Conflict" => 409,
                _ => 400
            };
            return Results.Problem(statusCode: status, title: result.Error.Code, detail: result.Error.Message);
        }

        protected static IResult Map<T>(Result<T> result) => result.IsFailure ? Failure(result) : Results.Ok(result.Value);
    }
}
