import axios, { type AxiosRequestConfig } from "axios";

export class IdentityApiError extends Error {
  constructor(message: string, readonly code?: string) {
    super(message);
    this.name = "IdentityApiError";
  }
}

const identityHttpClient = axios.create({
  baseURL: "/api/identity",
  headers: { Accept: "application/json" },
});

export async function identityRequest<T>(path: string, init?: RequestInit): Promise<T> {
  const config: AxiosRequestConfig = {
    url: path,
    method: init?.method ?? "GET",
    data: init?.body,
    headers: {
      ...(init?.body ? { "Content-Type": "application/json" } : {}),
      ...Object.fromEntries(new Headers(init?.headers).entries()),
    },
  };

  try {
    const response = await identityHttpClient.request<T>(config);
    if (response.status === 204) return undefined as T;
    return response.data;
  } catch (cause: unknown) {
    if (!axios.isAxiosError<Record<string, unknown>>(cause)) throw cause;
    const errorResponse = cause.response;
    const payload = errorResponse?.data;
    throw new IdentityApiError(
      String(payload?.detail ?? payload?.title ?? payload?.error ?? cause.message),
      typeof payload?.type === "string" ? payload.type : undefined,
    );
  }
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
