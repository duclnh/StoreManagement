"use client";

import { ArrowLeft, ArrowRight, Building2, Check, ClipboardList, Globe2, KeyRound, LogOut, Monitor, Pencil, Plus, Search, Settings2, Shield, ShieldCheck, Smartphone, Trash2, UsersRound, X } from "lucide-react";
import { useTranslations } from "next-intl";
import { useRouter } from "next/navigation";
import { useDeferredValue, useEffect, useRef, useState, type FormEvent } from "react";
import { useAppLocale } from "@/app/providers";
import { IdentityApiError } from "@/services/api-client";
import type { LoginCredentialsValues, TwoFactorCodeValues } from "../schemas/login.schema";
import { getCurrentUserProfile, getSession, requestEmailConfirmation, signIn as authenticate, signOut as endSession } from "../api/auth.api";
import {
  deleteRole as removeRole,
  getIdentityAdminAccess,
  listIdentityScopes,
  listIdentityTenants,
  listRoles,
  saveRole as persistRole,
} from "../api/roles.api";
import {
  listSessions,
  revokeOtherSessions as revokeOtherLoginSessions,
  revokeSession as revokeLoginSession,
} from "../api/sessions.api";
import { getSecurityAuditPage } from "../api/security-audit.api";
import {
  addRoleClaim,
  addUserClaim,
  listRoleClaims,
  listUserClaims,
  removeRoleClaim,
  removeUserClaim,
} from "../api/claims.api";
import {
  createUser as createUserAccount,
  emptyUserPage,
  listUserRoles,
  listUsers,
  updateUser,
  updateUserRoles,
  updateUserStatus,
} from "../api/users.api";
import AccountSecurity from "./account-security";
import PermissionsPage from "./permissions-page";
import UserSessions from "./user-sessions";
import LoginScreen from "./login-screen";
import TenantManagement from "./tenant-management";
import { useWorkspaceView, type WorkspaceView } from "./workspace-view-provider";
import { formatSessionDate, getSessionDeviceLabel } from "../utils/session-display";
import type { AccountProfile, AdminConsoleProps, IdentityAdminAccess, IdentityClaim, IdentityScope, IdentityTenant, LoginRequest, LoginSession, NewUserDraft, Role, RoleDraft, SecurityAuditPage, Session, User, UserPage } from "../interfaces";

type ClaimTarget =
  | { kind: "user"; id: string; name: string }
  | { kind: "role"; id: string; name: string };

const securityAuditEventLabels: Record<string, "audit.events.login" | "audit.events.logout" | "audit.events.sessionsRevoked" | "audit.events.passwordChanged" | "audit.events.passwordReset" | "audit.events.emailConfirmed" | "audit.events.twoFactorEnabled" | "audit.events.twoFactorDisabled" | "audit.events.userCreated" | "audit.events.userUpdated" | "audit.events.userStatusChanged" | "audit.events.userRolesChanged" | "audit.events.userClaimsChanged" | "audit.events.roleCreated" | "audit.events.roleUpdated" | "audit.events.roleDeleted" | "audit.events.rolePermissionsChanged" | "audit.events.roleClaimsChanged" | "audit.events.scopeCreated" | "audit.events.scopeStatusChanged" | "audit.events.newLoginIp" | "audit.events.deviceSessionReplaced" | "audit.events.unknown"> = {
  "Authentication.Login": "audit.events.login",
  "Authentication.Logout": "audit.events.logout",
  "Authentication.SessionsRevoked": "audit.events.sessionsRevoked",
  "Authentication.PasswordChanged": "audit.events.passwordChanged",
  "Authentication.PasswordReset": "audit.events.passwordReset",
  "Authentication.EmailConfirmed": "audit.events.emailConfirmed",
  "Authentication.TwoFactorEnabled": "audit.events.twoFactorEnabled",
  "Authentication.TwoFactorDisabled": "audit.events.twoFactorDisabled",
  "Users.Created": "audit.events.userCreated",
  "Users.Updated": "audit.events.userUpdated",
  "Users.StatusChanged": "audit.events.userStatusChanged",
  "Users.RolesChanged": "audit.events.userRolesChanged",
  "Users.ClaimsChanged": "audit.events.userClaimsChanged",
  "Roles.Created": "audit.events.roleCreated",
  "Roles.Updated": "audit.events.roleUpdated",
  "Roles.Deleted": "audit.events.roleDeleted",
  "Roles.PermissionsChanged": "audit.events.rolePermissionsChanged",
  "Roles.ClaimsChanged": "audit.events.roleClaimsChanged",
  "IdentityScope.Created": "audit.events.scopeCreated",
  "IdentityScope.StatusChanged": "audit.events.scopeStatusChanged",
  "SecurityAnomaly.NewLoginIp": "audit.events.newLoginIp",
  "Authentication.DeviceSessionReplaced": "audit.events.deviceSessionReplaced",
};

function toUtcDateTime(value: string): string | undefined {
  if (!value) return undefined;
  const date = new Date(value);
  return Number.isNaN(date.getTime()) ? undefined : date.toISOString();
}

