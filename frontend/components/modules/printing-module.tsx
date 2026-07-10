"use client";

import { useEffect, useMemo, useState } from "react";
import {
  createPrintingTestJob,
  getPrintingSettings,
  getWorkspaceOverview,
  requeuePrintOrder,
  rotatePrintingAgentKey,
  updatePrintingSettings,
  type PrintOrderSummary,
  type PrintingSettings,
} from "@/lib/api";
import { formatCurrency, formatDateTime, handleApiError, type AsyncVoid } from "@/components/modules/module-utils";

function renderPrintOrderTitle(order: PrintOrderSummary) {
  if (!order.isDeliveryOrder) {
    return `Pedido #${order.number}`;
  }

  return order.customerName ? `Delivery de ${order.customerName}` : "Delivery";
}

function renderPrintOrderSubtitle(order: PrintOrderSummary) {
  if (order.isDeliveryOrder) {
    return "Entrega";
  }

  return order.tableName;
}

function formatPrintStatus(value: string) {
  switch (value) {
    case "Pending":
      return "Aguardando";
    case "Processing":
      return "Em andamento";
    case "Printed":
      return "Impresso";
    case "Failed":
      return "Falhou";
    case "Disabled":
      return "Pausado";
    default:
      return value;
  }
}

function formatKitchenStatus(value: string) {
  switch (value) {
    case "Pending":
      return "Novo";
    case "InKitchen":
      return "Em preparo";
    case "Ready":
      return "Pronto";
    case "Delivered":
      return "Saiu da cozinha";
    case "Cancelled":
      return "Cancelado";
    default:
      return value;
  }
}

function isVirtualPrinter(printerName?: string | null) {
  return /pdf|xps/i.test(printerName ?? "");
}

function getPrinterState(printing: PrintingSettings, usesVirtualPrinter: boolean) {
  if (!printing.printerName) {
    return { label: "Sem impressora", detail: "Configure no app Windows", tone: "danger" };
  }

  if (usesVirtualPrinter) {
    return { label: "Modo previa", detail: printing.printerName, tone: "warning" };
  }

  return { label: "Impressora pronta", detail: printing.printerName, tone: "good" };
}

function sortRecentOrders(orders: PrintOrderSummary[]) {
  return [...orders].sort(
    (leftOrder, rightOrder) => new Date(rightOrder.submittedAtUtc).getTime() - new Date(leftOrder.submittedAtUtc).getTime(),
  );
}

