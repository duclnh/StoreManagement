"use client";

import { createContext, useCallback, useContext, useState, type ReactNode } from "react";
import { usePathname } from "next/navigation";

export type WorkspaceView = "users" | "roles" | "sessions" | "security" | "audit" | "tenants";
const activeTenantStorageKey = "identity-admin-active-tenant";

interface WorkspaceViewContextValue {
  view: WorkspaceView;
  setView: (view: WorkspaceView) => void;
  activeTenantId: string;
  setActiveTenantId: (tenantId: string) => void;
}

const WorkspaceViewContext = createContext<WorkspaceViewContextValue | null>(null);

export function useWorkspaceView() {
  const context = useContext(WorkspaceViewContext);
  if (!context) throw new Error("useWorkspaceView must be used within WorkspaceViewProvider.");
  return context;
}

export default function WorkspaceViewProvider({ children }: { children: ReactNode }) {
  const pathname = usePathname();
  const [view, setView] = useState<WorkspaceView>(pathname === "/settings" ? "security" : "users");
  const [activeTenantId, setActiveTenantIdState] = useState(() =>
    typeof window === "undefined" ? "" : window.localStorage.getItem(activeTenantStorageKey) ?? "",
  );

  const setActiveTenantId = useCallback((tenantId: string) => {
    setActiveTenantIdState(tenantId);
    if (tenantId) window.localStorage.setItem(activeTenantStorageKey, tenantId);
    else window.localStorage.removeItem(activeTenantStorageKey);
  }, []);

  return (
    <WorkspaceViewContext.Provider value={{ view, setView, activeTenantId, setActiveTenantId }}>
      {children}
    </WorkspaceViewContext.Provider>
  );
}
