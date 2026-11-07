"use client";

import { useEffect, useState, type FormEvent } from "react";
import {
  changePassword as changeAccountPassword,
  disableTwoFactor as disableTwoFactorRequest,
  enableTwoFactor as enableTwoFactorRequest,
  getAuthenticatorSetup,
} from "../api/auth.api";
import { getAccountProfile } from "../api/users.api";
import type { AccountSecurityProps, AuthenticatorSetup } from "../interfaces";

export default function AccountSecurity({ userId }: AccountSecurityProps) {
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
        if (mounted) setError(cause instanceof Error ? cause.message : "Could not load account security.");
      })
      .finally(() => {
        if (mounted) setLoadingProfile(false);
      });

    return () => { mounted = false; };
  }, [userId]);

  async function changePassword(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!userId || newPassword !== confirmPassword) return;
    setBusy(true); setError(""); setNotice("");
    try {
      await changeAccountPassword({ currentPassword, newPassword });
      setCurrentPassword(""); setNewPassword(""); setConfirmPassword("");
      setNotice("Password changed successfully.");
    } catch (cause) {
      setError(cause instanceof Error ? cause.message : "Could not change the password.");
    } finally {
      setBusy(false);
    }
  }

  async function createSetup() {
    setBusy(true); setError(""); setNotice("");
    try {
      setSetup(await getAuthenticatorSetup());
    } catch (cause) {
      setError(cause instanceof Error ? cause.message : "Could not create an authenticator setup.");
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
      setNotice("Two-factor authentication enabled.");
    } catch (cause) {
      setError(cause instanceof Error ? cause.message : "Could not enable two-factor authentication.");
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
      setNotice("Two-factor authentication disabled.");
    } catch (cause) {
      setError(cause instanceof Error ? cause.message : "Could not disable two-factor authentication.");
    } finally {
      setBusy(false);
    }
  }

  const visibleError = error || (!userId ? "Could not identify the signed-in account. Sign in again and retry." : "");

  return (
    <>
      <div className="page-heading">
        <div>
          <p className="eyebrow">ACCOUNT SECURITY</p>
          <h1>Security settings</h1>
          <p className="subtle">Update your password and strengthen sign-in with an authenticator app.</p>
        </div>
      </div>
      {(notice || visibleError) && (
        <div className={visibleError ? "notice notice-error" : "notice notice-success"} role={visibleError ? "alert" : "status"}>
          <span>{visibleError || notice}</span>
          <button aria-label="Dismiss" onClick={() => { setError(""); setNotice(""); }}>×</button>
        </div>
      )}
      <div className="security-settings-grid">
        <section className="security-settings-card">
          <div className="security-settings-heading">
            <span className="role-card-icon"><span aria-hidden="true">•••</span></span>
            <div><h2>Change password</h2><p>Choose a password with at least 8 characters and one digit.</p></div>
          </div>
          <form className="editor-form" onSubmit={changePassword}>
            <label>Current password<input type="password" autoComplete="current-password" value={currentPassword} onChange={(event) => setCurrentPassword(event.target.value)} required /></label>
            <label>New password<input type="password" autoComplete="new-password" minLength={8} pattern="(?=.*[0-9]).{8,}" title="Use at least 8 characters and one digit." value={newPassword} onChange={(event) => setNewPassword(event.target.value)} required /></label>
            <label>Confirm new password<input type="password" autoComplete="new-password" minLength={8} value={confirmPassword} onChange={(event) => setConfirmPassword(event.target.value)} required /></label>
            {confirmPassword && newPassword !== confirmPassword && <p className="security-field-error">Passwords do not match.</p>}
            <div className="editor-actions"><button className="primary-button" disabled={busy || !userId || newPassword !== confirmPassword}>{busy ? "Saving…" : "Change password"}</button></div>
          </form>
        </section>

        <section className="security-settings-card">
          <div className="security-settings-heading">
            <span className="role-card-icon">2FA</span>
            <div><h2>Authenticator app</h2><p>Require a time-based code in addition to your password when signing in.</p></div>
          </div>
          {loadingProfile ? <p className="security-loading">Loading account security…</p> : twoFactorEnabled ? (
            <form className="editor-form" onSubmit={disableTwoFactor}>
              <p className="security-state"><span className="online-dot" /> Two-factor authentication is enabled.</p>
              <label>Authenticator code<input inputMode="numeric" autoComplete="one-time-code" pattern="[0-9]{6}" maxLength={6} value={code} onChange={(event) => setCode(event.target.value.replace(/\D/g, ""))} required /></label>
              <p className="field-hint">Enter the current 6-digit code to turn off two-factor authentication.</p>
              <div className="editor-actions"><button className="secondary-button security-danger-button" disabled={busy || code.length !== 6}>{busy ? "Saving…" : "Disable 2FA"}</button></div>
            </form>
          ) : setup ? (
            <form className="editor-form" onSubmit={enableTwoFactor}>
              <p className="create-note">Add this account to your authenticator app using the setup key, then enter the current 6-digit code to verify it.</p>
              <label>Setup key<input className="security-secret" value={setup.sharedKey} readOnly onFocus={(event) => event.currentTarget.select()} /></label>
              <label>Authenticator link<input className="security-secret" value={setup.authenticatorUri} readOnly onFocus={(event) => event.currentTarget.select()} /></label>
              <label>Verification code<input inputMode="numeric" autoComplete="one-time-code" pattern="[0-9]{6}" maxLength={6} value={code} onChange={(event) => setCode(event.target.value.replace(/\D/g, ""))} required /></label>
              <div className="editor-actions"><button type="button" className="secondary-button" onClick={() => { setSetup(null); setCode(""); }}>Cancel</button><button className="primary-button" disabled={busy || code.length !== 6}>{busy ? "Verifying…" : "Enable 2FA"}</button></div>
            </form>
          ) : (
            <div className="security-enable">
              <p>Use an authenticator app to generate secure, time-based sign-in codes.</p>
              <button className="primary-button" onClick={() => void createSetup()} disabled={busy || loadingProfile}>{busy ? "Preparing…" : "Set up authenticator"}</button>
            </div>
          )}
        </section>
      </div>
    </>
  );
}
