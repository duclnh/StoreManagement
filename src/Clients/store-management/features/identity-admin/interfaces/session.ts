export interface Session {
  authenticated: boolean;
  email: string;
  userId?: string;
}

export interface LoginSession {
  sessionId: string;
  userAgent: string | null;
  deviceCategory: "Computer" | "Phone";
  ipAddress: string | null;
  createdAtUtc: string;
  lastActiveAtUtc: string;
  expiresAtUtc: string;
  isCurrent: boolean;
}
