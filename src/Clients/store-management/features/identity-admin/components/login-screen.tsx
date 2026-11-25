"use client";

import { useState } from "react";
import { useTranslations } from "next-intl";
import { Eye, EyeOff, ArrowRight, Cpu, Network, UsersRound, Globe2, KeyRound, ShieldCheck } from "lucide-react";
import Link from "next/link";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { useAppLocale } from "@/app/providers";
import type { LoginScreenProps } from "../interfaces";
import {
  loginCredentialsSchema,
  twoFactorCodeSchema,
  type LoginCredentialsValues,
  type TwoFactorCodeValues,
} from "../schemas/login.schema";

interface LoginFormState {
  busy: boolean;
  email: string;
  error: string;
  notice: string;
}

function CredentialsForm({
  busy,
  email,
  error,
  notice,
  onSubmit,
}: LoginFormState & { onSubmit: (credentials: LoginCredentialsValues) => void }) {
  const [showPassword, setShowPassword] = useState(false);
  const t = useTranslations("login");
  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<LoginCredentialsValues>({
    resolver: zodResolver(loginCredentialsSchema),
    defaultValues: { email, password: "" },
  });
  const emailError = errors.email?.message === "emailInvalid"
    ? t("validation.emailInvalid")
    : errors.email
      ? t("validation.required")
      : undefined;
  const passwordError = errors.password ? t("validation.required") : undefined;

  return (
    <form className="login-card" onSubmit={handleSubmit(onSubmit)} noValidate>
      <div className="login-symbol"><KeyRound size={19} /></div>
      <p className="eyebrow">{t("secure")}</p>
      <h2>{t("welcome")}</h2>
      <p className="subtle">{t("welcomeDescription")}</p>
      <label htmlFor="login-email">{t("email")}</label>
      <input
        id="login-email"
        type="email"
        autoComplete="username"
        placeholder={t("emailPlaceholder")}
        aria-invalid={Boolean(errors.email)}
        aria-describedby={errors.email ? "login-email-error" : undefined}
        {...register("email")}
      />
      {emailError && <p id="login-email-error" className="form-field-error" role="alert">{emailError}</p>}
      <label htmlFor="login-password">{t("password")}</label>
      <div className="password-control"><input
        id="login-password"
        type={showPassword ? "text" : "password"}
        autoComplete="current-password"
        placeholder={t("passwordPlaceholder")}
        aria-invalid={Boolean(errors.password)}
        aria-describedby={errors.password ? "login-password-error" : undefined}
        {...register("password")}
      /><button type="button" className="password-toggle" aria-label={showPassword ? t("hidePassword") : t("showPassword")} aria-pressed={showPassword} onClick={() => setShowPassword(value => !value)}>{showPassword ? <EyeOff size={18} /> : <Eye size={18} />}</button></div>
      {passwordError && <p id="login-password-error" className="form-field-error" role="alert">{passwordError}</p>}
      <Link className="login-forgot-link" href="/forgot-password">{t("forgotPassword")}</Link>
      {notice && <p className="login-notice" role="status">{notice}</p>}
      {error && <p className="form-error" role="alert">{error}</p>}
      <button className="primary-button full-button" disabled={busy}>
        {busy ? t("signingIn") : t("signIn")}<ArrowRight size={16} />
      </button>
      <div className="login-help">
        <p className="login-confirmation-link">{t("noAccount")} <Link href="/register">{t("registerLink")}</Link></p>
        <p className="login-confirmation-link">{t("confirmationHelp")} <Link href="/confirm-email">{t("confirmationResendLink")}</Link></p>
      </div>
      <p className="login-footnote"><ShieldCheck size={14} />{t("protection")}</p>
    </form>
  );
}

