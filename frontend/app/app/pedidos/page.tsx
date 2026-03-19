"use client";

import { useAppSession } from "@/components/app-session-provider";
import { OrdersModule } from "@/components/modules/orders-module";
import { WorkspaceModulePage } from "@/components/workspace-module-page";
import { getModuleBySlug } from "@/lib/owner-portal";

const moduleData = getModuleBySlug("pedidos");

export default function OrdersPage() {
  const { session, clearSession } = useAppSession();

  if (!moduleData) {
    return null;
  }

  return (
    <WorkspaceModulePage module={moduleData}>
      <OrdersModule token={session.token} onUnauthorized={clearSession} />
    </WorkspaceModulePage>
  );
}
