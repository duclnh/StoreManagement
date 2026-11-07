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
