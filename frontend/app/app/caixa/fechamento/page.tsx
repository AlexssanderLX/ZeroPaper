"use client";

import { useAppSession } from "@/components/app-session-provider";
import { CashClosingModule } from "@/components/modules/cash-closing-module";
import { WorkspaceModulePage } from "@/components/workspace-module-page";
import { getModuleBySlug } from "@/lib/owner-portal";

const moduleData = getModuleBySlug("caixa");

export default function CashClosingPage() {
  const { session, clearSession } = useAppSession();

  if (!moduleData) {
    return null;
  }

  return (
    <WorkspaceModulePage
      module={moduleData}
      token={session.token}
      onUnauthorized={clearSession}
      backHref="/app/caixa"
      backLabel="Voltar ao caixa"
      heading="Fechamento de caixa"
      description="Total vendido, pedidos, ticket medio, descontos e formas de pagamento do dia."
      showSummary={false}
    >
      <CashClosingModule token={session.token} onUnauthorized={clearSession} />
    </WorkspaceModulePage>
  );
}
