"use client";

import { NextIntlClientProvider } from "next-intl";
import { createContext, useCallback, useContext, useEffect, useSyncExternalStore, type ReactNode } from "react";
import enMessages from "@/features/identity-admin/locales/en.json";
import viMessages from "@/features/identity-admin/locales/vi.json";

export type AppLocale = "en" | "vi";

type LocaleContextValue = {
  locale: AppLocale;
  setLocale: (locale: AppLocale) => void;
};

const messages = { en: enMessages, vi: viMessages };
const LocaleContext = createContext<LocaleContextValue | null>(null);

function getStoredLocale(): AppLocale {
  const cookieLocale = document.cookie.match(/(?:^|;\s*)store-management-locale=(en|vi)(?:;|$)/)?.[1];
  if (cookieLocale === "en" || cookieLocale === "vi") return cookieLocale;
  return window.localStorage.getItem("store-management-locale") === "en" ? "en" : "vi";
}

function getServerLocale(): AppLocale {
  return "vi";
}

function subscribeToLocale(onChange: () => void) {
  const handleStorage = (event: StorageEvent) => {
    if (event.key === "store-management-locale") onChange();
  };
  window.addEventListener("store-management-locale-change", onChange);
  window.addEventListener("storage", handleStorage);
  return () => {
    window.removeEventListener("store-management-locale-change", onChange);
    window.removeEventListener("storage", handleStorage);
  };
}

export function useAppLocale() {
  const context = useContext(LocaleContext);
  if (!context) throw new Error("useAppLocale must be used within AppProviders.");
  return context;
}

export default function AppProviders({ children }: { children: ReactNode }) {
  const locale = useSyncExternalStore(subscribeToLocale, getStoredLocale, getServerLocale);
  const setLocale = useCallback((nextLocale: AppLocale) => {
    document.cookie = `store-management-locale=${nextLocale}; Path=/; Max-Age=31536000; SameSite=Lax`;
    window.localStorage.setItem("store-management-locale", nextLocale);
    window.dispatchEvent(new Event("store-management-locale-change"));
  }, []);

  useEffect(() => {
    document.documentElement.lang = locale;
  }, [locale]);

  return (
    <LocaleContext.Provider value={{ locale, setLocale }}>
      <NextIntlClientProvider locale={locale} messages={messages[locale]} timeZone="UTC">
        {children}
      </NextIntlClientProvider>
    </LocaleContext.Provider>
  );
}
