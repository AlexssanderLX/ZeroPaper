"use client";

import { useEffect, useState } from "react";
import { useAppSession } from "@/components/app-session-provider";
import { MenuModule } from "@/components/modules/menu-module";
import { WorkspaceModulePage } from "@/components/workspace-module-page";
import { getModuleBySlug } from "@/lib/owner-portal";
import { getWorkspaceOverview } from "@/lib/api";

const moduleData = getModuleBySlug("cardapio");

export default function MenuPage() {
  const { session, clearSession } = useAppSession();
  const [businessSegment, setBusinessSegment] = useState<number | null>(null);

  useEffect(() => {
    let mounted = true;
    void getWorkspaceOverview(session.token)
      .then((overview) => {
        if (mounted) setBusinessSegment(overview.businessSegment ?? 1);
      })
      .catch(() => {
        if (mounted) setBusinessSegment(1);
      });
    return () => {
      mounted = false;
    };
  }, [session.token]);

  if (!moduleData || businessSegment === null) {
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
