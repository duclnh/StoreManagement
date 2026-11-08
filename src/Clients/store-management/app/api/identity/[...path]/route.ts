import { createCipheriv, createDecipheriv, randomBytes } from "node:crypto";
import { cookies } from "next/headers";

type RouteContext = { params: Promise<{ path: string[] }> };

type Authenticated = {
  accessToken?: string;
  refreshToken?: string;
  refreshTokenExpiryTime?: string;
  userId?: string;
  sessionId?: string;
};

const cookieNames = {
  access: "app_7f2a",
  refresh: "app_91c4",
  userId: "app_3d8b",
  sessionId: "app_c6e1",
  email: "app_2a5f",
} as const;

const legacyCookieNames = [
  "identity_access",
  "identity_refresh",
  "identity_user_id",
  "identity_session_id",
  "identity_email",
] as const;

const identityApi = (process.env.IDENTITY_API_URL ?? "http://localhost:5168").replace(/\/$/, "");
const cookieCipher = "aes-256-gcm";
const cookieOptions = {
  httpOnly: true,
  secure: process.env.NODE_ENV === "production",
  sameSite: "lax" as const,
  path: "/api",
};

function getCookieEncryptionKey() {
  const encodedKey = process.env.IDENTITY_COOKIE_ENCRYPTION_KEY?.trim();
  if (!encodedKey || !/^[A-Za-z0-9+/]{43}=$/.test(encodedKey)) {
    throw new Error("IDENTITY_COOKIE_ENCRYPTION_KEY must be a base64-encoded 32-byte key.");
  }

  const key = Buffer.from(encodedKey, "base64");
  if (key.length !== 32) {
    throw new Error("IDENTITY_COOKIE_ENCRYPTION_KEY must be a base64-encoded 32-byte key.");
  }
  return key;
}

function encryptCookieValue(value: string) {
  const iv = randomBytes(12);
  const cipher = createCipheriv(cookieCipher, getCookieEncryptionKey(), iv);
  const ciphertext = Buffer.concat([cipher.update(value, "utf8"), cipher.final()]);
  return `v1.${iv.toString("base64url")}.${cipher.getAuthTag().toString("base64url")}.${ciphertext.toString("base64url")}`;
}

function decryptCookieValue(value: string | undefined) {
  if (!value) return undefined;
  const [version, encodedIv, encodedTag, encodedCiphertext, ...extra] = value.split(".");
  if (version !== "v1" || !encodedIv || !encodedTag || !encodedCiphertext || extra.length > 0) return undefined;

  const iv = Buffer.from(encodedIv, "base64url");
  const tag = Buffer.from(encodedTag, "base64url");
  const ciphertext = Buffer.from(encodedCiphertext, "base64url");
  if (iv.length !== 12 || tag.length !== 16 || ciphertext.length === 0) return undefined;

  try {
    const decipher = createDecipheriv(cookieCipher, getCookieEncryptionKey(), iv);
    decipher.setAuthTag(tag);
    return Buffer.concat([decipher.update(ciphertext), decipher.final()]).toString("utf8");
  } catch {
    return undefined;
  }
}

function unwrapAuthenticated(payload: unknown): Authenticated | undefined {
  if (!payload || typeof payload !== "object") return undefined;
  const value = payload as Record<string, unknown>;
  const result = (value.value ?? value.Value ?? value) as Record<string, unknown>;
  return {
    accessToken: (result.accessToken ?? result.AccessToken) as string | undefined,
    refreshToken: (result.refreshToken ?? result.RefreshToken) as string | undefined,
    refreshTokenExpiryTime: (result.refreshTokenExpiryTime ?? result.RefreshTokenExpiryTime) as string | undefined,
    userId: (result.userId ?? result.UserId) as string | undefined,
    sessionId: (result.sessionId ?? result.SessionId) as string | undefined,
  };
}

async function readPayload(response: Response) {
  return response.json().catch(() => null);
}

