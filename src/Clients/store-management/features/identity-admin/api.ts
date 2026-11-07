import type { UserPage } from "./types";

export async function identityApi<T>(path: string, init?: RequestInit): Promise<T> {
  const response = await fetch(`/api/identity/${path}`, {
    ...init,
    cache: "no-store",
    headers: { ...(init?.body ? { "Content-Type": "application/json" } : {}), ...init?.headers },
  });

  if (!response.ok) {
    const payload = await response.json().catch(() => null) as Record<string, unknown> | null;
    throw new Error(String(payload?.detail ?? payload?.title ?? payload?.error ?? "The request could not be completed."));
  }

  if (response.status === 204) return undefined as T;
  return response.json() as Promise<T>;
}

export const emptyUserPage: UserPage = {
  items: [],
  page: 1,
  pageSize: 25,
  totalCount: 0,
};
