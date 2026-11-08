"use client";

import { ArrowLeft, ArrowRight, Building2, Check, ClipboardList, Globe2, KeyRound, LogOut, Monitor, Pencil, Plus, Search, Settings2, Shield, ShieldCheck, UsersRound, X } from "lucide-react";
import { useTranslations } from "next-intl";
import { useRouter } from "next/navigation";
import { useDeferredValue, useEffect, useRef, useState, type FormEvent } from "react";
import { useAppLocale } from "@/app/providers";
import { IdentityApiError } from "@/services/api-client";
import type { LoginCredentialsValues, TwoFactorCodeValues } from "../schemas/login.schema";
import { getSession, requestEmailConfirmation, signIn as authenticate, signOut as endSession } from "../api/auth.api";
import {
  deleteRole as removeRole,
  getRolePermissions,
  listIdentityScopes,
  listIdentityTenants,
  listRoles,
  saveRole as persistRole,
  setRolePermissions as persistRolePermissions,
} from "../api/roles.api";
import {
  listSessions,
  revokeOtherSessions as revokeOtherLoginSessions,
  revokeSession as revokeLoginSession,
} from "../api/sessions.api";
import { getSecurityAuditPage } from "../api/security-audit.api";
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
import LoginScreen from "./login-screen";
import TenantManagement from "./tenant-management";
import { useWorkspaceView, type WorkspaceView } from "./workspace-view-provider";
import { formatSessionDate, getSessionDeviceLabel } from "../utils/session-display";
import type { AdminConsoleProps, IdentityScope, IdentityTenant, LoginRequest, LoginSession, NewUserDraft, Role, RoleDraft, RolePermission, SecurityAuditPage, Session, User, UserPage } from "../interfaces";

const permissionFunctionLabels: Record<string, "permissions.functions.users" | "permissions.functions.roles" | "permissions.functions.sessions" | "permissions.functions.security" | "permissions.functions.other"> = {
  "Identity.Users": "permissions.functions.users",
  "Identity.Roles": "permissions.functions.roles",
  "Identity.Sessions": "permissions.functions.sessions",
  "Identity.Security": "permissions.functions.security",
};

const permissionActionLabels: Record<string, "permissions.actions.view" | "permissions.actions.create" | "permissions.actions.update" | "permissions.actions.delete" | "permissions.actions.assign" | "permissions.actions.revoke" | "permissions.actions.other"> = {
  View: "permissions.actions.view",
  Create: "permissions.actions.create",
  Update: "permissions.actions.update",
  Delete: "permissions.actions.delete",
  Assign: "permissions.actions.assign",
  Revoke: "permissions.actions.revoke",
};

const securityAuditEventLabels: Record<string, "audit.events.login" | "audit.events.logout" | "audit.events.sessionsRevoked" | "audit.events.passwordChanged" | "audit.events.passwordReset" | "audit.events.emailConfirmed" | "audit.events.twoFactorEnabled" | "audit.events.twoFactorDisabled" | "audit.events.userCreated" | "audit.events.userUpdated" | "audit.events.userStatusChanged" | "audit.events.userRolesChanged" | "audit.events.roleCreated" | "audit.events.roleUpdated" | "audit.events.roleDeleted" | "audit.events.rolePermissionsChanged" | "audit.events.scopeCreated" | "audit.events.scopeStatusChanged" | "audit.events.newLoginIp" | "audit.events.deviceSessionReplaced" | "audit.events.unknown"> = {
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
  "Roles.Created": "audit.events.roleCreated",
  "Roles.Updated": "audit.events.roleUpdated",
  "Roles.Deleted": "audit.events.roleDeleted",
  "Roles.PermissionsChanged": "audit.events.rolePermissionsChanged",
  "IdentityScope.Created": "audit.events.scopeCreated",
  "IdentityScope.StatusChanged": "audit.events.scopeStatusChanged",
  "SecurityAnomaly.NewLoginIp": "audit.events.newLoginIp",
  "Authentication.DeviceSessionReplaced": "audit.events.deviceSessionReplaced",
};

