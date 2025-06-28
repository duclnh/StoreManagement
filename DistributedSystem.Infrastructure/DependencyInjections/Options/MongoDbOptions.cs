namespace DistributedSystem.Infrastructure.DependencyInjections.Options
{
    public interface IMongoDbOptions
    {
        public string DefaultConnectString { get; set; }
        public string DatabaseName { get; set; }
    }

    public class MongoDbOptions : IMongoDbOptions
    {
        public string DefaultConnectString { get; set; } = string.Empty;
        public string DatabaseName { get; set; } = string.Empty;
    }
}
