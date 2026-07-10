"use client";

import { useAppSession } from "@/components/app-session-provider";
import { FinishedOrdersModule } from "@/components/modules/finished-orders-module";
import { WorkspaceModulePage } from "@/components/workspace-module-page";
import { getModuleBySlug } from "@/lib/owner-portal";

const moduleData = getModuleBySlug("pedidos");

export default function FinishedOrdersPage() {
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
      backLabel="Voltar aos meus pedidos"
      heading="Finalizados"
      description="Confira pedidos encerrados sem interferir no fluxo da cozinha."
      showSummary={false}
    >
      <FinishedOrdersModule token={session.token} onUnauthorized={clearSession} />
    </WorkspaceModulePage>
  );
}
