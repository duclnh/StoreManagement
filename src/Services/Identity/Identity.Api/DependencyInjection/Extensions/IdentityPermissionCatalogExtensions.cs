using Identity.Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore;

namespace Identity.Api.DependencyInjection.Extensions
{
    public static class IdentityPermissionCatalogExtensions
    {
        private static readonly (string Id, string Name, string Url, string[] Actions)[] Catalog =
        [
            ("Identity.Users", "Users", "/users", ["View", "Create", "Update", "Assign"]),
            ("Identity.Roles", "Roles", "/roles", ["View", "Create", "Update", "Delete"]),
            ("Identity.Sessions", "Sessions", "/sessions", ["View", "Revoke"]),
            ("Identity.Security", "Security", "/security", ["View", "Update"])
        ];

        public static async Task SeedIdentityPermissionCatalogAsync(this WebApplication app)
        {
            await using var scope = app.Services.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<DbContext>();
            var actions = Catalog.SelectMany(item => item.Actions).Distinct(StringComparer.Ordinal);
            var existingFunctions = await dbContext.Set<Function>()
                .Select(function => function.Id)
                .ToListAsync();
            var existingActions = await dbContext.Set<Identity.Domain.Entities.Identity.Action>()
                .Select(action => action.Id)
                .ToListAsync();
            var existingPairs = await dbContext.Set<ActionInFunction>()
                .Select(pair => new { pair.FunctionId, pair.ActionId })
                .ToListAsync();
            var pairSet = existingPairs.Select(pair => (pair.FunctionId, pair.ActionId)).ToHashSet();

            dbContext.Set<Function>().AddRange(Catalog
                .Where(item => !existingFunctions.Contains(item.Id, StringComparer.Ordinal))
                .Select(item => new Function
                {
                    Id = item.Id,
                    Name = item.Name,
                    Url = item.Url,
                    ParrentId = string.Empty,
                    CssClasses = string.Empty,
                    IsActive = true
                }));
            dbContext.Set<Identity.Domain.Entities.Identity.Action>().AddRange(actions
                .Where(action => !existingActions.Contains(action, StringComparer.Ordinal))
                .Select(action => new Identity.Domain.Entities.Identity.Action
                {
                    Id = action,
                    Name = action,
                    IsActive = true
                }));
            dbContext.Set<ActionInFunction>().AddRange(Catalog
                .SelectMany(item => item.Actions.Select(action => (item.Id, ActionId: action)))
                .Where(pair => !pairSet.Contains(pair))
                .Select(pair => new ActionInFunction
                {
                    FunctionId = pair.Id,
                    ActionId = pair.ActionId
                }));

            await dbContext.SaveChangesAsync();
        }
    }
}
