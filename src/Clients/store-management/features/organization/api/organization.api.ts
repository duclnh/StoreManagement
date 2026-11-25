import { del, get, patch, post, put } from "@/services/api-client";

export interface OrgPage<T> {
  items: T[];
  pageNumber: number;
  pageSize: number;
  totalCount: number;
}

export interface TenantRecord { id: string; tenantId: string; code: string; name: string; contactEmail?: string; contactPhone?: string; configurationJson: string; status: boolean; }
export interface OrganizationRecord { id: string; tenantId: string; code: string; name: string; taxCode?: string; businessType: string; industry?: string; address?: string; phone?: string; email?: string; logoUrl?: string; currency: string; timeZone: string; language: string; status: boolean; }
export interface BranchRecord { id: string; tenantId: string; organizationId: string; code: string; name: string; address?: string; phone?: string; email?: string; managerUserId?: string; isDefault: boolean; status: boolean; }
export interface WarehouseRecord { id: string; tenantId: string; branchId: string; code: string; name: string; warehouseType: string; address?: string; isDefault: boolean; status: boolean; }
export interface PosRecord { id: string; tenantId: string; branchId: string; code: string; name: string; description?: string; status: boolean; }
export interface MemberRecord { id: string; organizationId: string; userId: string; branchId?: string; isDefault: boolean; status: boolean; }

const params = (values: Record<string, string | number | undefined>) => {
  const query = new URLSearchParams();
  Object.entries(values).forEach(([key, value]) => { if (value !== undefined && value !== "") query.set(key, String(value)); });
  return query.size ? `?${query}` : "";
};
const list = <T,>(path: string, query: Record<string, string | number | undefined> = {}) => get<OrgPage<T>>(`${path}${params(query)}`);
const status = (path: string, value: boolean) => patch(path, { status: value });

export const organizationApi = {
  tenants: () => list<TenantRecord>("tenants"),
  createTenant: (body: unknown) => post<TenantRecord>("tenants", body),
  updateTenant: (id: string, body: unknown) => put<TenantRecord>(`tenants/${id}`, body),
  tenantStatus: (id: string, active: boolean) => status(`tenants/${id}/status`, active),
  organizations: (tenantId: string) => list<OrganizationRecord>("organizations", { tenantId }),
  createOrganization: (tenantId: string, body: unknown) => post<OrganizationRecord>(`organizations?tenantId=${encodeURIComponent(tenantId)}`, body),
  updateOrganization: (id: string, body: unknown) => put<OrganizationRecord>(`organizations/${id}`, body),
  organizationStatus: (id: string, active: boolean) => status(`organizations/${id}/status`, active),
  branches: (organizationId: string) => list<BranchRecord>(`organizations/${organizationId}/branches`),
  createBranch: (organizationId: string, body: unknown) => post<BranchRecord>(`organizations/${organizationId}/branches`, body),
  updateBranch: (id: string, body: unknown) => put<BranchRecord>(`branches/${id}`, body),
  branchStatus: (id: string, active: boolean) => status(`branches/${id}/status`, active),
  warehouses: (branchId: string) => list<WarehouseRecord>(`branches/${branchId}/warehouses`),
  createWarehouse: (branchId: string, body: unknown) => post<WarehouseRecord>(`branches/${branchId}/warehouses`, body),
  updateWarehouse: (id: string, body: unknown) => put<WarehouseRecord>(`warehouses/${id}`, body),
  warehouseStatus: (id: string, active: boolean) => status(`warehouses/${id}/status`, active),
  pointsOfSale: (branchId: string) => list<PosRecord>(`branches/${branchId}/pos`),
  createPointOfSale: (branchId: string, body: unknown) => post<PosRecord>(`branches/${branchId}/pos`, body),
  updatePointOfSale: (id: string, body: unknown) => put<PosRecord>(`pos/${id}`, body),
  pointOfSaleStatus: (id: string, active: boolean) => status(`pos/${id}/status`, active),
  organizationMembers: (id: string) => list<MemberRecord>(`organizations/${id}/members`),
  addOrganizationMember: (id: string, userId: string) => post(`organizations/${id}/members`, { userId }),
  removeOrganizationMember: (id: string, userId: string) => del(`organizations/${id}/members/${userId}`),
  branchMembers: (id: string) => list<MemberRecord>(`branches/${id}/members`),
  addBranchMember: (id: string, userId: string) => post(`branches/${id}/members`, { userId }),
  removeBranchMember: (id: string, userId: string) => del(`branches/${id}/members/${userId}`),
};
