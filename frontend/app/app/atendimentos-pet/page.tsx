"use client";

import { useAppSession } from "@/components/app-session-provider";
import { PetAttendanceModule } from "@/components/modules/pet-attendance-module";
import { WorkspaceModulePage } from "@/components/workspace-module-page";
import { getModuleBySlug } from "@/lib/owner-portal";

const moduleData = getModuleBySlug("atendimentos-pet");

export default function AtendimentosPetPage() {
  const { session, clearSession } = useAppSession();

  if (!moduleData) {
    return null;
  }

  return (
    <WorkspaceModulePage module={moduleData} token={session.token} onUnauthorized={clearSession}>
      <PetAttendanceModule token={session.token} onUnauthorized={clearSession} />
    </WorkspaceModulePage>
  );
}
