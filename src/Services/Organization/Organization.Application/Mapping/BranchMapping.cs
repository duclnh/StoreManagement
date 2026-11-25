using Organization.Contract.Services.V1.Branches;
using Organization.Domain.Enumerations;
using Entity = global::Organization.Domain.Entities.Branches.Branch;

namespace Organization.Application.Mapping
{
    internal static class BranchMapping
    {
        public static Response.Detail ToResponse(Entity entity) => new(
            entity.Id,
            entity.TenantId,
            entity.OrganizationId,
            entity.Code,
            entity.Name,
            entity.Address,
            entity.Phone,
            entity.Email,
            entity.ManagerUserId,
            entity.IsDefault,
            entity.Status,
            entity.CreatedAt,
            entity.UpdatedAt);

        public static void Apply(Entity entity, Command.Data data)
        {
            entity.Rename(data.Code, data.Name);
            entity.Status = data.Status;
            entity.Address = data.Address;
            entity.Phone = data.Phone;
            entity.Email = data.Email;
            entity.ManagerUserId = data.ManagerUserId;
            entity.IsDefault = data.IsDefault;
        }
    }
}
