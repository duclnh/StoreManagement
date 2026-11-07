"use client";

import { useEffect, useState, type FormEvent } from "react";
import { useTranslations } from "next-intl";
import {
  changePassword as changeAccountPassword,
  disableTwoFactor as disableTwoFactorRequest,
  enableTwoFactor as enableTwoFactorRequest,
  getAuthenticatorSetup,
} from "../api/auth.api";
import { getAccountProfile } from "../api/users.api";
import type { AccountSecurityProps, AuthenticatorSetup } from "../interfaces";

export default function AccountSecurity({ userId, onPasswordChanged }: AccountSecurityProps) {
  const t = useTranslations("admin.security");
  const common = useTranslations("admin.common");
  const [twoFactorEnabled, setTwoFactorEnabled] = useState(false);
  const [loadingProfile, setLoadingProfile] = useState(Boolean(userId));
  const [busy, setBusy] = useState(false);
  const [setup, setSetup] = useState<AuthenticatorSetup | null>(null);
  const [currentPassword, setCurrentPassword] = useState("");
  const [newPassword, setNewPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");
  const [code, setCode] = useState("");
  const [notice, setNotice] = useState("");
  const [error, setError] = useState("");

  useEffect(() => {
    let mounted = true;
    if (!userId) return () => { mounted = false; };

    getAccountProfile(userId)
      .then((profile) => {
        if (mounted) setTwoFactorEnabled(profile.twoFactorEnabled);
      })
      .catch((cause: unknown) => {
        if (mounted) setError(cause instanceof Error ? cause.message : t("profileError"));
      })
      .finally(() => {
        if (mounted) setLoadingProfile(false);
      });

    return () => { mounted = false; };
  }, [t, userId]);

  async function changePassword(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!userId || newPassword !== confirmPassword) return;
    setBusy(true); setError(""); setNotice("");
    try {
      await changeAccountPassword({ currentPassword, newPassword });
      setCurrentPassword(""); setNewPassword(""); setConfirmPassword("");
      onPasswordChanged();
    } catch (cause) {
      setError(cause instanceof Error ? cause.message : t("changePasswordError"));
    } finally {
      setBusy(false);
    }
  }

  async function createSetup() {
    setBusy(true); setError(""); setNotice("");
    try {
      setSetup(await getAuthenticatorSetup());
    } catch (cause) {
      setError(cause instanceof Error ? cause.message : t("setupError"));
    } finally {
      setBusy(false);
    }
  }

  async function enableTwoFactor(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setBusy(true); setError(""); setNotice("");
    try {
      await enableTwoFactorRequest({ code });
      setTwoFactorEnabled(true); setSetup(null); setCode("");
      setNotice(t("twoFactorOn"));
    } catch (cause) {
      setError(cause instanceof Error ? cause.message : t("enableError"));
    } finally {
      setBusy(false);
    }
  }

  async function disableTwoFactor(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setBusy(true); setError(""); setNotice("");
    try {
      await disableTwoFactorRequest({ code });
      setTwoFactorEnabled(false); setCode("");
      setNotice(t("twoFactorDisabled"));
    } catch (cause) {
      setError(cause instanceof Error ? cause.message : t("disableError"));
    } finally {
      setBusy(false);
    }
  }

  const visibleError = error || (!userId ? t("passwordMissing") : "");

  return (
    <>
      <div className="page-heading">
        <div>
          <p className="eyebrow">{t("eyebrow")}</p>
          <h1>{t("title")}</h1>
          <p className="subtle">{t("description")}</p>
        </div>
      </div>
      {(notice || visibleError) && (
        <div className={visibleError ? "notice notice-error" : "notice notice-success"} role={visibleError ? "alert" : "status"}>
          <span>{visibleError || notice}</span>
          <button aria-label={common("dismiss")} onClick={() => { setError(""); setNotice(""); }}>×</button>
        </div>
      )}
      <div className="security-settings-grid">
        <section className="security-settings-card">
          <div className="security-settings-heading">
            <span className="role-card-icon"><span aria-hidden="true">•••</span></span>
            <div><h2>{t("changePassword")}</h2><p>{t("passwordHelp")}</p></div>
          </div>
          <form className="editor-form" onSubmit={changePassword}>
            <label>{t("currentPassword")}<input type="password" autoComplete="current-password" value={currentPassword} onChange={(event) => setCurrentPassword(event.target.value)} required /></label>
            <label>{t("newPassword")}<input type="password" autoComplete="new-password" minLength={8} pattern="(?=.*[0-9]).{8,}" title={t("passwordPattern")} value={newPassword} onChange={(event) => setNewPassword(event.target.value)} required /></label>
            <label>{t("confirmPassword")}<input type="password" autoComplete="new-password" minLength={8} value={confirmPassword} onChange={(event) => setConfirmPassword(event.target.value)} required /></label>
            {confirmPassword && newPassword !== confirmPassword && <p className="security-field-error">{t("passwordMismatch")}</p>}
            <div className="editor-actions"><button className="primary-button" disabled={busy || !userId || newPassword !== confirmPassword}>{busy ? common("saving") : t("save")}</button></div>
          </form>
        </section>

        <section className="security-settings-card">
          <div className="security-settings-heading">
            <span className="role-card-icon">2FA</span>
            <div><h2>{t("authenticator")}</h2><p>{t("authenticatorHelp")}</p></div>
          </div>
          {loadingProfile ? <p className="security-loading">{t("loading")}</p> : twoFactorEnabled ? (
            <form className="editor-form" onSubmit={disableTwoFactor}>
              <p className="security-state"><span className="online-dot" /> {t("twoFactorOn")}</p>
              <label>{t("authenticatorCode")}<input inputMode="numeric" autoComplete="one-time-code" pattern="[0-9]{6}" maxLength={6} value={code} onChange={(event) => setCode(event.target.value.replace(/\D/g, ""))} required /></label>
              <p className="field-hint">{t("disableHelp")}</p>
              <div className="editor-actions"><button className="secondary-button security-danger-button" disabled={busy || code.length !== 6}>{busy ? common("saving") : t("disable")}</button></div>
            </form>
          ) : setup ? (
            <form className="editor-form" onSubmit={enableTwoFactor}>
              <p className="create-note">{t("setupHelp")}</p>
              <label>{t("setupKey")}<input className="security-secret" value={setup.sharedKey} readOnly onFocus={(event) => event.currentTarget.select()} /></label>
              <label>{t("authenticatorLink")}<input className="security-secret" value={setup.authenticatorUri} readOnly onFocus={(event) => event.currentTarget.select()} /></label>
              <label>{t("verificationCode")}<input inputMode="numeric" autoComplete="one-time-code" pattern="[0-9]{6}" maxLength={6} value={code} onChange={(event) => setCode(event.target.value.replace(/\D/g, ""))} required /></label>
              <div className="editor-actions"><button type="button" className="secondary-button" onClick={() => { setSetup(null); setCode(""); }}>{common("cancel")}</button><button className="primary-button" disabled={busy || code.length !== 6}>{busy ? t("verifying") : t("enable")}</button></div>
            </form>
          ) : (
            <div className="security-enable">
              <p>{t("useAuthenticator")}</p>
              <button className="primary-button" onClick={() => void createSetup()} disabled={busy || loadingProfile}>{busy ? t("preparing") : t("setup")}</button>
            </div>
          )}
        </section>
      </div>
    </>
  );
}
