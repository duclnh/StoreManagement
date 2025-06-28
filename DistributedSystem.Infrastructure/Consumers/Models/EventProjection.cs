using DistributedSystem.Infrastructure.Consumers.Attributes;
using DistributedSystem.Infrastructure.Consumers.Constants;

namespace DistributedSystem.Infrastructure.Consumers.Models
{
    [BsonCollection(TableNames.Event)]
    public class EventProjection : Document
    {
        public Guid EventId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
    }
}