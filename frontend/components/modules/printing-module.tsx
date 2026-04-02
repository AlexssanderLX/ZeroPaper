"use client";

import Link from "next/link";
import { useEffect, useMemo, useState } from "react";
import {
  getPrintingSettings,
  requeuePrintOrder,
  rotatePrintingAgentKey,
  updatePrintingSettings,
  type PrintOrderSummary,
  type PrintingSettings,
} from "@/lib/api";
import { formatCurrency, formatDateTime, handleApiError, type AsyncVoid } from "@/components/modules/module-utils";

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

function formatPaperProfile(value: string) {
  switch (value) {
    case "A4":
      return "Impressora comum A4";
    case "Thermal80mm":
      return "Termica 80mm";
    default:
      return value;
  }
}

function formatOrdersPerPage(value: number) {
  return value <= 1 ? "1 pedido por folha" : `${value} pedidos por folha`;
}

function buildPrintingWarnings(printing: PrintingSettings) {
  const warnings: Array<{ id: string; title: string; copy: string }> = [];

  if (!printing.hasAgentKey) {
    warnings.push({
      id: "key",
      title: "Gere a chave da unidade",
      copy: "A chave vincula o app Windows a esta unidade e impede impressao cruzada entre restaurantes.",
    });
  }

  if (printing.enableAutomaticPrinting && !printing.agentOnline) {
    warnings.push({
      id: "agent",
      title: "Agente offline",
      copy: "Abra o app Windows na unidade para o backend entregar os pedidos automaticamente para a impressora.",
    });
  }

  if (isVirtualPrinter(printing.printerName)) {
    warnings.push({
      id: "printer",
      title: "Impressora virtual detectada",
      copy: "Microsoft Print to PDF ou XPS pedem para salvar arquivo e nao servem para impressao automatica da cozinha.",
    });
  }

  if (printing.failedJobs > 0) {
    warnings.push({
      id: "failed",
      title: "Existem falhas recentes",
      copy: "O sistema ja tentou imprimir novamente uma vez. Se ainda falhou, revise o erro do pedido e a impressora antes de reenviar.",
    });
  }

  if (printing.paperProfile === "A4" && printing.ordersPerPage > 1) {
    warnings.push({
      id: "a4-batch",
      title: "A4 com agrupamento rapido",
      copy: "No modo A4, o agente segura os pedidos por um intervalo curtissimo para tentar completar a folha sem atrasar a cozinha.",
    });
  }

  return warnings;
}

function sortRecentOrders(orders: PrintOrderSummary[]) {
  return [...orders].sort(
    (leftOrder, rightOrder) => new Date(rightOrder.submittedAtUtc).getTime() - new Date(leftOrder.submittedAtUtc).getTime(),
  );
}

