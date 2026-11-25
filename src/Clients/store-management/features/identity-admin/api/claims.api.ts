import type { IdentityClaim } from "../interfaces";
import { del, get, post } from "@/services/api-client";

const claimsQuery = (scopeId: string, type?: string, value?: string) =>
  new URLSearchParams({
    scopeId,
    ...(type === undefined ? {} : { type }),
    ...(value === undefined ? {} : { value }),
  });

export const listUserClaims = (userId: string, scopeId: string) =>
  get<IdentityClaim[]>(`admin/users/${encodeURIComponent(userId)}/claims?${claimsQuery(scopeId)}`);

export const addUserClaim = (userId: string, scopeId: string, claim: IdentityClaim) =>
  post(`admin/users/${encodeURIComponent(userId)}/claims?${claimsQuery(scopeId)}`, claim);

export const removeUserClaim = (userId: string, scopeId: string, claim: IdentityClaim) =>
  del(`admin/users/${encodeURIComponent(userId)}/claims?${claimsQuery(scopeId, claim.type, claim.value)}`);

export const listRoleClaims = (roleId: string, scopeId: string) =>
  get<IdentityClaim[]>(`admin/roles/${encodeURIComponent(roleId)}/claims?${claimsQuery(scopeId)}`);

export const addRoleClaim = (roleId: string, scopeId: string, claim: IdentityClaim) =>
  post(`admin/roles/${encodeURIComponent(roleId)}/claims?${claimsQuery(scopeId)}`, claim);

export const removeRoleClaim = (roleId: string, scopeId: string, claim: IdentityClaim) =>
  del(`admin/roles/${encodeURIComponent(roleId)}/claims?${claimsQuery(scopeId, claim.type, claim.value)}`);
