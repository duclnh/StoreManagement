import { Suspense } from "react";
import { ShieldCheck } from "lucide-react";
import PasswordRecoveryScreen from "@/features/identity-admin/components/password-recovery-screen";

export default function ForgotPasswordPage() {
  return (
    <Suspense fallback={<main className="boot-screen" aria-busy="true"><span className="brand-mark"><ShieldCheck size={20} /></span></main>}>
      <PasswordRecoveryScreen />
    </Suspense>
  );
}
