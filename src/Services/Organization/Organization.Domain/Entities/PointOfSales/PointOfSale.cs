using Organization.Domain.Abstractions.Entities;
using Organization.Domain.Enumerations;

namespace Organization.Domain.Entities.PointOfSales
{
    public sealed class PointOfSale : ScopedEntity
    {
        public Guid BranchId { get; set; }
        public string? Description { get; set; }
    }
}
