export interface Role {
  id: string;
  name: string;
  roleCode: string;
  description: string;
  userCount: number;
}

export interface IdentityScope {
  id: string;
  name: string;
  scopeType: number;
  parentId: string | null;
}

export interface IdentityAdminScope extends IdentityScope {
  isActive: boolean;
}

export interface IdentityTenant {
  id: string;
  name: string;
  isActive: boolean;
  createdAtUtc: string;
}

export interface IdentityAdminAccess {
  tenantId: string;
  scopeId: string;
  isAdministrator: boolean;
  canViewUsers: boolean;
  canCreateUsers: boolean;
  canUpdateUsers: boolean;
  canAssignUsers: boolean;
  canViewRoles: boolean;
  canCreateRoles: boolean;
  canUpdateRoles: boolean;
  canDeleteRoles: boolean;
  canManageRolePermissions: boolean;
  canViewAudit: boolean;
  canViewSessions: boolean;
  canRevokeSessions: boolean;
}

export interface RoleDraft {
  id?: string;
  name: string;
  roleCode: string;
  description: string;
}

export interface SaveRoleRequest {
  name: string;
  roleCode: string;
  description: string;
}

export interface RolePermission {
  functionId: string;
  functionName: string;
  actionId: string;
  actionName: string;
  isGranted: boolean;
}

export interface SetRolePermissionsRequest {
  permissions: Array<Pick<RolePermission, "functionId" | "actionId">>;
}
