namespace Organization.Contract.Services.V1.Members
{
    public static class Response
    {
        public sealed record Detail(Guid Id, Guid OrganizationId, Guid UserId, Guid? BranchId, bool IsDefault, bool Status);
    }
}
