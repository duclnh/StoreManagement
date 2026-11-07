import type { ReactNode } from "react";
import WorkspaceViewProvider from "@/features/identity-admin/components/workspace-view-provider";

export default function PrivateLayout({ children }: { children: ReactNode }) {
  return <WorkspaceViewProvider>{children}</WorkspaceViewProvider>;
}
