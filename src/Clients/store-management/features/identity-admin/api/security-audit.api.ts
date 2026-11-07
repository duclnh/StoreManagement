import type { SecurityAuditPage } from "../interfaces";
import { get } from "@/services/api-client";

export function getSecurityAuditPage(
  page: number,
  filters: { eventType?: string; outcome?: string },
) {
  const query = new URLSearchParams({ page: String(page), pageSize: "50" });
  if (filters.eventType) query.set("eventType", filters.eventType);
  if (filters.outcome) query.set("outcome", filters.outcome);
  return get<SecurityAuditPage>(`admin/audit-events?${query.toString()}`);
}
