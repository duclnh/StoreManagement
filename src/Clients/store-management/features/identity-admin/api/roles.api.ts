import type { IdentityScope, Role, RolePermission, SaveRoleRequest, SetRolePermissionsRequest } from "../interfaces";
import type { IdentityTenant } from "../interfaces";
import { del, get, identityRequest, post, put } from "@/services/api-client";

export const listRoles = () => get<Role[]>("admin/roles");

export const listIdentityScopes = () => get<IdentityScope[]>("admin/scopes");

export const listIdentityTenants = () => get<IdentityTenant[]>("admin/tenants");

export const listTenantAdminScopes = (tenantId: string) =>
  identityRequest<IdentityScope[]>(`admin/tenants/${encodeURIComponent(tenantId)}/scopes`, {
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

export function saveRole(id: string | undefined, input: SaveRoleRequest) {
  return id ? put(`admin/roles/${encodeURIComponent(id)}`, input) : post("admin/roles", input);
}

export const deleteRole = (id: string) =>
  del(`admin/roles/${encodeURIComponent(id)}`);
