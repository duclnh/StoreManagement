export interface LoginRequest {
  email: string;
  password: string;
  twoFactorCode?: string;
}

export interface RegisterUserRequest {
  email: string;
  password: string;
  firstName: string;
  lastName: string;
}

export interface ChangePasswordRequest {
  currentPassword: string;
  newPassword: string;
}

export interface TwoFactorCodeRequest {
  code: string;
}

export interface AuthenticatorSetup {
  sharedKey: string;
  authenticatorUri: string;
}

export interface AccountProfile {
  twoFactorEnabled: boolean;
}
