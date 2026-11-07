"use client";

import { useTranslations } from "next-intl";
import { ArrowRight, Boxes, Check, Globe2, KeyRound, Package, ShieldCheck, ShoppingBag, Store, TrendingUp } from "lucide-react";
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
      <input
        id="login-password"
        type="password"
        autoComplete="current-password"
        placeholder={t("passwordPlaceholder")}
        aria-invalid={Boolean(errors.password)}
        aria-describedby={errors.password ? "login-password-error" : undefined}
        {...register("password")}
      />
      {passwordError && <p id="login-password-error" className="form-field-error" role="alert">{passwordError}</p>}
      <Link className="login-forgot-link" href="/forgot-password">{t("forgotPassword")}</Link>
      <p className="login-confirmation-link">{t("noAccount")} <Link href="/register">{t("registerLink")}</Link></p>
      <p className="login-confirmation-link">{t("confirmationHelp")} <Link href="/confirm-email">{t("confirmationResendLink")}</Link></p>
      {notice && <p className="login-notice" role="status">{notice}</p>}
      {error && <p className="form-error" role="alert">{error}</p>}
      <button className="primary-button full-button" disabled={busy}>
        {busy ? t("signingIn") : t("signIn")}<ArrowRight size={16} />
      </button>
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
            <h1><span>{t("heroTitleFirst")}</span><br /><span>{t("heroTitleSecond")}</span></h1>
            <p>{t("heroDescription")}</p>
          </div>

          <div className="store-dashboard-preview" aria-hidden="true">
            <div className="preview-topline">
              <span className="preview-window-dots"><i /><i /><i /></span>
              <span>{t("overview")}</span>
              <span className="preview-period">{t("today")} <ArrowRight size={12} /></span>
            </div>
            <div className="preview-store-card">
              <span className="preview-icon"><Store size={16} /></span>
              <span><small>{t("storeStatus")}</small><strong>12 <em>{t("locations")}</em></strong></span>
              <span className="preview-live"><i /> {t("live")}</span>
            </div>
            <div className="preview-stats">
              <div className="preview-stat-card preview-sales-card">
                <span>{t("todaySales")}</span>
                <strong>$24,680</strong>
                <small><TrendingUp size={12} /> {t("salesTrend")}</small>
                <div className="preview-chart">{[28, 42, 34, 57, 46, 68, 51, 82, 62, 92, 73, 100].map((height, index) => <i key={index} style={{ height: `${height}%` }} />)}</div>
              </div>
              <div className="preview-stat-card preview-orders-card">
                <span className="preview-card-icon"><Package size={14} /></span>
                <small>{t("orders")}</small>
                <strong>186</strong>
                <span className="preview-mini-bars"><i /><i /><i /><i /><i /><i /><i /></span>
              </div>
            </div>
            <div className="preview-stock-row">
              <span className="preview-stock-icon"><Boxes size={15} /></span>
              <span className="preview-stock-copy"><small>{t("inventory")}</small><strong>92% <em>{t("products")}</em></strong></span>
              <span className="preview-stock-track"><i /></span>
            </div>
            <div className="preview-badge"><ShoppingBag size={13} /><span>{t("synced")}</span><Check size={13} /></div>
          </div>
        </div>

        <footer className="login-bottom"><span className="login-footer-mark"><ShieldCheck size={14} /></span>{t("footer")}<i /><span>© 2026 Store Management</span></footer>
      </section>
      <section className="login-area">
        <div className="login-language" role="group" aria-label={t("chooseLanguage")}>
          <Globe2 size={15} />
          <button type="button" className={locale === "vi" ? "selected" : ""} aria-label={t("vietnamese")} aria-pressed={locale === "vi"} onClick={() => setLocale("vi")}><span>VI</span> {t("vietnamese")}</button>
          <span aria-hidden="true">/</span>
          <button type="button" className={locale === "en" ? "selected" : ""} aria-label={t("english")} aria-pressed={locale === "en"} onClick={() => setLocale("en")}><span>EN</span> {t("english")}</button>
        </div>
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
        <div className="login-area-footer">STORE MANAGEMENT <span>·</span> {t("platformFooter")}</div>
      </section>
    </main>
  );
}
