namespace Organization.Domain.Abstractions
{
    public interface IOrganizationContext
    {
        Guid? OrganizationId { get; }
        Guid? BranchId { get; }
    }
}
