using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace DistributedSystem.Infrastructure.Consumers
{
    public abstract class Document : IDocument
    {
        public ObjectId Id { get; set; }

        [BsonRepresentation(BsonType.String)]
        public Guid DocumentId { get; set; }  // Id message

        public DateTimeOffset CreatedOnUtc => Id.CreationTime;

        public DateTimeOffset? ModifiedOnUtc { get; set; }
    }
}