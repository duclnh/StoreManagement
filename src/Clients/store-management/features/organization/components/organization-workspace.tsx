"use client";

import { useEffect, useMemo, useState, type FormEvent } from "react";
import { useTranslations } from "next-intl";
import { Building2, CirclePlus, RefreshCw } from "lucide-react";
import { organizationApi, type BranchRecord, type OrganizationRecord, type OrgPage, type PosRecord, type TenantRecord, type WarehouseRecord } from "../api/organization.api";

type Resource = "tenants" | "organizations" | "branches" | "warehouses" | "pos";
type Row = TenantRecord | OrganizationRecord | BranchRecord | WarehouseRecord | PosRecord;
const emptyPage = <T,>(): OrgPage<T> => ({ items: [], pageNumber: 1, pageSize: 100, totalCount: 0 });

export default function OrganizationWorkspace() {
  const a = useTranslations("organization");
  const [resource, setResource] = useState<Resource>("tenants");
  const [tenants, setTenants] = useState<TenantRecord[]>([]);
  const [organizations, setOrganizations] = useState<OrganizationRecord[]>([]);
  const [branches, setBranches] = useState<BranchRecord[]>([]);
  const [tenantId, setTenantId] = useState("");
  const [organizationId, setOrganizationId] = useState("");
  const [branchId, setBranchId] = useState("");
  const [rows, setRows] = useState<Row[]>([]);
  const [search, setSearch] = useState("");
  const [draft, setDraft] = useState<Row | null>(null);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState("");
  const [notice, setNotice] = useState("");
  const [refresh, setRefresh] = useState(0);

  useEffect(() => { let current = true; organizationApi.tenants().then(page => { if (!current) return; setTenants(page.items); setTenantId(value => value || page.items[0]?.id || ""); }).catch(cause => { if (current) setError(String(cause)); }); return () => { current = false; }; }, [refresh]);
  useEffect(() => { if (!tenantId) return; let current = true; organizationApi.organizations(tenantId).then(page => { if (!current) return; setOrganizations(page.items); setOrganizationId(value => page.items.some(x => x.id === value) ? value : page.items[0]?.id ?? ""); }).catch(cause => { if (current) setError(String(cause)); }); return () => { current = false; }; }, [tenantId, refresh]);
  useEffect(() => { if (!organizationId) return; let current = true; organizationApi.branches(organizationId).then(page => { if (!current) return; setBranches(page.items); setBranchId(value => page.items.some(x => x.id === value) ? value : page.items[0]?.id ?? ""); }).catch(cause => { if (current) setError(String(cause)); }); return () => { current = false; }; }, [organizationId, refresh]);
  useEffect(() => {
    const request = resource === "tenants" ? organizationApi.tenants()
      : resource === "organizations" && tenantId ? organizationApi.organizations(tenantId)
      : resource === "branches" && organizationId ? organizationApi.branches(organizationId)
      : resource === "warehouses" && branchId ? organizationApi.warehouses(branchId)
      : resource === "pos" && branchId ? organizationApi.pointsOfSale(branchId)
      : Promise.resolve(emptyPage<Row>());
    request.then(page => setRows(page.items as Row[])).catch(cause => setError(String(cause)));
  }, [resource, tenantId, organizationId, branchId, refresh]);

  const visibleRows = useMemo(() => rows.filter(row => `${row.code} ${row.name}`.toLowerCase().includes(search.toLowerCase())), [rows, search]);
  const fields = resource === "tenants" ? ["code", "name", "contactEmail", "contactPhone"]
    : resource === "organizations" ? ["code", "name", "businessType", "industry", "taxCode", "address", "phone", "email", "currency", "timeZone", "language"]
    : resource === "branches" ? ["code", "name", "address", "phone", "email", "managerUserId", "isDefault"]
    : resource === "warehouses" ? ["code", "name", "warehouseType", "address", "isDefault"]
    : ["code", "name", "description"];

  async function save(event: FormEvent<HTMLFormElement>) {
    event.preventDefault(); if (!draft) return;
    const form = new FormData(event.currentTarget); const body: Record<string, unknown> = {};
    for (const field of fields) { const value = form.get(field); body[field] = field === "isDefault" ? value === "on" : value || null; }
    body.status = true;
    setBusy(true); setError("");
    try {
      if (resource === "tenants") { if (draft.id) await organizationApi.updateTenant(draft.id, { ...draft, ...body }); else await organizationApi.createTenant({ configurationJson: "{}", ...body }); }
      else if (resource === "organizations") { if (draft.id) await organizationApi.updateOrganization(draft.id, { ...draft, ...body }); else await organizationApi.createOrganization(tenantId, { businessType: "GENERAL_RETAIL", currency: "VND", timeZone: "Asia/Ho_Chi_Minh", language: "vi", ...body }); }
      else if (resource === "branches") { if (draft.id) await organizationApi.updateBranch(draft.id, { ...draft, ...body }); else await organizationApi.createBranch(organizationId, { managerUserId: null, isDefault: false, ...body }); }
      else if (resource === "warehouses") { if (draft.id) await organizationApi.updateWarehouse(draft.id, { ...draft, ...body }); else await organizationApi.createWarehouse(branchId, { warehouseType: "MAIN", isDefault: false, ...body }); }
      else if (draft.id) await organizationApi.updatePointOfSale(draft.id, { ...draft, ...body }); else await organizationApi.createPointOfSale(branchId, body);
      setDraft(null); setNotice(a("organization.saved")); setRefresh(value => value + 1);
    } catch (cause) { setError(cause instanceof Error ? cause.message : a("organization.saveError")); } finally { setBusy(false); }
  }

  async function toggle(row: Row) {
    const active = !row.status;
    try {
      if (resource === "tenants") await organizationApi.tenantStatus(row.id, active);
      else if (resource === "organizations") await organizationApi.organizationStatus(row.id, active);
      else if (resource === "branches") await organizationApi.branchStatus(row.id, active);
      else if (resource === "warehouses") await organizationApi.warehouseStatus(row.id, active);
      else await organizationApi.pointOfSaleStatus(row.id, active);
      setRefresh(value => value + 1);
    } catch (cause) { setError(cause instanceof Error ? cause.message : a("organization.saveError")); }
  }

  return <section className="organization-workspace">
    <div className="page-heading"><div><p className="eyebrow">{a("eyebrow")}</p><h1>{a("title")}</h1><p className="subtle">{a("description")}</p></div><button className="secondary-button" onClick={() => setRefresh(value => value + 1)}><RefreshCw size={14}/>{a("refresh")}</button></div>
    <div className="organization-toolbar"><label>{a("organization.resource")}<select value={resource} onChange={event => setResource(event.target.value as Resource)}>{(["tenants", "organizations", "branches", "warehouses", "pos"] as Resource[]).map(key => <option key={key} value={key}>{a(`organization.resources.${key}`)}</option>)}</select></label>
      {resource !== "tenants" && <label>{a("organization.tenant")}<select value={tenantId} onChange={event => setTenantId(event.target.value)}>{tenants.map(item => <option key={item.id} value={item.id}>{item.name}</option>)}</select></label>}
      {["branches", "warehouses", "pos"].includes(resource) && <label>{a("organization.organization")}<select value={organizationId} onChange={event => setOrganizationId(event.target.value)}>{organizations.map(item => <option key={item.id} value={item.id}>{item.name}</option>)}</select></label>}
      {["warehouses", "pos"].includes(resource) && <label>{a("organization.branch")}<select value={branchId} onChange={event => setBranchId(event.target.value)}>{branches.map(item => <option key={item.id} value={item.id}>{item.name}</option>)}</select></label>}
      <label>{a("search")}<input type="search" value={search} onChange={event => setSearch(event.target.value)}/></label><button className="primary-button" onClick={() => setDraft({} as Row)}><CirclePlus size={15}/>{a("create")}</button>
    </div>
    {error && <p className="form-error" role="alert">{error}</p>}{notice && <p className="form-success" role="status">{notice}</p>}
    <div className="table-shell"><div className="table-scroll"><table className="user-table"><thead><tr><th>{a("code")}</th><th>{a("name")}</th><th>{a("status")}</th><th>{a("actions")}</th></tr></thead><tbody>{visibleRows.map(row => <tr key={row.id}><td>{row.code}</td><td>{row.name}</td><td>{row.status ? a("active") : a("inactive")}</td><td><button className="text-button" onClick={() => setDraft(row)}>{a("edit")}</button><button className="text-button" onClick={() => void toggle(row)}>{row.status ? a("disable") : a("enable")}</button></td></tr>)}{!visibleRows.length && <tr><td colSpan={4}><div className="empty-state"><Building2 size={22}/><strong>{a("emptyTitle")}</strong></div></td></tr>}</tbody></table></div></div>
    {draft && <div className="modal-backdrop" role="presentation" onMouseDown={event => { if (event.target === event.currentTarget) setDraft(null); }}><form className="side-panel" onSubmit={save}><header><div><p className="eyebrow">{a(`resources.${resource}`)}</p><h2>{draft.id ? a("edit") : a("create")}</h2></div><button type="button" className="icon-button" onClick={() => setDraft(null)}>×</button></header>{fields.map(field => <label key={field}>{a(`fields.${field}`)}{field === "isDefault" ? <input name={field} type="checkbox" defaultChecked={Boolean((draft as unknown as Record<string, unknown>)[field])}/> : <input name={field} required={["code", "name"].includes(field)} defaultValue={String((draft as unknown as Record<string, unknown>)[field] ?? "")}/>}</label>)}<footer><button type="button" className="secondary-button" onClick={() => setDraft(null)}>{a("cancel")}</button><button className="primary-button" disabled={busy}>{a("save")}</button></footer></form></div>}
  </section>;
}
