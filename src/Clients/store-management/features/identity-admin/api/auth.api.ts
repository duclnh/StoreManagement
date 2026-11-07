import type {
  AuthenticatorSetup,
  ChangePasswordRequest,
  LoginRequest,
  RegisterUserRequest,
  Session,
  TwoFactorCodeRequest,
} from "../interfaces";
import { get, identityRequest, post } from "@/services/api-client";

export const getSession = () => get<Session>("session");

export const signIn = (credentials: LoginRequest) =>
  post<Session>("auths/login", credentials);

export const signOut = () => post("auths/logout", {});

export async function registerUser(input: RegisterUserRequest) {
  const response = await post<unknown>("auths/register", input);
  const userId = typeof response === "string"
    ? response
    : response && typeof response === "object"
      ? (response as { value?: unknown; Value?: unknown }).value
        ?? (response as { Value?: unknown }).Value
      : undefined;
  if (typeof userId !== "string" || !userId) {
    throw new Error("Identity API returned an invalid account registration response.");
  }
  return userId;
}

export const changePassword = (input: ChangePasswordRequest) =>
  post("auths/password/change", input);

export const requestPasswordReset = (email: string) =>
  post("auths/password/forgot", { email });

export const resetPassword = (input: { email: string; token: string; newPassword: string }) =>
  post("auths/password/reset", input);

export const requestEmailConfirmation = (email: string) =>
  post("auths/email/confirmation/request", { email });

export const confirmEmail = (input: { email: string; token: string }) =>
  post("auths/email/confirm", input);

export function getAuthenticatorSetup() {
  return identityRequest<unknown>("auths/2fa/setup").then(readAuthenticatorSetup);
}

export const enableTwoFactor = (input: TwoFactorCodeRequest) =>
  post("auths/2fa/enable", input);

export const disableTwoFactor = (input: TwoFactorCodeRequest) =>
  post("auths/2fa/disable", input);

function readAuthenticatorSetup(value: unknown): AuthenticatorSetup {
  if (!value || typeof value !== "object") {
    throw new Error("Identity API returned an invalid authenticator setup.");
  }

  const response = value as Record<string, unknown>;
  const setup = (response.value ?? response.Value ?? value) as Record<string, unknown>;
  const sharedKey = setup.sharedKey ?? setup.SharedKey;
  const authenticatorUri = setup.authenticatorUri ?? setup.AuthenticatorUri;
  if (typeof sharedKey !== "string" || typeof authenticatorUri !== "string") {
    throw new Error("Identity API returned an invalid authenticator setup.");
  }
  return { sharedKey, authenticatorUri };
}
