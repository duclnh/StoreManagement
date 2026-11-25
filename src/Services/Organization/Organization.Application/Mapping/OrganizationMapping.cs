using Organization.Contract.Services.V1.Organizations;
using Organization.Domain.Enumerations;
using Entity = global::Organization.Domain.Entities.Organizations.Organization;

namespace Organization.Application.Mapping
{
    internal static class OrganizationMapping
    {
        public static Response.Detail ToResponse(Entity entity) => new(
            entity.Id,
            entity.TenantId,
            entity.Code,
            entity.Name,
            entity.TaxCode,
            entity.BusinessType.ToString(),
            entity.Industry,
            entity.Address,
            entity.Phone,
            entity.Email,
            entity.LogoUrl,
            entity.Currency,
            entity.TimeZone,
            entity.Language,
            entity.Status,
            entity.CreatedAt,
            entity.UpdatedAt);

        public static void Apply(Entity entity, Command.Data data)
        {
            entity.Rename(data.Code, data.Name);
            entity.Status = data.Status;
            entity.TaxCode = data.TaxCode;
            entity.BusinessType = Enum.Parse<BusinessType>(data.BusinessType);
            entity.Industry = data.Industry;
            entity.Address = data.Address;
            entity.Phone = data.Phone;
            entity.Email = data.Email;
            entity.LogoUrl = data.LogoUrl;
            entity.Currency = data.Currency;
            entity.TimeZone = data.TimeZone;
            entity.Language = data.Language;
        }
    }
}
