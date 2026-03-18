"use client";

import { createContext, useContext, useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { logoutPortal } from "@/lib/api";
import type { PortalSession } from "@/lib/owner-portal";
import { PORTAL_SESSION_KEY } from "@/lib/owner-portal";

type SessionContextValue = {
  session: PortalSession;
  clearSession: () => Promise<void>;
};

const SessionContext = createContext<SessionContextValue | null>(null);

export function AppSessionProvider({ children }: { children: React.ReactNode }) {
  const router = useRouter();
  const [session, setSession] = useState<PortalSession | null>(null);
  const [ready, setReady] = useState(false);

  useEffect(() => {
    const storedSession = window.sessionStorage.getItem(PORTAL_SESSION_KEY);

    if (!storedSession) {
      router.replace("/login");
      return;
    }

    try {
      setSession(JSON.parse(storedSession) as PortalSession);
    } catch {
      window.sessionStorage.removeItem(PORTAL_SESSION_KEY);
      router.replace("/login");
      return;
    }

    setReady(true);
  }, [router]);

  if (!ready || !session) {
    return (
      <main className="page-shell">
        <section className="surface-card app-loading-card ambient-panel subtle">
          <span className="eyebrow">ZeroPaper</span>
          <h1>Preparando seu acesso</h1>
          <p>Carregando o ambiente da sua unidade.</p>
        </section>
      </main>
    );
  }

  const value: SessionContextValue = {
    session,
    clearSession: async () => {
      try {
        await logoutPortal(session.token);
      } catch {
        // Best-effort logout keeps the local session from surviving stale tokens.
      }

      window.sessionStorage.removeItem(PORTAL_SESSION_KEY);
      setSession(null);
      router.replace("/login");
    },
  };

  return <SessionContext.Provider value={value}>{children}</SessionContext.Provider>;
}

export function useAppSession() {
  const context = useContext(SessionContext);

  if (!context) {
    throw new Error("useAppSession must be used within AppSessionProvider");
  }

  return context;
}
