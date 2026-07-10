"use client";

import { useAppSession } from "@/components/app-session-provider";
import { CashModule } from "@/components/modules/cash-module";
import { WorkspaceModulePage } from "@/components/workspace-module-page";
import { getModuleBySlug } from "@/lib/owner-portal";

const moduleData = getModuleBySlug("caixa");

export default function CashPage() {
  const { session, clearSession } = useAppSession();

  if (!moduleData) {
    return null;
  }

  return (
    <WorkspaceModulePage
      module={moduleData}
      token={session.token}
      onUnauthorized={clearSession}
      heading="Caixa"
      description="Veja a cobrar e pagos na mesma tela para fechar o caixa sem ficar entrando em varias paginas."
    >
      <CashModule token={session.token} onUnauthorized={clearSession} section="overview" />
    </WorkspaceModulePage>
  );
}
