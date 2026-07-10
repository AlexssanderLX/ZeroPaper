"use client";

import { useAppSession } from "@/components/app-session-provider";
import { ReportsModule } from "@/components/modules/reports-module";
import { WorkspaceModulePage } from "@/components/workspace-module-page";
import { getModuleBySlug } from "@/lib/owner-portal";

const moduleData = getModuleBySlug("relatorios");

export default function ReportsPage() {
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
      heading="Relatorios e fluxo do dia"
      description="Fechamento, PDF, relatorio detalhado e limpeza operacional ficam juntos."
    >
      <ReportsModule token={session.token} onUnauthorized={clearSession} variant="flow" />
    </WorkspaceModulePage>
  );
}
