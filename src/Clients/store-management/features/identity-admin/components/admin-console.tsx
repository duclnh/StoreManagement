"use client";

import { ArrowLeft, ArrowRight, Check, KeyRound, LogOut, Monitor, Pencil, Plus, Search, Settings2, Shield, ShieldCheck, UsersRound, X } from "lucide-react";
import { useTranslations } from "next-intl";
import { useRouter } from "next/navigation";
import { useDeferredValue, useEffect, useState, type FormEvent } from "react";
import { IdentityApiError } from "@/services/api-client";
import type { LoginCredentialsValues, TwoFactorCodeValues } from "../schemas/login.schema";
import { getSession, signIn as authenticate, signOut as endSession } from "../api/auth.api";
import { deleteRole as removeRole, listRoles, saveRole as persistRole } from "../api/roles.api";
import {
  listSessions,
  revokeOtherSessions as revokeOtherLoginSessions,
  revokeSession as revokeLoginSession,
} from "../api/sessions.api";
import {
  createUser as createUserAccount,
  emptyUserPage,
  listUsers,
  updateUser,
  updateUserRoles,
  updateUserStatus,
} from "../api/users.api";
import AccountSecurity from "./account-security";
import LoginScreen from "./login-screen";
import { useWorkspaceView } from "./workspace-view-provider";
import type { AdminConsoleProps, LoginRequest, LoginSession, NewUserDraft, Role, RoleDraft, Session, User, UserPage } from "../interfaces";

