import { getRequestConfig } from "next-intl/server";
import viMessages from "@/features/identity-admin/locales/vi.json";

export default getRequestConfig(async () => ({
  locale: "vi",
  messages: viMessages,
  timeZone: "UTC",
}));
