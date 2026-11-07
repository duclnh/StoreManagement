export interface SecurityAuditEvent {
  id: string;
  eventType: string;
  outcome: string;
  occurredAtUtc: string;
  actorUserId: string | null;
  subjectUserId: string | null;
  sessionId: string | null;
  ipAddress: string | null;
  details: string | null;
}

export interface SecurityAuditPage {
  items: SecurityAuditEvent[];
  page: number;
  pageSize: number;
  totalCount: number;
}
