"use client";

import { useAppSession } from "@/components/app-session-provider";
import { CashModule } from "@/components/modules/cash-module";
import { WorkspaceModulePage } from "@/components/workspace-module-page";
import { getModuleBySlug } from "@/lib/owner-portal";

const moduleData = getModuleBySlug("caixa");

export default function CashPage() {
  const { session, clearSession } = useAppSession();

  if (!moduleData) {
    return null;
  }

  return (
    <WorkspaceModulePage module={moduleData}>
      <CashModule token={session.token} onUnauthorized={clearSession} />
    </WorkspaceModulePage>
  );
}
