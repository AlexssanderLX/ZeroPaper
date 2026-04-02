"use client";

import { useAppSession } from "@/components/app-session-provider";
import { SetupModule } from "@/components/modules/setup-module";
import { WorkspaceModulePage } from "@/components/workspace-module-page";
import { getModuleBySlug } from "@/lib/owner-portal";

const moduleData = getModuleBySlug("implantacao");

export default function SetupPage() {
  const { session, clearSession } = useAppSession();

  if (!moduleData) {
    return null;
  }

  return (
    <WorkspaceModulePage module={moduleData}>
      <SetupModule token={session.token} onUnauthorized={clearSession} />
    </WorkspaceModulePage>
  );
}
