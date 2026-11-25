using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Organization.Api.Middleware
{
    public sealed class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;

        public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
            {
                context.Response.StatusCode = 499;
            }
            catch (DbUpdateException exception) when (exception.InnerException is SqlException { Number: 2601 or 2627 })
            {
                await Results.Problem(statusCode: 409, title: "Organization.Conflict", detail: "Code, membership or default already exists in this scope.").ExecuteAsync(context);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Organization request failed");
                await Results.Problem(statusCode: exception is HttpRequestException or OperationCanceledException ? 503 : 500,
                    title: "Organization request could not be completed.").ExecuteAsync(context);
            }
        }
    }
}
