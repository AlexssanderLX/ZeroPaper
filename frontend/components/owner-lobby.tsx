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
    { label: "Pratos disponiveis", value: String(overview?.publishedMenuItems ?? 0) },
    { label: "Itens no cardapio", value: String(overview?.totalMenuItems ?? 0) },
  ];

  return (
    <WorkspaceShell backHref="/login" backLabel="Trocar acesso">
      <section className="surface-card workspace-summary-card">
        <div className="workspace-summary-head">
          <div className="hero-stack">
            <span className="eyebrow">Unidade</span>
            <h1>{session.restaurantName}</h1>
          </div>

          <div className="workspace-owner-chip">
            <span className="eyebrow">Dono</span>
            <strong>{session.ownerName}</strong>
          </div>
        </div>

        <div className="overview-grid workspace-overview-grid">
          {quickOverview.map((item) => (
            <article key={item.label} className="info-card interactive-card compact-info-card">
              <p className="overview-label">{item.label}</p>
              <h2>{item.value}</h2>
            </article>
          ))}
        </div>
      </section>

      {errorMessage ? <p className="module-feedback error">{errorMessage}</p> : null}

      <section className="module-grid">
        {ownerModules.map((module) => (
          <Link key={module.slug} className="surface-card module-card interactive-card module-entry-link" href={`/app/${module.slug}`}>
            <span className="eyebrow">{module.eyebrow}</span>
            <h2>{module.title}</h2>
            <span className="ghost-link">{module.actionLabel}</span>
          </Link>
        ))}
      </section>
    </WorkspaceShell>
  );
}
