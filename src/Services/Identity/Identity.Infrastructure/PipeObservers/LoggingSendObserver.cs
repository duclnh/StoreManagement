using MassTransit;
using Serilog;

namespace Identity.Infrastructure.PipeObservers
{
    public class LoggingSendObserver : ISendObserver
    {
        public async Task PostSend<T>(SendContext<T> context) where T : class
        {
            await Task.Yield();

            Log.Debug("Sended {Message} message from {Namespace}, CorrelationId: {CorrelationId}",
                typeof(T).Name, typeof(T).Namespace, context.CorrelationId);
        }

        public async Task PreSend<T>(SendContext<T> context) where T : class
        {
            await Task.Yield();

            Log.Information("Sending {Message} message from {Namespace}, CorrelationId: {CorrelationId}",
                typeof(T).Name, typeof(T).Namespace, context.CorrelationId);
        }

        public async Task SendFault<T>(SendContext<T> context, Exception exception) where T : class
        {
            await Task.Yield();

            Log.Error("Fault on sending {Message} message from {Namespace}, CorrelationId: {CorrelationId}",
                typeof(T).Name, typeof(T).Namespace, context.CorrelationId);
        }
    }
}

