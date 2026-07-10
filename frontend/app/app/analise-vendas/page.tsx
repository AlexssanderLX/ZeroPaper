"use client";

import { useAppSession } from "@/components/app-session-provider";
import { ReportsModule } from "@/components/modules/reports-module";
import { WorkspaceModulePage } from "@/components/workspace-module-page";
import { getModuleBySlug } from "@/lib/owner-portal";

const moduleData = getModuleBySlug("analise-vendas");

export default function SalesAnalysisPage() {
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
      heading="Analise de vendas"
      description="Ticket medio, produtos, horarios, clientes e pagamentos em blocos simples."
    >
      <ReportsModule token={session.token} onUnauthorized={clearSession} variant="analysis" />
    </WorkspaceModulePage>
  );
}
