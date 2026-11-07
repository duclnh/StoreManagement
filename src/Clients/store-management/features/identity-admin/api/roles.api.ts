import type { IdentityScope, Role, RolePermission, SaveRoleRequest, SetRolePermissionsRequest } from "../interfaces";
import { del, get, post, put } from "@/services/api-client";

export const listRoles = () => get<Role[]>("admin/roles");

export const listIdentityScopes = () => get<IdentityScope[]>("admin/scopes");

export const getRolePermissions = (roleId: string, scopeId: string) =>
  get<RolePermission[]>(`admin/roles/${encodeURIComponent(roleId)}/permissions?${new URLSearchParams({ scopeId })}`);

export const setRolePermissions = (roleId: string, scopeId: string, input: SetRolePermissionsRequest) =>
  put(`admin/roles/${encodeURIComponent(roleId)}/permissions?${new URLSearchParams({ scopeId })}`, input);

export function saveRole(id: string | undefined, input: SaveRoleRequest) {
  return id ? put(`admin/roles/${encodeURIComponent(id)}`, input) : post("admin/roles", input);
}

export const deleteRole = (id: string) =>
  del(`admin/roles/${encodeURIComponent(id)}`);
