"use client";

import type { PortalModule } from "@/lib/owner-portal";
import { useAppSession } from "@/components/app-session-provider";
import { WorkspaceShell } from "@/components/workspace-shell";

export function WorkspaceModulePage({
  module,
  children,
}: {
  module: PortalModule;
  children: React.ReactNode;
}) {
  const { session } = useAppSession();

  return (
    <WorkspaceShell>
      <section className="surface-card workspace-summary-card module-summary-card">
        <div className="workspace-summary-head">
          <div className="hero-stack">
            <span className="eyebrow">{module.eyebrow}</span>
            <h1>{module.title}</h1>
          </div>

          <div className="workspace-owner-chip">
            <span className="eyebrow">Unidade</span>
            <strong>{session.restaurantName}</strong>
          </div>
        </div>
      </section>

      {children}
    </WorkspaceShell>
  );
}
