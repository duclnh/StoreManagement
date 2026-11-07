export interface Role {
  id: string;
  name: string;
  roleCode: string;
  description: string;
  userCount: number;
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
