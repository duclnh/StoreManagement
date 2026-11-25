using BuildingBlocks.Contracts.Message;

namespace Identity.Contract.Services.V1.Identity
{
    public sealed record UserBelongsToTenantQuery(Guid UserId, Guid TenantId) : IQuery<bool>;
}
