"use client";

import { createContext, useContext, useEffect, useState } from "react";
import { usePathname, useRouter } from "next/navigation";
import { useAppSession } from "@/components/app-session-provider";
import { ApiError, getWorkspaceOverview, type WorkspaceOverview } from "@/lib/api";

type WorkspaceContextValue = {
  overview: WorkspaceOverview;
};

const WorkspaceContext = createContext<WorkspaceContextValue | null>(null);

const restaurantOnlyRoutes = new Set([
  "caixa",
  "confirmacoes-pix",
  "cupons",
  "entrega",
  "estoque",
  "finalizados",
  "implantacao",
  "mesas",
  "pedidos",
  "vendedores",
]);

const petShopOnlyRoutes = new Set(["agenda", "animais", "atendimentos-pet", "tutores"]);

export function WorkspaceProvider({ children }: { children: React.ReactNode }) {
  const { session, clearSession } = useAppSession();
  const pathname = usePathname();
  const router = useRouter();
  const [overview, setOverview] = useState<WorkspaceOverview | null>(null);
  const [loadFailed, setLoadFailed] = useState(false);
  const [reloadKey, setReloadKey] = useState(0);

  useEffect(() => {
    let isMounted = true;

    setOverview(null);
    setLoadFailed(false);

    void (async () => {
      try {
        const workspaceOverview = await getWorkspaceOverview(session.token);

        if (isMounted) {
          setOverview(workspaceOverview);
        }
      } catch (error) {
        if (error instanceof ApiError && error.status === 401) {
          await clearSession();
          return;
        }

        if (isMounted) {
          setLoadFailed(true);
        }
      }
    })();

    return () => {
      isMounted = false;
    };
  }, [clearSession, reloadKey, session.token]);

  const isPetShop = overview?.businessSegment === 2;
  const requestedSlug = pathname.match(/^\/app\/([^/]+)/)?.[1];
  const redirectTarget = isPetShop ? "/app/agenda" : "/app";
  const isWrongWorkspace = Boolean(
    overview &&
      ((isPetShop && (pathname === "/app" || (requestedSlug && restaurantOnlyRoutes.has(requestedSlug)))) ||
        (!isPetShop && requestedSlug !== undefined && petShopOnlyRoutes.has(requestedSlug))),
  );

  useEffect(() => {
    if (isWrongWorkspace) {
      router.replace(redirectTarget);
    }
  }, [isWrongWorkspace, redirectTarget, router]);

  if (!overview) {
    return (
      <main className="page-shell">
        <section className="surface-card app-loading-card ambient-panel subtle">
          <span className="eyebrow">ZeroPaper</span>
          <h1>{loadFailed ? "Nao foi possivel abrir sua unidade" : "Preparando seu ambiente"}</h1>
          <p>
            {loadFailed
              ? "Verifique sua conexao e tente novamente."
              : "Identificando o segmento e carregando apenas os modulos da sua operacao."}
          </p>
          {loadFailed ? (
            <button className="primary-link button-link" type="button" onClick={() => setReloadKey((key) => key + 1)}>
              Tentar novamente
            </button>
          ) : null}
        </section>
      </main>
    );
  }

  if (isWrongWorkspace) {
    return (
      <main className="page-shell">
        <section className="surface-card app-loading-card ambient-panel subtle">
          <span className="eyebrow">ZeroPaper</span>
          <h1>Abrindo seu ambiente</h1>
          <p>Direcionando para os modulos da sua operacao.</p>
        </section>
      </main>
    );
  }

  return <WorkspaceContext.Provider value={{ overview }}>{children}</WorkspaceContext.Provider>;
}

export function useWorkspace() {
  const context = useContext(WorkspaceContext);

  if (!context) {
    throw new Error("useWorkspace must be used within WorkspaceProvider");
  }

  return context;
}
