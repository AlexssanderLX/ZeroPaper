"use client";

import { useAppSession } from "@/components/app-session-provider";
import { DeliveryModule } from "@/components/modules/delivery-module";
import { WorkspaceModulePage } from "@/components/workspace-module-page";
import { getModuleBySlug } from "@/lib/owner-portal";

const moduleData = getModuleBySlug("entrega");

export default function DeliveryPage() {
  const { session, clearSession } = useAppSession();

  if (!moduleData) {
    return null;
  }

  return (
    <WorkspaceModulePage
      module={moduleData}
      token={session.token}
      onUnauthorized={clearSession}
      heading="Entrega e frete"
      description="Configure o CEP da unidade, taxa base opcional e valor por KM para calcular frete no delivery."
    >
      <DeliveryModule token={session.token} onUnauthorized={clearSession} />
    </WorkspaceModulePage>
  );
}