export default function AdminConsole({ mode }: AdminConsoleProps) {
  const router = useRouter();
  const { locale, setLocale } = useAppLocale();
  const { view, setView, activeTenantId, setActiveTenantId } = useWorkspaceView();
  const [session, setSession] = useState<Session | null>(null);
  const [profile, setProfile] = useState<AccountProfile | null>(null);
  const [loading, setLoading] = useState(true);
  const [busy, setBusy] = useState(false);
  const [email, setEmail] = useState("");
  const [pendingCredentials, setPendingCredentials] = useState<Omit<LoginRequest, "twoFactorCode"> | null>(null);
  const [requiresTwoFactor, setRequiresTwoFactor] = useState(false);
  const t = useTranslations("login");
  const a = useTranslations("admin");
  const [users, setUsers] = useState<UserPage>(emptyUserPage);
  const [roles, setRoles] = useState<Role[]>([]);
  const [scopes, setScopes] = useState<IdentityScope[]>([]);
  const [scopesLoadedTenantId, setScopesLoadedTenantId] = useState("");
  const [tenantOptions, setTenantOptions] = useState<IdentityTenant[]>([]);
  const [loadedTenantDataKey, setLoadedTenantDataKey] = useState("");
  const [adminAccess, setAdminAccess] = useState<IdentityAdminAccess | null>(null);
  const loadedTenantDataKeyRef = useRef("");
  const [selectedScopeId, setSelectedScopeId] = useState("");
  const [sessions, setSessions] = useState<LoginSession[]>([]);
  const [auditPage, setAuditPage] = useState<SecurityAuditPage>({
    items: [],
    pageIndex: 1,
    pageSize: 50,
    totalCount: 0,
    hasNextPage: false,
    hasPreviousPage: false,
  });
  const [auditPageNumber, setAuditPageNumber] = useState(1);
  const [auditLoading, setAuditLoading] = useState(false);
  const [auditRefresh, setAuditRefresh] = useState(0);
  const [auditEventType, setAuditEventType] = useState("");
  const [auditOutcome, setAuditOutcome] = useState("");
  const [auditFromUtc, setAuditFromUtc] = useState("");
  const [auditToUtc, setAuditToUtc] = useState("");
  const [auditUserName, setAuditUserName] = useState("");
  const auditUserNameQuery = useDeferredValue(auditUserName);
  const [search, setSearch] = useState("");
  const searchQuery = useDeferredValue(search);
  const [page, setPage] = useState(1);
  const [deviceUser, setDeviceUser] = useState<User | null>(null);
  const [selectedUser, setSelectedUser] = useState<User | null>(null);
  const [firstName, setFirstName] = useState("");
  const [lastName, setLastName] = useState("");
  const [selectedRoles, setSelectedRoles] = useState<string[]>([]);
  const [originalScopeRoles, setOriginalScopeRoles] = useState<string[]>([]);
  const [active, setActive] = useState(true);
  const [newUserDraft, setNewUserDraft] = useState<NewUserDraft | null>(null);
  const [roleDraft, setRoleDraft] = useState<RoleDraft | null>(null);
  const [claimTarget, setClaimTarget] = useState<ClaimTarget | null>(null);
  const [identityClaims, setIdentityClaims] = useState<IdentityClaim[]>([]);
  const [claimType, setClaimType] = useState("");
  const [claimValue, setClaimValue] = useState("");
  const [loadingClaims, setLoadingClaims] = useState(false);
  const [notice, setNotice] = useState("");
  const [error, setError] = useState("");

  useEffect(() => {
    const expired = () => {
      setSession({ authenticated: false, email: "" });
      setProfile(null);
    };
    window.addEventListener("identity-session-expired", expired);
    return () => window.removeEventListener("identity-session-expired", expired);
  }, []);

  useEffect(() => {
    let mounted = true;
    getSession().then(async (value) => {
      if (mounted) {
        setSession(value);
        setEmail(value.email);
        if (value.authenticated) {
          try {
            const currentProfile = await getCurrentUserProfile();
            if (!mounted) return;
            setProfile(currentProfile);
            if (!currentProfile.isAdministrator) setActiveTenantId(currentProfile.tenantId);
          } catch (cause) {
            if (mounted) {
              if (cause instanceof IdentityApiError && cause.status === 401)
                setSession({ authenticated: false, email: "" });
              else setError(cause instanceof Error ? cause.message : a("security.profileError"));
            }
          }
        } else {
          setProfile(null);
        }
        if (mode === "login" && window.sessionStorage.getItem("identity-password-changed") === "1") {
          window.sessionStorage.removeItem("identity-password-changed");
          setNotice(t("passwordChangedSignIn"));
        }
        if (mode === "login" && window.sessionStorage.getItem("identity-logout-warning")) {
          setNotice(window.sessionStorage.getItem("identity-logout-warning")!);
          window.sessionStorage.removeItem("identity-logout-warning");
        }
      }
    }).catch(() => {
      if (mounted) setSession({ authenticated: false, email: "" });
    }).finally(() => {
      if (mounted) setLoading(false);
    });
    return () => { mounted = false; };
  }, [a, mode, setActiveTenantId, t]);

  useEffect(() => {
    if (loading) return;
    if (mode === "login" && session?.authenticated) {
      router.replace("/dashboard");
    } else if (mode !== "login" && !session?.authenticated) {
      router.replace("/login");
    }
  }, [loading, mode, router, session?.authenticated]);

  useEffect(() => {
    if (!session?.authenticated || !profile?.isAdministrator || mode !== "dashboard") return;
    let mounted = true;
    listIdentityTenants()
      .then((items) => {
        if (!mounted) return;
        setTenantOptions(items);
        if (!items.some((tenant) => tenant.id === activeTenantId)) {
          setActiveTenantId(items[0]?.id ?? "");
        }
      })
      .catch((cause: unknown) => {
        if (!mounted) return;
        setTenantOptions([]);
        setError(cause instanceof Error ? cause.message : a("tenants.loadError"));
      });
    return () => { mounted = false; };
  }, [a, activeTenantId, mode, profile?.isAdministrator, session?.authenticated, setActiveTenantId]);

  useEffect(() => {
    if (!session?.authenticated || mode !== "dashboard") return;
    let mounted = true;
    void listIdentityScopes().then((scopeList) => {
      if (!mounted) return;
      setScopes(scopeList);
      setScopesLoadedTenantId(activeTenantId);
      setSelectedScopeId((current) => scopeList.some((scope) => scope.id === current)
        ? current
        : scopeList.find((scope) => scope.scopeType === 1)?.id ?? "");
    }).catch((cause: unknown) => {
      if (mounted) {
        setScopes([]);
        setScopesLoadedTenantId(activeTenantId);
        setError(cause instanceof Error ? cause.message : a("scope.loadError"));
      }
    });
    void listSessions().then((loginSessions) => {
      if (mounted) setSessions(loginSessions);
    }).catch((cause: unknown) => {
      if (mounted) setError(cause instanceof Error ? cause.message : a("sessions.loadError"));
    });
    return () => { mounted = false; };
  }, [a, mode, session?.authenticated, activeTenantId]);

  useEffect(() => {
    if (!session?.authenticated || mode !== "dashboard" || !activeTenantId) return;
    const scopeId = selectedScopeId || activeTenantId;
    let mounted = true;
    getIdentityAdminAccess(scopeId)
      .then((access) => {
        if (!mounted) return;
        setAdminAccess(access);
        setUsers(emptyUserPage);
        setRoles([]);
        setError("");
        setView((current) => (
          (!access.canViewUsers && current === "users")
          || (!access.canViewRoles && current === "roles")
          || ((!access.canManageRolePermissions || !access.canViewRoles) && current === "permissions")
          || (!access.canViewAudit && current === "audit")
          ? "sessions"
          : current
        ));
      })
      .catch((cause: unknown) => {
        if (mounted) {
          setError(cause instanceof Error ? cause.message : a("scope.accessLoadError"));
          setView((current) => (
            current === "users" || current === "roles" || current === "permissions" || current === "audit" || current === "tenants"
              ? "sessions"
              : current
          ));
        }
      })
    return () => { mounted = false; };
  }, [a, activeTenantId, mode, selectedScopeId, session?.authenticated, setView]);

  useEffect(() => {
    if (!session?.authenticated || mode !== "dashboard"
      || !adminAccess || adminAccess.tenantId !== activeTenantId
      || adminAccess.scopeId !== (selectedScopeId || activeTenantId))
      return;

    let mounted = true;
    const requests: Promise<unknown>[] = [];
    if (adminAccess.canViewUsers) {
      requests.push(listUsers(searchQuery, page, 25, adminAccess.scopeId).then((userPage) => {
        if (mounted) setUsers(userPage);
      }).catch((cause: unknown) => {
        if (mounted) setError(cause instanceof Error ? cause.message : a("notice.usersLoadError"));
      }));
    }
    if (adminAccess.canViewRoles) {
      requests.push(listRoles(adminAccess.scopeId).then((roleList) => {
        if (mounted) setRoles(roleList);
      }).catch((cause: unknown) => {
        if (mounted) setError(cause instanceof Error ? cause.message : a("roles.loadError"));
      }));
    }

    const dataKey = `${session.userId ?? ""}:${activeTenantId}:${adminAccess.scopeId}`;
    if (loadedTenantDataKeyRef.current !== dataKey) {
      loadedTenantDataKeyRef.current = dataKey;
      setSelectedUser(null);
      setDeviceUser(null);
      setSelectedRoles([]);
      setOriginalScopeRoles([]);
      setUsers(emptyUserPage);
      setRoles([]);
      setNewUserDraft(null);
      setRoleDraft(null);

      setAuditPage({ items: [], pageIndex: 1, pageSize: 50, totalCount: 0, hasNextPage: false, hasPreviousPage: false });
      setAuditPageNumber(1);
    }
    void Promise.allSettled(requests).then(() => {
      if (mounted) setLoadedTenantDataKey(dataKey);
    });
    return () => { mounted = false; };
  }, [a, activeTenantId, adminAccess, mode, page, searchQuery, selectedScopeId, session?.authenticated, session?.userId]);

  useEffect(() => {
    if (!session?.authenticated || mode !== "dashboard" || view !== "audit"
      || !adminAccess?.canViewAudit || adminAccess.tenantId !== activeTenantId
      || adminAccess.scopeId !== (selectedScopeId || activeTenantId)) return;
    let mounted = true;
    getSecurityAuditPage(auditPageNumber, {
      eventType: auditEventType,
      outcome: auditOutcome,
      fromUtc: toUtcDateTime(auditFromUtc),
      toUtc: toUtcDateTime(auditToUtc),
      userName: auditUserNameQuery.trim() || undefined,
    }, selectedScopeId || activeTenantId)
      .then(result => { if (mounted) { setAuditPage(result); setError(""); } })
      .catch(cause => { if (mounted) setError(cause instanceof Error ? cause.message : a("audit.loadError")); })
      .finally(() => { if (mounted) setAuditLoading(false); });
    return () => { mounted = false; };
  }, [a, activeTenantId, adminAccess, auditEventType, auditFromUtc, auditOutcome, auditPageNumber, auditRefresh, auditToUtc, auditUserNameQuery, mode, selectedScopeId, session?.authenticated, view]);

  async function loadUsers() {
    if (!adminAccess?.canViewUsers) return;
    setUsers(await listUsers(searchQuery, page, 25, selectedScopeId || activeTenantId));
  }

  async function loadRoles() {
    if (!adminAccess?.canViewRoles) return;
    setRoles(await listRoles(selectedScopeId || activeTenantId));
  }

  async function loadSessions() {
    setSessions(await listSessions());
  }

  async function refreshSessions() {
    setError("");
    try { await loadSessions(); }
    catch (cause) { setError(cause instanceof Error ? cause.message : "Could not load sign-in sessions."); }
  }

  async function signIn(credentials: LoginRequest) {
    setBusy(true); setError(""); setNotice("");
    try {
      const result = await authenticate(credentials);
      setLoading(true);
      setSession({ authenticated: true, email: result.email, userId: result.userId });
      setPendingCredentials(null); setRequiresTwoFactor(false);
      router.replace("/dashboard");
    } catch (cause) {
      if (cause instanceof IdentityApiError && cause.code === "Identity.Authentication.TwoFactorRequired") {
        setEmail(credentials.email);
        setPendingCredentials({ email: credentials.email, password: credentials.password });
        setRequiresTwoFactor(true);
        setNotice(t("twoFactorPrompt"));
        return;
      }
      if (cause instanceof IdentityApiError && cause.code === "Identity.Authentication.InvalidCredentials") {
        setError(requiresTwoFactor ? t("invalidCode") : t("invalidCredentials"));
      } else {
        setError(cause instanceof Error ? cause.message : t("genericError"));
      }
    }
    finally { setBusy(false); }
  }

  function submitCredentials(credentials: LoginCredentialsValues) {
    void signIn(credentials);
  }

  function submitTwoFactor({ code }: TwoFactorCodeValues) {
    if (!pendingCredentials) {
      setError(t("genericError"));
      setRequiresTwoFactor(false);
      return;
    }
    void signIn({ ...pendingCredentials, twoFactorCode: code });
  }

  function backToCredentials() {
    setPendingCredentials(null);
    setRequiresTwoFactor(false);
    setNotice("");
    setError("");
  }

  async function signOut() {
    if (busy) return;
    setBusy(true);
    try { await endSession(); }
    catch (cause) {
      window.sessionStorage.setItem("identity-logout-warning", cause instanceof Error ? cause.message : a("sessions.revokeError"));
    }
    finally {
      setSession({ authenticated: false, email: "" });
      setUsers(emptyUserPage); setRoles([]); setSessions([]); setBusy(false);
      router.replace("/login");
    }
  }

  async function handlePasswordChanged() {
    window.sessionStorage.setItem("identity-password-changed", "1");
    try { await endSession(); }
    finally {
      setSession({ authenticated: false, email: "" });
      setUsers(emptyUserPage); setRoles([]); setSessions([]);
      router.replace("/login");
    }
  }

  function navigateToView(nextView: WorkspaceView) {
    if (nextView === "audit") setAuditLoading(true);
    setView(nextView);
    router.push(nextView === "security" ? "/settings" : nextView === "permissions" ? "/permissions" : "/dashboard");
  }

  async function editUser(user: User) {
    if (!selectedScopeId || (!adminAccess?.canAssignUsers && !adminAccess?.canUpdateUsers)) {
      setError(a("scope.loadError"));
      return;
    }
    setSelectedUser(user); setFirstName(user.firstName); setLastName(user.lastName);
    setSelectedRoles([]); setOriginalScopeRoles([]); setActive(user.isActive); setError("");
    if (!adminAccess.canAssignUsers) return;
    try {
      const assignedRoles = await listUserRoles(user.id, selectedScopeId);
      setSelectedRoles(assignedRoles);
      setOriginalScopeRoles(assignedRoles);
    } catch (cause) {
      setSelectedUser(null);
      setError(cause instanceof Error ? cause.message : a("notice.usersLoadError"));
    }
  }

  async function saveUser(event: FormEvent<HTMLFormElement>) {
    event.preventDefault(); if (!selectedUser) return;
    setBusy(true); setError("");
    try {
      if (adminAccess?.canUpdateUsers)
        await updateUser(selectedUser.id, { firstName, lastName }, selectedScopeId);
      if (adminAccess?.canAssignUsers
        && (selectedRoles.length !== originalScopeRoles.length || selectedRoles.some(role => !originalScopeRoles.includes(role)))) {
        await updateUserRoles(selectedUser.id, selectedScopeId, { roles: selectedRoles });
      }
      if (adminAccess?.canUpdateUsers && active !== selectedUser.isActive)
        await updateUserStatus(selectedUser.id, { isActive: active }, selectedScopeId);
      setSelectedUser(null); setNotice(a("users.saveSuccess")); await loadUsers(); await loadRoles();
    } catch (cause) { setError(cause instanceof Error ? cause.message : a("users.saveError")); }
    finally { setBusy(false); }
  }

  async function resendConfirmationEmail() {
    if (!selectedUser) return;
    setBusy(true); setError("");
    try {
      await requestEmailConfirmation(selectedUser.email);
      setNotice(a("users.confirmationRequested"));
    } catch {
      setError(a("users.confirmationRequestError"));
    } finally { setBusy(false); }
  }

  async function createUser(event: FormEvent<HTMLFormElement>) {
    event.preventDefault(); if (!newUserDraft) return;
    setBusy(true); setError("");
    try {
      await createUserAccount(newUserDraft, selectedScopeId || activeTenantId);
      setNewUserDraft(null); setNotice(a("users.createSuccess"));
      await loadUsers(); await loadRoles();
    } catch (cause) { setError(cause instanceof Error ? cause.message : a("users.createError")); }
    finally { setBusy(false); }
  }

  async function saveRole(event: FormEvent<HTMLFormElement>) {
    event.preventDefault(); if (!roleDraft) return;
    setBusy(true); setError(""); const editing = Boolean(roleDraft.id);
    try {
      await persistRole(editing ? roleDraft.id : undefined, {
        name: roleDraft.name,
        roleCode: roleDraft.roleCode,
        description: roleDraft.description,
      }, selectedScopeId || activeTenantId);
      setRoleDraft(null); setNotice(editing ? a("roles.updated") : a("roles.created")); await loadRoles();
    } catch (cause) { setError(cause instanceof Error ? cause.message : a("roles.saveError")); }
    finally { setBusy(false); }
  }

  async function manageClaims(target: ClaimTarget) {
    if (!selectedScopeId) {
      setError(a("scope.loadError"));
      return;
    }
    setClaimTarget(target);
    setIdentityClaims([]);
    setClaimType("");
    setClaimValue("");
    setLoadingClaims(true);
    setError("");
    try {
      setIdentityClaims(target.kind === "user"
        ? await listUserClaims(target.id, selectedScopeId)
        : await listRoleClaims(target.id, selectedScopeId));
    } catch (cause) {
      setClaimTarget(null);
      setError(cause instanceof Error ? cause.message : a("claims.loadError"));
    } finally {
      setLoadingClaims(false);
    }
  }

  async function addIdentityClaim(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!claimTarget) return;
    if (!selectedScopeId) return;
    const claim = { type: claimType.trim(), value: claimValue };
    setBusy(true);
    setError("");
    try {
      if (claimTarget.kind === "user") await addUserClaim(claimTarget.id, selectedScopeId, claim);
      else await addRoleClaim(claimTarget.id, selectedScopeId, claim);
      setIdentityClaims(current => current.some(item => item.type === claim.type && item.value === claim.value)
        ? current
        : [...current, claim].sort((left, right) => left.type.localeCompare(right.type) || left.value.localeCompare(right.value)));
      setClaimType("");
      setClaimValue("");
      setNotice(a("claims.added"));
    } catch (cause) {
      setError(cause instanceof Error ? cause.message : a("claims.saveError"));
    } finally {
      setBusy(false);
    }
  }

  async function deleteIdentityClaim(claim: IdentityClaim) {
    if (!claimTarget) return;
    if (!selectedScopeId) return;
    setBusy(true);
    setError("");
    try {
      if (claimTarget.kind === "user") await removeUserClaim(claimTarget.id, selectedScopeId, claim);
      else await removeRoleClaim(claimTarget.id, selectedScopeId, claim);
      setIdentityClaims(current => current.filter(item => item.type !== claim.type || item.value !== claim.value));
      setNotice(a("claims.deleted"));
    } catch (cause) {
      setError(cause instanceof Error ? cause.message : a("claims.deleteError"));
    } finally {
      setBusy(false);
    }
  }


  async function deleteRole(role: Role) {
    if (role.name === "Administrator" || role.userCount > 0) return;
    setBusy(true); setError("");
    try { await removeRole(role.id, selectedScopeId || activeTenantId); setNotice(a("roles.deleted")); await loadRoles(); }
    catch (cause) { setError(cause instanceof Error ? cause.message : a("roles.deleteError")); }
    finally { setBusy(false); }
  }

  async function revokeSession(session: LoginSession) {
    if (session.isCurrent || !window.confirm(a("sessions.confirmRevoke"))) return;
    setBusy(true); setError("");
    try {
      await revokeLoginSession(session.sessionId);
      setNotice(a("sessions.revoked")); await loadSessions();
    } catch (cause) { setError(cause instanceof Error ? cause.message : a("sessions.revokeError")); }
    finally { setBusy(false); }
  }

  async function revokeOtherSessions() {
    if (!sessions.some((session) => !session.isCurrent) || !window.confirm(a("sessions.confirmOthers"))) return;
    setBusy(true); setError("");
    try {
      await revokeOtherLoginSessions();
      setNotice(a("sessions.othersRevoked")); await loadSessions();
    } catch (cause) { setError(cause instanceof Error ? cause.message : a("sessions.revokeOthersError")); }
    finally { setBusy(false); }
  }

  const pageCount = Math.max(1, Math.ceil(users.totalCount / users.pageSize));
  if (!loading && session?.authenticated && !profile) {
    return (
      <main className="boot-screen" role="alert">
        <span className="brand-mark"><ShieldCheck size={20} /></span>
        <p>{error || a("security.profileError")}</p>
        <button className="secondary-button" onClick={() => void signOut()} disabled={busy}>{a("navigation.signOut")}</button>
      </main>
    );
  }
  const currentAccess = adminAccess?.tenantId === activeTenantId
    && adminAccess.scopeId === (selectedScopeId || activeTenantId)
    ? adminAccess
    : null;
  const accessLoading = mode === "dashboard" && Boolean(session?.authenticated) && !currentAccess && !error;
  const activeDataKey = `${session?.userId ?? ""}:${activeTenantId}:${currentAccess?.scopeId ?? ""}`;
  const identityDataReady = loadedTenantDataKey === activeDataKey;
  if (loading) return <main className="boot-screen"><span className="brand-mark"><ShieldCheck size={20} /></span><p>{a("common.loadingWorkspace")}</p></main>;

  if (!session?.authenticated && mode === "login") {
    return (
      <LoginScreen
        busy={busy}
        email={email}
        error={error}
        notice={notice}
        requiresTwoFactor={requiresTwoFactor}
        onCredentialsSubmit={submitCredentials}
        onTwoFactorSubmit={submitTwoFactor}
        onChangeAccount={backToCredentials}
      />
    );
  }

  if (!session?.authenticated || (mode === "login" && session.authenticated)) {
    return <main className="boot-screen"><span className="brand-mark"><ShieldCheck size={20} /></span><p>{a("common.loadingWorkspace")}</p></main>;
  }

  return (
    <main className="workspace-shell workspace-next technology-theme">
      <aside className="sidebar">
        <div className="brand-lockup sidebar-brand"><span className="brand-mark"><ShieldCheck size={19} /></span><span>Store<span className="brand-light">Management</span></span></div>
        <p className="workspace-label">{a("workspace")}</p>
        <nav className="side-nav" aria-label={a("navigation.label")}>
          {currentAccess?.canViewUsers && <button aria-current={view === "users" ? "page" : undefined} className={view === "users" ? "nav-item active" : "nav-item"} onClick={() => navigateToView("users")}><UsersRound size={17} /><span>{a("navigation.users")}</span><small>{users.totalCount}</small></button>}
          {currentAccess?.canViewRoles && <button aria-current={view === "roles" ? "page" : undefined} className={view === "roles" ? "nav-item active" : "nav-item"} onClick={() => navigateToView("roles")}><Shield size={17} /><span>{a("navigation.roles")}</span><small>{roles.length}</small></button>}
          {currentAccess?.canManageRolePermissions && currentAccess.canViewRoles && <button aria-current={view === "permissions" ? "page" : undefined} className={view === "permissions" ? "nav-item active" : "nav-item"} onClick={() => navigateToView("permissions")}><KeyRound size={17} /><span>{a("permissions.pageTitle")}</span></button>}
          <button aria-current={view === "sessions" ? "page" : undefined} className={view === "sessions" ? "nav-item active" : "nav-item"} onClick={() => navigateToView("sessions")}><Monitor size={17} /><span>{a("navigation.sessions")}</span><small>{sessions.length}</small></button>
          <button aria-current={view === "security" ? "page" : undefined} className={view === "security" ? "nav-item active" : "nav-item"} onClick={() => navigateToView("security")}><Settings2 size={17} /><span>{a("navigation.security")}</span></button>
          {currentAccess?.canViewAudit && <button aria-current={view === "audit" ? "page" : undefined} className={view === "audit" ? "nav-item active" : "nav-item"} onClick={() => navigateToView("audit")}><ClipboardList size={17} /><span>{a("navigation.audit")}</span></button>}
          {profile?.isAdministrator && <button aria-current={view === "tenants" ? "page" : undefined} className={view === "tenants" ? "nav-item active" : "nav-item"} onClick={() => navigateToView("tenants")}><Building2 size={17} /><span>{a("navigation.tenants")}</span></button>}
        </nav>
        <div className="sidebar-bottom"><div className="policy-note"><ShieldCheck size={16} /><span><strong>{a("navigation.protected")}</strong><small>{a("navigation.policy")}</small></span></div><div className="account-row"><span className="account-avatar">{session.email.slice(0, 1).toUpperCase()}</span><span className="account-email">{session.email}</span><button className="icon-button sidebar-signout" title={a("navigation.signOut")} aria-label={a("navigation.signOut")} onClick={signOut} disabled={busy}><LogOut size={16} /></button></div></div>
      </aside>
      <section className="main-panel">
        <header className="topbar">
          <div className="breadcrumb"><span>{a("identity")}</span><i>/</i><strong>{view === "users" ? a("users.breadcrumb") : view === "roles" ? a("navigation.roles") : view === "permissions" ? a("permissions.pageTitle") : view === "sessions" ? a("navigation.sessions") : view === "audit" ? a("audit.title") : view === "tenants" ? a("tenants.title") : a("security.title")}</strong></div>
          <div className="topbar-meta">
            <div className="admin-language" role="group" aria-label={a("language")}><Globe2 size={15} /><button type="button" aria-label={a("vietnamese")} aria-pressed={locale === "vi"} onClick={() => setLocale("vi")}>VI</button><span>/</span><button type="button" aria-label={a("english")} aria-pressed={locale === "en"} onClick={() => setLocale("en")}>EN</button></div>
            {tenantOptions.length > 1 && <label className="scope-picker"><span>{a("tenants.activeTenant")}</span><select aria-label={a("tenants.activeTenant")} value={tenantOptions.some(tenant => tenant.id === activeTenantId) ? activeTenantId : tenantOptions[0].id} onChange={(event) => {
              setActiveTenantId(event.target.value);
              setSelectedScopeId("");
              setDeviceUser(null);
              setSelectedUser(null);

            }}>{tenantOptions.map(tenant => <option key={tenant.id} value={tenant.id}>{tenant.name}</option>)}</select></label>}
            {view !== "tenants" && scopesLoadedTenantId === activeTenantId && scopes.length > 1 && <label className="scope-picker"><span>{a("scope.label")}</span><select aria-label={a("scope.label")} value={scopes.some(scope => scope.id === selectedScopeId) ? selectedScopeId : activeTenantId} onChange={(event) => {
              setSelectedScopeId(event.target.value);
              setDeviceUser(null);
              setSelectedUser(null);

              setNewUserDraft(null);
              setRoleDraft(null);
            }}>{scopes.map(scope => <option key={scope.id} value={scope.id}>{`${scope.scopeType === 1 ? "" : scope.scopeType === 2 ? "— " : "—— "}${scope.name}`}</option>)}</select></label>}
            <span className="console-label"><span className="online-dot" /> {a("console")}</span><span className="topbar-email">{session.email}</span><button className="icon-button mobile-signout" title={a("navigation.signOut")} aria-label={a("navigation.signOut")} onClick={signOut}><LogOut size={16} /></button>
          </div>
        </header>
        <div className="content-area">
          <section className="workspace-welcome">
            <div><p>{a("workspace")}</p><h2>{tenantOptions.find(tenant => tenant.id === activeTenantId)?.name ?? a("identity")}</h2><span>{a("navigation.protected")}</span></div>
            <div className="workspace-shortcuts">
              {currentAccess?.canViewUsers && <button onClick={() => navigateToView("users")}><UsersRound size={21} /><strong>{a("navigation.users")}</strong><ArrowRight size={16} /></button>}
              {currentAccess?.canViewRoles && <button onClick={() => navigateToView("roles")}><Shield size={21} /><strong>{a("navigation.roles")}</strong><ArrowRight size={16} /></button>}
              <button onClick={() => navigateToView("security")}><KeyRound size={21} /><strong>{a("navigation.security")}</strong><ArrowRight size={16} /></button>
            </div>
          </section>
          {(notice || error) && <div className={error ? "notice notice-error" : "notice notice-success"} role={error ? "alert" : "status"}><span>{error || notice}</span><button aria-label={a("common.dismiss")} onClick={() => { setError(""); setNotice(""); }}><X size={15} /></button></div>}
          {view === "tenants" && profile?.isAdministrator ? <TenantManagement /> : view !== "security" && view !== "sessions" && !identityDataReady ? (
            <div className="tenant-empty" role="status">{accessLoading ? a("common.loadingWorkspace") : error || a("scope.accessDenied")}</div>
          ) : view === "permissions" ? currentAccess?.canManageRolePermissions && currentAccess.canViewRoles ? <PermissionsPage key={`${activeTenantId}:${selectedScopeId}:${roles.map(role => role.id).join()}`} roles={roles} scopeId={selectedScopeId || activeTenantId} /> : <div className="empty-state">{a("scope.accessDenied")}</div> : view === "users" ? <>
            <div className="page-heading"><div><p className="eyebrow">{a("users.eyebrow")}</p><h1>{a("users.title")}</h1><p className="subtle">{a("users.description")}</p></div><div className="heading-actions"><div className="total-stat"><strong>{users.totalCount.toLocaleString()}</strong><span>{a("users.accounts")}</span></div>{currentAccess?.canCreateUsers && <button className="primary-button" onClick={() => setNewUserDraft({ email: "", firstName: "", lastName: "", password: "", roles: [] })}><Plus size={15} /> {a("users.new")}</button>}</div></div>
            <div className="toolbar"><label className="search-field"><Search size={16} /><input aria-label={a("users.searchLabel")} placeholder={a("users.search")} value={search} onChange={(event) => { setSearch(event.target.value); setPage(1); }} /></label><div className="live-label"><span className="online-dot" /> {a("users.live")} <button className="refresh-button" onClick={() => void loadUsers()}>{a("common.refresh")}</button></div></div>
            <div className="table-shell"><div className="table-scroll"><table className="user-table"><thead><tr><th>{a("users.member")}</th><th>{a("users.roleAccess")}</th><th>{a("users.security")}</th><th>{a("users.status")}</th><th /></tr></thead><tbody>
              {users.items.map((user, index) => <tr key={user.id} style={{ "--row": index } as React.CSSProperties}>
                <td><div className="member"><span className={`member-avatar avatar-${index % 5}`}>{`${user.firstName.slice(0, 1)}${user.lastName.slice(0, 1)}`.toUpperCase()}</span><span><strong>{user.fullName}</strong><small>{user.email}</small></span></div></td>
                <td><div className="role-list">{user.roles.length ? user.roles.slice(0, 2).map((role) => <span className="role-chip" key={role}>{role}</span>) : <span className="empty-role">{a("users.noRole")}</span>}{user.roles.length > 2 && <span className="role-chip extra-chip">+{user.roles.length - 2}</span>}</div></td>
                <td><div className="security-list"><span className={user.emailConfirmed ? "verified" : "unverified"}>{user.emailConfirmed ? a("users.emailVerified") : a("users.emailPending")}</span><small>{user.twoFactorEnabled ? a("users.twoFactorEnabled") : a("users.twoFactorOff")}</small></div></td>
                <td><span className={user.isActive ? "status active" : "status inactive"}><i />{user.isActive ? a("users.active") : a("users.inactive")}</span></td>
                <td>{currentAccess?.canViewSessions && <button className="icon-button" aria-label={a("sessions.userDevices")} title={a("sessions.userDevices")} onClick={() => setDeviceUser(user)}><Monitor size={16} /></button>}{(currentAccess?.canUpdateUsers || currentAccess?.canAssignUsers) && <button className="icon-button" title={a("users.editUser", { name: user.fullName })} aria-label={a("users.editUser", { name: user.fullName })} onClick={() => void editUser(user)}><Pencil size={15} /></button>}</td>
              </tr>)}
              {!users.items.length && <tr><td colSpan={5}><div className="empty-state"><UsersRound size={21} /><strong>{a("users.emptyTitle")}</strong><span>{a("users.emptyDescription")}</span></div></td></tr>}
            </tbody></table></div><footer className="table-footer"><span>{a("common.showing")} <strong>{users.totalCount ? (page - 1) * users.pageSize + 1 : 0}–{Math.min(page * users.pageSize, users.totalCount)}</strong> {a("common.of")} <strong>{users.totalCount}</strong></span><div className="pager"><button className="icon-button" aria-label={a("common.previous")} disabled={page <= 1} onClick={() => setPage((value) => Math.max(1, value - 1))}><ArrowLeft size={15} /></button><span>{a("common.page")} <strong>{page}</strong> {a("common.of")} {pageCount}</span><button className="icon-button" aria-label={a("common.next")} disabled={page >= pageCount} onClick={() => setPage((value) => Math.min(pageCount, value + 1))}><ArrowRight size={15} /></button></div></footer></div>
          </> : view === "roles" ? <>
            <div className="page-heading"><div><p className="eyebrow">{a("roles.eyebrow")}</p><h1>{a("roles.title")}</h1><p className="subtle">{a("roles.description")}</p></div>{currentAccess?.canCreateRoles && <button className="primary-button" onClick={() => setRoleDraft({ name: "", roleCode: "", description: "" })}><Plus size={16} /> {a("roles.new")}</button>}</div>
            <div className="role-summary"><span className="role-summary-icon"><Shield size={17} /></span><span><strong>{a("roles.defined", { count: roles.length })}</strong><small>{a("roles.protected")}</small></span><span className="summary-end"><ShieldCheck size={16} /> {a("roles.access")}</span></div>
            <div className="role-grid">{roles.map((role) => <article className="role-card" key={role.id}><div className="role-card-head"><span className="role-card-icon"><KeyRound size={16} /></span><span className="role-code">{role.roleCode}</span>{currentAccess?.canUpdateRoles && <button className="icon-button" title={a("roles.editTitle") + ` ${role.name}`} aria-label={a("roles.editTitle") + ` ${role.name}`} onClick={() => setRoleDraft({ id: role.id, name: role.name, roleCode: role.roleCode, description: role.description })}><Pencil size={15} /></button>}</div><h2>{role.name}</h2><p>{role.description}</p><footer><span><UsersRound size={14} />{role.userCount} {role.userCount === 1 ? a("roles.member") : a("roles.members")}</span><span className="role-card-actions">{role.name !== "Administrator" && currentAccess?.canManageRolePermissions && <><button className="text-button" onClick={() => navigateToView("permissions")}>{a("permissions.manage")}</button><button className="text-button" onClick={() => void manageClaims({ kind: "role", id: role.id, name: role.name })}>{a("claims.manage")}</button></>}{currentAccess?.canDeleteRoles && <button className="text-button" disabled={role.name === "Administrator" || role.userCount > 0} onClick={() => void deleteRole(role)}>{a("roles.delete")}</button>}</span></footer></article>)}{!roles.length && <div className="empty-state role-empty"><Shield size={21} /><strong>{a("roles.emptyTitle")}</strong><span>{a("roles.emptyDescription")}</span></div>}</div>
          </> : view === "sessions" ? <>
            <div className="page-heading"><div><p className="eyebrow">{a("sessions.eyebrow")}</p><h1>{a("sessions.title")}</h1><p className="subtle">{a("sessions.description")}</p><p className="subtle">{a("sessions.limitPolicy")}</p></div><div className="heading-actions"><button className="secondary-button" onClick={() => void refreshSessions()} disabled={busy}>{a("common.refresh")}</button><button className="primary-button" onClick={() => void revokeOtherSessions()} disabled={busy || !sessions.some((session) => !session.isCurrent)}>{a("sessions.signOutOthers")}</button></div></div>
            <div className="table-shell"><div className="table-scroll"><table className="user-table session-table"><thead><tr><th>{a("sessions.device")}</th><th>{a("sessions.ipAddress")}</th><th>{a("sessions.firstSignIn")}</th><th>{a("sessions.lastActive")}</th><th>{a("sessions.expires")}</th><th>{a("sessions.status")}</th><th /></tr></thead><tbody>
              {sessions.map((loginSession) => <tr key={loginSession.sessionId}>
                <td><div className="member"><span className="member-avatar avatar-3">{loginSession.deviceCategory === "Phone" ? <Smartphone size={16} /> : <Monitor size={16} />}</span><span><strong>{getSessionDeviceLabel(loginSession.userAgent, a("common.unknown")).browser}</strong><small>{a(loginSession.deviceCategory === "Phone" ? "sessions.phone" : "sessions.computer")} · {getSessionDeviceLabel(loginSession.userAgent, a("common.unknown")).operatingSystem} · {loginSession.isCurrent ? a("sessions.thisDevice") : a("sessions.otherDevice")}</small></span></div></td>
                <td>{loginSession.ipAddress || a("common.unavailable")}</td>
                <td>{formatSessionDate(loginSession.createdAtUtc, locale, a("common.unknown"))}</td>
                <td>{formatSessionDate(loginSession.lastActiveAtUtc, locale, a("common.unknown"))}</td>
                <td>{formatSessionDate(loginSession.expiresAtUtc, locale, a("common.unknown"))}</td>
                <td><span className={loginSession.isCurrent ? "status active" : "status inactive"}><i />{loginSession.isCurrent ? a("sessions.current") : a("sessions.active")}</span></td>
                <td>{!loginSession.isCurrent && <button className="text-button" onClick={() => void revokeSession(loginSession)} disabled={busy}>{a("sessions.revoke")}</button>}</td>
              </tr>)}
              {!sessions.length && <tr><td colSpan={7}><div className="empty-state"><Monitor size={21} /><strong>{a("sessions.emptyTitle")}</strong><span>{a("sessions.emptyDescription")}</span></div></td></tr>}
            </tbody></table></div></div>
          </> : view === "audit" ? <>
            <div className="page-heading"><div><p className="eyebrow">{a("audit.eyebrow")}</p><h1>{a("audit.title")}</h1><p className="subtle">{a("audit.description")}</p></div><div className="heading-actions"><span className="total-stat"><strong>{auditPage.totalCount.toLocaleString()}</strong><span>{a("audit.eventsCount")}</span></span><button className="secondary-button" onClick={() => { setAuditLoading(true); setAuditRefresh(value => value + 1); }} disabled={auditLoading}>{a("common.refresh")}</button></div></div>
            <div className="audit-filters"><label>{a("audit.filterEvent")}<select value={auditEventType} onChange={event => { setAuditLoading(true); setAuditPageNumber(1); setAuditEventType(event.target.value); }}><option value="">{a("audit.allEvents")}</option>{Object.keys(securityAuditEventLabels).filter(eventType => eventType !== "unknown").map(eventType => <option value={eventType} key={eventType}>{a(securityAuditEventLabels[eventType])}</option>)}</select></label><label>{a("audit.filterOutcome")}<select value={auditOutcome} onChange={event => { setAuditLoading(true); setAuditPageNumber(1); setAuditOutcome(event.target.value); }}><option value="">{a("audit.allOutcomes")}</option><option value="Succeeded">{a("audit.succeeded")}</option><option value="Failed">{a("audit.failed")}</option></select></label><label>{a("audit.filterFrom")}<input type="datetime-local" value={auditFromUtc} onChange={event => { setAuditLoading(true); setAuditPageNumber(1); setAuditFromUtc(event.target.value); }} /></label><label>{a("audit.filterTo")}<input type="datetime-local" value={auditToUtc} onChange={event => { setAuditLoading(true); setAuditPageNumber(1); setAuditToUtc(event.target.value); }} /></label><label>{a("audit.filterUser")}<input type="search" value={auditUserName} placeholder={a("audit.userPlaceholder")} onChange={event => { setAuditLoading(true); setAuditPageNumber(1); setAuditUserName(event.target.value); }} /></label></div>
            <div className="table-shell"><div className="table-scroll"><table className="user-table audit-table"><thead><tr><th>{a("audit.event")}</th><th>{a("audit.outcome")}</th><th>{a("audit.time")}</th><th>{a("audit.actor")}</th><th>{a("audit.subject")}</th><th>{a("audit.ipAddress")}</th></tr></thead><tbody>
              {auditPage.items.map(event => <tr key={event.id}>
                <td><div className="security-list"><strong>{a(securityAuditEventLabels[event.eventType] ?? "audit.events.unknown")}</strong>{event.details && <small>{event.details}</small>}</div></td>
                <td><span className={event.outcome === "Succeeded" ? "status active" : "status inactive"}><i />{event.outcome === "Succeeded" ? a("audit.succeeded") : a("audit.failed")}</span></td>
                <td>{formatSessionDate(event.occurredAtUtc, locale, a("common.unknown"))}</td>
                <td>{event.actorUserName ?? (event.actorUserId ? a("common.unavailable") : a("audit.systemActor"))}</td>
                <td>{event.subjectUserName ?? a("common.unavailable")}</td>
                <td>{event.ipAddress ?? a("common.unavailable")}</td>
              </tr>)}
              {!auditLoading && !auditPage.items.length && <tr><td colSpan={6}><div className="empty-state"><ClipboardList size={21} /><strong>{a("audit.emptyTitle")}</strong><span>{a("audit.emptyDescription")}</span></div></td></tr>}
              {auditLoading && <tr><td colSpan={6}><div className="empty-state">{a("common.loadingWorkspace")}</div></td></tr>}
            </tbody></table></div><footer className="table-footer"><span>{a("common.showing")} <strong>{auditPage.totalCount ? (auditPageNumber - 1) * auditPage.pageSize + 1 : 0}–{Math.min(auditPageNumber * auditPage.pageSize, auditPage.totalCount)}</strong> {a("common.of")} <strong>{auditPage.totalCount}</strong></span><div className="pager"><button className="icon-button" aria-label={a("common.previous")} disabled={auditPageNumber <= 1 || auditLoading} onClick={() => { setAuditLoading(true); setAuditPageNumber(value => Math.max(1, value - 1)); }}><ArrowLeft size={15} /></button><span>{a("common.page")} <strong>{auditPageNumber}</strong> {a("common.of")} {Math.max(1, Math.ceil(auditPage.totalCount / auditPage.pageSize))}</span><button className="icon-button" aria-label={a("common.next")} disabled={auditPageNumber >= Math.max(1, Math.ceil(auditPage.totalCount / auditPage.pageSize)) || auditLoading} onClick={() => { setAuditLoading(true); setAuditPageNumber(value => value + 1); }}><ArrowRight size={15} /></button></div></footer></div>
          </> : <AccountSecurity userId={session.userId} twoFactorEnabled={profile?.twoFactorEnabled ?? null} onPasswordChanged={() => void handlePasswordChanged()} />}
          <footer className="page-footer"><span>STORE MANAGEMENT <i /> {a("footer.service")}</span><span>{a("footer.operations")} <ShieldCheck size={13} /></span></footer>
        </div>
      </section>

      {selectedUser && <div className="modal-backdrop" onMouseDown={(event) => { if (event.target === event.currentTarget) setSelectedUser(null); }}><section className="editor-panel" role="dialog" aria-modal="true" aria-labelledby="user-edit-title">
        <header className="editor-header"><div><p className="eyebrow">{a("users.profile")}</p><h2 id="user-edit-title">{a("users.editTitle")}</h2></div><button className="icon-button" aria-label={a("common.dismiss")} onClick={() => setSelectedUser(null)}><X size={18} /></button></header>
        <div className="editor-user"><span className="member-avatar avatar-2">{`${selectedUser.firstName.slice(0, 1)}${selectedUser.lastName.slice(0, 1)}`.toUpperCase()}</span><span><strong>{selectedUser.email}</strong><small>{selectedUser.emailConfirmed ? a("users.emailVerified") : a("users.emailUnverified")}</small></span></div>
        {currentAccess?.canUpdateUsers && <div className="editor-actions"><button type="button" className="secondary-button" onClick={() => void manageClaims({ kind: "user", id: selectedUser.id, name: selectedUser.email })}>{a("claims.manage")}</button></div>}
        {currentAccess?.canUpdateUsers && !selectedUser.emailConfirmed && <div className="editor-actions"><button type="button" className="secondary-button" onClick={() => void resendConfirmationEmail()} disabled={busy}>{busy ? a("users.sendingConfirmation") : a("users.resendConfirmation")}</button></div>}
        <form className="editor-form" onSubmit={saveUser}><div className="form-pair"><label>{a("users.firstName")}<input value={firstName} maxLength={100} onChange={(event) => setFirstName(event.target.value)} required disabled={!currentAccess?.canUpdateUsers} /></label><label>{a("users.lastName")}<input value={lastName} maxLength={100} onChange={(event) => setLastName(event.target.value)} required disabled={!currentAccess?.canUpdateUsers} /></label></div>
          {currentAccess?.canAssignUsers && <fieldset className="role-picker"><legend>{a("users.roleMembership")}</legend><p>{a("users.roleHelp")}</p>{roles.filter((role) => role.name !== "Administrator" || selectedScopeId === activeTenantId).map((role) => {
            const isAdministrator = role.name === "Administrator";
            const administratorMembershipLocked = isAdministrator
              && (!profile?.isAdministrator || selectedUser.id === session?.userId);
            return <label className="check-row" key={role.id}><input type="checkbox" checked={selectedRoles.includes(role.name)} disabled={administratorMembershipLocked} onChange={(event) => setSelectedRoles(event.target.checked ? [...selectedRoles, role.name] : selectedRoles.filter((item) => item !== role.name))} /><span className="check-box"><Check size={12} /></span><span>{role.name}</span><small>{role.roleCode}</small></label>;
          })}</fieldset>}
          {currentAccess?.canUpdateUsers && <label className="active-row"><span><strong>{a("users.accountActive")}</strong><small>{a("users.inactiveHelp")}</small></span><input type="checkbox" checked={active} onChange={(event) => setActive(event.target.checked)} /><span className="switch" /></label>}
          <div className="editor-actions"><button type="button" className="secondary-button" onClick={() => setSelectedUser(null)}>{a("common.cancel")}</button>{(currentAccess?.canUpdateUsers || currentAccess?.canAssignUsers) && <button className="primary-button" disabled={busy}>{busy ? a("common.saving") : a("common.save")}<Check size={15} /></button>}</div>
        </form>
      </section></div>}

      {newUserDraft && <div className="modal-backdrop" onMouseDown={(event) => { if (event.target === event.currentTarget) setNewUserDraft(null); }}><section className="dialog-panel" role="dialog" aria-modal="true" aria-labelledby="new-user-title">
        <header className="editor-header"><div><p className="eyebrow">{a("users.breadcrumb")}</p><h2 id="new-user-title">{a("users.createTitle")}</h2></div><button className="icon-button" aria-label={a("common.dismiss")} onClick={() => setNewUserDraft(null)}><X size={18} /></button></header>
        <form className="editor-form" onSubmit={createUser}>
          <label>{a("users.emailAddress")}<input type="email" autoComplete="email" value={newUserDraft.email} onChange={(event) => setNewUserDraft({ ...newUserDraft, email: event.target.value })} required placeholder="person@company.com" /></label>
          <div className="form-pair"><label>{a("users.firstName")}<input value={newUserDraft.firstName} maxLength={100} onChange={(event) => setNewUserDraft({ ...newUserDraft, firstName: event.target.value })} required /></label><label>{a("users.lastName")}<input value={newUserDraft.lastName} maxLength={100} onChange={(event) => setNewUserDraft({ ...newUserDraft, lastName: event.target.value })} required /></label></div>
          <label>{a("users.temporaryPassword")}<input type="password" autoComplete="new-password" minLength={8} value={newUserDraft.password} onChange={(event) => setNewUserDraft({ ...newUserDraft, password: event.target.value })} required /><small className="field-hint">{a("users.passwordHelp")}</small></label>
          {currentAccess?.canAssignUsers && <fieldset className="role-picker"><legend>{a("users.initialRoles")} <span className="optional">{a("common.optional")}</span></legend>{roles.filter((role) => role.name !== "Administrator").map((role) => <label className="check-row" key={role.id}><input type="checkbox" checked={newUserDraft.roles.includes(role.name)} onChange={(event) => setNewUserDraft({ ...newUserDraft, roles: event.target.checked ? [...newUserDraft.roles, role.name] : newUserDraft.roles.filter((item) => item !== role.name) })} /><span className="check-box"><Check size={12} /></span><span>{role.name}</span><small>{role.roleCode}</small></label>)}</fieldset>}
          <p className="create-note">{a("users.emailConfirmation")}</p>
          <div className="editor-actions"><button className="secondary-button" type="button" onClick={() => setNewUserDraft(null)}>{a("common.cancel")}</button><button className="primary-button" disabled={busy}>{busy ? a("common.creating") : a("users.createTitle")}<Check size={15} /></button></div>
        </form>
      </section></div>}

      {roleDraft && <div className="modal-backdrop" onMouseDown={(event) => { if (event.target === event.currentTarget) setRoleDraft(null); }}><section className="dialog-panel" role="dialog" aria-modal="true" aria-labelledby="role-edit-title">
        <header className="editor-header"><div><p className="eyebrow">{a("roles.title")}</p><h2 id="role-edit-title">{roleDraft.id ? a("roles.editTitle") : a("roles.createTitle")}</h2></div><button className="icon-button" aria-label={a("common.dismiss")} onClick={() => setRoleDraft(null)}><X size={18} /></button></header>
        <form className="editor-form" onSubmit={saveRole}><label>{a("roles.name")}<input value={roleDraft.name} maxLength={50} onChange={(event) => setRoleDraft({ ...roleDraft, name: event.target.value })} required placeholder={a("roles.namePlaceholder")} /></label><label>{a("roles.code")}<input value={roleDraft.roleCode} maxLength={50} onChange={(event) => setRoleDraft({ ...roleDraft, roleCode: event.target.value.toUpperCase().replace(/[^A-Z0-9_-]/g, "") })} required placeholder={a("roles.codePlaceholder")} /></label><label>{a("roles.descriptionLabel")}<textarea rows={3} maxLength={250} value={roleDraft.description} onChange={(event) => setRoleDraft({ ...roleDraft, description: event.target.value })} required placeholder={a("roles.descriptionPlaceholder")} /></label><div className="editor-actions"><button className="secondary-button" type="button" onClick={() => setRoleDraft(null)}>{a("common.cancel")}</button><button className="primary-button" disabled={busy}>{busy ? a("common.saving") : roleDraft.id ? a("common.save") : a("roles.createTitle")}<Check size={15} /></button></div></form>
      </section></div>}

      {deviceUser && currentAccess?.canViewSessions && <UserSessions key={`${deviceUser.id}:${selectedScopeId}`} user={deviceUser} scopeId={selectedScopeId || activeTenantId} canRevoke={currentAccess.canRevokeSessions} canViewAudit={currentAccess.canViewAudit} onClose={() => setDeviceUser(null)} />}

      {claimTarget && <div className="modal-backdrop claim-modal-backdrop" onMouseDown={(event) => { if (event.target === event.currentTarget && !busy) setClaimTarget(null); }}><section className="dialog-panel claims-dialog" role="dialog" aria-modal="true" aria-labelledby="identity-claims-title">
        <header className="editor-header"><div><p className="eyebrow">{a("claims.eyebrow")}</p><h2 id="identity-claims-title">{a("claims.title", { name: claimTarget.name })}</h2></div><button className="icon-button" aria-label={a("common.dismiss")} onClick={() => setClaimTarget(null)} disabled={busy}><X size={18} /></button></header>
        <p className="field-hint">{a("claims.help")}</p>
        {error && <p className="claim-error" role="alert">{error}</p>}
        {loadingClaims ? <p className="security-loading">{a("common.loadingWorkspace")}</p> : <>
          <div className="identity-claim-list">{identityClaims.map(claim => <div className="identity-claim-row" key={`${claim.type}:${claim.value}`}>
            <span><strong>{claim.type}</strong><small>{claim.value || a("claims.emptyValue")}</small></span>
            <button type="button" className="icon-button" aria-label={a("claims.deleteClaim", { type: claim.type })} disabled={busy} onClick={() => void deleteIdentityClaim(claim)}><Trash2 size={15} /></button>
          </div>)}{identityClaims.length === 0 && <p className="claims-empty">{a("claims.empty")}</p>}</div>
          <form className="editor-form claims-form" onSubmit={addIdentityClaim}>
            <label>{a("claims.type")}<input value={claimType} maxLength={256} onChange={event => setClaimType(event.target.value)} required /></label>
            <label>{a("claims.value")}<input value={claimValue} maxLength={1024} onChange={event => setClaimValue(event.target.value)} required /></label>
            <div className="editor-actions"><button className="secondary-button" type="button" onClick={() => setClaimTarget(null)} disabled={busy}>{a("common.cancel")}</button><button className="primary-button" disabled={busy || !claimType.trim()}>{busy ? a("common.saving") : a("claims.add")}<Plus size={15} /></button></div>
          </form>
        </>}
      </section></div>}
    </main>
  );
}
