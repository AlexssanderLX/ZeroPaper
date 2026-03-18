"use client";

import Link from "next/link";
import type { PortalModule } from "@/lib/owner-portal";
import { useAppSession } from "@/components/app-session-provider";
import { KitchenModule } from "@/components/modules/kitchen-module";
import { OrdersModule } from "@/components/modules/orders-module";
import { SettingsModule } from "@/components/modules/settings-module";
import { StockModule } from "@/components/modules/stock-module";
import { TablesModule } from "@/components/modules/tables-module";
import { TeamModule } from "@/components/modules/team-module";

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
          <button className="ghost-link button-link" type="button" onClick={() => void clearSession()}>
            Sair
          </button>
        </div>
      </header>

      <section className="hero-panel module-main-panel">
        <div className="hero-stack">
          <span className="eyebrow">{module.eyebrow}</span>
          <h1>{module.title}</h1>
          <p className="hero-description">{module.summary}</p>
        </div>

        <section className="hero-showcase ambient-panel">
          <div className="showcase-header">
            <span className="eyebrow">Unidade</span>
            <strong>{session.restaurantName}</strong>
          </div>

          <div className="highlight-grid">
            {module.highlights.map((item) => (
              <article key={item} className="info-card interactive-card">
                <h2>{item}</h2>
              </article>
            ))}
          </div>
        </section>
      </section>

      {module.slug === "mesas" ? <TablesModule token={session.token} onUnauthorized={clearSession} /> : null}
      {module.slug === "pedidos" ? <OrdersModule token={session.token} onUnauthorized={clearSession} /> : null}
      {module.slug === "cozinha" ? <KitchenModule token={session.token} onUnauthorized={clearSession} /> : null}
      {module.slug === "estoque" ? <StockModule token={session.token} onUnauthorized={clearSession} /> : null}
      {module.slug === "equipe" ? <TeamModule token={session.token} onUnauthorized={clearSession} /> : null}
      {module.slug === "ajustes" ? <SettingsModule token={session.token} onUnauthorized={clearSession} /> : null}
    </main>
  );
}
