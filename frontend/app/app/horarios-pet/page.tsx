"use client";

import { useAppSession } from "@/components/app-session-provider";
import { PetScheduleSettingsModule } from "@/components/modules/pet-schedule-settings-module";
import { WorkspaceModulePage } from "@/components/workspace-module-page";
import { getModuleBySlug } from "@/lib/owner-portal";

const moduleData = getModuleBySlug("horarios-pet");

export default function HorariosPetPage() {
  const { session, clearSession } = useAppSession();

  if (!moduleData) {
    return null;
  }

  return (
    <WorkspaceModulePage module={moduleData} token={session.token} onUnauthorized={clearSession}>
      <PetScheduleSettingsModule token={session.token} onUnauthorized={clearSession} />
    </WorkspaceModulePage>
  );
}
