import type { LoginSession } from "../interfaces";
import { get, post } from "@/services/api-client";

export async function listSessions(): Promise<LoginSession[]> {
  const response = await get<unknown>("auths/sessions");
  const payload = response && typeof response === "object"
    ? response as { value?: unknown; Value?: unknown }
    : undefined;
  const sessions = Array.isArray(response) ? response : payload?.value ?? payload?.Value;
  if (!Array.isArray(sessions) || !sessions.every(isLoginSession)) {
    throw new Error("Identity API returned an invalid session list.");
  }
  return sessions;
}

export const revokeSession = (sessionId: string) =>
  post("auths/revoke", { sessionId });

export const revokeOtherSessions = () =>
  post("auths/sessions/revoke-others", {});

function isLoginSession(value: unknown): value is LoginSession {
  if (!value || typeof value !== "object") return false;
  const session = value as Record<string, unknown>;
  return typeof session.sessionId === "string"
    && (typeof session.userAgent === "string" || session.userAgent === null)
    && (typeof session.ipAddress === "string" || session.ipAddress === null)
    && typeof session.createdAtUtc === "string"
    && typeof session.lastActiveAtUtc === "string"
    && typeof session.expiresAtUtc === "string"
    && typeof session.isCurrent === "boolean";
}
