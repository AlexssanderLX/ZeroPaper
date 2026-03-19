"use client";

import { useAppSession } from "@/components/app-session-provider";
import { TablesModule } from "@/components/modules/tables-module";
import { WorkspaceModulePage } from "@/components/workspace-module-page";
import { getModuleBySlug } from "@/lib/owner-portal";

const moduleData = getModuleBySlug("mesas");

export default function TablesPage() {
  const { session, clearSession } = useAppSession();

  if (!moduleData) {
    return null;
  }

  return (
    <WorkspaceModulePage module={moduleData}>
      <TablesModule token={session.token} onUnauthorized={clearSession} />
    </WorkspaceModulePage>
  );
}
