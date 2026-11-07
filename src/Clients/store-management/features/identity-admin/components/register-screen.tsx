"use client";

import { zodResolver } from "@hookform/resolvers/zod";
import { ArrowRight, Globe2, KeyRound, ShieldCheck, Store } from "lucide-react";
import Link from "next/link";
import { useState } from "react";
import { useForm } from "react-hook-form";
import { useTranslations } from "next-intl";
import { useAppLocale } from "@/app/providers";
import { registerUser } from "../api/auth.api";
import { registerSchema, type RegisterValues } from "../schemas/register.schema";

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

export default function RegisterScreen() {
  const t = useTranslations("login");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState("");
  const [registered, setRegistered] = useState(false);
  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<RegisterValues>({
    resolver: zodResolver(registerSchema),
    defaultValues: { email: "", firstName: "", lastName: "", password: "", confirmPassword: "" },
  });

  function fieldError(message?: string) {
    if (!message) return undefined;
    if (message === "emailInvalid") return t("validation.emailInvalid");
    if (message === "passwordPattern") return t("passwordPattern");
    if (message === "passwordMismatch") return t("passwordMismatch");
    if (message === "nameTooLong") return t("validation.nameTooLong");
    return t("validation.required");
  }

  async function submit(values: RegisterValues) {
    setBusy(true);
    setError("");
    try {
      await registerUser({
        email: values.email,
        firstName: values.firstName,
        lastName: values.lastName,
        password: values.password,
      });
      setRegistered(true);
    } catch {
      setError(t("registrationError"));
    } finally {
      setBusy(false);
    }
  }

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
            <h1>{t("registerHeroTitle")}</h1>
            <p>{t("registerHeroDescription")}</p>
          </div>
        </div>
        <footer className="login-bottom"><span className="login-footer-mark"><ShieldCheck size={14} /></span>{t("footer")}<i /><span>© 2026 Store Management</span></footer>
      </section>
      <section className="login-area">
        <LanguageSelector />
        <form className="login-card" onSubmit={handleSubmit(submit)} noValidate>
          <div className="login-symbol"><KeyRound size={19} /></div>
          <p className="eyebrow">{t("secure")}</p>
          <h2>{registered ? t("registrationSuccessTitle") : t("registerTitle")}</h2>
          <p className="subtle">{registered ? t("registrationSuccess") : t("registerDescription")}</p>
          {registered ? (
            <Link className="primary-button full-button recovery-link-button" href="/login">{t("backToLogin")}<ArrowRight size={16} /></Link>
          ) : (
            <>
              <label htmlFor="register-email">{t("email")}</label>
              <input id="register-email" type="email" autoComplete="email" placeholder={t("emailPlaceholder")} aria-invalid={Boolean(errors.email)} aria-describedby={errors.email ? "register-email-error" : undefined} {...register("email")} />
              {errors.email && <p id="register-email-error" className="form-field-error" role="alert">{fieldError(errors.email.message)}</p>}
              <div className="form-pair">
                <label htmlFor="register-first-name">{t("firstName")}
                  <input id="register-first-name" autoComplete="given-name" maxLength={100} aria-invalid={Boolean(errors.firstName)} aria-describedby={errors.firstName ? "register-first-name-error" : undefined} {...register("firstName")} />
                  {errors.firstName && <span id="register-first-name-error" className="form-field-error" role="alert">{fieldError(errors.firstName.message)}</span>}
                </label>
                <label htmlFor="register-last-name">{t("lastName")}
                  <input id="register-last-name" autoComplete="family-name" maxLength={100} aria-invalid={Boolean(errors.lastName)} aria-describedby={errors.lastName ? "register-last-name-error" : undefined} {...register("lastName")} />
                  {errors.lastName && <span id="register-last-name-error" className="form-field-error" role="alert">{fieldError(errors.lastName.message)}</span>}
                </label>
              </div>
              <label htmlFor="register-password">{t("password")}</label>
              <input id="register-password" type="password" autoComplete="new-password" aria-invalid={Boolean(errors.password)} aria-describedby={errors.password ? "register-password-error" : undefined} {...register("password")} />
              {errors.password && <p id="register-password-error" className="form-field-error" role="alert">{fieldError(errors.password.message)}</p>}
              <label htmlFor="register-confirm-password">{t("confirmNewPassword")}</label>
              <input id="register-confirm-password" type="password" autoComplete="new-password" aria-invalid={Boolean(errors.confirmPassword)} aria-describedby={errors.confirmPassword ? "register-confirm-password-error" : undefined} {...register("confirmPassword")} />
              {errors.confirmPassword && <p id="register-confirm-password-error" className="form-field-error" role="alert">{fieldError(errors.confirmPassword.message)}</p>}
              {error && <p className="form-error" role="alert">{error}</p>}
              <button className="primary-button full-button" disabled={busy}>{busy ? t("registering") : t("registerSubmit")}<ArrowRight size={16} /></button>
              <p className="login-confirmation-link">{t("alreadyRegistered")} <Link href="/login">{t("backToLogin")}</Link></p>
            </>
          )}
          <p className="login-footnote"><ShieldCheck size={14} />{t("protection")}</p>
        </form>
        <div className="login-area-footer">STORE MANAGEMENT <span>·</span> {t("platformFooter")}</div>
      </section>
    </main>
  );
}
