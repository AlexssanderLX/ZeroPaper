"use client";

import { useAppSession } from "@/components/app-session-provider";
import { MenuModule } from "@/components/modules/menu-module";
import { WorkspaceModulePage } from "@/components/workspace-module-page";
import { getModuleBySlug } from "@/lib/owner-portal";

const moduleData = getModuleBySlug("cardapio");

export default function MenuItemsPage() {
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
      heading="Itens do cardapio"
      description="Organize categorias e produtos. Abra cada cadastro somente quando precisar editar."
    >
      <MenuModule token={session.token} onUnauthorized={clearSession} section="items" />
    </WorkspaceModulePage>
  );
}
