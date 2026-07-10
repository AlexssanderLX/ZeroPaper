"use client";

import { useAppSession } from "@/components/app-session-provider";
import { SellersModule } from "@/components/modules/sellers-module";
import { WorkspaceModulePage } from "@/components/workspace-module-page";
import { getModuleBySlug } from "@/lib/owner-portal";

const moduleData = getModuleBySlug("vendedores");

export default function VendedoresPage() {
  const { session, clearSession } = useAppSession();

  if (!moduleData) {
    return null;
  }

  return (
    <WorkspaceModulePage
      module={moduleData}
      token={session.token}
      onUnauthorized={clearSession}
      heading="Vendedores"
      description="Cada vendedor tem um link unico. Pedidos feitos pelo link ficam vinculados ao vendedor para acompanhamento."
      showSummary={false}
    >
      <SellersModule token={session.token} onUnauthorized={clearSession} />
    </WorkspaceModulePage>
  );
}