function TwoFactorForm({
  busy,
  email,
  error,
  notice,
  onSubmit,
  onChangeAccount,
}: LoginFormState & {
  onSubmit: (credentials: TwoFactorCodeValues) => void;
  onChangeAccount: () => void;
}) {
  const t = useTranslations("login");
  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<TwoFactorCodeValues>({
    resolver: zodResolver(twoFactorCodeSchema),
    defaultValues: { code: "" },
  });
  const codeError = errors.code ? t("validation.codeInvalid") : undefined;

  return (
    <form className="login-card" onSubmit={handleSubmit(onSubmit)} noValidate>
      <div className="login-symbol"><KeyRound size={19} /></div>
      <p className="eyebrow">{t("secure")}</p>
      <h2>{t("verify")}</h2>
      <p className="subtle">{t("verifyDescription")}</p>
      <p className="login-account">{email}</p>
      <label htmlFor="login-code">{t("code")}</label>
      <input
        id="login-code"
        type="text"
        inputMode="numeric"
        autoComplete="one-time-code"
        maxLength={6}
        autoFocus
        placeholder={t("codePlaceholder")}
        aria-invalid={Boolean(errors.code)}
        aria-describedby={errors.code ? "login-code-error" : undefined}
        {...register("code", { setValueAs: (value: string) => value.replace(/\D/g, "") })}
      />
      {codeError && <p id="login-code-error" className="form-field-error" role="alert">{codeError}</p>}
      <button type="button" className="login-back-button" onClick={onChangeAccount} disabled={busy}>{t("changeAccount")}</button>
      {notice && <p className="login-notice" role="status">{notice}</p>}
      {error && <p className="form-error" role="alert">{error}</p>}
      <button className="primary-button full-button" disabled={busy}>
        {busy ? t("signingIn") : t("verifySignIn")}<ArrowRight size={16} />
      </button>
      <p className="login-footnote"><ShieldCheck size={14} />{t("protection")}</p>
    </form>
  );
}

export default function LoginScreen({
  busy,
  email,
  error,
  notice,
  requiresTwoFactor,
  onCredentialsSubmit,
  onTwoFactorSubmit,
  onChangeAccount,
}: LoginScreenProps) {
  const { locale, setLocale } = useAppLocale();
  const t = useTranslations("login");

  return (
    <main className="access-page technology-theme">
      <header className="access-header">
        <Link className="access-brand" href="/"><span><Cpu size={22} /></span>store<span className="access-brand-light">management</span><i>WORKSPACE</i></Link>
        <div className="access-locale" role="group" aria-label={t("chooseLanguage")}><Globe2 size={16} />{(["vi", "en"] as const).map(language => <button type="button" key={language} aria-pressed={locale === language} onClick={() => setLocale(language)}>{language.toUpperCase()}</button>)}</div>
      </header>
      <div className="access-stage">
        <section className="access-intro">
          <p className="access-kicker"><span />{t("workspace")}</p>
          <h1>{t("heroTitleFirst")}<br /><em>{t("heroTitleSecond")}</em></h1>
          <p className="access-description">{t("heroDescription")}</p>
          <div className="system-blueprint" aria-hidden="true">
            <div className="blueprint-bar"><span><Network size={14} /> WORKSPACE / CORE</span><span className="blueprint-dots"><i /><i /><i /></span></div>
            <div className="blueprint-core"><span className="core-chip"><Cpu size={34} /></span><div><small>STORE MANAGEMENT</small><strong>{t("workspace")}</strong></div><span className="core-version">01</span></div>
            <div className="blueprint-connector"><i /><i /><i /></div>
            <div className="blueprint-modules">
              <div><UsersRound size={21} /><small>01 / TEAM</small><span>{t("featureTeam")}</span></div>
              <div><KeyRound size={21} /><small>02 / ACCESS</small><span>{t("featureAccess")}</span></div>
              <div><ShieldCheck size={21} /><small>03 / SECURITY</small><span>{t("featureSecurity")}</span></div>
            </div>
          </div>
          <div className="access-caption"><span>01 /</span>{t("platformFooter")}</div>
        </section>
        <section className="access-form-panel">
          <div className="access-form-top"><span>STORE ID</span><ShieldCheck size={20} /></div>
        {requiresTwoFactor ? (
          <TwoFactorForm
            busy={busy}
            email={email}
            error={error}
            notice={notice}
            onSubmit={onTwoFactorSubmit}
            onChangeAccount={onChangeAccount}
          />
        ) : (
          <CredentialsForm
            busy={busy}
            email={email}
            error={error}
            notice={notice}
            onSubmit={onCredentialsSubmit}
          />
        )}
        <div className="access-form-footer"><span /><span>{t("protection")}</span></div>
      </section>
      </div>
      <footer className="access-footer"><span>Store Management / 2026</span><span>{t("footer")}</span></footer>
    </main>
  );
}
