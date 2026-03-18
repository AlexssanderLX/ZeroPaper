"use client";

import Link from "next/link";
import { ownerModules } from "@/lib/owner-portal";
import { useAppSession } from "@/components/app-session-provider";

const quickOverview = [
  { label: "Frente da casa", value: "Mesas e pedidos em um so fluxo" },
  { label: "Cozinha", value: "Fila organizada por preparo" },
  { label: "Estoque", value: "Visibilidade dos itens criticos" },
];

const quickActions = [
  { label: "Abrir mesas", href: "/app/mesas" },
  { label: "Acompanhar pedidos", href: "/app/pedidos" },
  { label: "Entrar na cozinha", href: "/app/cozinha" },
];

export function OwnerLobby() {
  const { session, clearSession } = useAppSession();

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
          <button className="ghost-link button-link" type="button" onClick={clearSession}>
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
          <strong>{session.profile === "admin" ? "Operacao interna" : "Acesso da unidade"}</strong>
          <p>Entre no ambiente principal da unidade e siga o turno com mais clareza.</p>
        </article>

        <article className="surface-card metric-card interactive-card">
          <span className="eyebrow">Acesso</span>
          <strong>Entradas centralizadas</strong>
          <p>Mesas, pedidos, cozinha e estoque ficam no mesmo ponto de acesso.</p>
        </article>

        <article className="surface-card metric-card interactive-card">
          <span className="eyebrow">Fluxo</span>
          <strong>Base operacional unica</strong>
          <p>Atendimento, preparo e controle caminham juntos na rotina da casa.</p>
        </article>
      </section>

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
