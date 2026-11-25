import type { IdentityAdminAccess, IdentityScope, Role, RolePermission, SaveRoleRequest, SetRolePermissionsRequest } from "../interfaces";
import type { IdentityAdminScope, IdentityTenant } from "../interfaces";
import { del, get, identityRequest, post, put } from "@/services/api-client";

export const listRoles = (scopeId?: string) =>
  get<Role[]>(`admin/roles${scopeId ? `?${new URLSearchParams({ scopeId })}` : ""}`);

export const listIdentityScopes = () => get<IdentityScope[]>("admin/scopes");

export const getIdentityAdminAccess = (scopeId: string) =>
  get<IdentityAdminAccess>(`admin/access?${new URLSearchParams({ scopeId })}`);

export const listIdentityTenants = () => get<IdentityTenant[]>("admin/tenants");

export const listTenantAdminScopes = (tenantId: string) =>
  identityRequest<IdentityAdminScope[]>(`admin/tenants/${encodeURIComponent(tenantId)}/scopes`, {
    headers: { "X-Tenant-Id": tenantId },
  });

export const createIdentityTenant = (name: string) =>
  post<IdentityScope>("admin/tenants", { name });

export const createIdentityOrganization = (tenantId: string, name: string) =>
  post<IdentityScope>(`admin/tenants/${encodeURIComponent(tenantId)}/organizations`, { name });

export const createIdentityBranch = (tenantId: string, organizationId: string, name: string) =>
  post<IdentityScope>(
    `admin/tenants/${encodeURIComponent(tenantId)}/organizations/${encodeURIComponent(organizationId)}/branches`,
    { name },
  );

export const setIdentityTenantStatus = (tenantId: string, isActive: boolean) =>
  put(`admin/tenants/${encodeURIComponent(tenantId)}/status`, { isActive });

export const setIdentityOrganizationStatus = (tenantId: string, organizationId: string, isActive: boolean) =>
  put(`admin/tenants/${encodeURIComponent(tenantId)}/organizations/${encodeURIComponent(organizationId)}/status`, { isActive });

export const setIdentityBranchStatus = (
  tenantId: string,
  organizationId: string,
  branchId: string,
  isActive: boolean,
) =>
  put(
    `admin/tenants/${encodeURIComponent(tenantId)}/organizations/${encodeURIComponent(organizationId)}/branches/${encodeURIComponent(branchId)}/status`,
    { isActive },
  );

export const getRolePermissions = (roleId: string, scopeId: string) =>
  get<RolePermission[]>(`admin/roles/${encodeURIComponent(roleId)}/permissions?${new URLSearchParams({ scopeId })}`);

export const setRolePermissions = (roleId: string, scopeId: string, input: SetRolePermissionsRequest) =>
  put(`admin/roles/${encodeURIComponent(roleId)}/permissions?${new URLSearchParams({ scopeId })}`, input);

export function saveRole(id: string | undefined, input: SaveRoleRequest, scopeId?: string) {
  const suffix = scopeId ? `?${new URLSearchParams({ scopeId })}` : "";
  return id
    ? put(`admin/roles/${encodeURIComponent(id)}${suffix}`, input)
    : post(`admin/roles${suffix}`, input);
}

export const deleteRole = (id: string, scopeId?: string) =>
  del(`admin/roles/${encodeURIComponent(id)}${scopeId ? `?${new URLSearchParams({ scopeId })}` : ""}`);
