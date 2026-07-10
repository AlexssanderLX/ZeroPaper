"use client";

import Link from "next/link";
import { useEffect, useMemo, useState } from "react";
import { usePathname } from "next/navigation";
import { useAppSession } from "@/components/app-session-provider";
import { WaiterCallMonitor } from "@/components/waiter-call-monitor";
import {
  ApiError,
  ensureCashOrderTable,
  getAiAssistantQuickStatus,
  getCompanySettings,
  getPrintingSettings,
  getWorkspaceOverview,
  updateAiAssistantQuickStatus,
  updatePrintingSettings,
  type AiAssistantQuickStatus,
  type PrintingSettings,
  type WorkspaceOverview,
} from "@/lib/api";
import { isPortalModuleAvailable, ownerModules } from "@/lib/owner-portal";

export function WorkspaceShell({
  children,
  backHref = "/app",
  backLabel,
  showAlertCard = false,
}: {
  children: React.ReactNode;
  backHref?: string;
  backLabel?: string;
  showAlertCard?: boolean;
}) {
  const { session, clearSession } = useAppSession();
  const pathname = usePathname();
  const [logoUrl, setLogoUrl] = useState("");
  const [overview, setOverview] = useState<WorkspaceOverview | null>(null);
  const [overviewLoaded, setOverviewLoaded] = useState(false);
  const [isSidebarOpen, setIsSidebarOpen] = useState(false);
  const [aiQuickStatus, setAiQuickStatus] = useState<AiAssistantQuickStatus | null>(null);
  const [printingQuickStatus, setPrintingQuickStatus] = useState<PrintingSettings | null>(null);
  const [cashOrderAccessUrl, setCashOrderAccessUrl] = useState("");
  const [isUpdatingAiStatus, setIsUpdatingAiStatus] = useState(false);
  const [isUpdatingPrintingStatus, setIsUpdatingPrintingStatus] = useState(false);
  const [aiStatusError, setAiStatusError] = useState("");
  const [printingStatusError, setPrintingStatusError] = useState("");

  useEffect(() => {
    let isMounted = true;

    void (async () => {
      try {
        const settings = await getCompanySettings(session.token);

        if (isMounted) {
          setLogoUrl(settings.logoUrl ?? "");
        }
      } catch {
        if (isMounted) {
          setLogoUrl("");
        }
      }
    })();

    return () => {
      isMounted = false;
    };
  }, [session.token]);

  useEffect(() => {
    let isMounted = true;

    void (async () => {
      try {
        const workspaceOverview = await getWorkspaceOverview(session.token);

        if (isMounted) {
          setOverview(workspaceOverview);
          setOverviewLoaded(true);
        }
      } catch (error) {
        if (error instanceof ApiError && error.status === 401) {
          await clearSession();
          return;
        }

        if (isMounted) {
          setOverview(null);
          setOverviewLoaded(true);
        }
      }
    })();

    return () => {
      isMounted = false;
    };
  }, [clearSession, session.token]);

  useEffect(() => {
    let isMounted = true;

    if (!overviewLoaded || !overview?.includesAiAssistantModule) {
      setAiQuickStatus(null);
      return;
    }

    void (async () => {
      try {
        const status = await getAiAssistantQuickStatus(session.token);
        if (isMounted) {
          setAiQuickStatus(status);
          setAiStatusError("");
        }
      } catch (error) {
        if (error instanceof ApiError && error.status === 401) {
          await clearSession();
          return;
        }

        if (isMounted) {
          setAiStatusError("Nao foi possivel consultar a IA.");
        }
      }
    })();

    return () => {
      isMounted = false;
    };
  }, [clearSession, overview, overviewLoaded, session.token]);

  useEffect(() => {
    let isMounted = true;

    if (!overviewLoaded || !overview?.includesPrintingModule) {
      setPrintingQuickStatus(null);
      return;
    }

    void (async () => {
      try {
        const status = await getPrintingSettings(session.token);
        if (isMounted) {
          setPrintingQuickStatus(status);
          setPrintingStatusError("");
        }
      } catch (error) {
        if (error instanceof ApiError && error.status === 401) {
          await clearSession();
          return;
        }

        if (isMounted) {
          setPrintingStatusError("Nao foi possivel consultar a impressao.");
        }
      }
    })();

    return () => {
      isMounted = false;
    };
  }, [clearSession, overview, overviewLoaded, session.token]);

  useEffect(() => {
    let isMounted = true;

    void (async () => {
      try {
        const cashOrderTable = await ensureCashOrderTable(session.token);

        if (isMounted) {
          setCashOrderAccessUrl(cashOrderTable.accessUrl);
        }
      } catch (error) {
        if (error instanceof ApiError && error.status === 401) {
          await clearSession();
        }
      }
    })();

    return () => {
      isMounted = false;
    };
  }, [clearSession, session.token]);

  const visibleModules = useMemo(
    () =>
      ownerModules.filter((module) => {
        if (!module.featureKey) {
          return true;
        }

        if (!overviewLoaded || !overview) {
          return false;
        }

        return isPortalModuleAvailable(module, overview);
      }),
    [overview, overviewLoaded],
  );
  const mainSidebarSlugs = ["pedidos", "caixa"];
  const operationModules = visibleModules.filter((module) => mainSidebarSlugs.includes(module.slug));
  const configurationModules = visibleModules.filter((module) => !mainSidebarSlugs.includes(module.slug));

  async function handleSignOut() {
    const confirmed = window.confirm("Tem certeza que deseja sair?");

    if (!confirmed) {
      return;
    }

    await clearSession();
  }

  async function handleAiQuickToggle() {
    if (!aiQuickStatus || isUpdatingAiStatus) {
      return;
    }

    try {
      setIsUpdatingAiStatus(true);
      const status = await updateAiAssistantQuickStatus(session.token, !aiQuickStatus.isEnabled);
      setAiQuickStatus(status);
      setAiStatusError("");
    } catch (error) {
      if (error instanceof ApiError && error.status === 401) {
        await clearSession();
        return;
      }

      setAiStatusError("Nao foi possivel alterar a IA.");
    } finally {
      setIsUpdatingAiStatus(false);
    }
  }

  async function handlePrintingQuickToggle() {
    if (!printingQuickStatus || isUpdatingPrintingStatus) {
      return;
    }

    try {
      setIsUpdatingPrintingStatus(true);
      const status = await updatePrintingSettings(session.token, {
        enableAutomaticPrinting: !printingQuickStatus.enableAutomaticPrinting,
        paperProfile: printingQuickStatus.paperProfile,
        ordersPerPage: printingQuickStatus.ordersPerPage,
      });
      setPrintingQuickStatus(status);
      setPrintingStatusError("");
    } catch (error) {
      if (error instanceof ApiError && error.status === 401) {
        await clearSession();
        return;
      }

      setPrintingStatusError("Nao foi possivel alterar a impressao.");
    } finally {
      setIsUpdatingPrintingStatus(false);
    }
  }

  function isActiveHref(href: string) {
    if (href === "/app") {
      return pathname === "/app";
    }

    return pathname === href || pathname.startsWith(`${href}/`);
  }

  function renderSidebarLink({
    href,
    title,
    eyebrow,
    onClick,
    showEyebrow = true,
  }: {
    href: string;
    title: string;
    eyebrow: string;
    onClick?: () => void;
    showEyebrow?: boolean;
  }) {
    return (
      <Link
        key={href}
        className={`owner-sidebar-link ${isActiveHref(href) ? "is-active" : ""}`}
        href={href}
        onClick={() => {
          setIsSidebarOpen(false);
          onClick?.();
        }}
      >
        <strong>{title}</strong>
        {showEyebrow ? <small>{eyebrow}</small> : null}
      </Link>
    );
  }


  return (
    <main className="page-shell app-shell">
      <section className={`owner-lobby-layout workspace-shell-layout ${isSidebarOpen ? "is-sidebar-open" : ""}`}>
        {isSidebarOpen ? (
          <button
            className="owner-lobby-sidebar-backdrop"
            type="button"
            aria-label="Fechar navegacao do sistema"
            onClick={() => setIsSidebarOpen(false)}
          />
        ) : null}

        <button
          className="owner-lobby-mobile-tab"
          type="button"
          aria-label="Abrir navegacao do sistema"
          onClick={() => setIsSidebarOpen(true)}
        >
          <span>Menu</span>
        </button>

        <aside className="owner-lobby-sidebar workspace-sidebar" aria-label="Navegacao do owner">
          <div className="owner-lobby-sidebar-head">
            <Link className="owner-sidebar-brand" href="/app" onClick={() => setIsSidebarOpen(false)}>
              {logoUrl ? (
                <img className="workspace-sidebar-logo" src={logoUrl} alt={`Logo de ${session.restaurantName}`} />
              ) : (
                <span>ZP</span>
              )}
              <div>
                <strong>ZeroPaper</strong>
                <small>{session.restaurantName}</small>
              </div>
            </Link>
            <button className="ghost-link button-link owner-sidebar-close" type="button" onClick={() => setIsSidebarOpen(false)}>
              Fechar
            </button>
          </div>

          <div className="owner-sidebar-body">
            <nav className="owner-sidebar-nav" aria-label="Navegacao do painel">
              <div className="owner-sidebar-group">
                <span>Dia a dia</span>
                {renderSidebarLink({ href: "/app", title: "Meus pedidos", eyebrow: "Lobby" })}
                {cashOrderAccessUrl ? (
                  <Link
                    className="owner-sidebar-link owner-sidebar-cash-order-link"
                    href={cashOrderAccessUrl}
                    onClick={() => setIsSidebarOpen(false)}
                  >
                    <strong>Pedido no caixa</strong>
                    <small>Abrir mesa rapida</small>
                  </Link>
                ) : null}
                {overview?.includesAiAssistantModule
                  ? renderSidebarLink({
                      href: "/app/atendimento/horarios",
                      title: "Horarios",
                      eyebrow: "Horarios",
                      showEyebrow: false,
                    })
                  : null}
                {operationModules.map((module) =>
                  renderSidebarLink({
                    href: `/app/${module.slug}`,
                    title: module.title,
                    eyebrow: module.eyebrow,
                  }),
                )}
              </div>

              {configurationModules.length > 0 ? (
                <div className="owner-sidebar-group owner-sidebar-config-group">
                  <span>Configuracoes</span>
                  {configurationModules.map((module) =>
                    renderSidebarLink({
                      href: `/app/${module.slug}`,
                      title: module.title,
                      eyebrow: module.eyebrow,
                    }),
                  )}
                </div>
              ) : null}
            </nav>
          </div>
        </aside>

        <div className="workspace-app-main">
          <header className="app-topbar workspace-content-topbar">
            <div className="workspace-current-context">
              <span className="eyebrow">ZeroPaper</span>
              <strong>{session.restaurantName}</strong>
            </div>

            <div className="topbar-actions">
              {overview?.includesAiAssistantModule ? (
                <button
                  className={`wsh-quick-toggle${aiQuickStatus?.isEnabled ? " is-on" : " is-off"}`}
                  type="button"
                  disabled={!aiQuickStatus || isUpdatingAiStatus}
                  onClick={() => void handleAiQuickToggle()}
                  title={aiQuickStatus?.isEnabled ? "Pausar IA" : "Ativar IA"}
                >
                  <span className="wsh-toggle-dot" />
                  <span className="wsh-toggle-label">IA</span>
                  <span className="wsh-toggle-state">
                    {isUpdatingAiStatus ? "..." : aiQuickStatus?.isEnabled ? "ativa" : "pausada"}
                  </span>
                </button>
              ) : null}

              {overview?.includesPrintingModule ? (
                <button
                  className={`wsh-quick-toggle${printingQuickStatus?.enableAutomaticPrinting ? " is-on" : " is-off"}`}
                  type="button"
                  disabled={!printingQuickStatus || isUpdatingPrintingStatus}
                  onClick={() => void handlePrintingQuickToggle()}
                  title={printingQuickStatus?.enableAutomaticPrinting ? "Pausar impressao" : "Ativar impressao"}
                >
                  <span className="wsh-toggle-dot" />
                  <span className="wsh-toggle-label">Impressao</span>
                  <span className="wsh-toggle-state">
                    {isUpdatingPrintingStatus ? "..." : printingQuickStatus?.enableAutomaticPrinting ? "ativa" : "pausada"}
                  </span>
                </button>
              ) : null}

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

          <div className="workspace-app-content">{children}</div>
        </div>
      </section>

      <WaiterCallMonitor showCard={showAlertCard} />
    </main>
  );
}
