using MassTransit;
using Serilog;

namespace Identity.Infrastructure.PipeObservers
{
    public class LoggingPublishObserver : IPublishObserver
    {
        public async Task PostPublish<T>(PublishContext<T> context) where T : class
        {
            await Task.Yield();

            Log.Debug("Publishing {Message} event from {Namespace}, CorrelationId: {CorrelationId}",
                typeof(T).Name, typeof(T).Namespace, context.CorrelationId);
        }

        public async Task PrePublish<T>(PublishContext<T> context) where T : class
        {
            await Task.Yield();

            Log.Information("Publishing {Message} event from {Namespace}, CorrelationId: {CorrelationId}",
                typeof(T).Name, typeof(T).Namespace, context.CorrelationId);
        }

        public async Task PublishFault<T>(PublishContext<T> context, Exception exception) where T : class
        {
            await Task.Yield();

            Log.Error("Fault on publishing {Message} from {Namespace}, Error: {Error} ,CorrelationId: {CorrelationId}",
                typeof(T).Name, typeof(T).Namespace, exception.Message,context.CorrelationId);
        }
    }
}

