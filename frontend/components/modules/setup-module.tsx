"use client";

import Link from "next/link";
import { Check } from "lucide-react";
import { useEffect, useMemo, useState } from "react";
import {
  getAiAssistantSettings,
  getCompanySettings,
  getMenu,
  getPrintingSettings,
  getTables,
  getWorkspaceOverview,
  type AiAssistantSettings,
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
  ai: AiAssistantSettings;
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

const EMPTY_PRINTING_SETTINGS: PrintingSettings = {
  enableAutomaticPrinting: false,
  paperProfile: "Thermal80mm",
  ordersPerPage: 1,
  hasAgentKey: false,
  agentOnline: false,
  agentName: null,
  printerName: null,
  lastSeenAtUtc: null,
  pendingJobs: 0,
  failedJobs: 0,
  printedJobs: 0,
  downloadUrl: "/downloads/zeropaper-print-agent-win-x86.exe",
  downloadUrlX86: "/downloads/zeropaper-print-agent-win-x86.exe",
  downloadUrlX64: "/downloads/zeropaper-print-agent-win-x64.exe",
  legacyDownloadUrl: "/downloads/zeropaper-print-agent-legacy-net48.zip",
  recentOrders: [],
};

const EMPTY_AI_SETTINGS: AiAssistantSettings = {
  unitDisplayName: "",
  apiConfigured: false,
  whatsAppServerConfigured: false,
  isEnabled: false,
  model: "",
  systemPrompt: "",
  greetingMessage: "",
  redirectMessage: "",
  fallbackMessage: "",
  orderingLink: null,
  pixReceiverName: null,
  pixKey: null,
  pixMessage: null,
  serviceStartTime: null,
  serviceEndTime: null,
  maxOutputTokens: 0,
  whatsAppEnabled: false,
  whatsAppConfigured: false,
  whatsAppInstanceId: null,
  whatsAppInstanceTokenMasked: null,
  hasWhatsAppAccountSecurityToken: false,
  isWhatsAppConnected: false,
  whatsAppConnectedPhone: null,
  whatsAppConnectedAtUtc: null,
  whatsAppDisconnectedAtUtc: null,
  whatsAppLastIncomingAtUtc: null,
  whatsAppLastOutgoingAtUtc: null,
  whatsAppWebhookReceiveUrl: null,
  whatsAppWebhookMessageStatusUrl: null,
  whatsAppWebhookConnectedUrl: null,
  whatsAppWebhookDisconnectedUrl: null,
  recentWhatsAppConversations: [],
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
  const aiReady =
    state.ai.apiConfigured &&
    state.ai.isEnabled &&
    !!state.ai.orderingLink &&
    (!state.ai.whatsAppEnabled || state.ai.whatsAppConfigured);

  const steps: SetupStep[] = [
    {
      id: "unit",
      title: "Dados da unidade",
      summary: hasCompanyContact
        ? "A unidade ja tem nome e contato para operar."
        : "Feche os dados basicos antes de colocar a unidade para funcionar.",
      details: hasCompanyContact
        ? "Revise nome, contato e informacoes principais para garantir que a equipe e o suporte encontrem a unidade com facilidade."
        : "Preencha pelo menos um contato da unidade para facilitar suporte, configuracao e comunicacao com a equipe. Esse e o ponto de partida para tudo funcionar.",
      href: "/app/ajustes",
      cta: "Revisar dados da unidade",
      complete: Boolean(state.settings.tradeName && state.settings.legalName && hasCompanyContact),
      essential: true,
      metrics: [
        { label: "Unidade", value: state.settings.tradeName || "Sem nome" },
        { label: "Contato", value: hasCompanyContact ? "Pronto" : "Pendente" },
      ],
    },
  ];

  if (state.overview.includesMenuModule) {
    steps.push({
      id: "menu",
      title: "Cardapio para venda",
      summary:
        activeMenuItems > 0
          ? "O cardapio ja esta pronto para receber pedidos."
          : "Ainda falta publicar itens para o cliente pedir.",
      details:
        activeMenuItems > 0
          ? "Mantenha pelo menos um item ativo com nome claro e valor revisado para o cliente conseguir pedir sem duvida."
          : "Cadastre categorias e publique pelo menos um produto ativo. Sem itens no cardapio o link de pedido nao tem o que mostrar.",
      href: "/app/cardapio",
      cta: activeMenuItems > 0 ? "Revisar cardapio" : "Montar cardapio",
      complete: activeMenuItems > 0 && state.menu.length > 0,
      essential: true,
      metrics: [
        { label: "Categorias", value: String(state.menu.length) },
        { label: "Produtos ativos", value: String(activeMenuItems) },
        { label: "Total", value: String(totalMenuItems) },
      ],
    });
  }

  if (state.overview.includesTablesModule) {
    steps.push({
      id: "tables",
      title: "Mesas e link de pedido",
      summary:
        state.tables.length > 0
          ? "A unidade ja tem ponto de entrada para o cliente pedir."
          : "Crie a primeira mesa para liberar o pedido pelo celular.",
      details:
        state.tables.length > 0
          ? "Cada mesa gera um link unico. O cliente abre o link, ve o cardapio e manda o pedido direto para o painel sem precisar baixar nada."
          : "A primeira mesa libera o fluxo principal da unidade. O cliente escaneia um QR Code ou abre o link e ja consegue pedir.",
      href: "/app/mesas",
      cta: state.tables.length > 0 ? "Revisar mesas" : "Criar primeira mesa",
      complete: state.tables.length > 0,
      essential: true,
      metrics: [
        { label: "Mesas", value: String(state.tables.length) },
        { label: "Lugares", value: String(totalSeats) },
      ],
    });
  }

  if (state.overview.includesPrintingModule) {
    steps.push({
      id: "printing",
      title: "Impressao automatica",
      summary:
        state.printing.enableAutomaticPrinting && state.printing.agentOnline && printerIsValid
          ? "Os pedidos ja saem direto na impressora da unidade."
          : "Ainda falta ligar a impressora certa para a equipe receber os pedidos automaticamente.",
      details:
        state.printing.enableAutomaticPrinting && state.printing.agentOnline && printerIsValid
          ? "Com a impressao ativa, o pedido aparece na cozinha ou no atendimento sem que ninguem precise clicar em nada."
          : "Instale o agente de impressao no computador da unidade, conecte na impressora fisica e ative a impressao automatica aqui. Impressora virtual de PDF nao funciona para operacao.",
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
    });
  }

  if (state.overview.includesAiAssistantModule) {
    steps.push({
      id: "ai",
      title: "Atendimento com IA",
      summary: aiReady
        ? "A IA ja pode orientar o cliente no canal oficial e no WhatsApp."
        : "Falta revisar a IA para o cliente receber orientacao no canal oficial.",
      details: aiReady
        ? "A IA responde automaticamente, conduz o cliente para o pedido online e pode atender no WhatsApp quando ligado. Revise periodicamente para manter as mensagens atualizadas."
        : "Configure as mensagens de saudacao, redirecionamento e fallback. Defina o link do pedido para a IA saber para onde enviar o cliente. Se for usar WhatsApp, configure a instancia nesta mesma tela.",
      href: "/app/atendimento",
      cta: aiReady ? "Revisar IA" : "Configurar IA",
      complete: aiReady,
      essential: true,
      metrics: [
        { label: "Conexao", value: state.ai.apiConfigured ? "Pronta" : "Pendente" },
        { label: "IA", value: state.ai.isEnabled ? "Ativa" : "Desligada" },
        { label: "Link", value: state.ai.orderingLink ? "Pronto" : "Pendente" },
        {
          label: "WhatsApp",
          value: state.ai.whatsAppEnabled
            ? state.ai.whatsAppConfigured
              ? "Pronto"
              : "Pendente"
            : "Opcional",
        },
      ],
    });
  }

  return steps;
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
        const [overview, settings] = await Promise.all([
          getWorkspaceOverview(token),
          getCompanySettings(token),
        ]);
        const [ai, printing, tables, menu] = await Promise.all([
          overview.includesAiAssistantModule
            ? getAiAssistantSettings(token)
            : Promise.resolve(EMPTY_AI_SETTINGS),
          overview.includesPrintingModule
            ? getPrintingSettings(token)
            : Promise.resolve(EMPTY_PRINTING_SETTINGS),
          overview.includesTablesModule ? getTables(token) : Promise.resolve([]),
          overview.includesMenuModule ? getMenu(token) : Promise.resolve([]),
        ]);
        if (!isMounted) return;
        setState({ overview, settings, ai, printing, tables, menu });
        setErrorMessage("");
      } catch (error) {
        if (!isMounted) return;
        await handleApiError(error, onUnauthorized, setErrorMessage, "Nao foi possivel carregar a configuracao do app.");
      } finally {
        if (isMounted) setLoading(false);
      }
    }

    void loadSetup();
    return () => { isMounted = false; };
  }, [token]);

  const steps = useMemo(() => (state ? buildSetupSteps(state) : []), [state]);
  const essentialSteps = steps.filter((s) => s.essential);
  const completedEssential = essentialSteps.filter((s) => s.complete).length;
  const progressPercent =
    essentialSteps.length === 0 ? 0 : Math.round((completedEssential / essentialSteps.length) * 100);
  const nextPendingStep =
    steps.find((s) => s.essential && !s.complete) ?? steps.find((s) => !s.complete) ?? null;
  const allDone = essentialSteps.length > 0 && completedEssential === essentialSteps.length;

  return (
    <section className="module-body-grid single">
      <div className="stp-shell">

        {/* Hero */}
        <div className="stp-hero">
          <div className="stp-hero-copy">
            <span className="eyebrow">Configurar app</span>
            <h2>Como deixar o ZeroPaper pronto</h2>
            {!loading && state ? (
              <p>
                Siga cada etapa em ordem. Cada passo libera uma parte da operacao da unidade — dados, cardapio, mesas, impressao e atendimento automatico.
              </p>
            ) : null}
          </div>

          {!loading && state ? (
            <div className="stp-progress">
              <div className="stp-progress-track" aria-hidden="true">
                <span className="stp-progress-fill" style={{ width: `${progressPercent}%` }} />
              </div>
              <div className="stp-progress-row">
                <span>{completedEssential} de {essentialSteps.length} etapas prontas</span>
                <strong className="stp-progress-pct">{progressPercent}%</strong>
              </div>
            </div>
          ) : (
            <p className="stp-loading">Carregando os dados da unidade...</p>
          )}
        </div>

        {!loading && state ? (
          <>
            {/* Tutorial steps */}
            <ol className="stp-steps" aria-label="Tutorial de configuracao">
              {steps.map((step, index) => (
                <li key={step.id} className={`stp-step ${step.complete ? "is-done" : "is-todo"}`}>
                  <div className="stp-step-num" aria-hidden="true">
                    {step.complete ? (
                      <Check size={15} strokeWidth={2.5} />
                    ) : (
                      <span>{index + 1}</span>
                    )}
                  </div>

                  <div className="stp-step-body">
                    <div className="stp-step-head">
                      <div className="stp-step-title-group">
                        <span className="stp-step-eyebrow">
                          {step.essential ? "Essencial" : "Ajuste"}
                        </span>
                        <h3>{step.title}</h3>
                      </div>
                      <span className={`stp-badge ${step.complete ? "is-done" : "is-todo"}`}>
                        {step.complete ? "Pronto" : "Pendente"}
                      </span>
                    </div>

                    <p className="stp-step-summary">{step.summary}</p>
                    <p className="stp-step-details">{step.details}</p>

                    <div className="stp-metrics">
                      {step.metrics.map((m) => (
                        <span key={m.label} className="stp-metric">
                          <b>{m.label}</b>
                          <em>{m.value}</em>
                        </span>
                      ))}
                    </div>

                    <div className="stp-step-actions">
                      <Link className="primary-link button-link" href={step.href}>
                        {step.cta}
                      </Link>
                      {step.extraAction ? (
                        <a
                          className="ghost-link button-link"
                          href={step.extraAction.href}
                          download={step.extraAction.download}
                        >
                          {step.extraAction.label}
                        </a>
                      ) : null}
                    </div>
                  </div>
                </li>
              ))}
            </ol>

            {/* Bottom banner */}
            {allDone ? (
              <div className="stp-done-banner">
                <span className="stp-done-icon" aria-hidden="true">
                  <Check size={16} strokeWidth={2.5} />
                </span>
                <div>
                  <strong>Tudo pronto!</strong>
                  <span>
                    A unidade esta configurada e pode operar. Volte aqui quando mudar cardapio, impressora, WhatsApp ou dados da loja.
                  </span>
                </div>
              </div>
            ) : nextPendingStep ? (
              <div className="stp-next-hint">
                <div className="stp-next-copy">
                  <span className="stp-step-eyebrow">Proximo passo recomendado</span>
                  <strong>{nextPendingStep.title}</strong>
                  <span>{nextPendingStep.summary}</span>
                </div>
                <Link className="primary-link button-link" href={nextPendingStep.href}>
                  {nextPendingStep.cta}
                </Link>
              </div>
            ) : null}
          </>
        ) : null}

        {errorMessage ? <p className="module-feedback error">{errorMessage}</p> : null}
      </div>
    </section>
  );
}
