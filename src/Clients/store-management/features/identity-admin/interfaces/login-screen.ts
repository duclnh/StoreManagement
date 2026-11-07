import type { FormEvent } from "react";

export interface LoginScreenProps {
  busy: boolean;
  email: string;
  error: string;
  notice: string;
  password: string;
  requiresTwoFactor: boolean;
  twoFactorCode: string;
  onEmailChange: (email: string) => void;
  onPasswordChange: (password: string) => void;
  onTwoFactorCodeChange: (code: string) => void;
  onSubmit: (event: FormEvent<HTMLFormElement>) => void;
  onChangeAccount: () => void;
}
