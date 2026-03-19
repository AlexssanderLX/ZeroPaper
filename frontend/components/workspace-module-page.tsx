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
      <section className="hero-panel module-main-panel">
        <div className="hero-stack">
          <span className="eyebrow">{module.eyebrow}</span>
          <h1>{module.title}</h1>
        </div>

        <section className="hero-showcase ambient-panel">
          <div className="showcase-header">
            <span className="eyebrow">Unidade</span>
            <strong>{session.restaurantName}</strong>
          </div>
        </section>
      </section>

      {children}
    </WorkspaceShell>
  );
}
