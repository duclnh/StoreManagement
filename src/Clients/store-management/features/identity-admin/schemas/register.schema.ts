import { z } from "zod";

export const registerSchema = z.object({
  email: z.string().min(1, "required").email("emailInvalid"),
  firstName: z.string().trim().min(1, "required").max(100, "nameTooLong"),
  lastName: z.string().trim().min(1, "required").max(100, "nameTooLong"),
  password: z.string().min(1, "required").pipe(
    z.string().min(8, "passwordPattern").regex(/(?=.*\d).{8,}/, "passwordPattern"),
  ),
  confirmPassword: z.string().min(1, "required"),
}).refine((values) => values.password === values.confirmPassword, {
  path: ["confirmPassword"],
  message: "passwordMismatch",
});

export type RegisterValues = z.infer<typeof registerSchema>;
