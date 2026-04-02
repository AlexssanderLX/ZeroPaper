"use client";

import Link from "next/link";
import { useEffect, useMemo, useState } from "react";
import {
  getCompanySettings,
  getMenu,
  getPrintingSettings,
  getTables,
  getWorkspaceOverview,
  type CompanySettings,
  type DiningTable,
  type MenuCategory,
  type PrintingSettings,
  type WorkspaceOverview,
} from "@/lib/api";
import { handleApiError, type AsyncVoid } from "@/components/modules/module-utils";

type SetupState = {
  overview: WorkspaceOverview;
  settings: CompanySettings;
  printing: PrintingSettings;
  tables: DiningTable[];
  menu: MenuCategory[];
};

type SetupStep = {
  id: string;
  title: string;
  summary: string;
  details: string;
  href: string;
  cta: string;
  complete: boolean;
  essential?: boolean;
  metrics: Array<{ label: string; value: string }>;
  extraAction?: {
    label: string;
    href: string;
    download?: boolean;
  };
};

function isVirtualPrinter(printerName?: string | null) {
  return /pdf|xps/i.test(printerName ?? "");
}

function buildSetupSteps(state: SetupState): SetupStep[] {
  const activeMenuItems = state.menu.reduce(
    (total, category) => total + category.items.filter((item) => item.isActive).length,
    0,
  );
  const totalMenuItems = state.menu.reduce((total, category) => total + category.items.length, 0);
  const totalSeats = state.tables.reduce((total, table) => total + table.seats, 0);
  const printerIsValid = !!state.printing.printerName && !isVirtualPrinter(state.printing.printerName);
  const hasCompanyContact = !!state.settings.contactEmail || !!state.settings.contactPhone;
  const alertsEnabled = state.settings.alerts.enableOrderAlerts && state.settings.alerts.enableWaiterCallAlerts;

  return [
    {
      id: "unit",
      title: "Identidade da unidade",
      summary: hasCompanyContact ? "Dados principais preenchidos." : "Falta fechar os dados base da unidade.",
      details: hasCompanyContact
        ? "Nome, razao social e contato estao prontos para operacao e suporte."
        : "Complete email ou telefone para suporte, notificacoes e implantacao assistida.",
      href: "/app/ajustes",
      cta: "Abrir unidade",
      complete: Boolean(state.settings.tradeName && state.settings.legalName && hasCompanyContact),
      essential: true,
      metrics: [
        { label: "Unidade", value: state.settings.tradeName || "Sem nome" },
        { label: "Contato", value: hasCompanyContact ? "Pronto" : "Pendente" },
      ],
    },
    {
      id: "menu",
      title: "Cardapio inicial",
      summary: activeMenuItems > 0 ? "Clientes ja conseguem escolher pratos." : "Ainda nao ha prato publicado para pedido.",
      details:
        activeMenuItems > 0
          ? "Mantenha pelo menos um produto ativo e com imagem para o QR ficar pronto para venda."
          : "Cadastre pelo menos uma categoria e um produto ativo para a mesa publicar o cardapio.",
      href: "/app/cardapio",
      cta: activeMenuItems > 0 ? "Ajustar cardapio" : "Montar cardapio",
      complete: activeMenuItems > 0 && state.menu.length > 0,
      essential: true,
      metrics: [
        { label: "Categorias", value: String(state.menu.length) },
        { label: "Produtos ativos", value: String(activeMenuItems) },
        { label: "Total", value: String(totalMenuItems) },
      ],
    },
    {
      id: "tables",
      title: "Mesas e QR",
      summary: state.tables.length > 0 ? "As mesas ja podem receber pedidos." : "Crie pelo menos uma mesa para operar no salao.",
      details:
        state.tables.length > 0
          ? "Os links publicos e QRs da unidade ja podem ser impressos e usados na operacao."
          : "A primeira mesa deixa o fluxo pronto para o cliente abrir o cardapio pelo celular.",
      href: "/app/mesas",
      cta: state.tables.length > 0 ? "Gerenciar mesas" : "Criar primeira mesa",
      complete: state.tables.length > 0,
      essential: true,
      metrics: [
        { label: "Mesas", value: String(state.tables.length) },
        { label: "Lugares", value: String(totalSeats) },
      ],
    },
    {
      id: "printing",
      title: "Impressao automatica",
      summary:
        state.printing.enableAutomaticPrinting && state.printing.agentOnline && printerIsValid
          ? "O agente esta conectado e pronto para imprimir sozinho."
          : "Falta fechar o agente ou a impressora fisica da unidade.",
      details:
        state.printing.enableAutomaticPrinting && state.printing.agentOnline && printerIsValid
          ? "Os pedidos novos ja podem sair direto na cozinha sem clicar em imprimir."
          : "Use uma impressora fisica no Windows. Impressoras PDF/XPS nao servem para a fila automatica.",
      href: "/app/impressao",
      cta: state.printing.agentOnline ? "Revisar impressao" : "Configurar impressao",
      complete:
        state.printing.enableAutomaticPrinting &&
        state.printing.hasAgentKey &&
        state.printing.agentOnline &&
        printerIsValid,
      essential: true,
      metrics: [
        { label: "Automatica", value: state.printing.enableAutomaticPrinting ? "Ativa" : "Pausada" },
        { label: "Agente", value: state.printing.agentOnline ? "Online" : "Offline" },
        { label: "Falhas", value: String(state.printing.failedJobs) },
      ],
      extraAction: {
        label: "Baixar agente",
        href: state.printing.downloadUrl,
        download: true,
      },
    },
    {
      id: "alerts",
      title: "Alertas do salao",
      summary: alertsEnabled ? "Pedidos novos e chamado de atendente estao ativos." : "Revise o som e os alertas antes de operar.",
      details:
        alertsEnabled
          ? "A operacao interna ja recebe som para pedido novo e chamado de mesa."
          : "Ative os dois alertas para a equipe ouvir pedidos novos e chamadas do salao.",
      href: "/app/ajustes",
      cta: "Ajustar alertas",
      complete: alertsEnabled,
      metrics: [
        { label: "Pedidos", value: state.settings.alerts.enableOrderAlerts ? "Ativo" : "Off" },
        { label: "Atendente", value: state.settings.alerts.enableWaiterCallAlerts ? "Ativo" : "Off" },
        { label: "Som", value: state.settings.alerts.hasCustomSound ? "Proprio" : "Padrao" },
      ],
    },
  ];
}

