"use client";

import { useAppSession } from "@/components/app-session-provider";
import { PetsModule } from "@/components/modules/pets-module";
import { WorkspaceModulePage } from "@/components/workspace-module-page";
import { getModuleBySlug } from "@/lib/owner-portal";

const moduleData = getModuleBySlug("animais");

export default function AnimaisPage() {
  const { session, clearSession } = useAppSession();

  if (!moduleData) {
    return null;
  }

  return (
    <WorkspaceModulePage module={moduleData} token={session.token} onUnauthorized={clearSession}>
      <PetsModule token={session.token} onUnauthorized={clearSession} />
    </WorkspaceModulePage>
  );
}