export default function AdminConsole({ mode }: AdminConsoleProps) {
  const router = useRouter();
  const { locale, setLocale } = useAppLocale();
  const { view, setView, activeTenantId, setActiveTenantId } = useWorkspaceView();
  const [session, setSession] = useState<Session | null>(null);
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
  const [tenantOptions, setTenantOptions] = useState<IdentityTenant[]>([]);
  const [loadedTenantDataKey, setLoadedTenantDataKey] = useState("");
  const loadedTenantDataKeyRef = useRef("");
  const [selectedScopeId, setSelectedScopeId] = useState("");
  const [sessions, setSessions] = useState<LoginSession[]>([]);
  const [auditPage, setAuditPage] = useState<SecurityAuditPage>({ items: [], page: 1, pageSize: 50, totalCount: 0 });
  const [auditPageNumber, setAuditPageNumber] = useState(1);
  const [auditLoading, setAuditLoading] = useState(false);
  const [auditRefresh, setAuditRefresh] = useState(0);
  const [auditEventType, setAuditEventType] = useState("");
  const [auditOutcome, setAuditOutcome] = useState("");
  const [search, setSearch] = useState("");
  const searchQuery = useDeferredValue(search);
  const [page, setPage] = useState(1);
  const [selectedUser, setSelectedUser] = useState<User | null>(null);
  const [firstName, setFirstName] = useState("");
  const [lastName, setLastName] = useState("");
  const [selectedRoles, setSelectedRoles] = useState<string[]>([]);
  const [originalScopeRoles, setOriginalScopeRoles] = useState<string[]>([]);
  const [active, setActive] = useState(true);
  const [newUserDraft, setNewUserDraft] = useState<NewUserDraft | null>(null);
  const [roleDraft, setRoleDraft] = useState<RoleDraft | null>(null);
  const [permissionRole, setPermissionRole] = useState<Role | null>(null);
  const [rolePermissions, setRolePermissions] = useState<RolePermission[]>([]);
  const [loadingPermissions, setLoadingPermissions] = useState(false);
  const [notice, setNotice] = useState("");
  const [error, setError] = useState("");

  useEffect(() => {
    let mounted = true;
    getSession().then((value) => {
      if (mounted) {
        setSession(value);
        setEmail(value.email);
        if (mode === "login" && window.sessionStorage.getItem("identity-password-changed") === "1") {
          window.sessionStorage.removeItem("identity-password-changed");
          setNotice(t("passwordChangedSignIn"));
        }
      }
    }).catch(() => {
      if (mounted) setSession({ authenticated: false, email: "" });
    }).finally(() => {
      if (mounted) setLoading(false);
    });
    return () => { mounted = false; };
  }, [mode, t]);

  useEffect(() => {
    if (loading) return;
    if (mode === "login" && session?.authenticated) {
      router.replace("/dashboard");
    } else if (mode !== "login" && !session?.authenticated) {
      router.replace("/login");
    }
  }, [loading, mode, router, session?.authenticated]);

  useEffect(() => {
    if (!session?.authenticated || mode !== "dashboard") return;
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
  }, [a, activeTenantId, mode, session?.authenticated, setActiveTenantId]);

  useEffect(() => {
    if (!session?.authenticated || mode !== "dashboard") return;
    let mounted = true;
    const dataKey = `${session.userId ?? ""}:${activeTenantId}`;
    Promise.all([
      listUsers(searchQuery, page),
      listRoles(),
      listSessions(),
      listIdentityScopes(),
    ]).then(([userPage, roleList, loginSessions, scopeList]) => {
      if (mounted) {
        if (loadedTenantDataKeyRef.current !== dataKey) {
          loadedTenantDataKeyRef.current = dataKey;
          setSelectedScopeId("");
          setSelectedUser(null);
          setSelectedRoles([]);
          setOriginalScopeRoles([]);
          setNewUserDraft(null);
          setRoleDraft(null);
          setPermissionRole(null);
          setRolePermissions([]);
          setAuditPage({ items: [], page: 1, pageSize: 50, totalCount: 0 });
          setAuditPageNumber(1);
        }
        setUsers(userPage);
        setRoles(roleList);
        setSessions(loginSessions);
        setScopes(scopeList);
        setLoadedTenantDataKey(dataKey);
        setSelectedScopeId(current => scopeList.some(scope => scope.id === current)
          ? current
          : scopeList.find(scope => scope.scopeType === 1)?.id || "");
        setError("");
      }
    }).catch((cause: unknown) => {
      if (mounted) setError(cause instanceof Error ? cause.message : a("notice.usersLoadError"));
    });
    return () => { mounted = false; };
  }, [a, mode, session?.authenticated, session?.userId, searchQuery, page, activeTenantId]);

  useEffect(() => {
    if (!session?.authenticated || mode !== "dashboard" || view !== "audit") return;
    let mounted = true;
    getSecurityAuditPage(auditPageNumber, { eventType: auditEventType, outcome: auditOutcome })
      .then(result => { if (mounted) { setAuditPage(result); setError(""); } })
      .catch(cause => { if (mounted) setError(cause instanceof Error ? cause.message : a("audit.loadError")); })
      .finally(() => { if (mounted) setAuditLoading(false); });
    return () => { mounted = false; };
  }, [a, activeTenantId, auditEventType, auditOutcome, auditPageNumber, auditRefresh, mode, session?.authenticated, view]);

  async function loadUsers() {
    setUsers(await listUsers(searchQuery, page));
  }

  async function loadRoles() {
    setRoles(await listRoles());
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
    setBusy(true);
    try { await endSession(); }
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
    if (nextView === "security" && mode !== "settings") router.push("/settings");
    if (nextView !== "security" && mode === "settings") router.push("/dashboard");
  }

  async function editUser(user: User) {
    if (!selectedScopeId) {
      setError(a("scope.loadError"));
      return;
    }
    setSelectedUser(user); setFirstName(user.firstName); setLastName(user.lastName);
    setSelectedRoles([]); setOriginalScopeRoles([]); setActive(user.isActive); setError("");
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
      await updateUser(selectedUser.id, { firstName, lastName });
      if (selectedRoles.length !== originalScopeRoles.length || selectedRoles.some(role => !originalScopeRoles.includes(role))) {
        await updateUserRoles(selectedUser.id, selectedScopeId, { roles: selectedRoles });
      }
      if (active !== selectedUser.isActive) await updateUserStatus(selectedUser.id, { isActive: active });
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
      await createUserAccount(newUserDraft);
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
      });
      setRoleDraft(null); setNotice(editing ? a("roles.updated") : a("roles.created")); await loadRoles();
    } catch (cause) { setError(cause instanceof Error ? cause.message : a("roles.saveError")); }
    finally { setBusy(false); }
  }

  async function editRolePermissions(role: Role) {
    if (!selectedScopeId) {
      setError(a("scope.loadError"));
      return;
    }
    setPermissionRole(role);
    setRolePermissions([]);
    setLoadingPermissions(true);
    setError("");
    try {
      setRolePermissions(await getRolePermissions(role.id, selectedScopeId));
    } catch (cause) {
      setPermissionRole(null);
      setError(cause instanceof Error ? cause.message : a("permissions.loadError"));
    } finally {
      setLoadingPermissions(false);
    }
  }

  async function saveRolePermissions(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!permissionRole) return;
    setBusy(true);
    setError("");
    try {
      await persistRolePermissions(permissionRole.id, selectedScopeId, {
        permissions: rolePermissions
          .filter(permission => permission.isGranted)
          .map(({ functionId, actionId }) => ({ functionId, actionId })),
      });
      setPermissionRole(null);
      setNotice(a("permissions.saved"));
    } catch (cause) {
      setError(cause instanceof Error ? cause.message : a("permissions.saveError"));
    } finally {
      setBusy(false);
    }
  }

  async function deleteRole(role: Role) {
    if (role.name === "Administrator" || role.userCount > 0) return;
    setBusy(true); setError("");
    try { await removeRole(role.id); setNotice(a("roles.deleted")); await loadRoles(); }
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
    <main className="workspace-shell">
      <aside className="sidebar">
        <div className="brand-lockup sidebar-brand"><span className="brand-mark"><ShieldCheck size={19} /></span><span>Store<span className="brand-light">Management</span></span></div>
        <p className="workspace-label">{a("workspace")}</p>
        <nav className="side-nav" aria-label={a("navigation.label")}>
          <button className={view === "users" ? "nav-item active" : "nav-item"} onClick={() => navigateToView("users")}><UsersRound size={17} /><span>{a("navigation.users")}</span><small>{users.totalCount}</small></button>
          <button className={view === "roles" ? "nav-item active" : "nav-item"} onClick={() => navigateToView("roles")}><Shield size={17} /><span>{a("navigation.roles")}</span><small>{roles.length}</small></button>
          <button className={view === "sessions" ? "nav-item active" : "nav-item"} onClick={() => navigateToView("sessions")}><Monitor size={17} /><span>{a("navigation.sessions")}</span><small>{sessions.length}</small></button>
          <button className={view === "security" ? "nav-item active" : "nav-item"} onClick={() => navigateToView("security")}><Settings2 size={17} /><span>{a("navigation.security")}</span></button>
          <button className={view === "audit" ? "nav-item active" : "nav-item"} onClick={() => navigateToView("audit")}><ClipboardList size={17} /><span>{a("navigation.audit")}</span></button>
          <button className={view === "tenants" ? "nav-item active" : "nav-item"} onClick={() => navigateToView("tenants")}><Building2 size={17} /><span>{a("navigation.tenants")}</span></button>
        </nav>
        <div className="sidebar-bottom"><div className="policy-note"><ShieldCheck size={16} /><span><strong>{a("navigation.protected")}</strong><small>{a("navigation.policy")}</small></span></div><div className="account-row"><span className="account-avatar">{session.email.slice(0, 1).toUpperCase()}</span><span className="account-email">{session.email}</span><button className="icon-button sidebar-signout" title={a("navigation.signOut")} aria-label={a("navigation.signOut")} onClick={signOut} disabled={busy}><LogOut size={16} /></button></div></div>
      </aside>
      <section className="main-panel">
        <header className="topbar">
          <div className="breadcrumb"><span>{a("identity")}</span><i>/</i><strong>{view === "users" ? a("users.breadcrumb") : view === "roles" ? a("navigation.roles") : view === "sessions" ? a("navigation.sessions") : view === "audit" ? a("audit.title") : view === "tenants" ? a("tenants.title") : a("security.title")}</strong></div>
          <div className="topbar-meta">
            <div className="admin-language" role="group" aria-label={a("language")}><Globe2 size={15} /><button type="button" aria-label={a("vietnamese")} aria-pressed={locale === "vi"} onClick={() => setLocale("vi")}>VI</button><span>/</span><button type="button" aria-label={a("english")} aria-pressed={locale === "en"} onClick={() => setLocale("en")}>EN</button></div>
            {tenantOptions.length > 1 && <label className="scope-picker"><span>{a("tenants.activeTenant")}</span><select aria-label={a("tenants.activeTenant")} value={tenantOptions.some(tenant => tenant.id === activeTenantId) ? activeTenantId : tenantOptions[0].id} onChange={(event) => {
              setActiveTenantId(event.target.value);
              setSelectedScopeId("");
              setSelectedUser(null);
              setPermissionRole(null);
            }}>{tenantOptions.map(tenant => <option key={tenant.id} value={tenant.id}>{tenant.name}</option>)}</select></label>}
            {view !== "tenants" && loadedTenantDataKey === `${session.userId ?? ""}:${activeTenantId}` && scopes.length > 0 && <label className="scope-picker"><span>{a("scope.label")}</span><select aria-label={a("scope.label")} value={selectedScopeId} onChange={(event) => {
              setSelectedScopeId(event.target.value);
              setSelectedUser(null);
              setPermissionRole(null);
            }}>{scopes.map(scope => <option key={scope.id} value={scope.id}>{`${scope.scopeType === 1 ? "" : scope.scopeType === 2 ? "— " : "—— "}${scope.name}`}</option>)}</select></label>}
            <span className="console-label"><span className="online-dot" /> {a("console")}</span><span className="topbar-email">{session.email}</span><button className="icon-button mobile-signout" title={a("navigation.signOut")} aria-label={a("navigation.signOut")} onClick={signOut}><LogOut size={16} /></button>
          </div>
        </header>
        <div className="content-area">
          {(notice || error) && <div className={error ? "notice notice-error" : "notice notice-success"} role={error ? "alert" : "status"}><span>{error || notice}</span><button aria-label={a("common.dismiss")} onClick={() => { setError(""); setNotice(""); }}><X size={15} /></button></div>}
          {view === "tenants" ? <TenantManagement /> : loadedTenantDataKey !== `${session.userId ?? ""}:${activeTenantId}` ? (
            <div className="tenant-empty" role="status">{a("common.loadingWorkspace")}</div>
          ) : view === "users" ? <>
            <div className="page-heading"><div><p className="eyebrow">{a("users.eyebrow")}</p><h1>{a("users.title")}</h1><p className="subtle">{a("users.description")}</p></div><div className="heading-actions"><div className="total-stat"><strong>{users.totalCount.toLocaleString()}</strong><span>{a("users.accounts")}</span></div><button className="primary-button" onClick={() => setNewUserDraft({ email: "", firstName: "", lastName: "", password: "", roles: [] })}><Plus size={15} /> {a("users.new")}</button></div></div>
            <div className="toolbar"><label className="search-field"><Search size={16} /><input aria-label={a("users.searchLabel")} placeholder={a("users.search")} value={search} onChange={(event) => { setSearch(event.target.value); setPage(1); }} /></label><div className="live-label"><span className="online-dot" /> {a("users.live")} <button className="refresh-button" onClick={() => void loadUsers()}>{a("common.refresh")}</button></div></div>
            <div className="table-shell"><div className="table-scroll"><table className="user-table"><thead><tr><th>{a("users.member")}</th><th>{a("users.roleAccess")}</th><th>{a("users.security")}</th><th>{a("users.status")}</th><th /></tr></thead><tbody>
              {users.items.map((user, index) => <tr key={user.id} style={{ "--row": index } as React.CSSProperties}>
                <td><div className="member"><span className={`member-avatar avatar-${index % 5}`}>{`${user.firstName.slice(0, 1)}${user.lastName.slice(0, 1)}`.toUpperCase()}</span><span><strong>{user.fullName}</strong><small>{user.email}</small></span></div></td>
                <td><div className="role-list">{user.roles.length ? user.roles.slice(0, 2).map((role) => <span className="role-chip" key={role}>{role}</span>) : <span className="empty-role">{a("users.noRole")}</span>}{user.roles.length > 2 && <span className="role-chip extra-chip">+{user.roles.length - 2}</span>}</div></td>
                <td><div className="security-list"><span className={user.emailConfirmed ? "verified" : "unverified"}>{user.emailConfirmed ? a("users.emailVerified") : a("users.emailPending")}</span><small>{user.twoFactorEnabled ? a("users.twoFactorEnabled") : a("users.twoFactorOff")}</small></div></td>
                <td><span className={user.isActive ? "status active" : "status inactive"}><i />{user.isActive ? a("users.active") : a("users.inactive")}</span></td>
                <td><button className="icon-button" title={a("users.editUser", { name: user.fullName })} aria-label={a("users.editUser", { name: user.fullName })} onClick={() => editUser(user)}><Pencil size={15} /></button></td>
              </tr>)}
              {!users.items.length && <tr><td colSpan={5}><div className="empty-state"><UsersRound size={21} /><strong>{a("users.emptyTitle")}</strong><span>{a("users.emptyDescription")}</span></div></td></tr>}
            </tbody></table></div><footer className="table-footer"><span>{a("common.showing")} <strong>{users.totalCount ? (page - 1) * users.pageSize + 1 : 0}–{Math.min(page * users.pageSize, users.totalCount)}</strong> {a("common.of")} <strong>{users.totalCount}</strong></span><div className="pager"><button className="icon-button" aria-label={a("common.previous")} disabled={page <= 1} onClick={() => setPage((value) => Math.max(1, value - 1))}><ArrowLeft size={15} /></button><span>{a("common.page")} <strong>{page}</strong> {a("common.of")} {pageCount}</span><button className="icon-button" aria-label={a("common.next")} disabled={page >= pageCount} onClick={() => setPage((value) => Math.min(pageCount, value + 1))}><ArrowRight size={15} /></button></div></footer></div>
          </> : view === "roles" ? <>
            <div className="page-heading"><div><p className="eyebrow">{a("roles.eyebrow")}</p><h1>{a("roles.title")}</h1><p className="subtle">{a("roles.description")}</p></div><button className="primary-button" onClick={() => setRoleDraft({ name: "", roleCode: "", description: "" })}><Plus size={16} /> {a("roles.new")}</button></div>
            <div className="role-summary"><span className="role-summary-icon"><Shield size={17} /></span><span><strong>{a("roles.defined", { count: roles.length })}</strong><small>{a("roles.protected")}</small></span><span className="summary-end"><ShieldCheck size={16} /> {a("roles.access")}</span></div>
            <div className="role-grid">{roles.map((role) => <article className="role-card" key={role.id}><div className="role-card-head"><span className="role-card-icon"><KeyRound size={16} /></span><span className="role-code">{role.roleCode}</span><button className="icon-button" title={a("roles.editTitle") + ` ${role.name}`} aria-label={a("roles.editTitle") + ` ${role.name}`} onClick={() => setRoleDraft({ id: role.id, name: role.name, roleCode: role.roleCode, description: role.description })}><Pencil size={15} /></button></div><h2>{role.name}</h2><p>{role.description}</p><footer><span><UsersRound size={14} />{role.userCount} {role.userCount === 1 ? a("roles.member") : a("roles.members")}</span><span className="role-card-actions">{role.name !== "Administrator" && <button className="text-button" onClick={() => void editRolePermissions(role)}>{a("permissions.manage")}</button>}<button className="text-button" disabled={role.name === "Administrator" || role.userCount > 0} onClick={() => void deleteRole(role)}>{a("roles.delete")}</button></span></footer></article>)}{!roles.length && <div className="empty-state role-empty"><Shield size={21} /><strong>{a("roles.emptyTitle")}</strong><span>{a("roles.emptyDescription")}</span></div>}</div>
          </> : view === "sessions" ? <>
            <div className="page-heading"><div><p className="eyebrow">{a("sessions.eyebrow")}</p><h1>{a("sessions.title")}</h1><p className="subtle">{a("sessions.description")}</p></div><div className="heading-actions"><button className="secondary-button" onClick={() => void refreshSessions()} disabled={busy}>{a("common.refresh")}</button><button className="primary-button" onClick={() => void revokeOtherSessions()} disabled={busy || !sessions.some((session) => !session.isCurrent)}>{a("sessions.signOutOthers")}</button></div></div>
            <div className="table-shell"><div className="table-scroll"><table className="user-table session-table"><thead><tr><th>{a("sessions.device")}</th><th>{a("sessions.ipAddress")}</th><th>{a("sessions.firstSignIn")}</th><th>{a("sessions.lastActive")}</th><th>{a("sessions.expires")}</th><th>{a("sessions.status")}</th><th /></tr></thead><tbody>
              {sessions.map((loginSession) => <tr key={loginSession.sessionId}>
                <td><div className="member"><span className="member-avatar avatar-3"><Monitor size={16} /></span><span><strong>{getSessionDeviceLabel(loginSession.userAgent, a("common.unknown")).browser}</strong><small>{a(loginSession.deviceCategory === "Phone" ? "sessions.phone" : "sessions.computer")} · {getSessionDeviceLabel(loginSession.userAgent, a("common.unknown")).operatingSystem} · {loginSession.isCurrent ? a("sessions.thisDevice") : a("sessions.otherDevice")}</small></span></div></td>
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
            <div className="audit-filters"><label>{a("audit.filterEvent")}<select value={auditEventType} onChange={event => { setAuditLoading(true); setAuditPageNumber(1); setAuditEventType(event.target.value); }}><option value="">{a("audit.allEvents")}</option>{Object.keys(securityAuditEventLabels).filter(eventType => eventType !== "unknown").map(eventType => <option value={eventType} key={eventType}>{a(securityAuditEventLabels[eventType])}</option>)}</select></label><label>{a("audit.filterOutcome")}<select value={auditOutcome} onChange={event => { setAuditLoading(true); setAuditPageNumber(1); setAuditOutcome(event.target.value); }}><option value="">{a("audit.allOutcomes")}</option><option value="Succeeded">{a("audit.succeeded")}</option><option value="Failed">{a("audit.failed")}</option></select></label></div>
            <div className="table-shell"><div className="table-scroll"><table className="user-table audit-table"><thead><tr><th>{a("audit.event")}</th><th>{a("audit.outcome")}</th><th>{a("audit.time")}</th><th>{a("audit.actor")}</th><th>{a("audit.subject")}</th><th>{a("audit.ipAddress")}</th></tr></thead><tbody>
              {auditPage.items.map(event => <tr key={event.id}>
                <td><div className="security-list"><strong>{a(securityAuditEventLabels[event.eventType] ?? "audit.events.unknown")}</strong>{event.details && <small>{event.details}</small>}</div></td>
                <td><span className={event.outcome === "Succeeded" ? "status active" : "status inactive"}><i />{event.outcome === "Succeeded" ? a("audit.succeeded") : a("audit.failed")}</span></td>
                <td>{formatSessionDate(event.occurredAtUtc, locale, a("common.unknown"))}</td>
                <td className="audit-id">{event.actorUserId ?? a("audit.systemActor")}</td>
                <td className="audit-id">{event.subjectUserId ?? a("common.unavailable")}</td>
                <td>{event.ipAddress ?? a("common.unavailable")}</td>
              </tr>)}
              {!auditLoading && !auditPage.items.length && <tr><td colSpan={6}><div className="empty-state"><ClipboardList size={21} /><strong>{a("audit.emptyTitle")}</strong><span>{a("audit.emptyDescription")}</span></div></td></tr>}
              {auditLoading && <tr><td colSpan={6}><div className="empty-state">{a("common.loadingWorkspace")}</div></td></tr>}
            </tbody></table></div><footer className="table-footer"><span>{a("common.showing")} <strong>{auditPage.totalCount ? (auditPageNumber - 1) * auditPage.pageSize + 1 : 0}–{Math.min(auditPageNumber * auditPage.pageSize, auditPage.totalCount)}</strong> {a("common.of")} <strong>{auditPage.totalCount}</strong></span><div className="pager"><button className="icon-button" aria-label={a("common.previous")} disabled={auditPageNumber <= 1 || auditLoading} onClick={() => { setAuditLoading(true); setAuditPageNumber(value => Math.max(1, value - 1)); }}><ArrowLeft size={15} /></button><span>{a("common.page")} <strong>{auditPageNumber}</strong> {a("common.of")} {Math.max(1, Math.ceil(auditPage.totalCount / auditPage.pageSize))}</span><button className="icon-button" aria-label={a("common.next")} disabled={auditPageNumber >= Math.max(1, Math.ceil(auditPage.totalCount / auditPage.pageSize)) || auditLoading} onClick={() => { setAuditLoading(true); setAuditPageNumber(value => value + 1); }}><ArrowRight size={15} /></button></div></footer></div>
          </> : <AccountSecurity userId={session.userId} onPasswordChanged={() => void handlePasswordChanged()} />}
          <footer className="page-footer"><span>STORE MANAGEMENT <i /> {a("footer.service")}</span><span>{a("footer.operations")} <ShieldCheck size={13} /></span></footer>
        </div>
      </section>

      {selectedUser && <div className="modal-backdrop" onMouseDown={(event) => { if (event.target === event.currentTarget) setSelectedUser(null); }}><section className="editor-panel" role="dialog" aria-modal="true" aria-labelledby="user-edit-title">
        <header className="editor-header"><div><p className="eyebrow">{a("users.profile")}</p><h2 id="user-edit-title">{a("users.editTitle")}</h2></div><button className="icon-button" aria-label={a("common.dismiss")} onClick={() => setSelectedUser(null)}><X size={18} /></button></header>
        <div className="editor-user"><span className="member-avatar avatar-2">{`${selectedUser.firstName.slice(0, 1)}${selectedUser.lastName.slice(0, 1)}`.toUpperCase()}</span><span><strong>{selectedUser.email}</strong><small>{selectedUser.emailConfirmed ? a("users.emailVerified") : a("users.emailUnverified")}</small></span></div>
        {!selectedUser.emailConfirmed && <div className="editor-actions"><button type="button" className="secondary-button" onClick={() => void resendConfirmationEmail()} disabled={busy}>{busy ? a("users.sendingConfirmation") : a("users.resendConfirmation")}</button></div>}
        <form className="editor-form" onSubmit={saveUser}><div className="form-pair"><label>{a("users.firstName")}<input value={firstName} maxLength={100} onChange={(event) => setFirstName(event.target.value)} required /></label><label>{a("users.lastName")}<input value={lastName} maxLength={100} onChange={(event) => setLastName(event.target.value)} required /></label></div>
          <fieldset className="role-picker"><legend>{a("users.roleMembership")}</legend><p>{a("users.roleHelp")}</p>{roles.map((role) => <label className="check-row" key={role.id}><input type="checkbox" checked={selectedRoles.includes(role.name)} onChange={(event) => setSelectedRoles(event.target.checked ? [...selectedRoles, role.name] : selectedRoles.filter((item) => item !== role.name))} /><span className="check-box"><Check size={12} /></span><span>{role.name}</span><small>{role.roleCode}</small></label>)}</fieldset>
          <label className="active-row"><span><strong>{a("users.accountActive")}</strong><small>{a("users.inactiveHelp")}</small></span><input type="checkbox" checked={active} onChange={(event) => setActive(event.target.checked)} /><span className="switch" /></label>
          <div className="editor-actions"><button type="button" className="secondary-button" onClick={() => setSelectedUser(null)}>{a("common.cancel")}</button><button className="primary-button" disabled={busy}>{busy ? a("common.saving") : a("common.save")}<Check size={15} /></button></div>
        </form>
      </section></div>}

      {newUserDraft && <div className="modal-backdrop" onMouseDown={(event) => { if (event.target === event.currentTarget) setNewUserDraft(null); }}><section className="dialog-panel" role="dialog" aria-modal="true" aria-labelledby="new-user-title">
        <header className="editor-header"><div><p className="eyebrow">{a("users.breadcrumb")}</p><h2 id="new-user-title">{a("users.createTitle")}</h2></div><button className="icon-button" aria-label={a("common.dismiss")} onClick={() => setNewUserDraft(null)}><X size={18} /></button></header>
        <form className="editor-form" onSubmit={createUser}>
          <label>{a("users.emailAddress")}<input type="email" autoComplete="email" value={newUserDraft.email} onChange={(event) => setNewUserDraft({ ...newUserDraft, email: event.target.value })} required placeholder="person@company.com" /></label>
          <div className="form-pair"><label>{a("users.firstName")}<input value={newUserDraft.firstName} maxLength={100} onChange={(event) => setNewUserDraft({ ...newUserDraft, firstName: event.target.value })} required /></label><label>{a("users.lastName")}<input value={newUserDraft.lastName} maxLength={100} onChange={(event) => setNewUserDraft({ ...newUserDraft, lastName: event.target.value })} required /></label></div>
          <label>{a("users.temporaryPassword")}<input type="password" autoComplete="new-password" minLength={8} value={newUserDraft.password} onChange={(event) => setNewUserDraft({ ...newUserDraft, password: event.target.value })} required /><small className="field-hint">{a("users.passwordHelp")}</small></label>
          <fieldset className="role-picker"><legend>{a("users.initialRoles")} <span className="optional">{a("common.optional")}</span></legend>{roles.filter((role) => role.name !== "Administrator").map((role) => <label className="check-row" key={role.id}><input type="checkbox" checked={newUserDraft.roles.includes(role.name)} onChange={(event) => setNewUserDraft({ ...newUserDraft, roles: event.target.checked ? [...newUserDraft.roles, role.name] : newUserDraft.roles.filter((item) => item !== role.name) })} /><span className="check-box"><Check size={12} /></span><span>{role.name}</span><small>{role.roleCode}</small></label>)}</fieldset>
          <p className="create-note">{a("users.emailConfirmation")}</p>
          <div className="editor-actions"><button className="secondary-button" type="button" onClick={() => setNewUserDraft(null)}>{a("common.cancel")}</button><button className="primary-button" disabled={busy}>{busy ? a("common.creating") : a("users.createTitle")}<Check size={15} /></button></div>
        </form>
      </section></div>}

      {roleDraft && <div className="modal-backdrop" onMouseDown={(event) => { if (event.target === event.currentTarget) setRoleDraft(null); }}><section className="dialog-panel" role="dialog" aria-modal="true" aria-labelledby="role-edit-title">
        <header className="editor-header"><div><p className="eyebrow">{a("roles.title")}</p><h2 id="role-edit-title">{roleDraft.id ? a("roles.editTitle") : a("roles.createTitle")}</h2></div><button className="icon-button" aria-label={a("common.dismiss")} onClick={() => setRoleDraft(null)}><X size={18} /></button></header>
        <form className="editor-form" onSubmit={saveRole}><label>{a("roles.name")}<input value={roleDraft.name} maxLength={50} onChange={(event) => setRoleDraft({ ...roleDraft, name: event.target.value })} required placeholder={a("roles.namePlaceholder")} /></label><label>{a("roles.code")}<input value={roleDraft.roleCode} maxLength={50} onChange={(event) => setRoleDraft({ ...roleDraft, roleCode: event.target.value.toUpperCase().replace(/[^A-Z0-9_-]/g, "") })} required placeholder={a("roles.codePlaceholder")} /></label><label>{a("roles.descriptionLabel")}<textarea rows={3} maxLength={250} value={roleDraft.description} onChange={(event) => setRoleDraft({ ...roleDraft, description: event.target.value })} required placeholder={a("roles.descriptionPlaceholder")} /></label><div className="editor-actions"><button className="secondary-button" type="button" onClick={() => setRoleDraft(null)}>{a("common.cancel")}</button><button className="primary-button" disabled={busy}>{busy ? a("common.saving") : roleDraft.id ? a("common.save") : a("roles.createTitle")}<Check size={15} /></button></div></form>
      </section></div>}

      {permissionRole && <div className="modal-backdrop" onMouseDown={(event) => { if (event.target === event.currentTarget && !busy) setPermissionRole(null); }}><section className="dialog-panel permission-dialog" role="dialog" aria-modal="true" aria-labelledby="role-permissions-title">
        <header className="editor-header"><div><p className="eyebrow">{a("permissions.eyebrow")}</p><h2 id="role-permissions-title">{a("permissions.title", { role: permissionRole.name })}</h2></div><button className="icon-button" aria-label={a("common.dismiss")} onClick={() => setPermissionRole(null)} disabled={busy}><X size={18} /></button></header>
        {loadingPermissions ? <p className="security-loading">{a("common.loadingWorkspace")}</p> : rolePermissions.length === 0 ? <div className="empty-state"><Shield size={21} /><strong>{a("permissions.emptyTitle")}</strong><span>{a("permissions.emptyDescription")}</span></div> : <form className="editor-form" onSubmit={saveRolePermissions}>
          <p className="field-hint">{a("permissions.help")}</p>
          <div className="permission-list">{rolePermissions.map((permission) => <label className="check-row" key={`${permission.functionId}:${permission.actionId}`}>
            <input type="checkbox" checked={permission.isGranted} onChange={(event) => setRolePermissions(current => current.map(item => item.functionId === permission.functionId && item.actionId === permission.actionId ? { ...item, isGranted: event.target.checked } : item))} />
            <span className="check-box"><Check size={12} /></span>
            <span>{a(permissionFunctionLabels[permission.functionId] ?? "permissions.functions.other")}</span>
            <small>{a(permissionActionLabels[permission.actionId] ?? "permissions.actions.other")}</small>
          </label>)}</div>
          <div className="editor-actions"><button type="button" className="secondary-button" onClick={() => setPermissionRole(null)} disabled={busy}>{a("common.cancel")}</button><button className="primary-button" disabled={busy}>{busy ? a("common.saving") : a("common.save")}<Check size={15} /></button></div>
        </form>}
      </section></div>}
    </main>
  );
}