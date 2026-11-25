import type { PageResult } from "./page";

export interface User {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  fullName: string;
  isActive: boolean;
  emailConfirmed: boolean;
  twoFactorEnabled: boolean;
  roles: string[];
}

export type UserPage = PageResult<User>;

export interface NewUserDraft {
  email: string;
  firstName: string;
  lastName: string;
  password: string;
  roles: string[];
}

export interface UpdateUserRequest {
  firstName: string;
  lastName: string;
}

export interface UserRolesRequest {
  roles: string[];
}

export interface UpdateUserStatusRequest {
  isActive: boolean;
}