async function saveSession(auth: Authenticated, email: string) {
  const cookieStore = await cookies();
  const refreshExpires = auth.refreshTokenExpiryTime ? new Date(auth.refreshTokenExpiryTime) : undefined;
  cookieStore.set(cookieNames.access, encryptCookieValue(auth.accessToken!), { ...cookieOptions, maxAge: 5 * 60 });
  cookieStore.set(cookieNames.refresh, encryptCookieValue(auth.refreshToken!), {
    ...cookieOptions,
    maxAge: refreshExpires && !Number.isNaN(refreshExpires.getTime())
      ? Math.max(0, Math.floor((refreshExpires.getTime() - Date.now()) / 1000))
      : 30 * 24 * 60 * 60,
  });
  cookieStore.set(cookieNames.userId, encryptCookieValue(auth.userId!), { ...cookieOptions, maxAge: 30 * 24 * 60 * 60 });
  cookieStore.set(cookieNames.sessionId, encryptCookieValue(auth.sessionId!), { ...cookieOptions, maxAge: 30 * 24 * 60 * 60 });
  cookieStore.set(cookieNames.email, encryptCookieValue(email), { ...cookieOptions, maxAge: 30 * 24 * 60 * 60 });
}

async function refreshAccessToken() {
  const cookieStore = await cookies();
  const refreshCookie = cookieStore.get(cookieNames.refresh)?.value;
  const userId = decryptCookieValue(cookieStore.get(cookieNames.userId)?.value);
  const sessionId = decryptCookieValue(cookieStore.get(cookieNames.sessionId)?.value);
  const token = decryptCookieValue(refreshCookie);
  if (refreshCookie && (!userId || !sessionId || !token)) {
    await clearSession();
    return undefined;
  }
  if (!userId || !sessionId || !token) return undefined;

  const response = await fetch(`${identityApi}/api/v1/auths/refresh`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ userId, sessionId, token }),
    cache: "no-store",
  }).catch(() => undefined);
  if (!response?.ok) return undefined;

  const auth = unwrapAuthenticated(await readPayload(response));
  if (!auth?.accessToken || !auth.refreshToken || !auth.userId || !auth.sessionId) return undefined;

  await saveSession(auth, cookieStore.get(cookieNames.email)?.value ?? "");
  return auth.accessToken;
}

async function clearSession() {
  const cookieStore = await cookies();
  for (const name of [...Object.values(cookieNames), ...legacyCookieNames]) cookieStore.delete(name);
}

