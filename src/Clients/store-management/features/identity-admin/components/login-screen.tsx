"use client";

import { useTranslations } from "next-intl";
import { ArrowRight, Boxes, Check, Globe2, KeyRound, Package, ShieldCheck, ShoppingBag, Store, TrendingUp } from "lucide-react";
import Link from "next/link";
import { useAppLocale } from "@/app/providers";
import type { LoginScreenProps } from "../interfaces";

export default function LoginScreen({
  busy,
  email,
  error,
  notice,
  password,
  requiresTwoFactor,
  twoFactorCode,
  onEmailChange,
  onPasswordChange,
  onTwoFactorCodeChange,
  onSubmit,
  onChangeAccount,
}: LoginScreenProps) {
  const t = useTranslations("login");
  const { locale, setLocale } = useAppLocale();

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
        <form className="login-card" onSubmit={onSubmit}>
          <div className="login-symbol"><KeyRound size={19} /></div>
          <p className="eyebrow">{t("secure")}</p>
          <h2>{requiresTwoFactor ? t("verify") : t("welcome")}</h2>
          <p className="subtle">{requiresTwoFactor ? t("verifyDescription") : t("welcomeDescription")}</p>
          {!requiresTwoFactor ? <>
            <label htmlFor="login-email">{t("email")}</label>
            <input id="login-email" type="email" autoComplete="username" value={email} onChange={(event) => onEmailChange(event.target.value)} required placeholder={t("emailPlaceholder")} />
            <label htmlFor="login-password">{t("password")}</label>
            <input id="login-password" type="password" autoComplete="current-password" value={password} onChange={(event) => onPasswordChange(event.target.value)} required placeholder={t("passwordPlaceholder")} />
          </> : <>
            <label htmlFor="login-code">{t("code")}</label>
            <input id="login-code" type="text" inputMode="numeric" autoComplete="one-time-code" pattern="[0-9]{6}" maxLength={6} autoFocus value={twoFactorCode} onChange={(event) => onTwoFactorCodeChange(event.target.value.replace(/\D/g, ""))} required placeholder={t("codePlaceholder")} />
            <button type="button" className="login-back-button" onClick={onChangeAccount} disabled={busy}>{t("changeAccount")}</button>
          </>}
          {notice && <p className="login-notice" role="status">{notice}</p>}
          {error && <p className="form-error" role="alert">{error}</p>}
          <button className="primary-button full-button" disabled={busy || (requiresTwoFactor && twoFactorCode.length !== 6)}>
            {busy ? t("signingIn") : requiresTwoFactor ? t("verifySignIn") : t("signIn")}<ArrowRight size={16} />
          </button>
          <p className="login-footnote"><ShieldCheck size={14} />{t("protection")}</p>
        </form>
        <div className="login-area-footer">STORE MANAGEMENT <span>·</span> {t("platformFooter")}</div>
      </section>
    </main>
  );
}
