"use client";

import { useEffect, useState } from "react";
import Link from "next/link";
import { ApiError, getWorkspaceOverview, type WorkspaceOverview } from "@/lib/api";
import { ownerModules } from "@/lib/owner-portal";
import { useAppSession } from "@/components/app-session-provider";

const quickActions = [
  { label: "Abrir mesas", href: "/app/mesas" },
  { label: "Acompanhar pedidos", href: "/app/pedidos" },
  { label: "Entrar na cozinha", href: "/app/cozinha" },
];

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
    <main className="page-shell app-shell">
      <header className="app-topbar">
        <Link className="brand-lockup" href="/app">
          <div className="brand-mark" aria-hidden="true">
            <span>Z</span>
            <span>P</span>
          </div>
          <div className="brand-copy">
            <span className="eyebrow">ZeroPaper</span>
            <strong>{session.restaurantName}</strong>
          </div>
        </Link>

        <div className="topbar-actions">
          <div className="account-pill">
            <span>{session.ownerName}</span>
            <small>{session.email}</small>
          </div>
          <button className="ghost-link button-link" type="button" onClick={() => void clearSession()}>
            Sair
          </button>
        </div>
      </header>

      <section className="workspace-hero hero-panel">
        <div className="hero-stack">
          <span className="eyebrow">Lobby da unidade</span>
          <h1>Tudo da operacao em um unico ponto de entrada.</h1>
          <p className="hero-description">
            Acesse mesas, pedidos, cozinha, estoque e equipe sem sair do fluxo da
            sua casa.
          </p>

          <div className="hero-actions app-actions">
            {quickActions.map((action) => (
              <Link key={action.href} className="primary-link" href={action.href}>
                {action.label}
              </Link>
            ))}
          </div>
        </div>

        <section className="hero-showcase ambient-panel">
          <div className="showcase-header">
            <span className="eyebrow">Visao geral</span>
            <strong>Pontos centrais do dia</strong>
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

      <section className="metrics-grid">
        <article className="surface-card metric-card interactive-card">
          <span className="eyebrow">Conta</span>
          <strong>{session.profile === "admin" ? "Operacao interna" : session.role}</strong>
          <p>Entre no ambiente principal da unidade e siga o turno com mais clareza.</p>
        </article>

        <article className="surface-card metric-card interactive-card">
          <span className="eyebrow">Acesso</span>
          <strong>Entradas centralizadas</strong>
          <p>{overview?.activeTables ?? 0} mesas e {overview?.openOrders ?? 0} pedidos no mesmo ponto de acesso.</p>
        </article>

        <article className="surface-card metric-card interactive-card">
          <span className="eyebrow">Fluxo</span>
          <strong>Base operacional unica</strong>
          <p>{overview?.teamMembers ?? 0} acessos ativos e {overview?.lowStockItems ?? 0} alertas de reposicao.</p>
        </article>
      </section>

      {errorMessage ? <p className="module-feedback error">{errorMessage}</p> : null}

      <section className="module-grid">
        {ownerModules.map((module) => (
          <article key={module.slug} className="surface-card module-card interactive-card">
            <span className="eyebrow">{module.eyebrow}</span>
            <h2>{module.title}</h2>
            <p>{module.summary}</p>

            <div className="module-stat-list">
              {module.stats.map((stat) => (
                <span key={stat} className="mini-chip">
                  {stat}
                </span>
              ))}
            </div>

            <div className="module-card-footer">
              <div className="module-highlight-stack">
                {module.highlights.map((item) => (
                  <span key={item}>{item}</span>
                ))}
              </div>
              <Link className="ghost-link" href={`/app/${module.slug}`}>
                {module.actionLabel}
              </Link>
            </div>
          </article>
        ))}
      </section>
    </main>
  );
}
