"use client";

import { useEffect, useState } from "react";
import Link from "next/link";
import { ApiError, getWorkspaceOverview, type WorkspaceOverview } from "@/lib/api";
import { ownerModules } from "@/lib/owner-portal";
import { useAppSession } from "@/components/app-session-provider";
import { WorkspaceShell } from "@/components/workspace-shell";

export function OwnerLobby() {
  const { session, clearSession } = useAppSession();
  const [overview, setOverview] = useState<WorkspaceOverview | null>(null);
  const [errorMessage, setErrorMessage] = useState("");

  useEffect(() => {
    let isMounted = true;

    void (async () => {
      try {
        const response = await getWorkspaceOverview(session.token);

        if (!isMounted) {
          return;
        }

        setOverview(response);
        setErrorMessage("");
      } catch (error) {
        if (!isMounted) {
          return;
        }

        if (error instanceof ApiError && error.status === 401) {
          await clearSession();
          return;
        }

        setErrorMessage("Nao foi possivel carregar a visao geral agora.");
      }
    })();

    return () => {
      isMounted = false;
    };
  }, [session.token]);

  const quickOverview = [
    { label: "Mesas ativas", value: String(overview?.activeTables ?? 0) },
    { label: "Pedidos abertos", value: String(overview?.openOrders ?? 0) },
    { label: "Alertas de estoque", value: String(overview?.lowStockItems ?? 0) },
  ];

  return (
    <WorkspaceShell backHref="/login" backLabel="Trocar acesso">
      <section className="workspace-hero hero-panel">
        <div className="hero-stack">
          <span className="eyebrow">Painel da unidade</span>
          <h1>{session.restaurantName}</h1>
        </div>

        <section className="hero-showcase ambient-panel">
          <div className="showcase-header">
            <span className="eyebrow">Conta ativa</span>
            <strong>{session.ownerName}</strong>
          </div>

          <div className="overview-grid">
            {quickOverview.map((item) => (
              <article key={item.label} className="info-card interactive-card">
                <p className="overview-label">{item.label}</p>
                <h2>{item.value}</h2>
              </article>
            ))}
          </div>
        </section>
      </section>

      {errorMessage ? <p className="module-feedback error">{errorMessage}</p> : null}

      <section className="module-grid">
        {ownerModules.map((module) => (
          <Link key={module.slug} className="surface-card module-card interactive-card module-entry-link" href={`/app/${module.slug}`}>
            <span className="eyebrow">{module.eyebrow}</span>
            <h2>{module.title}</h2>

            <div className="module-card-footer">
              <span className="ghost-link">
                {module.actionLabel}
              </span>
            </div>
          </Link>
        ))}
      </section>
    </WorkspaceShell>
  );
}
