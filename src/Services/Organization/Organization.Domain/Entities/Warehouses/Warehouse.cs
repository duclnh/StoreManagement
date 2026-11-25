using Organization.Domain.Abstractions.Entities;
using Organization.Domain.Enumerations;

namespace Organization.Domain.Entities.Warehouses
{
    public sealed class Warehouse : ScopedEntity
    {
        public Guid BranchId { get; set; }
        public WarehouseType WarehouseType { get; set; } = WarehouseType.MAIN;
        public string? Address { get; set; }
        public bool IsDefault { get; set; }
    }
}