export function PrintingModule({ token, onUnauthorized }: { token: string; onUnauthorized: AsyncVoid }) {
  const [printing, setPrinting] = useState<PrintingSettings | null>(null);
  const [hasAutoPrint, setHasAutoPrint] = useState<boolean | null>(null);
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [isToggling, setIsToggling] = useState(false);
  const [isRotatingKey, setIsRotatingKey] = useState(false);
  const [pendingOrderId, setPendingOrderId] = useState<string | null>(null);
  const [generatedKey, setGeneratedKey] = useState("");
  const [isSendingTest, setIsSendingTest] = useState(false);
  const [successMessage, setSuccessMessage] = useState("");
  const [errorMessage, setErrorMessage] = useState("");

  async function loadPrinting(silent = false) {
    if (!silent) {
      setLoading(true);
    } else {
      setRefreshing(true);
    }

    try {
      const response = await getPrintingSettings(token);
      setPrinting(response);
      setErrorMessage("");
    } catch (error) {
      if (!silent) {
        await handleApiError(error, onUnauthorized, setErrorMessage, "Nao foi possivel carregar a impressao.");
      }
    } finally {
      if (!silent) {
        setLoading(false);
      } else {
        setRefreshing(false);
      }
    }
  }

  useEffect(() => {
    let isMounted = true;

    void (async () => {
      if (!isMounted) return;
      const [, overview] = await Promise.allSettled([
        loadPrinting(),
        getWorkspaceOverview(token),
      ]);
      if (isMounted && overview.status === "fulfilled") {
        setHasAutoPrint(overview.value.hasAutoPrint ?? true);
      } else if (isMounted) {
        setHasAutoPrint(true);
      }
    })();

    const intervalId = window.setInterval(() => {
      if (isMounted) {
        void loadPrinting(true);
      }
    }, 5000);

    return () => {
      isMounted = false;
      window.clearInterval(intervalId);
    };
  }, [token]);

  async function handleToggleAutomaticPrinting(nextValue: boolean) {
    if (!printing) {
      return;
    }

    try {
      setIsToggling(true);
      const response = await updatePrintingSettings(token, {
        enableAutomaticPrinting: nextValue,
        paperProfile: printing.paperProfile,
        ordersPerPage: printing.ordersPerPage,
      });

      setPrinting(response);
      setSuccessMessage(nextValue ? "Impressao automatica ativada." : "Impressao automatica pausada.");
      setErrorMessage("");
    } catch (error) {
      await handleApiError(error, onUnauthorized, setErrorMessage, "Nao foi possivel atualizar a automacao.");
    } finally {
      setIsToggling(false);
    }
  }

  async function handleRotateAgentKey() {
    try {
      setIsRotatingKey(true);
      const response = await rotatePrintingAgentKey(token);
      setPrinting(response.printing);
      setGeneratedKey(response.agentToken || response.agentKey);
      setSuccessMessage("Codigo criado. Cole no app Windows da unidade.");
      setErrorMessage("");
    } catch (error) {
      await handleApiError(error, onUnauthorized, setErrorMessage, "Nao foi possivel gerar o codigo do agente.");
    } finally {
      setIsRotatingKey(false);
    }
  }

  async function handleCopyAgentKey() {
    if (!generatedKey) {
      return;
    }

    try {
      await navigator.clipboard.writeText(generatedKey);
      setSuccessMessage("Codigo copiado.");
      setErrorMessage("");
    } catch {
      setErrorMessage("Nao foi possivel copiar o codigo agora.");
    }
  }

  async function handleSendTest() {
    try {
      setIsSendingTest(true);
      const response = await createPrintingTestJob(token, "Teste de impressao pelo painel");
      setPrinting(response.printing);
      setSuccessMessage("Teste enviado para a fila. O agente imprime ou salva a previa, conforme o modo configurado.");
      setErrorMessage("");
    } catch (error) {
      await handleApiError(error, onUnauthorized, setErrorMessage, "Nao foi possivel enviar o teste de impressao.");
    } finally {
      setIsSendingTest(false);
    }
  }

  async function handleRequeue(orderId: string) {
    try {
      setPendingOrderId(orderId);
      await requeuePrintOrder(token, orderId);
      await loadPrinting(true);
      setSuccessMessage("Pedido reenviado para impressao.");
      setErrorMessage("");
    } catch (error) {
      await handleApiError(error, onUnauthorized, setErrorMessage, "Nao foi possivel reenviar esse pedido.");
    } finally {
      setPendingOrderId(null);
    }
  }

  const recentOrders = useMemo(() => (printing ? sortRecentOrders(printing.recentOrders) : []), [printing]);
  const usesVirtualPrinter = isVirtualPrinter(printing?.printerName);
  const printerState = printing ? getPrinterState(printing, usesVirtualPrinter) : null;
  const automaticEnabled = Boolean(printing?.autoPrintEnabled ?? printing?.enableAutomaticPrinting);
  const hasAgentToken = Boolean(printing?.hasAgentToken ?? printing?.hasAgentKey);
  const automaticReady = Boolean(
    printing &&
      automaticEnabled &&
      hasAgentToken &&
      printing.agentOnline &&
      printing.printerName &&
      !usesVirtualPrinter,
  );
  const automationTone = automaticReady ? "good" : automaticEnabled ? "warning" : "muted";
  const codeDone = hasAgentToken;
  const connectDone = Boolean(printing?.agentOnline && printing?.printerName && !usesVirtualPrinter);
  const recommendedAgentUrl = printing?.downloadUrlX86 || printing?.downloadUrl || "";
  const windows64AgentUrl = printing?.downloadUrlX64 || printing?.downloadUrl || "";
  const legacyAgentUrl = printing?.legacyDownloadUrl || "";

  return (
    <section className="module-body-grid single">
      <section className="surface-card zpprint-shell">
        <div className="zpprint-head">
          <div className="zpprint-head-copy">
            <span className="eyebrow">Impressao</span>
            <h2>{hasAutoPrint === false ? "Impressao manual" : "Impressao automatica"}</h2>
          </div>
          {printing && hasAutoPrint !== false ? (
            <span className={`zpprint-chip ${automaticReady ? "is-ready" : "is-pending"}`}>
              {automaticReady ? "Pronta" : "Configurar"}
            </span>
          ) : null}
        </div>

        {loading || !printing || hasAutoPrint === null ? (
          <p className="loading-state">Carregando impressao...</p>
        ) : hasAutoPrint === false ? (
          <div className="zpprint-manual-notice">
            <p>
              Seu plano inclui <strong>impressao manual</strong>. Para imprimir um pedido, abra o modulo
              de Cozinha, selecione o pedido e use o botao de imprimir.
            </p>
            <p className="zpprint-manual-hint">
              Impressao automatica via agente Windows esta disponivel no plano Operacao e Gestao.
            </p>
          </div>
        ) : (
          <>
            {/* ── Estado atual (cada info uma vez) ───────────────── */}
            <div className="zpprint-status-row">
              <article className={`zpprint-stat is-${automationTone}`}>
                <span className="zpprint-stat-label">Automacao</span>
                <strong>{automaticEnabled ? "Ativa" : "Pausada"}</strong>
                <label className="zpprint-switch">
                  <input
                    type="checkbox"
                    checked={automaticEnabled}
                    disabled={isToggling}
                    onChange={(event) => void handleToggleAutomaticPrinting(event.target.checked)}
                  />
                  <span className="zpprint-switch-track" aria-hidden="true">
                    <span className="zpprint-switch-thumb" />
                  </span>
                  <em>{automaticEnabled ? "Pedidos novos entram na fila" : "Pausada para novos pedidos"}</em>
                </label>
              </article>

              {printerState ? (
                <article className={`zpprint-stat is-${printerState.tone}`}>
                  <span className="zpprint-stat-label">Impressora</span>
                  <strong>{printerState.label}</strong>
                  <small>{printerState.detail}</small>
                </article>
              ) : null}

              <article className="zpprint-stat is-queue">
                <span className="zpprint-stat-label">Fila de impressao</span>
                <div className="zpprint-queue">
                  <div>
                    <strong>{printing.pendingJobs}</strong>
                    <small>aguardando</small>
                  </div>
                  <div>
                    <strong className={printing.failedJobs > 0 ? "is-danger" : ""}>{printing.failedJobs}</strong>
                    <small>falhas</small>
                  </div>
                  <div>
                    <strong className="is-good">{printing.printedJobs}</strong>
                    <small>impressos</small>
                  </div>
                </div>
              </article>
            </div>

            {/* ── Passos para ligar ──────────────────────────────── */}
            <div className="zpprint-steps">
              <div className="zpprint-steps-head">
                <span className="eyebrow">Agente Windows</span>
                <strong>Configure, conecte e teste sem depender da impressora fisica.</strong>
              </div>

              <ol className="zpprint-step-list">
                <li className="zpprint-step">
                  <span className="zpprint-step-num">1</span>
                  <div className="zpprint-step-body">
                    <strong>Baixar o agente</strong>
                    <p>Instale o app no computador que fica perto da impressora.</p>
                    <div className="zpprint-step-actions">
                      <a className="zpprint-btn is-primary" href={recommendedAgentUrl} download>
                        Baixar app
                      </a>
                      <a className="zpprint-btn is-ghost" href={windows64AgentUrl} download>
                        Windows 64 bits
                      </a>
                      {legacyAgentUrl ? (
                        <a className="zpprint-btn is-ghost" href={legacyAgentUrl} download>
                          Windows antigo
                        </a>
                      ) : null}
                    </div>
                  </div>
                </li>

                <li className={`zpprint-step ${codeDone ? "is-done" : ""}`}>
                  <span className="zpprint-step-num">{codeDone ? "OK" : "2"}</span>
                  <div className="zpprint-step-body">
                    <strong>Gerar codigo</strong>
                    <p>Copie este codigo uma vez e cole no app Windows.</p>
                    <div className="zpprint-step-actions">
                      <button
                        className="zpprint-btn is-ghost"
                        type="button"
                        disabled={isRotatingKey}
                        onClick={() => void handleRotateAgentKey()}
                      >
                        {isRotatingKey ? "Gerando..." : codeDone ? "Gerar novo codigo" : "Gerar codigo"}
                      </button>
                    </div>
                    {generatedKey ? (
                      <div className="zpprint-code-row">
                        <input className="zpprint-code-input" readOnly value={generatedKey} />
                        <button className="zpprint-btn is-ghost" type="button" onClick={() => void handleCopyAgentKey()}>
                          Copiar
                        </button>
                      </div>
                    ) : null}
                  </div>
                </li>

                <li className={`zpprint-step ${connectDone ? "is-done" : ""}`}>
                  <span className="zpprint-step-num">{connectDone ? "OK" : "3"}</span>
                  <div className="zpprint-step-body">
                    <strong>Conectar o agente</strong>
                    <p>Escolha impressora real ou modo de previa em arquivo.</p>
                    <span className={`zpprint-pill ${printing.agentOnline ? "is-good" : "is-danger"}`}>
                      {printing.agentOnline ? "Agente online" : "Agente offline"}
                    </span>
                    {printing.agentName || printing.appVersion ? (
                      <small className="zpprint-step-hint">
                        {[printing.agentName, printing.appVersion ? `v${printing.appVersion}` : ""].filter(Boolean).join(" - ")}
                      </small>
                    ) : null}
                    {printing.lastSeenAtUtc ? (
                      <small className="zpprint-step-hint">Ultimo contato: {formatDateTime(printing.lastSeenAtUtc)}</small>
                    ) : null}
                  </div>
                </li>

                <li className="zpprint-step">
                  <span className="zpprint-step-num">4</span>
                  <div className="zpprint-step-body">
                    <strong>Testar a impressao</strong>
                    <p>
                      Envia um cupom de teste. Sem impressora, use &quot;Salvar previa em arquivo&quot; no app.
                    </p>
                    <div className="zpprint-step-actions">
                      <button
                        className="zpprint-btn is-primary"
                        type="button"
                        disabled={isSendingTest || !hasAgentToken}
                        onClick={() => void handleSendTest()}
                      >
                        {isSendingTest ? "Enviando..." : "Enviar teste"}
                      </button>
                    </div>
                    {!hasAgentToken ? (
                      <small className="zpprint-step-hint">Gere o codigo no passo 2 antes de testar.</small>
                    ) : null}
                  </div>
                </li>
              </ol>
            </div>

            {/* ── Erro do agente ─────────────────────────────────── */}
            {printing.lastError ? (
              <div className="zpprint-alert">
                <strong>Ultimo erro do agente</strong>
                <p>
                  {printing.lastError}
                  {printing.lastErrorAtUtc ? ` (${formatDateTime(printing.lastErrorAtUtc)})` : ""}
                </p>
              </div>
            ) : null}

            {/* ── Pedidos recentes ───────────────────────────────── */}
            <div className="zpprint-jobs">
              <div className="zpprint-jobs-head">
                <span className="eyebrow">Pedidos recentes</span>
                {refreshing ? <span className="zpprint-mini">Atualizando</span> : null}
              </div>

              {recentOrders.length === 0 ? (
                <div className="zpprint-empty">
                  <strong>Sem pedidos por enquanto</strong>
                  <p>Quando um pedido novo entrar, ele aparece aqui e o agente tenta imprimir sozinho.</p>
                </div>
              ) : (
                <div className="zpprint-job-list">
                  {recentOrders.map((order) => (
                    <article key={order.id} className={`zpprint-job is-${order.printStatus.toLowerCase()}`}>
                      <div className="zpprint-job-head">
                        <div>
                          <strong>{renderPrintOrderTitle(order)}</strong>
                          <p>{renderPrintOrderSubtitle(order)}</p>
                        </div>
                        <span className={`zpprint-pill is-${order.printStatus.toLowerCase()}`}>
                          {formatPrintStatus(order.printStatus)}
                        </span>
                      </div>

                      <div className="zpprint-job-meta">
                        <span>{formatKitchenStatus(order.status)}</span>
                        <span>{formatDateTime(order.submittedAtUtc)}</span>
                        <span>{formatCurrency(order.totalAmount)}</span>
                        <span>{order.printAttempts} tentativa(s)</span>
                      </div>

                      {order.printLastError ? <p className="zpprint-job-error">{order.printLastError}</p> : null}

                      <button
                        className="zpprint-btn is-ghost zpprint-job-action"
                        type="button"
                        disabled={pendingOrderId === order.id}
                        onClick={() => void handleRequeue(order.id)}
                      >
                        {pendingOrderId === order.id
                          ? "Reenviando..."
                          : order.printStatus === "Printed"
                            ? "Reimprimir"
                            : "Imprimir agora"}
                      </button>
                    </article>
                  ))}
                </div>
              )}
            </div>
          </>
        )}

        {successMessage ? <p className="module-feedback success">{successMessage}</p> : null}
        {errorMessage ? <p className="module-feedback error">{errorMessage}</p> : null}
      </section>
    </section>
  );
}
