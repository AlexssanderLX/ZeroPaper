"use client";

import { useAppSession } from "@/components/app-session-provider";
import { CashModule } from "@/components/modules/cash-module";
import { WorkspaceModulePage } from "@/components/workspace-module-page";
import { getModuleBySlug } from "@/lib/owner-portal";

const moduleData = getModuleBySlug("caixa");

export default function CashPendingPage() {
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
      heading="A cobrar"
      description="Feche os pedidos pendentes em uma area dedicada do caixa, com menos ruido visual e mais foco no atendimento."
    >
      <CashModule token={session.token} onUnauthorized={clearSession} section="pending" />
    </WorkspaceModulePage>
  );
}
