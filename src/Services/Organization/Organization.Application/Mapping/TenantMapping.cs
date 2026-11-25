using Organization.Contract.Services.V1.Tenants;
using Organization.Domain.Enumerations;
using Entity = global::Organization.Domain.Entities.Tenants.Tenant;

namespace Organization.Application.Mapping
{
    internal static class TenantMapping
    {
        public static Response.Detail ToResponse(Entity entity) => new(
            entity.Id,
            entity.TenantId,
            entity.Code,
            entity.Name,
            entity.ContactEmail,
            entity.ContactPhone,
            entity.ConfigurationJson,
            entity.Status,
            entity.CreatedAt,
            entity.UpdatedAt);

        public static void Apply(Entity entity, Command.Data data)
        {
            entity.Rename(data.Code, data.Name);
            entity.Status = data.Status;
            entity.ContactEmail = data.ContactEmail;
            entity.ContactPhone = data.ContactPhone;
            entity.ConfigurationJson = data.ConfigurationJson;
        }
    }
}
