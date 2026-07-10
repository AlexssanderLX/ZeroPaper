"use client";

import { useAppSession } from "@/components/app-session-provider";
import { OrdersModule } from "@/components/modules/orders-module";
import { WorkspaceModulePage } from "@/components/workspace-module-page";
import { getModuleBySlug } from "@/lib/owner-portal";

const moduleData = getModuleBySlug("pedidos");

export default function OrdersReadyPage() {
  const { session, clearSession } = useAppSession();

  if (!moduleData) {
    return null;
  }

  return (
    <WorkspaceModulePage
      module={moduleData}
      token={session.token}
      onUnauthorized={clearSession}
      backHref="/app/pedidos"
      backLabel="Voltar a cozinha"
      heading="Prontos"
      description="Veja os pedidos prontos para saida e volte algum para a fazer se precisar corrigir o preparo."
      showSummary={false}
    >
      <OrdersModule token={session.token} onUnauthorized={clearSession} section="ready" />
    </WorkspaceModulePage>
  );
}
