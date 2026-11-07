import AdminConsole from "@/features/identity-admin/components/admin-console";
import WorkspaceViewProvider from "@/features/identity-admin/components/workspace-view-provider";

export default function LoginPage() {
  return (
    <WorkspaceViewProvider>
      <AdminConsole mode="login" />
    </WorkspaceViewProvider>
  );
}
