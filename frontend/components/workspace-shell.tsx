"use client";

import Link from "next/link";
import { useAppSession } from "@/components/app-session-provider";
import { BrandMark } from "@/components/brand-mark";

export function WorkspaceShell({
  children,
  backHref = "/app",
  backLabel = "Voltar ao painel",
}: {
  children: React.ReactNode;
  backHref?: string;
  backLabel?: string;
}) {
  const { session, clearSession } = useAppSession();

  return (
    <main className="page-shell app-shell">
      <header className="app-topbar">
        <Link className="brand-lockup" href="/app">
          <BrandMark />
          <div className="brand-copy">
            <span className="eyebrow">ZeroPaper</span>
            <strong>{session.restaurantName}</strong>
          </div>
        </Link>

        <div className="topbar-actions">
          <Link className="ghost-link" href={backHref}>
            {backLabel}
          </Link>
          <button className="ghost-link button-link" type="button" onClick={() => void clearSession()}>
            Sair
          </button>
        </div>
      </header>

      {children}
    </main>
  );
}
