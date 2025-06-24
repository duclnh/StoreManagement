namespace DistributedSystem.Domain.Abstractions.Entities
{
    public interface IAuditTableEntity
    {
        DateTimeOffset CreatedOnUtc { get; set; }
        DateTimeOffset? UpdatedOnUtc { get; set; }
    }
}
