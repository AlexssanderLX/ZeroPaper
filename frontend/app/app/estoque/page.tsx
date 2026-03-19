"use client";

import { useAppSession } from "@/components/app-session-provider";
import { StockModule } from "@/components/modules/stock-module";
import { WorkspaceModulePage } from "@/components/workspace-module-page";
import { getModuleBySlug } from "@/lib/owner-portal";

const moduleData = getModuleBySlug("estoque");

export default function StockPage() {
  const { session, clearSession } = useAppSession();

  if (!moduleData) {
    return null;
  }

  return (
    <WorkspaceModulePage module={moduleData}>
      <StockModule token={session.token} onUnauthorized={clearSession} />
    </WorkspaceModulePage>
  );
}
