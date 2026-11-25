using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Organization.Application.Behaviors
{
    public sealed class PerformancePipelineBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : notnull
    {
        private readonly ILogger<PerformancePipelineBehavior<TRequest, TResponse>> _logger;

        public PerformancePipelineBehavior(ILogger<PerformancePipelineBehavior<TRequest, TResponse>> logger)
        {
            _logger = logger;
        }

        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            var start = Stopwatch.GetTimestamp();
            try
            {
                return await next();
            }
            finally
            {
                _logger.LogDebug("Request {RequestType} completed in {ElapsedMilliseconds} ms",
                    typeof(TRequest).Name, Stopwatch.GetElapsedTime(start).TotalMilliseconds);
            }
        }
    }
}
