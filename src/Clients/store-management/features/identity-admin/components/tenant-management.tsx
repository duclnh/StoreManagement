"use client";

import { Building2, CirclePlus, RefreshCw, ShieldCheck } from "lucide-react";
import { useTranslations } from "next-intl";
import { useEffect, useMemo, useState, type FormEvent } from "react";
import {
  createIdentityBranch,
  createIdentityOrganization,
  createIdentityTenant,
  listIdentityTenants,
  listTenantAdminScopes,
  setIdentityBranchStatus,
  setIdentityOrganizationStatus,
  setIdentityTenantStatus,
} from "../api/roles.api";
import type { IdentityScope, IdentityTenant } from "../interfaces";
import { useWorkspaceView } from "./workspace-view-provider";

const EMPTY_SCOPES: IdentityScope[] = [];

export default function TenantManagement() {
  const a = useTranslations("admin");
  const { activeTenantId, setActiveTenantId } = useWorkspaceView();
  const [tenants, setTenants] = useState<IdentityTenant[]>([]);
  const [scopes, setScopes] = useState<IdentityScope[]>([]);
  const [tenantName, setTenantName] = useState("");
  const [organizationName, setOrganizationName] = useState("");
  const [branchNames, setBranchNames] = useState<Record<string, string>>({});
  const [tenantsLoadedRefreshKey, setTenantsLoadedRefreshKey] = useState(-1);
  const [scopesLoadedRequestKey, setScopesLoadedRequestKey] = useState("");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState("");
  const [notice, setNotice] = useState("");
  const [refreshKey, setRefreshKey] = useState(0);

  const loadingTenants = tenantsLoadedRefreshKey !== refreshKey;
  const scopesRequestKey = `${activeTenantId}:${refreshKey}`;
  const loadingScopes = Boolean(activeTenantId) && scopesLoadedRequestKey !== scopesRequestKey;
  const selectedTenant = tenants.find((tenant) => tenant.id === activeTenantId) ?? null;
  const visibleScopes = scopesLoadedRequestKey === scopesRequestKey ? scopes : EMPTY_SCOPES;
  const organizations = useMemo(
    () => visibleScopes.filter((scope) => scope.scopeType === 2),
    [visibleScopes],
  );
  const branches = useMemo(
    () => visibleScopes.filter((scope) => scope.scopeType === 3),
    [visibleScopes],
  );

  useEffect(() => {
    let active = true;
    listIdentityTenants()
      .then((items) => {
        if (!active) return;
        setTenants(items);
        setActiveTenantId(
          activeTenantId && items.some((tenant) => tenant.id === activeTenantId)
            ? activeTenantId
            : items[0]?.id ?? "",
        );
        setError("");
      })
      .catch((cause: unknown) => {
        if (active) setError(cause instanceof Error ? cause.message : a("tenants.loadError"));
      })
      .finally(() => {
        if (active) setTenantsLoadedRefreshKey(refreshKey);
      });
    return () => { active = false; };
  }, [a, activeTenantId, refreshKey, setActiveTenantId]);

  useEffect(() => {
    if (!activeTenantId) return;

    let active = true;
    listTenantAdminScopes(activeTenantId)
      .then((items) => {
        if (active) {
          setScopes(items);
          setScopesLoadedRequestKey(scopesRequestKey);
          setError("");
        }
      })
      .catch((cause: unknown) => {
        if (active) {
          setScopes([]);
          setScopesLoadedRequestKey(scopesRequestKey);
          setError(cause instanceof Error ? cause.message : a("tenants.loadScopesError"));
        }
      });
    return () => { active = false; };
  }, [a, activeTenantId, refreshKey, scopesRequestKey]);

  function refresh() {
    setError("");
    setRefreshKey((current) => current + 1);
  }

  async function submitTenant(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setBusy(true);
    setError("");
    setNotice("");
    try {
      const created = await createIdentityTenant(tenantName.trim());
      setTenantName("");
      setNotice(a("tenants.tenantCreated"));
      setActiveTenantId(created.id);
      setRefreshKey((current) => current + 1);
    } catch (cause) {
      setError(cause instanceof Error ? cause.message : a("tenants.createError"));
    } finally {
      setBusy(false);
    }
  }

  async function submitOrganization(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!selectedTenant) return;
    setBusy(true);
    setError("");
    setNotice("");
    try {
      await createIdentityOrganization(selectedTenant.id, organizationName.trim());
      setOrganizationName("");
      setNotice(a("tenants.organizationCreated"));
      setRefreshKey((current) => current + 1);
    } catch (cause) {
      setError(cause instanceof Error ? cause.message : a("tenants.createError"));
    } finally {
      setBusy(false);
    }
  }

  async function submitBranch(event: FormEvent<HTMLFormElement>, organization: IdentityScope) {
    event.preventDefault();
    const name = branchNames[organization.id]?.trim();
    if (!selectedTenant || !name) return;
    setBusy(true);
    setError("");
    setNotice("");
    try {
      await createIdentityBranch(selectedTenant.id, organization.id, name);
      setBranchNames((current) => ({ ...current, [organization.id]: "" }));
      setNotice(a("tenants.branchCreated"));
      setRefreshKey((current) => current + 1);
    } catch (cause) {
      setError(cause instanceof Error ? cause.message : a("tenants.createError"));
    } finally {
      setBusy(false);
    }
  }

  async function updateStatus(
    scope: IdentityScope,
    isActive: boolean,
    parent?: IdentityScope,
  ) {
    if (!selectedTenant) return;
    if (!isActive && !window.confirm(a("tenants.confirmDisable"))) return;

    setBusy(true);
    setError("");
    setNotice("");
    try {
      if (scope.scopeType === 1) {
        await setIdentityTenantStatus(scope.id, isActive);
      } else if (scope.scopeType === 2) {
        await setIdentityOrganizationStatus(selectedTenant.id, scope.id, isActive);
      } else if (parent) {
        await setIdentityBranchStatus(selectedTenant.id, parent.id, scope.id, isActive);
      }
      setNotice(isActive ? a("tenants.scopeEnabled") : a("tenants.scopeDisabled"));
      setRefreshKey((current) => current + 1);
    } catch (cause) {
      setError(cause instanceof Error ? cause.message : a("tenants.statusError"));
    } finally {
      setBusy(false);
    }
  }

  const tenantScope = visibleScopes.find((scope) => scope.scopeType === 1);

  return (
    <section className="tenant-management" aria-busy={busy || loadingTenants || loadingScopes}>
      <div className="page-heading">
        <div>
          <p className="eyebrow">{a("tenants.eyebrow")}</p>
          <h1>{a("tenants.title")}</h1>
          <p className="subtle">{a("tenants.description")}</p>
        </div>
        <button className="secondary-button" type="button" onClick={() => void refresh()} disabled={busy || loadingTenants}>
          <RefreshCw size={15} /> {a("common.refresh")}
        </button>
      </div>

      {(error || notice) && (
        <div className={error ? "notice notice-error" : "notice notice-success"} role={error ? "alert" : "status"}>
          {error || notice}
        </div>
      )}

      <div className="tenant-layout">
        <section className="role-card tenant-create-card">
          <div className="role-card-head"><span className="role-card-icon"><CirclePlus size={16} /></span></div>
          <h2>{a("tenants.createTenant")}</h2>
          <p>{a("tenants.createTenantHelp")}</p>
          <form className="editor-form" onSubmit={(event) => void submitTenant(event)}>
            <label>
              <span>{a("tenants.tenantName")}</span>
              <input
                autoComplete="off"
                maxLength={150}
                required
                value={tenantName}
                onChange={(event) => setTenantName(event.target.value)}
              />
            </label>
            <button className="primary-button" type="submit" disabled={busy || !tenantName.trim()}>
              <CirclePlus size={15} /> {busy ? a("common.creating") : a("tenants.createTenant")}
            </button>
          </form>
        </section>

        <section className="role-card tenant-tree-card">
          <div className="tenant-section-heading">
            <span className="role-card-icon"><Building2 size={16} /></span>
            <div><h2>{a("tenants.hierarchy")}</h2><p>{a("tenants.hierarchyHelp")}</p></div>
          </div>
          <label className="tenant-select">
            <span>{a("tenants.selectTenant")}</span>
            <select
              value={activeTenantId}
              onChange={(event) => setActiveTenantId(event.target.value)}
              disabled={loadingTenants || tenants.length === 0}
            >
              {tenants.map((tenant) => (
                <option key={tenant.id} value={tenant.id}>
                  {tenant.name}{tenant.isActive ? "" : ` · ${a("tenants.inactive")}`}
                </option>
              ))}
            </select>
          </label>

          {loadingTenants ? (
            <div className="tenant-empty">{a("common.loadingWorkspace")}</div>
          ) : !selectedTenant ? (
            <div className="tenant-empty">{a("tenants.emptyTenants")}</div>
          ) : (
            <div className="tenant-tree">
              <article className="tenant-scope-row tenant-root">
                <div className="tenant-scope-icon"><ShieldCheck size={16} /></div>
                <div className="tenant-scope-details">
                  <strong>{selectedTenant.name}</strong>
                  <small>{a("tenants.tenant")}</small>
                </div>
                <span className={`status ${selectedTenant.isActive ? "active" : "inactive"}`}>
                  <i />{selectedTenant.isActive ? a("tenants.active") : a("tenants.inactive")}
                </span>
                <button
                  type="button"
                  className="text-button"
                  disabled={busy || selectedTenant.id === "11111111-1111-1111-1111-111111111111" && selectedTenant.isActive}
                  onClick={() => void updateStatus(
                    tenantScope ?? { id: selectedTenant.id, name: selectedTenant.name, scopeType: 1, parentId: null, isActive: selectedTenant.isActive },
                    !selectedTenant.isActive,
                  )}
                >
                  {selectedTenant.isActive ? a("tenants.disable") : a("tenants.enable")}
                </button>
              </article>

              {selectedTenant.isActive && (
                <form className="tenant-inline-form" onSubmit={(event) => void submitOrganization(event)}>
                  <input
                    aria-label={a("tenants.organizationName")}
                    autoComplete="off"
                    maxLength={150}
                    placeholder={a("tenants.organizationPlaceholder")}
                    required
                    value={organizationName}
                    onChange={(event) => setOrganizationName(event.target.value)}
                  />
                  <button className="secondary-button" type="submit" disabled={busy || !organizationName.trim()}>
                    <CirclePlus size={14} /> {a("tenants.addOrganization")}
                  </button>
                </form>
              )}

              {loadingScopes ? (
                <div className="tenant-empty">{a("tenants.loadingScopes")}</div>
              ) : (
                <div className="tenant-organizations">
                  {organizations.map((organization) => {
                    const organizationBranches = branches.filter((branch) => branch.parentId === organization.id);
                    return (
                      <article className="tenant-organization" key={organization.id}>
                        <div className="tenant-scope-row">
                          <div className="tenant-scope-icon"><Building2 size={15} /></div>
                          <div className="tenant-scope-details">
                            <strong>{organization.name}</strong>
                            <small>{a("tenants.organization")}</small>
                          </div>
                          <span className={`status ${organization.isActive && selectedTenant.isActive ? "active" : "inactive"}`}>
                            <i />{organization.isActive && selectedTenant.isActive ? a("tenants.active") : a("tenants.inactive")}
                          </span>
                          <button
                            type="button"
                            className="text-button"
                            disabled={busy || (!selectedTenant.isActive && !organization.isActive)}
                            onClick={() => void updateStatus(organization, !organization.isActive)}
                          >
                            {organization.isActive ? a("tenants.disable") : a("tenants.enable")}
                          </button>
                        </div>
                        <div className="tenant-branches">
                          {organizationBranches.map((branch) => (
                            <div className="tenant-scope-row tenant-branch" key={branch.id}>
                              <div className="tenant-scope-icon"><Building2 size={14} /></div>
                              <div className="tenant-scope-details">
                                <strong>{branch.name}</strong>
                                <small>{a("tenants.branch")}</small>
                              </div>
                              <span className={`status ${branch.isActive && organization.isActive && selectedTenant.isActive ? "active" : "inactive"}`}>
                                <i />{branch.isActive && organization.isActive && selectedTenant.isActive ? a("tenants.active") : a("tenants.inactive")}
                              </span>
                              <button
                                type="button"
                                className="text-button"
                                disabled={busy || (!selectedTenant.isActive && !branch.isActive) || (!organization.isActive && !branch.isActive)}
                                onClick={() => void updateStatus(branch, !branch.isActive, organization)}
                              >
                                {branch.isActive ? a("tenants.disable") : a("tenants.enable")}
                              </button>
                            </div>
                          ))}
                          {organization.isActive && selectedTenant.isActive && (
                            <form className="tenant-inline-form tenant-branch-form" onSubmit={(event) => void submitBranch(event, organization)}>
                              <input
                                aria-label={a("tenants.branchName")}
                                autoComplete="off"
                                maxLength={150}
                                placeholder={a("tenants.branchPlaceholder")}
                                required
                                value={branchNames[organization.id] ?? ""}
                                onChange={(event) => setBranchNames((current) => ({ ...current, [organization.id]: event.target.value }))}
                              />
                              <button className="secondary-button" type="submit" disabled={busy || !branchNames[organization.id]?.trim()}>
                                <CirclePlus size={14} /> {a("tenants.addBranch")}
                              </button>
                            </form>
                          )}
                        </div>
                      </article>
                    );
                  })}
                  {!organizations.length && !loadingScopes && (
                    <div className="tenant-empty">{a("tenants.emptyOrganizations")}</div>
                  )}
                </div>
              )}
            </div>
          )}
        </section>
      </div>
    </section>
  );
}
