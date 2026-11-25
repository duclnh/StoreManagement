using BuildingBlocks.Contracts.Message;

namespace Organization.Contract.Services.V1.Members
{
    public static class Command
    {
        public sealed record Data(Guid UserId, bool IsDefault = false, bool Status = true);
        public sealed record AddOrganizationMember(Guid OrganizationId, Data Data) : ICommand<Response.Detail>;
        public sealed record AssignBranchMember(Guid BranchId, Data Data) : ICommand<Response.Detail>;
        public sealed record RemoveOrganizationMember(Guid OrganizationId, Guid UserId) : ICommand<bool>;
        public sealed record RemoveBranchMember(Guid BranchId, Guid UserId) : ICommand<bool>;
    }
}
