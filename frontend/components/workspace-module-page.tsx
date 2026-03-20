"use client";

import type { PortalModule } from "@/lib/owner-portal";
import { WorkspaceShell } from "@/components/workspace-shell";

export function WorkspaceModulePage({
  module,
  children,
}: {
  module: PortalModule;
  children: React.ReactNode;
}) {
  return (
    <WorkspaceShell backHref="/app" backLabel="Voltar ao painel">
      <section className="surface-card workspace-summary-card module-summary-card simple-module-summary">
        <div className="workspace-summary-head">
          <div className="hero-stack">
            <h1>{module.title}</h1>
          </div>
        </div>
      </section>

      {children}
    </WorkspaceShell>
  );
}
