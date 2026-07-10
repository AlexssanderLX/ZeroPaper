"use client";

import { useAppSession } from "@/components/app-session-provider";
import { PrintingModule } from "@/components/modules/printing-module";
import { WorkspaceModulePage } from "@/components/workspace-module-page";
import { getModuleBySlug } from "@/lib/owner-portal";

const moduleData = getModuleBySlug("impressao");

export default function PrintingPage() {
  const { session, clearSession } = useAppSession();

  if (!moduleData) {
    return null;
  }

  return (
    <WorkspaceModulePage module={moduleData} token={session.token} onUnauthorized={clearSession}>
      <PrintingModule token={session.token} onUnauthorized={clearSession} />
    </WorkspaceModulePage>
  );
}
