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
  access: "identity_access",
  refresh: "identity_refresh",
  userId: "identity_user_id",
  sessionId: "identity_session_id",
  email: "identity_email",
} as const;

const identityApi = (process.env.IDENTITY_API_URL ?? "http://localhost:5168").replace(/\/$/, "");
const cookieOptions = {
  httpOnly: true,
  secure: process.env.NODE_ENV === "production",
  sameSite: "lax" as const,
  path: "/api",
};

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
  cookieStore.set(cookieNames.access, auth.accessToken!, { ...cookieOptions, maxAge: 5 * 60 });
  cookieStore.set(cookieNames.refresh, auth.refreshToken!, {
    ...cookieOptions,
    maxAge: refreshExpires && !Number.isNaN(refreshExpires.getTime())
      ? Math.max(0, Math.floor((refreshExpires.getTime() - Date.now()) / 1000))
      : 30 * 24 * 60 * 60,
  });
  cookieStore.set(cookieNames.userId, auth.userId!, { ...cookieOptions, maxAge: 30 * 24 * 60 * 60 });
  cookieStore.set(cookieNames.sessionId, auth.sessionId!, { ...cookieOptions, maxAge: 30 * 24 * 60 * 60 });
  cookieStore.set(cookieNames.email, email, { ...cookieOptions, maxAge: 30 * 24 * 60 * 60 });
}

async function refreshAccessToken() {
  const cookieStore = await cookies();
  const userId = cookieStore.get(cookieNames.userId)?.value;
  const sessionId = cookieStore.get(cookieNames.sessionId)?.value;
  const token = cookieStore.get(cookieNames.refresh)?.value;
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
  for (const name of Object.values(cookieNames)) cookieStore.delete(name);
}

async function handle(request: Request, context: RouteContext) {
  const { path } = await context.params;
  const resourcePath = path.join("/");
  const url = new URL(request.url);

  if (request.method === "GET" && resourcePath === "session") {
    const cookieStore = await cookies();
    const hasSession = Boolean(cookieStore.get(cookieNames.refresh)?.value);
    return Response.json({
      authenticated: hasSession,
      email: hasSession ? cookieStore.get(cookieNames.email)?.value ?? "" : "",
      userId: hasSession ? cookieStore.get(cookieNames.userId)?.value ?? "" : "",
    });
  }

  if (!new Set(["GET", "HEAD", "OPTIONS"]).has(request.method)) {
    const origin = request.headers.get("origin");
    if (!origin || origin !== url.origin) return Response.json({ error: "Invalid request origin." }, { status: 403 });
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
    const cookieStore = await cookies();
    const sessionId = cookieStore.get(cookieNames.sessionId)?.value;
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

  const cookieStore = await cookies();
  let accessToken = cookieStore.get(cookieNames.access)?.value ?? await refreshAccessToken();
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