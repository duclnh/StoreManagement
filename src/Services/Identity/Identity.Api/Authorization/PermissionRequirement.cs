using Microsoft.AspNetCore.Authorization;

namespace Identity.Api.Authorization
{
    public sealed record PermissionRequirement(string FunctionId, string ActionId) : IAuthorizationRequirement;
}