export default function AdminConsole({ mode }: AdminConsoleProps) {
  const router = useRouter();
  const { view, setView } = useWorkspaceView();
  const [session, setSession] = useState<Session | null>(null);
  const [loading, setLoading] = useState(true);
  const [busy, setBusy] = useState(false);
  const [email, setEmail] = useState("");
  const [pendingCredentials, setPendingCredentials] = useState<Omit<LoginRequest, "twoFactorCode"> | null>(null);
  const [requiresTwoFactor, setRequiresTwoFactor] = useState(false);
  const t = useTranslations("login");
  const [users, setUsers] = useState<UserPage>(emptyUserPage);
  const [roles, setRoles] = useState<Role[]>([]);
  const [sessions, setSessions] = useState<LoginSession[]>([]);
  const [search, setSearch] = useState("");
  const searchQuery = useDeferredValue(search);
  const [page, setPage] = useState(1);
  const [selectedUser, setSelectedUser] = useState<User | null>(null);
  const [firstName, setFirstName] = useState("");
  const [lastName, setLastName] = useState("");
  const [selectedRoles, setSelectedRoles] = useState<string[]>([]);
  const [active, setActive] = useState(true);
  const [newUserDraft, setNewUserDraft] = useState<NewUserDraft | null>(null);
  const [roleDraft, setRoleDraft] = useState<RoleDraft | null>(null);
  const [notice, setNotice] = useState("");
  const [error, setError] = useState("");

  useEffect(() => {
    let mounted = true;
    getSession().then((value) => {
      if (mounted) { setSession(value); setEmail(value.email); }
    }).catch(() => {
      if (mounted) setSession({ authenticated: false, email: "" });
    }).finally(() => {
      if (mounted) setLoading(false);
    });
    return () => { mounted = false; };
  }, []);

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
    Promise.all([
      listUsers(searchQuery, page),
      listRoles(),
      listSessions(),
    ]).then(([userPage, roleList, loginSessions]) => {
      if (mounted) { setUsers(userPage); setRoles(roleList); setSessions(loginSessions); setError(""); }
    }).catch((cause: unknown) => {
      if (mounted) setError(cause instanceof Error ? cause.message : "Could not load the directory.");
    });
    return () => { mounted = false; };
  }, [mode, session?.authenticated, searchQuery, page]);

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

  function navigateToView(nextView: "users" | "roles" | "sessions" | "security") {
    setView(nextView);
    if (nextView === "security" && mode !== "settings") router.push("/settings");
    if (nextView !== "security" && mode === "settings") router.push("/dashboard");
  }

  function editUser(user: User) {
    setSelectedUser(user); setFirstName(user.firstName); setLastName(user.lastName);
    setSelectedRoles([...user.roles]); setActive(user.isActive);
  }

  async function saveUser(event: FormEvent<HTMLFormElement>) {
    event.preventDefault(); if (!selectedUser) return;
    setBusy(true); setError("");
    try {
      await updateUser(selectedUser.id, { firstName, lastName });
      await updateUserRoles(selectedUser.id, { roles: selectedRoles });
      if (active !== selectedUser.isActive) await updateUserStatus(selectedUser.id, { isActive: active });
      setSelectedUser(null); setNotice("User changes saved."); await loadUsers(); await loadRoles();
    } catch (cause) { setError(cause instanceof Error ? cause.message : "Could not save user changes."); }
    finally { setBusy(false); }
  }

  async function createUser(event: FormEvent<HTMLFormElement>) {
    event.preventDefault(); if (!newUserDraft) return;
    setBusy(true); setError("");
    try {
      await createUserAccount(newUserDraft);
      setNewUserDraft(null); setNotice("Account created. Email confirmation is required before sign-in.");
      await loadUsers(); await loadRoles();
    } catch (cause) { setError(cause instanceof Error ? cause.message : "Could not create account."); }
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
      setRoleDraft(null); setNotice(editing ? "Role updated." : "Role created."); await loadRoles();
    } catch (cause) { setError(cause instanceof Error ? cause.message : "Could not save role."); }
    finally { setBusy(false); }
  }

  async function deleteRole(role: Role) {
    if (role.name === "Administrator" || role.userCount > 0) return;
    setBusy(true); setError("");
    try { await removeRole(role.id); setNotice("Role deleted."); await loadRoles(); }
    catch (cause) { setError(cause instanceof Error ? cause.message : "Could not delete role."); }
    finally { setBusy(false); }
  }

  async function revokeSession(session: LoginSession) {
    if (session.isCurrent || !window.confirm("Revoke this sign-in session?")) return;
    setBusy(true); setError("");
    try {
      await revokeLoginSession(session.sessionId);
      setNotice("Sign-in session revoked."); await loadSessions();
    } catch (cause) { setError(cause instanceof Error ? cause.message : "Could not revoke the session."); }
    finally { setBusy(false); }
  }

  async function revokeOtherSessions() {
    if (!sessions.some((session) => !session.isCurrent) || !window.confirm("Sign out all other devices?")) return;
    setBusy(true); setError("");
    try {
      await revokeOtherLoginSessions();
      setNotice("All other sign-in sessions were revoked."); await loadSessions();
    } catch (cause) { setError(cause instanceof Error ? cause.message : "Could not revoke other sessions."); }
    finally { setBusy(false); }
  }

  const pageCount = Math.max(1, Math.ceil(users.totalCount / users.pageSize));
  if (loading) return <main className="boot-screen"><span className="brand-mark"><ShieldCheck size={20} /></span><p>Preparing identity workspace</p></main>;

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
    return <main className="boot-screen"><span className="brand-mark"><ShieldCheck size={20} /></span><p>Preparing identity workspace</p></main>;
  }

  return (
    <main className="workspace-shell">
      <aside className="sidebar">
        <div className="brand-lockup sidebar-brand"><span className="brand-mark"><ShieldCheck size={19} /></span><span>Store<span className="brand-light">Management</span></span></div>
        <p className="workspace-label">WORKSPACE</p>
        <nav className="side-nav" aria-label="Identity administration">
          <button className={view === "users" ? "nav-item active" : "nav-item"} onClick={() => navigateToView("users")}><UsersRound size={17} /><span>User directory</span><small>{users.totalCount}</small></button>
          <button className={view === "roles" ? "nav-item active" : "nav-item"} onClick={() => navigateToView("roles")}><Shield size={17} /><span>Roles</span><small>{roles.length}</small></button>
          <button className={view === "sessions" ? "nav-item active" : "nav-item"} onClick={() => navigateToView("sessions")}><Monitor size={17} /><span>Sessions</span><small>{sessions.length}</small></button>
          <button className={view === "security" ? "nav-item active" : "nav-item"} onClick={() => navigateToView("security")}><Settings2 size={17} /><span>Security</span></button>
        </nav>
        <div className="sidebar-bottom"><div className="policy-note"><ShieldCheck size={16} /><span><strong>Protected workspace</strong><small>Administrator policy active</small></span></div><div className="account-row"><span className="account-avatar">{session.email.slice(0, 1).toUpperCase()}</span><span className="account-email">{session.email}</span><button className="icon-button sidebar-signout" title="Sign out" aria-label="Sign out" onClick={signOut} disabled={busy}><LogOut size={16} /></button></div></div>
      </aside>
      <section className="main-panel">
        <header className="topbar"><div className="breadcrumb"><span>Identity</span><i>/</i><strong>{view === "users" ? "User directory" : view === "roles" ? "Roles" : view === "sessions" ? "Sessions" : "Security settings"}</strong></div><div className="topbar-meta"><span className="console-label"><span className="online-dot" /> ADMIN CONSOLE</span><span className="topbar-email">{session.email}</span><button className="icon-button mobile-signout" title="Sign out" aria-label="Sign out" onClick={signOut}><LogOut size={16} /></button></div></header>
        <div className="content-area">
          {(notice || error) && <div className={error ? "notice notice-error" : "notice notice-success"} role={error ? "alert" : "status"}><span>{error || notice}</span><button aria-label="Dismiss" onClick={() => { setError(""); setNotice(""); }}><X size={15} /></button></div>}
          {view === "users" ? <>
            <div className="page-heading"><div><p className="eyebrow">ACCESS GOVERNANCE</p><h1>User directory</h1><p className="subtle">Review account status, authentication, and role membership.</p></div><div className="heading-actions"><div className="total-stat"><strong>{users.totalCount.toLocaleString()}</strong><span>accounts</span></div><button className="primary-button" onClick={() => setNewUserDraft({ email: "", firstName: "", lastName: "", password: "", roles: [] })}><Plus size={15} /> New user</button></div></div>
            <div className="toolbar"><label className="search-field"><Search size={16} /><input aria-label="Search users" placeholder="Search name or email" value={search} onChange={(event) => { setSearch(event.target.value); setPage(1); }} /></label><div className="live-label"><span className="online-dot" /> Live directory <button className="refresh-button" onClick={() => void loadUsers()}>Refresh</button></div></div>
            <div className="table-shell"><div className="table-scroll"><table className="user-table"><thead><tr><th>MEMBER</th><th>ROLE ACCESS</th><th>SECURITY</th><th>STATUS</th><th /></tr></thead><tbody>
              {users.items.map((user, index) => <tr key={user.id} style={{ "--row": index } as React.CSSProperties}>
                <td><div className="member"><span className={`member-avatar avatar-${index % 5}`}>{`${user.firstName.slice(0, 1)}${user.lastName.slice(0, 1)}`.toUpperCase()}</span><span><strong>{user.fullName}</strong><small>{user.email}</small></span></div></td>
                <td><div className="role-list">{user.roles.length ? user.roles.slice(0, 2).map((role) => <span className="role-chip" key={role}>{role}</span>) : <span className="empty-role">No role assigned</span>}{user.roles.length > 2 && <span className="role-chip extra-chip">+{user.roles.length - 2}</span>}</div></td>
                <td><div className="security-list"><span className={user.emailConfirmed ? "verified" : "unverified"}>{user.emailConfirmed ? "Email verified" : "Email pending"}</span><small>{user.twoFactorEnabled ? "2FA enabled" : "2FA off"}</small></div></td>
                <td><span className={user.isActive ? "status active" : "status inactive"}><i />{user.isActive ? "Active" : "Inactive"}</span></td>
                <td><button className="icon-button" title={`Edit ${user.fullName}`} aria-label={`Edit ${user.fullName}`} onClick={() => editUser(user)}><Pencil size={15} /></button></td>
              </tr>)}
              {!users.items.length && <tr><td colSpan={5}><div className="empty-state"><UsersRound size={21} /><strong>No accounts found</strong><span>Try another name or email address.</span></div></td></tr>}
            </tbody></table></div><footer className="table-footer"><span>Showing <strong>{users.totalCount ? (page - 1) * users.pageSize + 1 : 0}–{Math.min(page * users.pageSize, users.totalCount)}</strong> of <strong>{users.totalCount}</strong></span><div className="pager"><button className="icon-button" aria-label="Previous page" disabled={page <= 1} onClick={() => setPage((value) => Math.max(1, value - 1))}><ArrowLeft size={15} /></button><span>Page <strong>{page}</strong> of {pageCount}</span><button className="icon-button" aria-label="Next page" disabled={page >= pageCount} onClick={() => setPage((value) => Math.min(pageCount, value + 1))}><ArrowRight size={15} /></button></div></footer></div>
          </> : view === "roles" ? <>
            <div className="page-heading"><div><p className="eyebrow">ACCESS GOVERNANCE</p><h1>Role catalog</h1><p className="subtle">Organize account access into clearly named roles.</p></div><button className="primary-button" onClick={() => setRoleDraft({ name: "", roleCode: "", description: "" })}><Plus size={16} /> New role</button></div>
            <div className="role-summary"><span className="role-summary-icon"><Shield size={17} /></span><span><strong>{roles.length} roles defined</strong><small>Administrator is protected from deletion.</small></span><span className="summary-end"><ShieldCheck size={16} /> Role-based access</span></div>
            <div className="role-grid">{roles.map((role) => <article className="role-card" key={role.id}><div className="role-card-head"><span className="role-card-icon"><KeyRound size={16} /></span><span className="role-code">{role.roleCode}</span><button className="icon-button" title={`Edit ${role.name}`} aria-label={`Edit ${role.name}`} onClick={() => setRoleDraft({ id: role.id, name: role.name, roleCode: role.roleCode, description: role.description })}><Pencil size={15} /></button></div><h2>{role.name}</h2><p>{role.description}</p><footer><span><UsersRound size={14} />{role.userCount} {role.userCount === 1 ? "member" : "members"}</span><button className="text-button" disabled={role.name === "Administrator" || role.userCount > 0} onClick={() => void deleteRole(role)}>Delete</button></footer></article>)}{!roles.length && <div className="empty-state role-empty"><Shield size={21} /><strong>No roles configured</strong><span>Create a role to organize access.</span></div>}</div>
          </> : view === "sessions" ? <>
            <div className="page-heading"><div><p className="eyebrow">ACCOUNT SECURITY</p><h1>Active sessions</h1><p className="subtle">Review your recent sign-ins and remove devices you no longer use.</p></div><div className="heading-actions"><button className="secondary-button" onClick={() => void refreshSessions()} disabled={busy}>Refresh</button><button className="primary-button" onClick={() => void revokeOtherSessions()} disabled={busy || !sessions.some((session) => !session.isCurrent)}>Sign out other devices</button></div></div>
            <div className="table-shell"><div className="table-scroll"><table className="user-table session-table"><thead><tr><th>DEVICE</th><th>IP ADDRESS</th><th>FIRST SIGN-IN</th><th>LAST ACTIVE</th><th>EXPIRES</th><th>STATUS</th><th /></tr></thead><tbody>
              {sessions.map((loginSession) => <tr key={loginSession.sessionId}>
                <td><div className="member"><span className="member-avatar avatar-3"><Monitor size={16} /></span><span><strong>{loginSession.userAgent || "Unknown device"}</strong><small>{loginSession.isCurrent ? "This device" : "Other device"}</small></span></div></td>
                <td>{loginSession.ipAddress || "Unavailable"}</td>
                <td>{new Date(loginSession.createdAtUtc).toLocaleString()}</td>
                <td>{new Date(loginSession.lastActiveAtUtc).toLocaleString()}</td>
                <td>{new Date(loginSession.expiresAtUtc).toLocaleString()}</td>
                <td><span className={loginSession.isCurrent ? "status active" : "status inactive"}><i />{loginSession.isCurrent ? "Current" : "Active"}</span></td>
                <td>{!loginSession.isCurrent && <button className="text-button" onClick={() => void revokeSession(loginSession)} disabled={busy}>Revoke</button>}</td>
              </tr>)}
              {!sessions.length && <tr><td colSpan={7}><div className="empty-state"><Monitor size={21} /><strong>No active sessions</strong><span>Your current sign-in session will appear here.</span></div></td></tr>}
            </tbody></table></div></div>
          </> : <AccountSecurity userId={session.userId} />}
          <footer className="page-footer"><span>STORE MANAGEMENT <i /> IDENTITY SERVICE</span><span>ACCESS OPERATIONS <ShieldCheck size={13} /></span></footer>
        </div>
      </section>

      {selectedUser && <div className="modal-backdrop" onMouseDown={(event) => { if (event.target === event.currentTarget) setSelectedUser(null); }}><section className="editor-panel" role="dialog" aria-modal="true" aria-labelledby="user-edit-title">
        <header className="editor-header"><div><p className="eyebrow">ACCOUNT PROFILE</p><h2 id="user-edit-title">Edit user</h2></div><button className="icon-button" aria-label="Close editor" onClick={() => setSelectedUser(null)}><X size={18} /></button></header>
        <div className="editor-user"><span className="member-avatar avatar-2">{`${selectedUser.firstName.slice(0, 1)}${selectedUser.lastName.slice(0, 1)}`.toUpperCase()}</span><span><strong>{selectedUser.email}</strong><small>{selectedUser.emailConfirmed ? "Email verified" : "Email unverified"}</small></span></div>
        <form className="editor-form" onSubmit={saveUser}><div className="form-pair"><label>First name<input value={firstName} maxLength={100} onChange={(event) => setFirstName(event.target.value)} required /></label><label>Last name<input value={lastName} maxLength={100} onChange={(event) => setLastName(event.target.value)} required /></label></div>
          <fieldset className="role-picker"><legend>Role membership</legend><p>Choose the roles that grant this account access.</p>{roles.map((role) => <label className="check-row" key={role.id}><input type="checkbox" checked={selectedRoles.includes(role.name)} onChange={(event) => setSelectedRoles(event.target.checked ? [...selectedRoles, role.name] : selectedRoles.filter((item) => item !== role.name))} /><span className="check-box"><Check size={12} /></span><span>{role.name}</span><small>{role.roleCode}</small></label>)}</fieldset>
          <label className="active-row"><span><strong>Account active</strong><small>Inactive accounts cannot sign in.</small></span><input type="checkbox" checked={active} onChange={(event) => setActive(event.target.checked)} /><span className="switch" /></label>
          <div className="editor-actions"><button type="button" className="secondary-button" onClick={() => setSelectedUser(null)}>Cancel</button><button className="primary-button" disabled={busy}>{busy ? "Saving…" : "Save changes"}<Check size={15} /></button></div>
        </form>
      </section></div>}

      {newUserDraft && <div className="modal-backdrop" onMouseDown={(event) => { if (event.target === event.currentTarget) setNewUserDraft(null); }}><section className="dialog-panel" role="dialog" aria-modal="true" aria-labelledby="new-user-title">
        <header className="editor-header"><div><p className="eyebrow">USER DIRECTORY</p><h2 id="new-user-title">Create account</h2></div><button className="icon-button" aria-label="Close dialog" onClick={() => setNewUserDraft(null)}><X size={18} /></button></header>
        <form className="editor-form" onSubmit={createUser}>
          <label>Email address<input type="email" autoComplete="email" value={newUserDraft.email} onChange={(event) => setNewUserDraft({ ...newUserDraft, email: event.target.value })} required placeholder="person@company.com" /></label>
          <div className="form-pair"><label>First name<input value={newUserDraft.firstName} maxLength={100} onChange={(event) => setNewUserDraft({ ...newUserDraft, firstName: event.target.value })} required /></label><label>Last name<input value={newUserDraft.lastName} maxLength={100} onChange={(event) => setNewUserDraft({ ...newUserDraft, lastName: event.target.value })} required /></label></div>
          <label>Temporary password<input type="password" autoComplete="new-password" minLength={8} value={newUserDraft.password} onChange={(event) => setNewUserDraft({ ...newUserDraft, password: event.target.value })} required /><small className="field-hint">At least 8 characters and one digit. The user can change it after signing in.</small></label>
          <fieldset className="role-picker"><legend>Initial roles <span className="optional">optional</span></legend>{roles.map((role) => <label className="check-row" key={role.id}><input type="checkbox" checked={newUserDraft.roles.includes(role.name)} onChange={(event) => setNewUserDraft({ ...newUserDraft, roles: event.target.checked ? [...newUserDraft.roles, role.name] : newUserDraft.roles.filter((item) => item !== role.name) })} /><span className="check-box"><Check size={12} /></span><span>{role.name}</span><small>{role.roleCode}</small></label>)}</fieldset>
          <p className="create-note">Email confirmation is required before sign-in. Delivery requires SMTP configuration.</p>
          <div className="editor-actions"><button className="secondary-button" type="button" onClick={() => setNewUserDraft(null)}>Cancel</button><button className="primary-button" disabled={busy}>{busy ? "Creating…" : "Create account"}<Check size={15} /></button></div>
        </form>
      </section></div>}

      {roleDraft && <div className="modal-backdrop" onMouseDown={(event) => { if (event.target === event.currentTarget) setRoleDraft(null); }}><section className="dialog-panel" role="dialog" aria-modal="true" aria-labelledby="role-edit-title">
        <header className="editor-header"><div><p className="eyebrow">ROLE CATALOG</p><h2 id="role-edit-title">{roleDraft.id ? "Edit role" : "Create role"}</h2></div><button className="icon-button" aria-label="Close dialog" onClick={() => setRoleDraft(null)}><X size={18} /></button></header>
        <form className="editor-form" onSubmit={saveRole}><label>Role name<input value={roleDraft.name} maxLength={50} onChange={(event) => setRoleDraft({ ...roleDraft, name: event.target.value })} required placeholder="e.g. Store Manager" /></label><label>Role code<input value={roleDraft.roleCode} maxLength={50} onChange={(event) => setRoleDraft({ ...roleDraft, roleCode: event.target.value.toUpperCase().replace(/[^A-Z0-9_-]/g, "") })} required placeholder="STORE_MANAGER" /></label><label>Description<textarea rows={3} maxLength={250} value={roleDraft.description} onChange={(event) => setRoleDraft({ ...roleDraft, description: event.target.value })} required placeholder="Describe the role's responsibility" /></label><div className="editor-actions"><button className="secondary-button" type="button" onClick={() => setRoleDraft(null)}>Cancel</button><button className="primary-button" disabled={busy}>{busy ? "Saving…" : roleDraft.id ? "Save role" : "Create role"}<Check size={15} /></button></div></form>
      </section></div>}
    </main>
  );
}