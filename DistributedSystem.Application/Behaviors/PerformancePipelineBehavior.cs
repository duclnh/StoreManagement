using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DistributedSystem.Application.Behaviors
{
    public class PerformancePipelineBehavior<TRequest, TResponse> : 
        IPipelineBehavior<TRequest,TResponse>
        where TRequest : IRequest<TResponse>
    {
        private readonly ILogger<TRequest> _logger;
        private readonly Stopwatch _timer;
        public PerformancePipelineBehavior(ILogger<TRequest> logger)
        {
            _logger = logger;
            _timer = new Stopwatch();
        }

        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            _timer.Start();
            var response = await next();
            _timer.Stop();

            var elapseMiliseconds = _timer.ElapsedMilliseconds;

            if(elapseMiliseconds <= 5000)
                return response;

            var requestName = typeof(TRequest).Name;
            _logger.LogWarning("Long Time Running - Request Details: {Name} ({ElapseMiliseconds}) miliseconds {@Request}", requestName, elapseMiliseconds, request);

            return response;
        }
    }
}
