# Identity and Security Feature Plan

## Status

### Available backend contracts with frontend coverage

- **Authentication:** ASP.NET Core Identity registration and password hashing/verification, login, logout, JWT access tokens, rotating refresh tokens, password change/reset, email confirmation, 2FA, and lockout after three failed attempts for two minutes.
- **Account recovery:** public registration, confirmation-link request/verification, and password-reset request/completion are available through the same-origin frontend BFF.
- **Session management:** the API records a server-generated session ID, user-agent string, IP address, creation/last-active/expiry timestamps, and current-session status. The UI lists active sessions, revokes one other session, or revokes all other sessions.
- **User management:** administrators can create, list/search/page, edit, activate/deactivate, and assign roles to accounts. Deactivation and password change revoke refresh sessions.
- **Role management:** administrators can list, create, update, and delete roles, subject to backend protections.
- **Role permissions:** a seeded Identity permission catalog drives binary role grants. The admin UI can read/update each role's grants; protected user/role endpoints evaluate `FunctionId + ActionId` and deny when no grant exists. Administrator is the root role. Permission editing and changes to Administrator membership are restricted to administrators.
- **Security audit:** authentication success/failure, logout/session revocation, password changes/resets, email confirmation, 2FA changes, user/role changes, and permission changes are appended to `SecurityAuditEvents`. The administrator console offers a paginated, event/outcome-filterable view with actor, subject, UTC time, IP, session, and safe detail codes.
- **Tenant/organization/branch scope:** users and roles are tenant-bound; role assignments and permission grants can be stored at tenant, organization, or branch scope and inherited down the hierarchy. User administration, role administration, authorization checks, and audit queries are tenant-filtered. The admin console includes a scope selector, and scoped user-role assignment and role-permission endpoints use the selected scope.
- **Frontend UX:** English/Vietnamese copy, validation, localized session timestamps, informational browser/OS parsing, password-reset and email-confirmation callback screens, and a forced sign-in after password change.

User-agent browser/OS labels are display hints only. The server-generated session ID is the session identifier; a browser-provided user-agent is not trusted as a device identity.

### Backend gaps before the full requested scope is complete

- **Permission verification:** the permission API, catalog seed, authorization handler, and role matrix UI are implemented, but automated tests and end-to-end verification against a live Identity database/API are still needed. Role administration remains protected by explicit per-action requirements; permission editing is Administrator-only. Managed account creation cannot assign the Administrator role, and only an Administrator can add/remove that role later.
- **Tenant/organization/branch scope:** scope creation and tenant onboarding endpoints are not implemented yet; the scope selector lists the active tenant and its existing organizations/branches. The tenant foundation migration is generated and backfills existing users, roles, permissions, role assignments, and audit rows into a `Legacy` tenant, but has not been applied to a database. Cross-tenant isolation still needs automated and live API/database tests. Role names remain globally unique under the current ASP.NET Identity role store.
- **Security audit verification/operations:** audit events are implemented with an append-only application model and a generated SQL Server migration. Apply the migration before using the endpoint/UI, then add automated tests and live DB/API verification. Retention/archival and broader anomaly detection remain to be designed; failure events other than sign-in are not yet comprehensive.
- **Session policy and lifecycle:** there is no configured simultaneous-device limit. Lockout, deactivation, and password change revoke the user's stored sessions; JWT middleware checks session status on every authenticated request, so revocation also denies already-issued access JWTs.
- **Authentication feedback:** lockout is enforced by Identity, while login deliberately returns a generic invalid-credentials response; the UI cannot distinguish a lockout from a wrong password without a safe backend contract.

## Delivery plan

1. **Finish FE for existing Identity APIs — implementation complete**
   - Provide self-registration, confirmation and resend, password recovery, 2FA/security controls, account administration, role management, and session operations.
   - Keep recovery responses non-enumerating and never treat user-agent data as trusted identity.
   - Verified lint, production build, and localization parity. Runtime registration/email delivery still depends on running the API and configuring SMTP/callback URLs.
2. **Add backend permission contracts — implementation present, validation incomplete**
   - Define stable permission identifiers and binary grant semantics.
   - Add role-permission read/write endpoints and request validation.
   - Add authorization handlers/policies that enforce permissions at endpoint and business-operation boundaries.
   - Add tests for denied-by-default behavior, grant, revoke, Administrator bypass, and privileged-role assignment safeguards.
3. **Add tenant and organizational scope — foundation implemented**
   - Tenant, organization, and branch ownership, hierarchical scope validation, and inheritance are implemented.
   - Scoped user-role assignments and permission evaluation are implemented; tenant claims are issued on sign-in and refresh.
   - Tenant-scoped admin data, scope listing, and FE scope selection are implemented.
   - Next: tenant/organization/branch create/update/disable APIs, tenant onboarding and cross-tenant admin UX, plus automated isolation tests.
4. **Add security audit events — implementation present, validation incomplete**
   - Persist append-only events for authentication, logout/revocation, password/security changes, account status, role, and permission changes.
   - Capture actor/subject, UTC time, IP, session, action, result, and safe metadata.
   - Add an authorized, paginated/filterable audit API and admin UI.
   - Apply the migration and add tests for event recording, filtering, and authorization.
5. **Add session policy and operational hardening**
   - Decide and configure the concurrent-session limit.
   - Add basic anomaly signals and tests without exposing sensitive details to unauthenticated callers.

## Acceptance criteria

- A new account can register, receive an email confirmation, confirm it, and sign in; invalid or expired links can be re-requested.
- Password reset is usable end-to-end and does not reveal whether an email is registered.
- 2FA can be enabled, used for sign-in, and disabled; changing a password requires a fresh sign-in because sessions are revoked.
- Session rows show current status, informational browser/OS, IP, and localized timestamps; remote revocation updates the list.
- User and role administration are backed by existing authorized API endpoints, with loading/error/empty states.
- Permission enforcement, scope isolation, audit history, and session policies have automated backend tests before corresponding UI controls are exposed.
