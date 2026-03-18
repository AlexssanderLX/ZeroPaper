"use client";

import Link from "next/link";
import type { PortalModule } from "@/lib/owner-portal";
import { useAppSession } from "@/components/app-session-provider";

export function ModuleScreen({ module }: { module: PortalModule }) {
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
          <Link className="ghost-link" href="/app">
            Voltar ao lobby
          </Link>
          <button className="ghost-link button-link" type="button" onClick={clearSession}>
            Sair
          </button>
        </div>
      </header>

      <section className="module-detail-layout">
        <section className="hero-panel module-main-panel">
          <div className="hero-stack">
            <span className="eyebrow">{module.eyebrow}</span>
            <h1>{module.title}</h1>
            <p className="hero-description">{module.summary}</p>

            <div className="hero-actions">
              <Link className="primary-link" href="/app">
                Voltar ao painel
              </Link>
            </div>
          </div>

          <section className="hero-showcase ambient-panel">
            <div className="showcase-header">
              <span className="eyebrow">Foco da area</span>
              <strong>Rotina central da unidade</strong>
            </div>

            <div className="highlight-grid">
              {module.highlights.map((item) => (
                <article key={item} className="info-card interactive-card">
                  <h2>{item}</h2>
                  <p>Area preparada para concentrar essa parte da rotina da unidade.</p>
                </article>
              ))}
            </div>
          </section>
        </section>

        <aside className="module-side-stack">
          <section className="surface-card interactive-card">
            <span className="eyebrow">Unidade</span>
            <h2>{session.restaurantName}</h2>
            <p>{session.ownerName}</p>
          </section>

          <section className="surface-card interactive-card">
            <span className="eyebrow">Entradas da area</span>
            <div className="module-stat-list">
              {module.stats.map((stat) => (
                <span key={stat} className="mini-chip">
                  {stat}
                </span>
              ))}
            </div>
          </section>
        </aside>
      </section>
    </main>
  );
}
