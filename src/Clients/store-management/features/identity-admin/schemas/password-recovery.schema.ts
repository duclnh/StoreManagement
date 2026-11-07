import { z } from "zod";

export const requestPasswordResetSchema = z.object({
  email: z.string().min(1, "required").email("emailInvalid"),
});

export const resetPasswordSchema = z.object({
  newPassword: z.string().min(1, "required").pipe(
    z.string().min(8, "passwordPattern").regex(/(?=.*\d).{8,}/, "passwordPattern"),
  ),
  confirmPassword: z.string().min(1, "required"),
}).refine((values) => values.newPassword === values.confirmPassword, {
  path: ["confirmPassword"],
  message: "passwordMismatch",
});

export type RequestPasswordResetValues = z.infer<typeof requestPasswordResetSchema>;
export type ResetPasswordValues = z.infer<typeof resetPasswordSchema>;
