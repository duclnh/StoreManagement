export type Session = { authenticated: boolean; email: string };

export type LoginSession = {
  sessionId: string;
  userAgent: string | null;
  ipAddress: string | null;
  createdAtUtc: string;
  lastActiveAtUtc: string;
  expiresAtUtc: string;
  isCurrent: boolean;
};

export type User = {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  fullName: string;
  isActive: boolean;
  emailConfirmed: boolean;
  twoFactorEnabled: boolean;
  roles: string[];
};

export type UserPage = {
  items: User[];
  page: number;
  pageSize: number;
  totalCount: number;
};

export type Role = {
  id: string;
  name: string;
  roleCode: string;
  description: string;
  userCount: number;
};

export type RoleDraft = {
  id?: string;
  name: string;
  roleCode: string;
  description: string;
};

export type NewUserDraft = {
  email: string;
  firstName: string;
  lastName: string;
  password: string;
  roles: string[];
};
