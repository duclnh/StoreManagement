export interface SecurityAuditEvent {
  id: string;
  eventType: string;
  outcome: string;
  occurredAtUtc: string;
  actorUserId: string | null;
  actorUserName: string | null;
  subjectUserId: string | null;
  subjectUserName: string | null;
  sessionId: string | null;
  ipAddress: string | null;
  details: string | null;
}

import type { PageResult } from "./page";

export type SecurityAuditPage = PageResult<SecurityAuditEvent>;
