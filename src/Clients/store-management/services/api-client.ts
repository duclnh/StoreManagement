export class IdentityApiError extends Error {
  constructor(message: string, readonly code?: string) {
    super(message);
    this.name = "IdentityApiError";
  }
}

export async function identityRequest<T>(path: string, init?: RequestInit): Promise<T> {
  const response = await fetch(`/api/identity/${path}`, {
    ...init,
    cache: "no-store",
    headers: {
      ...(init?.body ? { "Content-Type": "application/json" } : {}),
      ...init?.headers,
    },
  });

  if (!response.ok) {
    const payload = await response.json().catch(() => null) as Record<string, unknown> | null;
    throw new IdentityApiError(
      String(payload?.detail ?? payload?.title ?? payload?.error ?? "The request could not be completed."),
      typeof payload?.type === "string" ? payload.type : undefined,
    );
  }

  if (response.status === 204) return undefined as T;
  return response.json() as Promise<T>;
}

export function get<T>(path: string): Promise<T> {
  return identityRequest<T>(path);
}

export function post<T = void>(path: string, body?: unknown): Promise<T> {
  return identityRequest<T>(path, {
    method: "POST",
    ...(body === undefined ? {} : { body: JSON.stringify(body) }),
  });
}

export function put<T = void>(path: string, body: unknown): Promise<T> {
  return identityRequest<T>(path, { method: "PUT", body: JSON.stringify(body) });
}

export function patch<T = void>(path: string, body: unknown): Promise<T> {
  return identityRequest<T>(path, { method: "PATCH", body: JSON.stringify(body) });
}

export function del<T = void>(path: string): Promise<T> {
  return identityRequest<T>(path, { method: "DELETE" });
}
