"use client";

import Link from "next/link";
import type { PortalModule } from "@/lib/owner-portal";
import { isPortalModuleAvailable, isPortalModuleForSegment } from "@/lib/owner-portal";
import { WorkspaceShell } from "@/components/workspace-shell";
import { useWorkspace } from "@/components/workspace-context";

export function WorkspaceModulePage({
  module,
  heading,
  description,
  backHref = "/app",
  backLabel = "Voltar ao painel",
  showSummary = false,
  children,
}: {
  module: PortalModule;
  token?: string;
  onUnauthorized?: () => Promise<void>;
  heading?: string;
  description?: string;
  backHref?: string;
  backLabel?: string;
  showSummary?: boolean;
  children: React.ReactNode;
}) {
  const { overview } = useWorkspace();
  const hasSegmentAccess = isPortalModuleForSegment(module, overview.businessSegment);
  const hasPlanAccess = !module.featureKey || isPortalModuleAvailable(module, overview);

  if (!hasSegmentAccess || !hasPlanAccess) {
    return (
      <WorkspaceShell backHref={backHref} backLabel={backLabel}>
        <section className="surface-card workspace-summary-card module-summary-card simple-module-summary">
          <div className="workspace-summary-head">
            <div className="hero-stack">
              <h1>{heading ?? module.title}</h1>
              <p className="body-copy">
                Este modulo nao esta disponivel para o segmento ou plano atual da unidade.
              </p>
              <div className="toolbar-actions compact">
                <Link className="primary-link button-link" href={backHref}>
                  {backLabel}
                </Link>
              </div>
            </div>
          </div>
        </section>
      </WorkspaceShell>
    );
  }

  return (
    <WorkspaceShell backHref={backHref} backLabel={backLabel}>
      {showSummary ? (
        <section className="surface-card workspace-summary-card module-summary-card simple-module-summary">
          <div className="workspace-summary-head">
            <div className="hero-stack">
              <h1>{heading ?? module.title}</h1>
              {description ? <p className="body-copy">{description}</p> : null}
            </div>
          </div>
        </section>
      ) : null}

      {children}
    </WorkspaceShell>
  );
}
