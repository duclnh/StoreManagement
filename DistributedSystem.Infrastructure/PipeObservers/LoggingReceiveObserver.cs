using MassTransit;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DistributedSystem.Infrastructure.PipeObservers
{
    public class LoggingReceiveObserver : IReceiveObserver
    {
        private const string ExchangeKey = "RabbitMQ-ExchangeName";

        public async Task ConsumeFault<T>(ConsumeContext<T> context, TimeSpan duration, string consumerType, Exception exception) where T : class
        {
            await Task.Yield();

            Log.Error("Fault on consuming {Message} from {Namespace}, Duration: {Duration}, ConsumerType: {ConsumerType}, CorrelationId: {CorrelationId}",
                typeof(T).Name,
                typeof(T).Namespace,
                duration,
                consumerType,
                context.CorrelationId);
        }

        public async Task PostConsume<T>(ConsumeContext<T> context, TimeSpan duration, string consumerType) where T : class
        {
            await Task.Yield();

            Log.Debug("Consuming {Message} from {Namespace}, Duration: {Duration}, ConsumerType: {ConsumerType}, CorrelationId: {CorrelationId}",
             typeof(T).Name,
             typeof(T).Namespace,
             duration,
             consumerType,
             context.CorrelationId);
        }

        public async Task PostReceive(ReceiveContext context)
        {
            await Task.Yield();

            Log.Debug("Message was receive from exchange {Exchange}, Redelivered: {Redelivered}, CorrelationId: {CorrelationId}",
                context.TransportHeaders.Get<string>(ExchangeKey), context.Redelivered, context.GetCorrelationId());
        }

        public async Task PreReceive(ReceiveContext context)
        {
            await Task.Yield();

            Log.Debug("Receiving message from exchange  {Exchange}, Redelivered: {Redelivered}, CorrelationId: {CorrelationId}",
                context.TransportHeaders.Get<string>(ExchangeKey), context.Redelivered, context.GetCorrelationId());
        }

        public async Task ReceiveFault(ReceiveContext context, Exception exception)
        {
            await Task.Yield();

            Log.Error("Fault on receive from exchange {Exchange}, Redelivered: {Redelivered}, CorrelationId: {CorrelationId}",
                context.TransportHeaders.Get<string>(ExchangeKey), context.Redelivered, context.GetCorrelationId());
        }
    }
}
