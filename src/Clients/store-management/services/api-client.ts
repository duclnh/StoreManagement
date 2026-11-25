import axios, { type AxiosRequestConfig } from "axios";

export class IdentityApiError extends Error {
  constructor(message: string, readonly code?: string, readonly status?: number) {
    super(message);
    this.name = "IdentityApiError";
  }
}

const identityHttpClient = axios.create({
  baseURL: "/api/identity",
  timeout: 15_000,
  headers: { Accept: "application/json" },
});

export async function identityRequest<T>(path: string, init?: RequestInit): Promise<T> {
  const selectedTenantId = typeof window === "undefined"
    ? null
    : window.localStorage.getItem("identity-admin-active-tenant");
  const config: AxiosRequestConfig = {
    url: path,
    method: init?.method ?? "GET",
    data: init?.body,
    headers: {
      ...(selectedTenantId ? { "X-Tenant-Id": selectedTenantId } : {}),
      ...(init?.body ? { "Content-Type": "application/json" } : {}),
      ...Object.fromEntries(new Headers(init?.headers).entries()),
    },
  };

  try {
    const response = await identityHttpClient.request<T>(config);
    if (response.status === 204) return undefined as T;
    return unwrapResultValue(response.data);
  } catch (cause: unknown) {
    if (!axios.isAxiosError<Record<string, unknown>>(cause)) throw cause;
    const errorResponse = cause.response;
    if (errorResponse?.status === 401 && typeof window !== "undefined")
      window.dispatchEvent(new Event("identity-session-expired"));
    const payload = errorResponse?.data;
    throw new IdentityApiError(
      String(payload?.detail ?? payload?.title ?? payload?.error ?? cause.message),
      typeof payload?.type === "string" ? payload.type : undefined,
      errorResponse?.status,
    );
  }
}

function unwrapResultValue<T>(payload: T): T {
  if (!payload || typeof payload !== "object") return payload;
  const result = payload as Record<string, unknown>;
  const isSuccess = result.isSuccess ?? result.IsSuccess;
  if (isSuccess !== true) return payload;
  if ("value" in result) return result.value as T;
  if ("Value" in result) return result.Value as T;
  return payload;
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
