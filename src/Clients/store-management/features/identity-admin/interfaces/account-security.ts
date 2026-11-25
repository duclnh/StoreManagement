export interface AccountSecurityProps {
  userId?: string;
  twoFactorEnabled: boolean | null;
  onPasswordChanged: () => void;
}
