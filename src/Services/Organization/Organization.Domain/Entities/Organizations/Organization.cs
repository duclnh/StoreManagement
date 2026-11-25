using Organization.Domain.Abstractions.Entities;
using Organization.Domain.Enumerations;

namespace Organization.Domain.Entities.Organizations
{
    public sealed class Organization : ScopedEntity
    {
        public string? TaxCode { get; set; }
        public BusinessType BusinessType { get; set; } = BusinessType.GENERAL_RETAIL;
        public string? Industry { get; set; }
        public string? Address { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? LogoUrl { get; set; }
        public string Currency { get; set; } = "VND";
        public string TimeZone { get; set; } = "Asia/Ho_Chi_Minh";
        public string Language { get; set; } = "vi";
    }
}
