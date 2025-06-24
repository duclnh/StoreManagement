using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DistributedSystem.Persistence.Constants
{
    internal static class TableNames
    {

        // ***************Plural*************************
        internal const string Actions = nameof(Actions);
        internal const string Functions = nameof(Functions);
        internal const string ActionInFunctions = nameof(ActionInFunctions);
        internal const string Permissions = nameof(Permissions);

        internal const string AppUsers = nameof(AppUsers);
        internal const string AppRoles = nameof(AppRoles);
        internal const string AppUserRoles = nameof(AppUserRoles);

        internal const string AppUserClaims = nameof(AppUserClaims);
        internal const string AppRoleClaims = nameof(AppRoleClaims);
        internal const string AppUserLogins = nameof(AppUserLogins);
        internal const string AppUserTokens = nameof(AppUserTokens);

        // Outbox pattern
        internal const string OutboxMessages= nameof(OutboxMessages);

        // ************Single***************************
        internal const string Product = nameof(Product);

    }
}
