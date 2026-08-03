"use client";

import { useAppSession } from "@/components/app-session-provider";
import { useWorkspace } from "@/components/workspace-context";
import { MenuModule } from "@/components/modules/menu-module";
import { WorkspaceModulePage } from "@/components/workspace-module-page";
import { getModuleBySlug } from "@/lib/owner-portal";

const moduleData = getModuleBySlug("cardapio");

export default function MenuPage() {
  const { session, clearSession } = useAppSession();
  const { overview } = useWorkspace();
  const businessSegment = overview.businessSegment;

  if (!moduleData) {
    return null;
  }

  return (
    <WorkspaceModulePage
      module={moduleData}
      token={session.token}
      onUnauthorized={clearSession}
      heading={businessSegment === 2 ? "Servicos" : "Cardapio"}
      description={businessSegment === 2
        ? "Organize os servicos, valores e disponibilidade do Pet Shop."
        : "Organize categorias e produtos sem carregar tudo de uma vez."}
    >
      <MenuModule token={session.token} onUnauthorized={clearSession} section="items" itemKind={businessSegment === 2 ? 2 : 1} />
    </WorkspaceModulePage>
  );
}
