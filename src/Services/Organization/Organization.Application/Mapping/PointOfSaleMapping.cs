using Organization.Contract.Services.V1.PointOfSales;
using Organization.Domain.Enumerations;
using Entity = global::Organization.Domain.Entities.PointOfSales.PointOfSale;

namespace Organization.Application.Mapping
{
    internal static class PointOfSaleMapping
    {
        public static Response.Detail ToResponse(Entity entity) => new(
            entity.Id,
            entity.TenantId,
            entity.BranchId,
            entity.Code,
            entity.Name,
            entity.Description,
            entity.Status,
            entity.CreatedAt,
            entity.UpdatedAt);

        public static void Apply(Entity entity, Command.Data data)
        {
            entity.Rename(data.Code, data.Name);
            entity.Status = data.Status;
            entity.Description = data.Description;
        }
    }
}
