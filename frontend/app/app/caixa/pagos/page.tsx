"use client";

import { useAppSession } from "@/components/app-session-provider";
import { CashModule } from "@/components/modules/cash-module";
import { WorkspaceModulePage } from "@/components/workspace-module-page";
import { getModuleBySlug } from "@/lib/owner-portal";

const moduleData = getModuleBySlug("caixa");

export default function CashPaidPage() {
  const { session, clearSession } = useAppSession();

  if (!moduleData) {
    return null;
  }

  return (
    <WorkspaceModulePage
      module={moduleData}
      token={session.token}
      onUnauthorized={clearSession}
      backHref="/app"
      backLabel="Voltar ao painel"
      heading="Pagos"
      description="Consulte o historico de pedidos pagos do dia em uma pagina separada, sem misturar com a cobranca atual."
    >
      <CashModule token={session.token} onUnauthorized={clearSession} section="paid" />
    </WorkspaceModulePage>
  );
}
