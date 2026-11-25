import enMessages from "./features/identity-admin/locales/en.json";
import organizationMessages from "./features/organization/locales/en.json";

declare module "next-intl" {
  interface AppConfig {
    Locale: "en" | "vi";
    Messages: typeof enMessages & typeof organizationMessages;
  }
}
