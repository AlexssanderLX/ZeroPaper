"use client";

import { useAppSession } from "@/components/app-session-provider";
import { AppointmentsModule } from "@/components/modules/appointments-module";
import { WorkspaceModulePage } from "@/components/workspace-module-page";
import { getModuleBySlug } from "@/lib/owner-portal";

const moduleData = getModuleBySlug("agenda");

export default function AgendaPage() {
  const { session, clearSession } = useAppSession();

  if (!moduleData) {
    return null;
  }

  return (
    <WorkspaceModulePage module={moduleData} token={session.token} onUnauthorized={clearSession}>
      <AppointmentsModule token={session.token} onUnauthorized={clearSession} />
    </WorkspaceModulePage>
  );
}
