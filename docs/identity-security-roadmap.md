# Identity and Security Feature Plan

## Status

### Available backend contracts with frontend coverage

- **Authentication:** ASP.NET Core Identity registration and password hashing/verification, login, logout, JWT access tokens, rotating refresh tokens, password change/reset, email confirmation, 2FA, and lockout after three failed attempts for two minutes.
- **Account recovery:** public registration, confirmation-link request/verification, and password-reset request/completion are available through the same-origin frontend BFF.
- **Session management:** the API records a server-generated session ID, user-agent string, IP address, creation/last-active/expiry timestamps, and current-session status. The UI lists active sessions, revokes one other session, or revokes all other sessions. Each account is limited to one computer session and one phone/tablet session. A new login replaces and revokes existing active sessions in the same category; sessions in the other category remain active. Category is inferred from user-agent for policy grouping only, not as a device identity.
- **User management:** administrators can create, list/search/page, edit, activate/deactivate, and assign roles to accounts. Deactivation and password change revoke refresh sessions.
- **Role management:** administrators can list, create, update, and delete roles, subject to backend protections.
- **Role permissions:** a seeded Identity permission catalog drives binary role grants. The admin UI can read/update each role's grants; protected user/role endpoints evaluate `FunctionId + ActionId` and deny when no grant exists. Administrator is the root role. Permission editing and changes to Administrator membership are restricted to administrators.
- **Security audit:** authentication success/failure, logout/session revocation, password changes/resets, email confirmation, 2FA changes, user/role changes, and permission changes are appended to `SecurityAuditEvents`. The administrator console offers a paginated, event/outcome-filterable view with actor, subject, UTC time, IP, session, and safe detail codes.
- **Basic anomaly signal:** a successful sign-in from an IP address different from the user's latest successful sign-in is recorded as `SecurityAnomaly.NewLoginIp` and shown in the audit console. This is a review signal only; it does not block sign-in and can flag legitimate network changes.
- **Tenant/organization/branch scope:** users and roles are tenant-bound; role assignments and permission grants can be stored at tenant, organization, or branch scope and inherited down the hierarchy. User administration, role administration, authorization checks, and audit queries are tenant-filtered. The admin console includes a cross-tenant selector, tenant onboarding, hierarchy management, and scoped user-role assignment and role-permission controls.
- **Frontend UX:** English/Vietnamese copy, validation, localized session timestamps, informational browser/OS parsing, password-reset and email-confirmation callback screens, and a forced sign-in after password change.

User-agent browser/OS labels are display hints only. The server-generated session ID is the session identifier; a browser-provided user-agent is not trusted as a device identity.

### Backend gaps before the full requested scope is complete

- **Permission verification:** the permission API, catalog seed, authorization handler, and role matrix UI are implemented, but automated tests and end-to-end verification against a live Identity database/API are still needed. Role administration remains protected by explicit per-action requirements; permission editing is Administrator-only. Managed account creation cannot assign the Administrator role, and only an Administrator can add/remove that role later.
- **Tenant/organization/branch scope:** platform-administrator APIs list/read/create tenants, create organizations/branches, and enable/disable each level. Disabling a parent prevents new child scopes and denies permission evaluation in that subtree; data and assignments are retained. The administrator UI supports tenant onboarding, cross-tenant selection, hierarchy management, and lifecycle changes. The two security/tenant migrations were applied to the local `IdentityDatabase` on 2026-11-08. Cross-tenant isolation still needs automated and live API/database tests. Role names remain globally unique under the current ASP.NET Identity role store.
- **Security audit verification/operations:** audit events are implemented with an append-only application model and SQL Server migration, and the migration is applied to the local `IdentityDatabase`. Add automated tests and broader live API verification. Retention/archival and additional anomaly signals remain to be designed; failure events other than sign-in are not yet comprehensive.
- **Session policy and lifecycle:** concurrent sessions are capped at one computer and one phone/tablet per account. A new same-category login replaces prior same-category sessions inside a serializable database transaction; revocation denies existing refresh tokens and access JWTs. Lockout, deactivation, and password change also revoke stored sessions.
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
   - Tenant/organization/branch lifecycle APIs and the administrator UI for tenant onboarding, cross-tenant switching, hierarchy management, and activation/deactivation are implemented.
   - Cross-tenant isolation still needs automated and live API/database tests.
4. **Add security audit events — implementation present, validation incomplete**
   - Persist append-only events for authentication, logout/revocation, password/security changes, account status, role, and permission changes.
   - Capture actor/subject, UTC time, IP, session, action, result, and safe metadata.
   - Add an authorized, paginated/filterable audit API and admin UI.
   - Migration is applied to the local Identity database; add tests for event recording, filtering, and authorization.
5. **Add session policy and operational hardening**
   - Verify the one-computer and one-phone/tablet concurrent session policy; a new login replaces sessions in its own category.
   - Review user-agent based category inference and new-IP sign-in signals; add automated tests and extend anomaly signals without exposing sensitive details to unauthenticated callers.

## Acceptance criteria

- A new account can register, receive an email confirmation, confirm it, and sign in; invalid or expired links can be re-requested.
- Password reset is usable end-to-end and does not reveal whether an email is registered.
- 2FA can be enabled, used for sign-in, and disabled; changing a password requires a fresh sign-in because sessions are revoked.
- Session rows show current status, informational browser/OS, IP, and localized timestamps; remote revocation updates the list.
- User and role administration are backed by existing authorized API endpoints, with loading/error/empty states.
- Permission enforcement, scope isolation, audit history, and session policies have automated backend tests before corresponding UI controls are exposed.
