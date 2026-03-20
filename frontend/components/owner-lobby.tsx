"use client";

import { useEffect, useState } from "react";
import Link from "next/link";
import { ApiError, getWorkspaceOverview, type WorkspaceOverview } from "@/lib/api";
import { ownerModules } from "@/lib/owner-portal";
import { useAppSession } from "@/components/app-session-provider";
import { WorkspaceShell } from "@/components/workspace-shell";

export function OwnerLobby() {
  const { session } = useAppSession();
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
    {
      slug: "cardapio",
      value: String(overview?.publishedMenuItems ?? 0),
      label: "Disponiveis",
    },
    {
      slug: "mesas",
      value: String(overview?.activeTables ?? 0),
      label: "Ativas",
    },
    {
      slug: "pedidos",
      value: String(overview?.openOrders ?? 0),
      label: "Abertos",
    },
    {
      slug: "ajustes",
      value: session.ownerName,
      label: "Responsavel",
    },
  ];

  return (
    <WorkspaceShell>
      <section className="surface-card workspace-summary-card owner-hero-card">
        <div className="workspace-summary-head owner-summary-head">
          <div className="owner-summary-stage" aria-hidden="true">
            <div className="owner-summary-glow owner-summary-glow-one" />
            <div className="owner-summary-glow owner-summary-glow-two" />
            <div className="owner-summary-orbit" />
            <div className="owner-summary-name-motion">
              <strong>{session.restaurantName}</strong>
            </div>
          </div>
        </div>
      </section>

      {errorMessage ? <p className="module-feedback error">{errorMessage}</p> : null}

      <section className="module-grid">
        {ownerModules.map((module) => {
          const moduleMetric = quickOverview.find((item) => item.slug === module.slug);

          return (
          <Link key={module.slug} className="surface-card module-card interactive-card module-entry-link" href={`/app/${module.slug}`}>
            <span className="eyebrow">{module.eyebrow}</span>
            <h2>{module.title}</h2>
            {moduleMetric ? (
              <div className="module-card-metric">
                <strong>{moduleMetric.value}</strong>
                <span>{moduleMetric.label}</span>
              </div>
            ) : null}
          </Link>
          );
        })}
      </section>
    </WorkspaceShell>
  );
}
