"use client";

import { useAppSession } from "@/components/app-session-provider";
import { CouponsModule } from "@/components/modules/coupons-module";
import { WorkspaceModulePage } from "@/components/workspace-module-page";
import { getModuleBySlug } from "@/lib/owner-portal";

const moduleData = getModuleBySlug("cupons");

export default function CouponsPage() {
  const { session, clearSession } = useAppSession();

  if (!moduleData) {
    return null;
  }

  return (
    <WorkspaceModulePage
      module={moduleData}
      token={session.token}
      onUnauthorized={clearSession}
      heading="Cupons de desconto"
      description="Crie e gerencie cupons da unidade. Veja validade, uso e status sem abrir cada cupom."
      showSummary={false}
    >
      <CouponsModule token={session.token} onUnauthorized={clearSession} />
    </WorkspaceModulePage>
  );
}
