using Identity.Domain.Entities.Identity;
using Microsoft.AspNetCore.Identity;

namespace Identity.Api.DependencyInjection.Extensions
{
    public static class IdentityBootstrapper
    {
        private const string AdministratorRole = "Administrator";

        public static async Task SeedAdministratorFromEnvironmentAsync(this WebApplication app)
        {
            var email = app.Configuration["IdentityBootstrapAdmin:Email"];
            var password = app.Configuration["IdentityBootstrapAdmin:Password"];
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                app.Logger.LogInformation("Administrator bootstrap skipped because credentials are not configured.");
                return;
            }

            await using var scope = app.Services.CreateAsyncScope();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<AppRole>>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();

            var role = await roleManager.FindByNameAsync(AdministratorRole);
            if (role is null)
            {
                var createRole = await roleManager.CreateAsync(new AppRole
                {
                    TenantId = TenantDefaults.LegacyTenantId,
                    Name = AdministratorRole,
                    RoleCode = "ADMIN",
                    Description = "Identity administration"
                });
                if (!createRole.Succeeded)
                    throw new InvalidOperationException($"Could not create the administrator role: {string.Join("; ", createRole.Errors.Select(error => error.Description))}");
            }

            var user = await userManager.FindByEmailAsync(email);
            if (user is null)
            {
                user = new AppUser
                {
                    UserName = email,
                    Email = email,
                    TenantId = TenantDefaults.LegacyTenantId,
                    EmailConfirmed = true,
                    IsActive = true,
                    FirstName = "System",
                    LastName = "Administrator",
                    FullName = "System Administrator"
                };

                var createUser = await userManager.CreateAsync(user, password);
                if (!createUser.Succeeded)
                    throw new InvalidOperationException($"Could not create the bootstrap administrator: {string.Join("; ", createUser.Errors.Select(error => error.Description))}");
            }

            if (!await userManager.IsInRoleAsync(user, AdministratorRole))
            {
                var addRole = await userManager.AddToRoleAsync(user, AdministratorRole);
                if (!addRole.Succeeded)
                    throw new InvalidOperationException($"Could not assign the administrator role: {string.Join("; ", addRole.Errors.Select(error => error.Description))}");
            }
        }
    }
}