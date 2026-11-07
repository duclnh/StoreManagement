import type { LoginCredentialsValues, TwoFactorCodeValues } from "../schemas/login.schema";

export interface LoginScreenProps {
  busy: boolean;
  email: string;
  error: string;
  notice: string;
  requiresTwoFactor: boolean;
  onCredentialsSubmit: (credentials: LoginCredentialsValues) => void;
  onTwoFactorSubmit: (credentials: TwoFactorCodeValues) => void;
  onChangeAccount: () => void;
}
