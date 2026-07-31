"use client";

import { useAppSession } from "@/components/app-session-provider";
import { PetCustomersModule } from "@/components/modules/pet-customers-module";
import { WorkspaceModulePage } from "@/components/workspace-module-page";
import { getModuleBySlug } from "@/lib/owner-portal";

const moduleData = getModuleBySlug("tutores");

export default function TutoresPage() {
  const { session, clearSession } = useAppSession();

  if (!moduleData) {
    return null;
  }

  return (
    <WorkspaceModulePage module={moduleData} token={session.token} onUnauthorized={clearSession}>
      <PetCustomersModule token={session.token} onUnauthorized={clearSession} />
    </WorkspaceModulePage>
  );
}
