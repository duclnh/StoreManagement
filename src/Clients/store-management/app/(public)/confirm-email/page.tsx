import { Suspense } from "react";
import { ShieldCheck } from "lucide-react";
import EmailConfirmationScreen from "@/features/identity-admin/components/email-confirmation-screen";

export default function ConfirmEmailPage() {
  return (
    <Suspense fallback={<main className="boot-screen" aria-busy="true"><span className="brand-mark"><ShieldCheck size={20} /></span></main>}>
      <EmailConfirmationScreen />
    </Suspense>
  );
}
