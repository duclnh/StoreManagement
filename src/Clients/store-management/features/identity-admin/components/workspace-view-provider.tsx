"use client";

import { createContext, useContext, useState, type ReactNode } from "react";
import { usePathname } from "next/navigation";

export type WorkspaceView = "users" | "roles" | "sessions" | "security" | "audit";

interface WorkspaceViewContextValue {
  view: WorkspaceView;
  setView: (view: WorkspaceView) => void;
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

  return (
    <WorkspaceViewContext.Provider value={{ view, setView }}>
      {children}
    </WorkspaceViewContext.Provider>
  );
}
