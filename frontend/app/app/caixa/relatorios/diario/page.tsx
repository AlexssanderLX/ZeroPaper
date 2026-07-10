"use client";

import { useAppSession } from "@/components/app-session-provider";
import { DailySalesReportModule } from "@/components/modules/daily-sales-report-module";
import { WorkspaceModulePage } from "@/components/workspace-module-page";
import { getModuleBySlug } from "@/lib/owner-portal";

const moduleData = getModuleBySlug("relatorios");

export default function DailySalesReportPage() {
  const { session, clearSession } = useAppSession();

  if (!moduleData) {
    return null;
  }

  return (
    <WorkspaceModulePage
      module={moduleData}
      token={session.token}
      onUnauthorized={clearSession}
      backHref="/app/caixa/relatorios"
      backLabel="Voltar aos relatorios"
      heading="Relatorio diario de vendas"
      description="Selecione uma data e acompanhe o resumo financeiro, os contadores de pedidos e as formas de pagamento do dia."
    >
      <DailySalesReportModule token={session.token} onUnauthorized={clearSession} />
    </WorkspaceModulePage>
  );
}
