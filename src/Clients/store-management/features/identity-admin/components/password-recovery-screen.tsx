"use client";

import { zodResolver } from "@hookform/resolvers/zod";
import { ArrowRight, Globe2, KeyRound, ShieldCheck, Store } from "lucide-react";
import Link from "next/link";
import { useSearchParams } from "next/navigation";
import { useState } from "react";
import { useForm } from "react-hook-form";
import { useTranslations } from "next-intl";
import { useAppLocale } from "@/app/providers";
import { requestPasswordReset, resetPassword } from "../api/auth.api";
import {
  requestPasswordResetSchema,
  resetPasswordSchema,
  type RequestPasswordResetValues,
  type ResetPasswordValues,
} from "../schemas/password-recovery.schema";

function LanguageSelector() {
  const { locale, setLocale } = useAppLocale();
  const t = useTranslations("login");

  return (
    <div className="login-language" role="group" aria-label={t("chooseLanguage")}>
      <Globe2 size={15} />
      <button type="button" className={locale === "vi" ? "selected" : ""} aria-label={t("vietnamese")} aria-pressed={locale === "vi"} onClick={() => setLocale("vi")}><span>VI</span> {t("vietnamese")}</button>
      <span aria-hidden="true">/</span>
      <button type="button" className={locale === "en" ? "selected" : ""} aria-label={t("english")} aria-pressed={locale === "en"} onClick={() => setLocale("en")}><span>EN</span> {t("english")}</button>
    </div>
  );
}

function RequestResetForm({ initialEmail, invalidLink = false }: { initialEmail: string; invalidLink?: boolean }) {
  const t = useTranslations("login");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState(invalidLink ? t("invalidResetLink") : "");
  const [sent, setSent] = useState(false);
  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<RequestPasswordResetValues>({
    resolver: zodResolver(requestPasswordResetSchema),
    defaultValues: { email: initialEmail },
  });
  const emailError = errors.email
    ? errors.email.message === "emailInvalid"
      ? t("validation.emailInvalid")
      : t("validation.required")
    : undefined;

  async function submit(values: RequestPasswordResetValues) {
    setBusy(true);
    setError("");
    try {
      await requestPasswordReset(values.email);
      setSent(true);
    } catch {
      setError(t("requestResetError"));
    } finally {
      setBusy(false);
    }
  }

  return (
    <form className="login-card" onSubmit={handleSubmit(submit)} noValidate>
      <div className="login-symbol"><KeyRound size={19} /></div>
      <p className="eyebrow">{t("secure")}</p>
      <h2>{t("forgotTitle")}</h2>
      <p className="subtle">{t("forgotDescription")}</p>
      {sent ? (
        <>
          <p className="login-notice" role="status">{t("resetEmailSent")}</p>
          <Link className="primary-button full-button recovery-link-button" href="/login">{t("backToLogin")}<ArrowRight size={16} /></Link>
        </>
      ) : (
        <>
          <label htmlFor="recovery-email">{t("email")}</label>
          <input
            id="recovery-email"
            type="email"
            autoComplete="email"
            placeholder={t("emailPlaceholder")}
            aria-invalid={Boolean(errors.email)}
            aria-describedby={errors.email ? "recovery-email-error" : undefined}
            {...register("email")}
          />
          {emailError && <p id="recovery-email-error" className="form-field-error" role="alert">{emailError}</p>}
          {error && <p className="form-error" role="alert">{error}</p>}
          <button className="primary-button full-button" disabled={busy}>
            {busy ? t("sendingReset") : t("requestReset")}<ArrowRight size={16} />
          </button>
          <Link className="recovery-back-link" href="/login">{t("backToLogin")}</Link>
        </>
      )}
      <p className="login-footnote"><ShieldCheck size={14} />{t("protection")}</p>
    </form>
  );
}

