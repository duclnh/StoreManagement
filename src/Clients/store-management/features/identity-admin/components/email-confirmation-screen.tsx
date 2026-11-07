"use client";

import { zodResolver } from "@hookform/resolvers/zod";
import { ArrowRight, Globe2, KeyRound, ShieldCheck, Store } from "lucide-react";
import Link from "next/link";
import { useSearchParams } from "next/navigation";
import { useEffect, useRef, useState } from "react";
import { useForm } from "react-hook-form";
import { useTranslations } from "next-intl";
import { useAppLocale } from "@/app/providers";
import { confirmEmail, requestEmailConfirmation } from "../api/auth.api";
import { requestPasswordResetSchema, type RequestPasswordResetValues } from "../schemas/password-recovery.schema";

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

function RequestConfirmationForm({ initialEmail, invalidLink = false }: { initialEmail: string; invalidLink?: boolean }) {
  const t = useTranslations("login");
  const [busy, setBusy] = useState(false);
  const [sent, setSent] = useState(false);
  const [error, setError] = useState("");
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
      await requestEmailConfirmation(values.email);
      setSent(true);
    } catch {
      setError(t("requestConfirmationError"));
    } finally {
      setBusy(false);
    }
  }

  return (
    <form className="login-card" onSubmit={handleSubmit(submit)} noValidate>
      <div className="login-symbol"><KeyRound size={19} /></div>
      <p className="eyebrow">{t("secure")}</p>
      <h2>{t(sent ? "confirmationEmailSentTitle" : "confirmationRequestTitle")}</h2>
      <p className="subtle">{t(sent ? "confirmationEmailSent" : "confirmationRequestDescription")}</p>
      {invalidLink && <p className="form-error" role="alert">{t("invalidConfirmationLink")}</p>}
      {sent ? (
        <Link className="primary-button full-button recovery-link-button" href="/login">{t("backToLogin")}<ArrowRight size={16} /></Link>
      ) : (
        <>
          <label htmlFor="confirmation-email">{t("email")}</label>
          <input
            id="confirmation-email"
            type="email"
            autoComplete="email"
            placeholder={t("emailPlaceholder")}
            aria-invalid={Boolean(errors.email)}
            aria-describedby={errors.email ? "confirmation-email-error" : undefined}
            {...register("email")}
          />
          {emailError && <p id="confirmation-email-error" className="form-field-error" role="alert">{emailError}</p>}
          {error && <p className="form-error" role="alert">{error}</p>}
          <button className="primary-button full-button" disabled={busy}>
            {busy ? t("sendingConfirmation") : t("requestConfirmation")}<ArrowRight size={16} />
          </button>
          <Link className="recovery-back-link" href="/login">{t("backToLogin")}</Link>
        </>
      )}
      <p className="login-footnote"><ShieldCheck size={14} />{t("protection")}</p>
    </form>
  );
}

export default function EmailConfirmationScreen() {
  const searchParams = useSearchParams();
  const email = searchParams.get("email") ?? "";
  const token = searchParams.get("token") ?? "";
  const t = useTranslations("login");
  const requestKey = `${email}\n${token}`;
  const attemptedRequest = useRef("");
  const [status, setStatus] = useState<"loading" | "confirmed" | "invalid">("loading");

  useEffect(() => {
    if (!email || !token) return;
    if (attemptedRequest.current === requestKey) return;
    attemptedRequest.current = requestKey;

    confirmEmail({ email, token })
      .then(() => setStatus("confirmed"))
      .catch(() => setStatus("invalid"));
  }, [email, requestKey, token]);

  const hasLink = Boolean(email && token);

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
            <h1>{t("confirmationHeroTitle")}</h1>
            <p>{t("confirmationHeroDescription")}</p>
          </div>
        </div>
        <footer className="login-bottom"><span className="login-footer-mark"><ShieldCheck size={14} /></span>{t("footer")}<i /><span>© 2026 Store Management</span></footer>
      </section>
      <section className="login-area">
        <LanguageSelector />
        {!hasLink || status === "invalid" ? (
          <RequestConfirmationForm initialEmail={email} invalidLink={Boolean(token) && (!email || status === "invalid")} />
        ) : (
          <div className="login-card">
            <div className="login-symbol"><ShieldCheck size={19} /></div>
            <p className="eyebrow">{t("secure")}</p>
            <h2>{status === "confirmed" ? t("confirmationSuccessTitle") : t("confirmationCheckingTitle")}</h2>
            <p className="subtle">{status === "confirmed" ? t("confirmationSuccess") : t("confirmationChecking")}</p>
            {status === "confirmed" && <Link className="primary-button full-button recovery-link-button" href="/login">{t("backToLogin")}<ArrowRight size={16} /></Link>}
            <p className="login-footnote"><ShieldCheck size={14} />{t("protection")}</p>
          </div>
        )}
        <div className="login-area-footer">STORE MANAGEMENT <span>·</span> {t("platformFooter")}</div>
      </section>
    </main>
  );
}