export function PrintingModule({ token, onUnauthorized }: { token: string; onUnauthorized: AsyncVoid }) {
  const [printing, setPrinting] = useState<PrintingSettings | null>(null);
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);
  const [isToggling, setIsToggling] = useState(false);
  const [isSavingProfile, setIsSavingProfile] = useState(false);
  const [isRotatingKey, setIsRotatingKey] = useState(false);
  const [pendingOrderId, setPendingOrderId] = useState<string | null>(null);
  const [generatedKey, setGeneratedKey] = useState("");
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

    async function runInitialLoad() {
      if (!isMounted) {
        return;
      }

      await loadPrinting();
    }

    void runInitialLoad();

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

  async function handleUpdateProfile(nextPaperProfile: string, nextOrdersPerPage: number) {
    if (!printing) {
      return;
    }

    try {
      setIsSavingProfile(true);
      const response = await updatePrintingSettings(token, {
        enableAutomaticPrinting: printing.enableAutomaticPrinting,
        paperProfile: nextPaperProfile,
        ordersPerPage: nextOrdersPerPage,
      });

      setPrinting(response);
      setSuccessMessage("Perfil de impressao atualizado.");
      setErrorMessage("");
    } catch (error) {
      await handleApiError(error, onUnauthorized, setErrorMessage, "Nao foi possivel salvar o perfil de impressao.");
    } finally {
      setIsSavingProfile(false);
    }
  }

  async function handleRotateAgentKey() {
    try {
      setIsRotatingKey(true);
      const response = await rotatePrintingAgentKey(token);
      setPrinting(response.printing);
      setGeneratedKey(response.agentKey);
      setSuccessMessage("Nova chave criada. Cole essa chave no app Windows da unidade.");
      setErrorMessage("");
    } catch (error) {
      await handleApiError(error, onUnauthorized, setErrorMessage, "Nao foi possivel gerar a chave do agente.");
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
      setSuccessMessage("Chave copiada.");
      setErrorMessage("");
    } catch {
      setErrorMessage("Nao foi possivel copiar a chave agora.");
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

  const warnings = useMemo(() => (printing ? buildPrintingWarnings(printing) : []), [printing]);
  const recentOrders = useMemo(() => (printing ? sortRecentOrders(printing.recentOrders) : []), [printing]);
  const usesVirtualPrinter = isVirtualPrinter(printing?.printerName);
  const automaticReady = Boolean(
    printing &&
      printing.enableAutomaticPrinting &&
      printing.hasAgentKey &&
      printing.agentOnline &&
      printing.printerName &&
      !usesVirtualPrinter,
  );

  return (
    <section className="module-body-grid single">
      <section className="surface-card module-form-card print-module-shell">
        <span className="eyebrow">Impressao</span>
        <h2>Operacao automatica da cozinha</h2>

        {loading || !printing ? (
          <p className="loading-state">Carregando impressao...</p>
        ) : (
          <>
            <section className="print-module-grid">
              <article className="surface-card print-summary-card print-summary-card-primary">
                <div className="module-section-head compact-order-column-head">
                  <div className="kitchen-column-copy">
                    <span className="eyebrow">Estado atual</span>
                    <strong>{automaticReady ? "Pronto para imprimir sozinho" : "Ainda precisa de configuracao"}</strong>
                  </div>
                  <span className={`status-chip ${automaticReady ? "ready" : "pending"}`}>
                    {automaticReady ? "Operando" : "Ajustar"}
                  </span>
                </div>

                <div className="print-summary-stack">
                  <div className="print-summary-line">
                    <strong>Agente</strong>
                    <span>{printing.agentOnline ? printing.agentName || "Online" : "Offline"}</span>
                  </div>
                  <div className="print-summary-line">
                    <strong>Impressora</strong>
                    <span>{printing.printerName || "Escolha feita no Windows"}</span>
                  </div>
                  <div className="print-summary-line">
                    <strong>Perfil</strong>
                    <span>{formatPaperProfile(printing.paperProfile)}</span>
                  </div>
                  <div className="print-summary-line">
                    <strong>Folha</strong>
                    <span>{formatOrdersPerPage(printing.ordersPerPage)}</span>
                  </div>
                  <div className="print-summary-line">
                    <strong>Ultimo contato</strong>
                    <span>{printing.lastSeenAtUtc ? formatDateTime(printing.lastSeenAtUtc) : "Sem conexao ainda"}</span>
                  </div>
                </div>

                <div className="print-metric-grid">
                  <article className="print-metric-card">
                    <small>Automatico</small>
                    <strong>{printing.enableAutomaticPrinting ? "Ativo" : "Pausado"}</strong>
                  </article>
                  <article className="print-metric-card">
                    <small>Aguardando</small>
                    <strong>{printing.pendingJobs}</strong>
                  </article>
                  <article className="print-metric-card">
                    <small>Falhas</small>
                    <strong>{printing.failedJobs}</strong>
                  </article>
                  <article className="print-metric-card">
                    <small>Impressos</small>
                    <strong>{printing.printedJobs}</strong>
                  </article>
                </div>
              </article>

              <article className="surface-card print-summary-card">
                <div className="module-section-head compact-order-column-head">
                  <div className="kitchen-column-copy">
                    <span className="eyebrow">Instalacao do agente</span>
                    <strong>Windows da unidade</strong>
                  </div>
                  {refreshing ? <span className="mini-chip">Atualizando</span> : null}
                </div>

                <label className="settings-alert-toggle print-toggle">
                  <input
                    type="checkbox"
                    checked={printing.enableAutomaticPrinting}
                    disabled={isToggling}
                    onChange={(event) => void handleToggleAutomaticPrinting(event.target.checked)}
                  />
                  <div>
                    <strong>Imprimir pedidos novos automaticamente</strong>
                    <p>O backend fila o pedido e o agente da unidade tenta imprimir sem acao manual da equipe.</p>
                  </div>
                </label>

                <section className="print-profile-card">
                  <div className="module-section-head compact-order-column-head">
                    <div className="kitchen-column-copy">
                      <span className="eyebrow">Perfil da impressora</span>
                      <strong>Escolha o tipo de folha da unidade</strong>
                    </div>
                    {isSavingProfile ? <span className="mini-chip">Salvando</span> : null}
                  </div>

                  <div className="print-option-grid">
                    <button
                      className={`print-option-button ${printing.paperProfile === "Thermal80mm" ? "active" : ""}`}
                      type="button"
                      disabled={isSavingProfile}
                      onClick={() => void handleUpdateProfile("Thermal80mm", 1)}
                    >
                      <strong>Termica 80mm</strong>
                      <span>Pedido por pedido, sem agrupamento e sem risco de salvar arquivo.</span>
                    </button>

                    <button
                      className={`print-option-button ${printing.paperProfile === "A4" ? "active" : ""}`}
                      type="button"
                      disabled={isSavingProfile}
                      onClick={() =>
                        void handleUpdateProfile(
                          "A4",
                          printing.paperProfile === "A4" ? printing.ordersPerPage : 1,
                        )
                      }
                    >
                      <strong>Impressora comum A4</strong>
                      <span>Permite imprimir 1, 2 ou 4 pedidos por folha mantendo a fila automatica.</span>
                    </button>
                  </div>

                  <div className="print-sheet-grid">
                    {[1, 2, 4].map((value) => {
                      const disabled = printing.paperProfile !== "A4" && value !== 1;
                      const active = printing.ordersPerPage === value;

                      return (
                        <button
                          key={value}
                          className={`print-sheet-button ${active ? "active" : ""}`}
                          type="button"
                          disabled={isSavingProfile || disabled}
                          onClick={() =>
                            void handleUpdateProfile(printing.paperProfile === "A4" ? "A4" : "Thermal80mm", value)
                          }
                        >
                          <strong>{value}x</strong>
                          <span>{formatOrdersPerPage(value)}</span>
                        </button>
                      );
                    })}
                  </div>

                  <p className="print-agent-hint">
                    No modo termico, o sistema trava em 1 pedido por folha para evitar configuracao invalida. No A4 voce pode alternar entre 1, 2 ou 4 sem quebrar a automacao.
                  </p>
                </section>

                <p className="print-agent-hint">
                  Baixe o app, gere a chave da unidade e selecione uma impressora fisica. Depois disso o pedido novo vai direto para a cozinha.
                </p>

                <div className="toolbar-actions compact print-module-actions">
                  <a className="primary-link button-link" href={printing.downloadUrl}>
                    Baixar agente
                  </a>
                  <button className="ghost-link button-link" type="button" disabled={isRotatingKey} onClick={() => void handleRotateAgentKey()}>
                    {isRotatingKey ? "Gerando..." : printing.hasAgentKey ? "Gerar nova chave" : "Gerar chave"}
                  </button>
                  <Link className="ghost-link button-link" href="/app/implantacao">
                    Abrir implantacao
                  </Link>
                </div>
              </article>
            </section>

            {generatedKey ? (
              <article className="surface-card print-key-card">
                <div className="module-section-head compact-order-column-head">
                  <div className="kitchen-column-copy">
                    <span className="eyebrow">Chave segura da unidade</span>
                    <strong>Cole no app Windows</strong>
                  </div>
                </div>

                <div className="print-key-row">
                  <input className="print-key-input" readOnly value={generatedKey} />
                  <button className="ghost-link button-link" type="button" onClick={() => void handleCopyAgentKey()}>
                    Copiar chave
                  </button>
                </div>
              </article>
            ) : null}

            {warnings.length > 0 ? (
              <section className="print-warning-grid">
                {warnings.map((warning) => (
                  <article key={warning.id} className="surface-card print-warning-card">
                    <span className="eyebrow">Atencao</span>
                    <strong>{warning.title}</strong>
                    <p>{warning.copy}</p>
                  </article>
                ))}
              </section>
            ) : null}

            <article className="surface-card print-jobs-card">
              <div className="module-section-head compact-order-column-head">
                <div className="kitchen-column-copy">
                  <span className="eyebrow">Pedidos recentes</span>
                  <strong>Impressao quase invisivel na operacao</strong>
                </div>
              </div>

              {recentOrders.length === 0 ? (
                <div className="module-empty-state compact-empty-state">
                  <strong>Sem pedidos para mostrar</strong>
                  <p>Quando um pedido novo entrar, ele passa aqui e o agente tenta imprimir sozinho.</p>
                </div>
              ) : (
                <div className="module-card-list print-job-list">
                  {recentOrders.map((order) => (
                    <article key={order.id} className="module-entity-card print-job-card">
                      <div className="entity-head">
                        <div>
                          <h3>Pedido #{order.number}</h3>
                          <p>{order.tableName}</p>
                        </div>
                        <span className={`status-chip status-${order.printStatus.toLowerCase()}`}>{formatPrintStatus(order.printStatus)}</span>
                      </div>

                      <div className="entity-meta-grid">
                        <span>{formatKitchenStatus(order.status)}</span>
                        <span>{formatDateTime(order.submittedAtUtc)}</span>
                        <span>{formatCurrency(order.totalAmount)}</span>
                        <span>{order.printAttempts} tentativa(s)</span>
                      </div>

                      {order.printedAtUtc ? <p className="print-job-note">Impresso em {formatDateTime(order.printedAtUtc)}.</p> : null}
                      {!order.printedAtUtc && !order.printLastError ? <p className="print-job-note">O agente ainda esta tentando ou aguardando coleta.</p> : null}
                      {order.printLastError ? <p className="module-feedback error">{order.printLastError}</p> : null}

                      <div className="toolbar-actions compact print-module-actions">
                        <button
                          className="ghost-link button-link module-action-button"
                          type="button"
                          disabled={pendingOrderId === order.id}
                          onClick={() => void handleRequeue(order.id)}
                        >
                          {pendingOrderId === order.id ? "Reenviando..." : order.printStatus === "Printed" ? "Reimprimir" : "Imprimir agora"}
                        </button>
                      </div>
                    </article>
                  ))}
                </div>
              )}
            </article>
          </>
        )}

        {successMessage ? <p className="module-feedback success">{successMessage}</p> : null}
        {errorMessage ? <p className="module-feedback error">{errorMessage}</p> : null}
      </section>
    </section>
  );
}
