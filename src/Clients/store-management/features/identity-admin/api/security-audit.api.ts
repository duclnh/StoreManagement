import type { SecurityAuditPage } from "../interfaces";
import { get } from "@/services/api-client";

export function getSecurityAuditPage(
  page: number,
  filters: { eventType?: string; outcome?: string; fromUtc?: string; toUtc?: string; userName?: string; userId?: string },
  scopeId?: string,
) {
  const query = new URLSearchParams({ page: String(page), pageSize: "50" });
  if (scopeId) query.set("scopeId", scopeId);
  if (filters.eventType) query.set("eventType", filters.eventType);
  if (filters.outcome) query.set("outcome", filters.outcome);
  if (filters.fromUtc) query.set("fromUtc", filters.fromUtc);
  if (filters.toUtc) query.set("toUtc", filters.toUtc);
  if (filters.userName) query.set("userName", filters.userName);
  if (filters.userId) query.set("userId", filters.userId);
  return get<SecurityAuditPage>(`admin/audit-events?${query.toString()}`);
}