async function handle(request: Request, context: RouteContext) {
  const cookieStore = await cookies();
  for (const name of legacyCookieNames) {
    if (cookieStore.get(name)) cookieStore.delete(name);
  }

  const { path } = await context.params;
  const resourcePath = path.join("/");
  const url = new URL(request.url);

  if (request.method === "GET" && resourcePath === "session") {
    const refreshCookie = cookieStore.get(cookieNames.refresh)?.value;
    const refreshToken = decryptCookieValue(refreshCookie);
    const userId = decryptCookieValue(cookieStore.get(cookieNames.userId)?.value);
    const sessionId = decryptCookieValue(cookieStore.get(cookieNames.sessionId)?.value);
    const email = decryptCookieValue(cookieStore.get(cookieNames.email)?.value);
    const hasSession = Boolean(refreshToken && userId && sessionId);
    if (refreshCookie && !hasSession) await clearSession();
    return Response.json({
      authenticated: hasSession,
      email: hasSession ? email ?? "" : "",
      userId: hasSession ? userId : "",
    });
  }

  if (!new Set(["GET", "HEAD", "OPTIONS"]).has(request.method)) {
    const origin = request.headers.get("origin");
    const requestHost = request.headers.get("host")?.toLowerCase();
    const forwardedProtocol = request.headers.get("x-forwarded-proto")?.split(",")[0]?.trim();
    const acceptedProtocols = new Set([url.protocol]);
    if (forwardedProtocol === "http" || forwardedProtocol === "https")
      acceptedProtocols.add(`${forwardedProtocol}:`);

    let isSameOrigin = false;
    if (origin && requestHost) {
      try {
        const originUrl = new URL(origin);
        isSameOrigin = originUrl.origin === origin
          && originUrl.host === requestHost
          && acceptedProtocols.has(originUrl.protocol);
      } catch {
        isSameOrigin = false;
      }
    }

    if (!isSameOrigin) {
      return Response.json({ error: "Invalid request origin." }, { status: 403 });
    }
  }

  if (
    request.method === "POST"
    && new Set([
      "auths/password/forgot",
      "auths/password/reset",
      "auths/register",
      "auths/email/confirmation/request",
      "auths/email/confirm",
    ]).has(resourcePath)
  ) {
    try {
      const upstream = await fetch(`${identityApi}/api/v1/${resourcePath}`, {
        method: "POST",
        headers: {
          "Content-Type": request.headers.get("content-type") ?? "application/json",
          Accept: request.headers.get("accept") ?? "application/json",
        },
        body: await request.text(),
        cache: "no-store",
      });
      return new Response(upstream.body, {
        status: upstream.status,
        headers: { "Content-Type": upstream.headers.get("content-type") ?? "application/json" },
      });
    } catch {
      return Response.json({ error: "Identity API is unavailable." }, { status: 502 });
    }
  }

  if (request.method === "POST" && resourcePath === "auths/login") {
    getCookieEncryptionKey();
    const body = await request.text();
    const email = (() => {
      try {
        return String((JSON.parse(body) as { email?: unknown }).email ?? "");
      } catch {
        return "";
      }
    })();

    const response = await fetch(`${identityApi}/api/v1/auths/login`, {
      method: "POST",
      headers: { "Content-Type": request.headers.get("content-type") ?? "application/json" },
      body,
      cache: "no-store",
    }).catch(() => undefined);
    if (!response) return Response.json({ error: "Identity API is unavailable." }, { status: 502 });

    const payload = await readPayload(response);
    if (!response.ok) return Response.json(payload, { status: response.status });

    const auth = unwrapAuthenticated(payload);
    if (!auth?.accessToken || !auth.refreshToken || !auth.userId || !auth.sessionId)
      return Response.json({ error: "Identity API returned an invalid login response." }, { status: 502 });

    await saveSession(auth, email);
    return Response.json({ authenticated: true, email, userId: auth.userId });
  }

  if (request.method === "POST" && resourcePath === "auths/logout") {
    const accessToken = await refreshAccessToken();
    const sessionId = decryptCookieValue(cookieStore.get(cookieNames.sessionId)?.value);
    if (accessToken && sessionId) {
      await fetch(`${identityApi}/api/v1/auths/revoke`, {
        method: "POST",
        headers: {
          Authorization: `Bearer ${accessToken}`,
          "Content-Type": "application/json",
        },
        body: JSON.stringify({ sessionId }),
        cache: "no-store",
      }).catch(() => undefined);
    }
    await clearSession();
    return new Response(null, { status: 204 });
  }

  let accessToken = decryptCookieValue(cookieStore.get(cookieNames.access)?.value) ?? await refreshAccessToken();
  if (!accessToken) {
    await clearSession();
    return Response.json({ error: "Authentication required." }, { status: 401 });
  }

  const upstreamUrl = `${identityApi}/api/v1/${resourcePath}${url.search}`;
  const forward = (token: string) => fetch(upstreamUrl, {
    method: request.method,
    headers: {
      Authorization: `Bearer ${token}`,
      ...(request.headers.get("content-type") ? { "Content-Type": request.headers.get("content-type")! } : {}),
      ...(request.headers.get("x-tenant-id") ? { "X-Tenant-Id": request.headers.get("x-tenant-id")! } : {}),
      Accept: request.headers.get("accept") ?? "application/json",
    },
    ...(request.method === "GET" || request.method === "HEAD" ? {} : { body: request.body }),
    cache: "no-store",
    // @ts-expect-error duplex is required by Node's fetch when forwarding a request stream.
    duplex: request.method === "GET" || request.method === "HEAD" ? undefined : "half",
  });

  let upstream: Response;
  try {
    upstream = await forward(accessToken);
    if (upstream.status === 401) {
      accessToken = await refreshAccessToken() ?? "";
      if (!accessToken) {
        await clearSession();
        return Response.json({ error: "Your session has expired." }, { status: 401 });
      }
      upstream = await forward(accessToken);
    }
  } catch {
    return Response.json({ error: "Identity API is unavailable." }, { status: 502 });
  }

  return new Response(upstream.body, {
    status: upstream.status,
    headers: {
      "Content-Type": upstream.headers.get("content-type") ?? "application/json",
      ...(upstream.headers.get("location") ? { Location: upstream.headers.get("location")! } : {}),
    },
  });
}

export const GET = handle;
export const POST = handle;
export const PUT = handle;
export const PATCH = handle;
export const DELETE = handle;