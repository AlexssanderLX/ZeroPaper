"use client";

import { useAppSession } from "@/components/app-session-provider";
import { PaymentsModule } from "@/components/modules/payments-module";
import { WorkspaceModulePage } from "@/components/workspace-module-page";
import { getModuleBySlug } from "@/lib/owner-portal";

const moduleData = getModuleBySlug("pagamentos");

export default function PaymentsPage() {
  const { session, clearSession } = useAppSession();

  if (!moduleData) {
    return null;
  }

  return (
    <WorkspaceModulePage
      module={moduleData}
      token={session.token}
      onUnauthorized={clearSession}
      heading="Pagamento online"
      description="Conecte a conta Mercado Pago da unidade. O dinheiro cai direto na sua conta e as taxas ficam com voce."
      showSummary={false}
    >
      <PaymentsModule token={session.token} onUnauthorized={clearSession} />
    </WorkspaceModulePage>
  );
}
