import { z } from "zod";

export const loginCredentialsSchema = z.object({
  email: z.string().min(1, "required").email("emailInvalid"),
  password: z.string().min(1, "required"),
});

export const twoFactorCodeSchema = z.object({
  code: z.string().regex(/^\d{6}$/, "codeInvalid"),
});

export type LoginCredentialsValues = z.infer<typeof loginCredentialsSchema>;
export type TwoFactorCodeValues = z.infer<typeof twoFactorCodeSchema>;
