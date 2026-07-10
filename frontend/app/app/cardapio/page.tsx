"use client";

import { useAppSession } from "@/components/app-session-provider";
import { MenuModule } from "@/components/modules/menu-module";
import { WorkspaceModulePage } from "@/components/workspace-module-page";
import { getModuleBySlug } from "@/lib/owner-portal";

const moduleData = getModuleBySlug("cardapio");

export default function MenuPage() {
  const { session, clearSession } = useAppSession();

  if (!moduleData) {
    return null;
  }

  return (
    <WorkspaceModulePage
      module={moduleData}
      token={session.token}
      onUnauthorized={clearSession}
      heading="Cardapio"
      description="Organize categorias e produtos sem carregar tudo de uma vez."
    >
      <MenuModule token={session.token} onUnauthorized={clearSession} section="items" />
    </WorkspaceModulePage>
  );
}
