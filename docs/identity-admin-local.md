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
   $env:IdentityBootstrapAdmin__Email = "admin@example.com"
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

For registration confirmation and password-reset messages, configure the API's `IdentityEmailOptions` SMTP and callback URL environment variables. Without SMTP configuration, account emails cannot be delivered.