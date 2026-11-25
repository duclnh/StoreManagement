"use client";

import { useEffect, useState } from "react";
import { useLocale, useTranslations } from "next-intl";
import { Monitor, X } from "lucide-react";
import { listUserSessions, revokeUserSession } from "../api/sessions.api";
import { getSecurityAuditPage } from "../api/security-audit.api";
import { formatSessionDate, getSessionDeviceLabel } from "../utils/session-display";
import type { LoginSession, SecurityAuditPage, User } from "../interfaces";

export default function UserSessions({ user, scopeId, canRevoke, canViewAudit, onClose }: {
  user: User; scopeId: string; canRevoke: boolean; canViewAudit: boolean; onClose: () => void;
}) {
  const a = useTranslations("admin");
  const locale = useLocale();
  const [sessions, setSessions] = useState<LoginSession[]>([]);
  const [audit, setAudit] = useState<SecurityAuditPage | null>(null);
  const [page, setPage] = useState(1);
  const [loading, setLoading] = useState(true);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState("");
  useEffect(() => {
    let active = true;
    Promise.all([listUserSessions(user.id, scopeId), canViewAudit
      ? getSecurityAuditPage(page, { userId: user.id, eventType: "Authentication.Login" }, scopeId) : Promise.resolve(null)])
      .then(([items, events]) => { if (active) { setSessions(items); setAudit(events); } })
      .catch(cause => { if (active) setError(cause instanceof Error ? cause.message : a("sessions.loadError")); })
      .finally(() => { if (active) setLoading(false); });
    return () => { active = false; };
  }, [a, user.id, scopeId, canViewAudit, page]);

  async function revoke(session: LoginSession) {
    if (!window.confirm(a("sessions.confirmRevoke"))) return;
    setBusy(true); setError("");
    try {
      await revokeUserSession(user.id, session.sessionId, scopeId);
      setSessions(current => current.filter(item => item.sessionId !== session.sessionId));
    } catch (cause) { setError(cause instanceof Error ? cause.message : a("sessions.revokeError")); }
    finally { setBusy(false); }
  }

  return <div className="modal-backdrop"><section className="dialog-panel user-sessions-dialog" role="dialog" aria-modal="true" aria-labelledby="user-sessions-title">
    <header className="editor-header"><div><p className="eyebrow">{user.email}</p><h2 id="user-sessions-title">{a("sessions.userDevices")}</h2></div><button className="icon-button" aria-label={a("common.dismiss")} onClick={onClose} disabled={busy}><X size={18} /></button></header>
    {error && <p role="alert" className="form-error">{error}</p>}
    {loading ? <p role="status">{a("common.loadingWorkspace")}</p> : <>
      <div className="user-session-list">{sessions.map(session => <article key={session.sessionId}><Monitor size={21} /><div><strong>{getSessionDeviceLabel(session.userAgent, a("common.unknown")).browser}</strong><small>{getSessionDeviceLabel(session.userAgent, a("common.unknown")).operatingSystem} · {session.ipAddress ?? a("common.unknown")}</small><small>{a("sessions.lastActive")}: {formatSessionDate(session.lastActiveAtUtc, locale, a("common.unknown"))}</small></div>{session.isCurrent ? <span className="status active">{a("sessions.current")}</span> : canRevoke && <button className="secondary-button" disabled={busy} onClick={() => void revoke(session)}>{a("sessions.revoke")}</button>}</article>)}{!sessions.length && <p>{a("sessions.emptyTitle")}</p>}</div>
      {audit && <><h3>{a("sessions.loginHistory")}</h3><div className="table-scroll"><table className="user-table"><thead><tr><th>{a("audit.time")}</th><th>{a("audit.outcome")}</th><th>{a("audit.ipAddress")}</th></tr></thead><tbody>{audit.items.map(event => <tr key={event.id}><td>{formatSessionDate(event.occurredAtUtc, locale, a("common.unknown"))}</td><td>{event.outcome === "Succeeded" ? a("audit.succeeded") : a("audit.failed")}</td><td>{event.ipAddress}</td></tr>)}</tbody></table></div><div className="editor-actions"><button className="secondary-button" disabled={page === 1 || loading} onClick={() => { setLoading(true); setPage(p => p - 1); }}>{a("common.previous")}</button><span>{page}</span><button className="secondary-button" disabled={!audit.hasNextPage || loading} onClick={() => { setLoading(true); setPage(p => p + 1); }}>{a("common.next")}</button></div></>}
    </>}
  </section></div>;
}
