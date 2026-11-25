using Organization.Contract.Services.V1.Warehouses;
using Organization.Domain.Enumerations;
using Entity = global::Organization.Domain.Entities.Warehouses.Warehouse;

namespace Organization.Application.Mapping
{
    internal static class WarehouseMapping
    {
        public static Response.Detail ToResponse(Entity entity) => new(
            entity.Id,
            entity.TenantId,
            entity.BranchId,
            entity.Code,
            entity.Name,
            entity.WarehouseType.ToString(),
            entity.Address,
            entity.IsDefault,
            entity.Status,
            entity.CreatedAt,
            entity.UpdatedAt);

        public static void Apply(Entity entity, Command.Data data)
        {
            entity.Rename(data.Code, data.Name);
            entity.Status = data.Status;
            entity.WarehouseType = Enum.Parse<WarehouseType>(data.WarehouseType);
            entity.Address = data.Address;
            entity.IsDefault = data.IsDefault;
        }
    }
}
