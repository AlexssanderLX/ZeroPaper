"use client";

import { useAppSession } from "@/components/app-session-provider";
import { OrdersModule } from "@/components/modules/orders-module";
import { WorkspaceModulePage } from "@/components/workspace-module-page";
import { getModuleBySlug } from "@/lib/owner-portal";

const moduleData = getModuleBySlug("pedidos");

export default function OrdersTodoPage() {
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
      heading="A fazer"
      description="Acompanhe os pedidos novos e em preparo em uma tela focada, sem misturar com os que ja ficaram prontos."
      showSummary={false}
    >
      <OrdersModule token={session.token} onUnauthorized={clearSession} section="todo" />
    </WorkspaceModulePage>
  );
}
