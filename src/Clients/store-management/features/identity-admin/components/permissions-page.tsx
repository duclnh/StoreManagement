"use client";

import { useEffect, useState, type FormEvent } from "react";
import { useTranslations } from "next-intl";
import { Check, Search, ShieldCheck } from "lucide-react";
import { getRolePermissions, setRolePermissions } from "../api/roles.api";
import type { Role, RolePermission } from "../interfaces";

export default function PermissionsPage({ roles, scopeId }: { roles: Role[]; scopeId: string }) {
  const a = useTranslations("admin");
  const available = roles.filter(role => role.name !== "Administrator");
  const [roleId, setRoleId] = useState(available[0]?.id ?? "");
  const [permissions, setPermissions] = useState<RolePermission[] | null>(null);
  const [original, setOriginal] = useState<RolePermission[]>([]);
  const [search, setSearch] = useState("");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState("");
  const [notice, setNotice] = useState("");
  const dirty = permissions !== null && JSON.stringify(permissions) !== JSON.stringify(original);

  useEffect(() => {
    if (!roleId) return;
    let active = true;
    getRolePermissions(roleId, scopeId).then(result => {
      if (active) { setPermissions(result); setOriginal(result); }
    }).catch(cause => { if (active) setError(cause instanceof Error ? cause.message : a("permissions.loadError")); });
    return () => { active = false; };
  }, [a, roleId, scopeId]);

  async function save(event: FormEvent) {
    event.preventDefault();
    if (!permissions || !dirty || busy) return;
    setBusy(true); setError(""); setNotice("");
    try {
      await setRolePermissions(roleId, scopeId, { permissions: permissions.filter(p => p.isGranted && p.actionId !== "Revoke").map(({ functionId, actionId }) => ({ functionId, actionId })) });
      setOriginal(permissions); setNotice(a("permissions.saved"));
    } catch (cause) { setError(cause instanceof Error ? cause.message : a("permissions.saveError")); }
    finally { setBusy(false); }
  }

  const actions = Array.from(new Set(permissions?.map(p => p.actionId).filter(action => action !== "Revoke") ?? []));
  const functions = Array.from(new Map(permissions?.map(p => [p.functionId, p.functionName]) ?? []));
  const resources: Record<string, "users" | "roles" | "sessions" | "security"> = { "Identity.Users": "users", "Identity.Roles": "roles", "Identity.Sessions": "sessions", "Identity.Security": "security" };
  const actionKeys: Record<string, "view" | "create" | "update" | "delete" | "import" | "export" | "assign"> = { View: "view", Create: "create", Update: "update", Delete: "delete", Import: "import", Export: "export", Assign: "assign" };

  return <>
    <div className="page-heading"><div><p className="eyebrow">{a("permissions.eyebrow")}</p><h1>{a("permissions.pageTitle")}</h1><p className="subtle">{a("permissions.help")}</p></div><ShieldCheck size={30} /></div>
    <form className="permissions-workbench" onSubmit={save}>
      <div className="permissions-controls">
        <label className="tenant-select">{a("navigation.roles")}<select value={roleId} disabled={busy || dirty} onChange={event => { setRoleId(event.target.value); setPermissions(null); setError(""); setNotice(""); }}>{available.map(role => <option value={role.id} key={role.id}>{role.name}</option>)}</select></label>
        <label className="search-field"><Search size={16} /><input type="search" value={search} onChange={event => setSearch(event.target.value)} aria-label={a("permissions.search")} placeholder={a("permissions.search")} /></label>
        <span className="permissions-counter">{permissions?.filter(p => p.isGranted && p.actionId !== "Revoke").length ?? 0} / {actions.length * functions.length}</span>
      </div>
      {error && <p className="form-error" role="alert">{error}</p>}
      {notice && <p className="login-notice" role="status">{notice}</p>}
      {roleId && !permissions && !error ? <div className="empty-state" role="status">{a("common.loadingWorkspace")}</div> : !available.length ? <div className="empty-state"><ShieldCheck /><strong>{a("roles.emptyTitle")}</strong><span>{a("permissions.administratorNote")}</span></div> : <div className="permission-matrix-wrap">
        <table className="permission-matrix"><thead><tr><th>{a("permissions.resource")}</th>{actions.map(action => <th key={action}>{actionKeys[action] ? a(`permissions.actions.${actionKeys[action]}`) : action}</th>)}</tr></thead>
          <tbody>{functions.filter(([id, name]) => `${id} ${name} ${resources[id] ? a(`permissions.functions.${resources[id]}`) : ""}`.toLocaleLowerCase().includes(search.toLocaleLowerCase())).map(([id, name]) => <tr key={id}><th scope="row">{resources[id] ? a(`permissions.functions.${resources[id]}`) : name}<small>{id}</small></th>{actions.map(action => {
            const item = permissions?.find(p => p.functionId === id && p.actionId === action);
            return <td key={action}>{item ? <label className="permission-cell"><input type="checkbox" checked={item.isGranted} disabled={busy} aria-label={`${name}: ${action}`} onChange={event => setPermissions(current => current?.map(p => p === item ? { ...p, isGranted: event.target.checked } : p) ?? null)} /><span className="check-box"><Check size={12} /></span></label> : <span aria-label={a("permissions.notApplicable")}>—</span>}</td>;
          })}</tr>)}</tbody></table>
      </div>}
      <footer className="permissions-savebar"><span>{dirty ? a("permissions.unsaved") : a("permissions.administratorNote")}</span><div><button type="button" className="secondary-button" disabled={!dirty || busy} onClick={() => { setPermissions(original); setNotice(""); }}>{a("common.cancel")}</button><button className="primary-button" disabled={!dirty || busy}>{busy ? a("common.saving") : a("common.save")}<Check size={15} /></button></div></footer>
    </form>
  </>;
}
