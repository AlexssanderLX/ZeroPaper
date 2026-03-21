"use client";

import Link from "next/link";
import { useAppSession } from "@/components/app-session-provider";
import { BrandMark } from "@/components/brand-mark";

export function WorkspaceShell({
  children,
  backHref = "/app",
  backLabel,
}: {
  children: React.ReactNode;
  backHref?: string;
  backLabel?: string;
}) {
  const { session, clearSession } = useAppSession();

  async function handleSignOut() {
    const confirmed = window.confirm("Tem certeza que deseja sair?");

    if (!confirmed) {
      return;
    }

    await clearSession();
  }

  return (
    <main className="page-shell app-shell">
      <header className="app-topbar">
        <Link className="brand-lockup" href="/app">
          <BrandMark small variant="full" />
          <div className="brand-copy">
            <span className="eyebrow">ZeroPaper</span>
            <strong>{session.restaurantName}</strong>
          </div>
        </Link>

        <div className="topbar-actions">
          <div className="account-pill topbar-account-pill">
            <span>{session.ownerName}</span>
            <small>{session.email}</small>
          </div>
          {backLabel ? (
            <Link className="ghost-link" href={backHref}>
              {backLabel}
            </Link>
          ) : null}
          <button className="ghost-link button-link" type="button" onClick={() => void handleSignOut()}>
            Sair
          </button>
        </div>
      </header>

      {children}
    </main>
  );
}
