"use client";

import Link from "next/link";
import { useEffect, useState } from "react";
import { ApiError, getWorkspaceOverview } from "@/lib/api";
import type { PortalModule } from "@/lib/owner-portal";
import { isPortalModuleAvailable } from "@/lib/owner-portal";
import { WorkspaceShell } from "@/components/workspace-shell";

export function WorkspaceModulePage({
  module,
  token,
  onUnauthorized,
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
  const [accessState, setAccessState] = useState<"checking" | "allowed" | "blocked" | "error">(
    module.featureKey ? "checking" : "allowed",
  );
  const [errorMessage, setErrorMessage] = useState("");

  useEffect(() => {
    let isMounted = true;

    if (!module.featureKey || !token) {
      setAccessState("allowed");
      setErrorMessage("");
      return;
    }

    void (async () => {
      try {
        const overview = await getWorkspaceOverview(token);

        if (!isMounted) {
          return;
        }

        setAccessState(isPortalModuleAvailable(module, overview) ? "allowed" : "blocked");
        setErrorMessage("");
      } catch (error) {
        if (!isMounted) {
          return;
        }

        if (error instanceof ApiError && error.status === 401 && onUnauthorized) {
          await onUnauthorized();
          return;
        }

        setAccessState("error");
        setErrorMessage("Nao foi possivel validar o plano da unidade agora.");
      }
    })();

    return () => {
      isMounted = false;
    };
  }, [module, onUnauthorized, token]);

  if (accessState === "checking") {
    return (
      <WorkspaceShell backHref={backHref} backLabel={backLabel}>
        <p className="workspace-inline-loading">Carregando...</p>
      </WorkspaceShell>
    );
  }

  if (accessState === "blocked" || accessState === "error") {
    return (
      <WorkspaceShell backHref={backHref} backLabel={backLabel}>
        <section className="surface-card workspace-summary-card module-summary-card simple-module-summary">
          <div className="workspace-summary-head">
            <div className="hero-stack">
              <h1>{heading ?? module.title}</h1>
              <p className="body-copy">
                {accessState === "blocked"
                  ? "Este modulo nao faz parte do plano atual da unidade."
                  : errorMessage}
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
