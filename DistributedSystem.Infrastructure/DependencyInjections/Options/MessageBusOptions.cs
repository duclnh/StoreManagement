namespace DistributedSystem.Infrastructure.DependencyInjections.Options
{
    public class MessageBusOptions
    {
        public int RetryLimit { get; set; }
        public TimeSpan InitialInterval { get; set; }
        public TimeSpan IntervalIncrement { get; set; }
    }
}
