"use client";

import { createContext, useCallback, useContext, useState, type Dispatch, type ReactNode, type SetStateAction } from "react";
import { usePathname } from "next/navigation";

export type WorkspaceView = "users" | "roles" | "permissions" | "sessions" | "security" | "audit" | "tenants" | "organization";
const activeTenantStorageKey = "identity-admin-active-tenant";

interface WorkspaceViewContextValue {
  view: WorkspaceView;
  setView: Dispatch<SetStateAction<WorkspaceView>>;
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
  const [selectedView, setView] = useState<WorkspaceView>("users");
  const view = pathname === "/settings" ? "security" : pathname === "/permissions" ? "permissions"
    : selectedView === "permissions" || selectedView === "security" ? "users" : selectedView;
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
