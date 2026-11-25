import type {
  NewUserDraft,
  UpdateUserRequest,
  UpdateUserStatusRequest,
  UserPage,
  UserRolesRequest,
} from "../interfaces";
import { get, patch, post, put } from "@/services/api-client";

export const emptyUserPage: UserPage = {
  items: [],
  pageIndex: 1,
  pageSize: 25,
  totalCount: 0,
  hasNextPage: false,
  hasPreviousPage: false,
};

export function listUsers(search: string, page: number, pageSize = 25, scopeId?: string) {
  const query = new URLSearchParams({ search, page: String(page), pageSize: String(pageSize) });
  if (scopeId) query.set("scopeId", scopeId);
  return get<UserPage>(`admin/users?${query}`);
}

export const updateUser = (userId: string, input: UpdateUserRequest, scopeId?: string) =>
  put(`admin/users/${encodeURIComponent(userId)}${scopeId ? `?${new URLSearchParams({ scopeId })}` : ""}`, input);

export const listUserRoles = (userId: string, scopeId: string) =>
  get<string[]>(`admin/users/${encodeURIComponent(userId)}/roles?${new URLSearchParams({ scopeId })}`);

export const updateUserRoles = (userId: string, scopeId: string, input: UserRolesRequest) =>
  put(`admin/users/${encodeURIComponent(userId)}/roles?${new URLSearchParams({ scopeId })}`, input);

export const updateUserStatus = (userId: string, input: UpdateUserStatusRequest, scopeId?: string) =>
  patch(`admin/users/${encodeURIComponent(userId)}/status${scopeId ? `?${new URLSearchParams({ scopeId })}` : ""}`, input);

export async function createUser(input: NewUserDraft, scopeId?: string) {
  const response = await post<unknown>(`admin/users${scopeId ? `?${new URLSearchParams({ scopeId })}` : ""}`, input);
  const userId = typeof response === "string"
    ? response
    : response && typeof response === "object"
      ? (response as { value?: unknown; Value?: unknown }).value
        ?? (response as { Value?: unknown }).Value
      : undefined;
  if (typeof userId !== "string" || !userId) {
    throw new Error("Identity API returned an invalid managed user response.");
  }
}
