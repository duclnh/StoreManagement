import type {
  AccountProfile,
  NewUserDraft,
  UpdateUserRequest,
  UpdateUserStatusRequest,
  UserPage,
  UserRolesRequest,
} from "../interfaces";
import { get, patch, post, put } from "@/services/api-client";

export const emptyUserPage: UserPage = {
  items: [],
  page: 1,
  pageSize: 25,
  totalCount: 0,
};

export function listUsers(search: string, page: number, pageSize = 25) {
  const query = new URLSearchParams({ search, page: String(page), pageSize: String(pageSize) });
  return get<UserPage>(`admin/users?${query}`);
}

export const updateUser = (userId: string, input: UpdateUserRequest) =>
  put(`admin/users/${encodeURIComponent(userId)}`, input);

export const updateUserRoles = (userId: string, input: UserRolesRequest) =>
  put(`admin/users/${encodeURIComponent(userId)}/roles`, input);

export const updateUserStatus = (userId: string, input: UpdateUserStatusRequest) =>
  patch(`admin/users/${encodeURIComponent(userId)}/status`, input);

export async function createUser(input: NewUserDraft) {
  const response = await post<unknown>("admin/users", input);
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

export async function getAccountProfile(userId: string): Promise<AccountProfile> {
  const response = await get<unknown>(`admin/users/${encodeURIComponent(userId)}`);
  if (!response || typeof response !== "object") {
    throw new Error("Identity API returned an invalid account profile.");
  }
  const profile = response as Record<string, unknown>;
  if (typeof profile.twoFactorEnabled !== "boolean") {
    throw new Error("Identity API returned an invalid account profile.");
  }
  return { twoFactorEnabled: profile.twoFactorEnabled };
}
