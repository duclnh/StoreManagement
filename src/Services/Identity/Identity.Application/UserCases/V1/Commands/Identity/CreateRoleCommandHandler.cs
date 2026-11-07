using Identity.Contract.Abstraction.Message;
using Identity.Contract.Abstraction.Shared;
using Identity.Domain.Entities.Identity;
using Microsoft.AspNetCore.Identity;
using static Identity.Contract.Services.V1.Identity.Command;

namespace Identity.Application.UserCases.V1.Commands.Identity
{
    public sealed class CreateRoleCommandHandler : ICommandHandler<CreateRole, Guid>
    {
        private readonly RoleManager<AppRole> _roleManager;

        public CreateRoleCommandHandler(RoleManager<AppRole> roleManager)
        {
            _roleManager = roleManager;
        }

        public async Task<Result<Guid>> Handle(CreateRole request, CancellationToken cancellationToken)
        {
            if (await _roleManager.RoleExistsAsync(request.Name))
                return Result.Failure<Guid>(new Error("Identity.Role.AlreadyExists", "A role with this name already exists."));

            var role = new AppRole
            {
                Name = request.Name.Trim(),
                RoleCode = request.RoleCode.Trim(),
                Description = request.Description.Trim()
            };

            var result = await _roleManager.CreateAsync(role);
            return result.Succeeded
                ? Result.Success(role.Id)
                : Result.Failure<Guid>(new Error("Identity.Role.CreateFailed", string.Join("; ", result.Errors.Select(error => error.Description))));
        }
    }
}