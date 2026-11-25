using BuildingBlocks.Contracts.Message;

namespace Organization.Contract.Services.V1.Branches
{
    public static class Command
    {
        public sealed record Data(
            string Code,
            string Name,
            string? Address,
            string? Phone,
            string? Email,
            Guid? ManagerUserId,
            bool IsDefault,
            bool Status = true);

        public sealed record CreateBranch(Guid OrganizationId, Data Data) : ICommand<Response.Detail>;
        public sealed record UpdateBranch(Guid Id, Data Data) : ICommand<Response.Detail>;
        public sealed record SetBranchStatus(Guid Id, bool Status) : ICommand<Response.Detail>;
    }
}
