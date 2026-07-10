"use client";

import { useAppSession } from "@/components/app-session-provider";
import { DailySalesReportModule } from "@/components/modules/daily-sales-report-module";
import { WorkspaceModulePage } from "@/components/workspace-module-page";
import { getModuleBySlug } from "@/lib/owner-portal";

const moduleData = getModuleBySlug("relatorios");

export default function CashReportsPage() {
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
      heading="Relatorios de vendas"
      description="Escolha o dia e veja o resumo financeiro, formas de pagamento e contadores da unidade."
    >
      <DailySalesReportModule token={session.token} onUnauthorized={clearSession} />
    </WorkspaceModulePage>
  );
}
