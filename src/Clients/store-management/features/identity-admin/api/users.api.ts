import type {
  AccountProfile,
  NewUserDraft,
  UpdateUserRequest,
  UpdateUserStatusRequest,
  UserPage,
  UserRolesRequest,
  RegisterUserRequest,
} from "../interfaces";
import { get, patch, put } from "@/services/api-client";
import { registerUser } from "./auth.api";

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
  const registration: RegisterUserRequest = {
    email: input.email,
    password: input.password,
    firstName: input.firstName,
    lastName: input.lastName,
  };
  const userId = await registerUser(registration);
  if (userId && input.roles.length) {
    await updateUserRoles(userId, { roles: input.roles });
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
