import type { Role, SaveRoleRequest } from "../interfaces";
import { del, get, post, put } from "@/services/api-client";

export const listRoles = () => get<Role[]>("admin/roles");

export function saveRole(id: string | undefined, input: SaveRoleRequest) {
  return id ? put(`admin/roles/${encodeURIComponent(id)}`, input) : post("admin/roles", input);
}

export const deleteRole = (id: string) =>
  del(`admin/roles/${encodeURIComponent(id)}`);