function NewPasswordForm({ email, token }: { email: string; token: string }) {
  const t = useTranslations("login");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState("");
  const [complete, setComplete] = useState(false);
  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<ResetPasswordValues>({
    resolver: zodResolver(resetPasswordSchema),
    defaultValues: { newPassword: "", confirmPassword: "" },
  });

  async function submit(values: ResetPasswordValues) {
    setBusy(true);
    setError("");
    try {
      await resetPassword({ email, token, newPassword: values.newPassword });
      setComplete(true);
    } catch {
      setError(t("resetPasswordError"));
    } finally {
      setBusy(false);
    }
  }

  return (
    <form className="login-card" onSubmit={handleSubmit(submit)} noValidate>
      <div className="login-symbol"><KeyRound size={19} /></div>
      <p className="eyebrow">{t("secure")}</p>
      <h2>{complete ? t("passwordResetSuccessTitle") : t("resetTitle")}</h2>
      <p className="subtle">{complete ? t("passwordResetSuccess") : t("resetDescription")}</p>
      {complete ? (
        <Link className="primary-button full-button recovery-link-button" href="/login">{t("backToLogin")}<ArrowRight size={16} /></Link>
      ) : (
        <>
          <label htmlFor="new-password">{t("newPassword")}</label>
          <input
            id="new-password"
            type="password"
            autoComplete="new-password"
            aria-invalid={Boolean(errors.newPassword)}
            aria-describedby={errors.newPassword ? "new-password-error" : undefined}
            {...register("newPassword")}
          />
          {errors.newPassword && <p id="new-password-error" className="form-field-error" role="alert">{t("passwordPattern")}</p>}
          <label htmlFor="confirm-new-password">{t("confirmNewPassword")}</label>
          <input
            id="confirm-new-password"
            type="password"
            autoComplete="new-password"
            aria-invalid={Boolean(errors.confirmPassword)}
            aria-describedby={errors.confirmPassword ? "confirm-password-error" : undefined}
            {...register("confirmPassword")}
          />
          {errors.confirmPassword && <p id="confirm-password-error" className="form-field-error" role="alert">{t(errors.confirmPassword.message === "passwordMismatch" ? "passwordMismatch" : "validation.required")}</p>}
          {error && <p className="form-error" role="alert">{error}</p>}
          <button className="primary-button full-button" disabled={busy}>
            {busy ? t("resettingPassword") : t("updatePassword")}<ArrowRight size={16} />
          </button>
          <Link className="recovery-back-link" href="/forgot-password">{t("requestAnotherReset")}</Link>
        </>
      )}
      <p className="login-footnote"><ShieldCheck size={14} />{t("protection")}</p>
    </form>
  );
}

export default function PasswordRecoveryScreen() {
  const searchParams = useSearchParams();
  const email = searchParams.get("email") ?? "";
  const token = searchParams.get("token") ?? "";
  const t = useTranslations("login");
  const hasResetLink = Boolean(email && token);
  const invalidLink = Boolean(token && !email);

  return (
    <main className="login-shell login-shell-store">
      <section className="login-art">
        <header className="login-brand-row">
          <Link className="brand-lockup" href="/" aria-label="Store Management home">
            <span className="brand-mark"><Store size={19} /></span>
            <span>Store<span className="brand-light">Management</span></span>
          </Link>
          <span className="login-platform-label"><span className="online-dot" /> {t("workspace")}</span>
        </header>
        <div className="login-showcase">
          <div className="login-message">
            <p className="eyebrow">{t("workspace")}</p>
            <h1>{t("recoveryHeroTitle")}</h1>
            <p>{t("recoveryHeroDescription")}</p>
          </div>
        </div>
        <footer className="login-bottom"><span className="login-footer-mark"><ShieldCheck size={14} /></span>{t("footer")}<i /><span>© 2026 Store Management</span></footer>
      </section>
      <section className="login-area">
        <LanguageSelector />
        {hasResetLink
          ? <NewPasswordForm email={email} token={token} />
          : <RequestResetForm initialEmail={email} invalidLink={invalidLink} />}
        <div className="login-area-footer">STORE MANAGEMENT <span>·</span> {t("platformFooter")}</div>
      </section>
    </main>
  );
}
