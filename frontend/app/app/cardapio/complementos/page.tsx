"use client";

import { useAppSession } from "@/components/app-session-provider";
import { MenuModule } from "@/components/modules/menu-module";
import { WorkspaceModulePage } from "@/components/workspace-module-page";
import { getModuleBySlug } from "@/lib/owner-portal";

const moduleData = getModuleBySlug("cardapio");

export default function MenuAdditionalsPage() {
  const { session, clearSession } = useAppSession();

  if (!moduleData) {
    return null;
  }

  return (
    <WorkspaceModulePage
      module={moduleData}
      token={session.token}
      onUnauthorized={clearSession}
      backHref="/app/cardapio"
      backLabel="Voltar ao Cardapio"
      heading="Complementos"
      description="Cadastre extras reutilizaveis e vincule-os aos produtos quando precisar."
    >
      <MenuModule token={session.token} onUnauthorized={clearSession} section="additionals" />
    </WorkspaceModulePage>
  );
}
