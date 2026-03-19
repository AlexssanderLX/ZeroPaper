"use client";

import { useAppSession } from "@/components/app-session-provider";
import { SettingsModule } from "@/components/modules/settings-module";
import { WorkspaceModulePage } from "@/components/workspace-module-page";
import { getModuleBySlug } from "@/lib/owner-portal";

const moduleData = getModuleBySlug("ajustes");

export default function SettingsPage() {
  const { session, clearSession } = useAppSession();

  if (!moduleData) {
    return null;
  }

  return (
    <WorkspaceModulePage module={moduleData}>
      <SettingsModule token={session.token} onUnauthorized={clearSession} />
    </WorkspaceModulePage>
  );
}