export function SetupModule({ token, onUnauthorized }: { token: string; onUnauthorized: AsyncVoid }) {
  const [state, setState] = useState<SetupState | null>(null);
  const [loading, setLoading] = useState(true);
  const [errorMessage, setErrorMessage] = useState("");

  useEffect(() => {
    let isMounted = true;

    async function loadSetup() {
      setLoading(true);

      try {
        const [overview, settings, printing, tables, menu] = await Promise.all([
          getWorkspaceOverview(token),
          getCompanySettings(token),
          getPrintingSettings(token),
          getTables(token),
          getMenu(token),
        ]);

        if (!isMounted) {
          return;
        }

        setState({
          overview,
          settings,
          printing,
          tables,
          menu,
        });
        setErrorMessage("");
      } catch (error) {
        if (!isMounted) {
          return;
        }

        await handleApiError(error, onUnauthorized, setErrorMessage, "Nao foi possivel carregar a implantacao.");
      } finally {
        if (isMounted) {
          setLoading(false);
        }
      }
    }

    void loadSetup();

    return () => {
      isMounted = false;
    };
  }, [token]);

  const steps = useMemo(() => (state ? buildSetupSteps(state) : []), [state]);
  const essentialSteps = steps.filter((step) => step.essential);
  const completedEssential = essentialSteps.filter((step) => step.complete).length;
  const progressPercent = essentialSteps.length === 0 ? 0 : Math.round((completedEssential / essentialSteps.length) * 100);
  const nextPendingStep = steps.find((step) => step.essential && !step.complete) ?? steps.find((step) => !step.complete) ?? null;

  return (
    <section className="module-body-grid single">
      <section className="surface-card module-form-card setup-shell">
        <span className="eyebrow">Implantacao</span>
        <h2>Guia da unidade para entrar em operacao</h2>

        {loading || !state ? (
          <p className="loading-state">Montando a implantacao da unidade...</p>
        ) : (
          <>
            <section className="setup-hero-grid">
              <article className="surface-card setup-progress-card">
                <div className="setup-progress-head">
                  <div>
                    <span className="eyebrow">Progresso essencial</span>
                    <strong>{completedEssential} de {essentialSteps.length} etapas prontas</strong>
                  </div>
                  <strong className="setup-progress-value">{progressPercent}%</strong>
                </div>

                <div className="setup-progress-bar" aria-hidden="true">
                  <span style={{ width: `${progressPercent}%` }} />
                </div>

                <p className="setup-progress-copy">
                  {nextPendingStep
                    ? `Proximo passo recomendado: ${nextPendingStep.title.toLowerCase()}.`
                    : "A unidade ja esta pronta para operar no salao com cardapio, mesas e impressao."}
                </p>

                <div className="setup-progress-metrics">
                  <span className="mini-chip">Mesas {state.overview.activeTables}</span>
                  <span className="mini-chip">Cardapio {state.overview.publishedMenuItems}</span>
                  <span className="mini-chip">A cobrar {state.overview.pendingPayments}</span>
                  <span className="mini-chip">Impressos {state.overview.printedPrints}</span>
                </div>
              </article>

              <article className="surface-card setup-next-card">
                <span className="eyebrow">Sugestao de agora</span>
                <strong>{nextPendingStep ? nextPendingStep.title : "Revisar operacao"}</strong>
                <p>
                  {nextPendingStep
                    ? nextPendingStep.details
                    : "Use esta area para revisar os pontos principais da unidade sempre que precisar implantar um novo restaurante."}
                </p>
                <div className="toolbar-actions compact">
                  <Link className="primary-link button-link" href={nextPendingStep?.href ?? "/app"}>
                    {nextPendingStep?.cta ?? "Voltar ao painel"}
                  </Link>
                </div>
              </article>
            </section>

            <section className="setup-steps-grid">
              {steps.map((step) => (
                <article
                  key={step.id}
                  className={`surface-card setup-step-card ${step.complete ? "is-ready" : "is-pending"}`}
                >
                  <div className="setup-step-head">
                    <div>
                      <span className="eyebrow">{step.essential ? "Essencial" : "Operacao"}</span>
                      <h3>{step.title}</h3>
                    </div>
                    <span className={`setup-step-status ${step.complete ? "is-ready" : "is-pending"}`}>
                      {step.complete ? "Pronto" : "Pendente"}
                    </span>
                  </div>

                  <strong className="setup-step-summary">{step.summary}</strong>
                  <p>{step.details}</p>

                  <div className="setup-step-metrics">
                    {step.metrics.map((metric) => (
                      <article key={`${step.id}-${metric.label}`} className="setup-step-metric">
                        <small>{metric.label}</small>
                        <strong>{metric.value}</strong>
                      </article>
                    ))}
                  </div>

                  <div className="toolbar-actions compact setup-step-actions">
                    <Link className="primary-link button-link" href={step.href}>
                      {step.cta}
                    </Link>
                    {step.extraAction ? (
                      <a className="ghost-link button-link" href={step.extraAction.href} download={step.extraAction.download}>
                        {step.extraAction.label}
                      </a>
                    ) : null}
                  </div>
                </article>
              ))}
            </section>
          </>
        )}

        {errorMessage ? <p className="module-feedback error">{errorMessage}</p> : null}
      </section>
    </section>
  );
}
