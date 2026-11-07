# Identity Admin Local Setup

The Store Management console uses a same-origin Next.js BFF. Access and refresh tokens are kept in HttpOnly cookies; the browser never reads them directly.

## Start the API

1. Start SQL Server using the repository's Docker Compose configuration.
2. Apply pending Identity migrations from the repository root:

   ```powershell
   dotnet ef database update --project src/Services/Identity/Identity.Persistence/Identity.Persistence.csproj --startup-project src/Services/Identity/Identity.Api/Identity.Api.csproj --context ApplicationDbContext
   ```

3. Set temporary administrator credentials in the same shell that starts the API. Use a strong password that meets the Identity policy; do not put the password in appsettings or commit it:

   ```powershell
   $env:IdentityBootstrapAdmin__Email = "admin@store.local"
   $env:IdentityBootstrapAdmin__Password = "<temporary-strong-password>"
   dotnet run --project src/Services/Identity/Identity.Api/Identity.Api.csproj --launch-profile http
   ```

If either environment variable is absent, bootstrap is skipped. When configured, the account is confirmed and assigned the protected `Administrator` role at startup. Existing account passwords are not overwritten.

## Start the console

The default Identity API URL is `http://localhost:5168`. If it differs, copy `src/Clients/store-management/.env.local.example` to `.env.local` and change `IDENTITY_API_URL`.

```powershell
Push-Location src/Clients/store-management
bun install
bun dev
Pop-Location
```

Open `http://localhost:3000` and sign in with the bootstrap account.

The Identity admin workspace includes **Security audit** and a scope selector for the active tenant's tenant, organization, and branch scopes. Its audit API requires the `Identity.Security / View` permission (the `Administrator` role is the built-in root role). The pending Identity migrations create `SecurityAuditEvents` and the tenant hierarchy. The tenant migration creates a `Legacy` tenant and backfills existing users, roles, permissions, role assignments, and audit rows into it. These migrations have not been applied automatically; review them and apply them to the intended database with the command above. Audit event records should be treated as append-only security records and are not exposed through update/delete endpoints.

For registration confirmation and password-reset messages, configure the API's `IdentityEmailOptions` SMTP and callback URL environment variables. For local development, the callback URLs should be:

```powershell
$env:IdentityEmailOptions__EmailConfirmationUrl = "http://localhost:3000/confirm-email"
$env:IdentityEmailOptions__PasswordResetUrl = "http://localhost:3000/forgot-password"
```

Configure SMTP host, port, sender, and credentials using `IdentityEmailOptions__...` environment variables as well. Without SMTP configuration, account emails cannot be delivered. The confirmation and recovery pages display bilingual guidance and allow requesting a fresh link.