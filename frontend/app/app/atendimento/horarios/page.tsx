"use client";

import { useAppSession } from "@/components/app-session-provider";
import { HorariosModule } from "@/components/modules/horarios-module";
import { WorkspaceShell } from "@/components/workspace-shell";

export default function HorariosPage() {
  const { session, clearSession } = useAppSession();

  return (
    <WorkspaceShell>
      <HorariosModule token={session.token} onUnauthorized={clearSession} />
    </WorkspaceShell>
  );
}
