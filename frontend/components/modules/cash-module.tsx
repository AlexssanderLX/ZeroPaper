"use client";

import { useEffect, useMemo, useState } from "react";
import {
  deleteAllPaidOrders,
  deleteOrder,
  deletePaidOrder,
  deleteTodayOrderFlow,
  downloadDailyCashReportPdf,
  getOrders,
  updateOrderPayment,
  type CustomerOrder,
} from "@/lib/api";
import {
  formatCurrency,
  formatDateTime,
  formatPaymentMethod,
  formatPaymentStatus,
  handleApiError,
  type AsyncVoid,
} from "@/components/modules/module-utils";
import { OrderItemsCompact } from "@/components/modules/order-items-compact";

type CashColumn = {
  key: string;
  title: string;
  items: CustomerOrder[];
};

type PaymentSummary = {
  value: string;
  label: string;
  count: number;
  total: number;
};

const paymentOptions = [
  { value: "Pix", label: "Pix" },
  { value: "Credit", label: "Credito" },
  { value: "Debit", label: "Debito" },
  { value: "Cash", label: "Dinheiro" },
];

function sumOrders(orders: CustomerOrder[]) {
  return orders.reduce((total, order) => total + order.totalAmount, 0);
}

function sortOrdersByOldestFirst(orders: CustomerOrder[]) {
  return [...orders].sort((leftOrder, rightOrder) => {
    const submittedDifference =
      new Date(leftOrder.submittedAtUtc).getTime() - new Date(rightOrder.submittedAtUtc).getTime();

    if (submittedDifference !== 0) {
      return submittedDifference;
    }

    return leftOrder.number - rightOrder.number;
  });
}

function buildPaymentSummaries(orders: CustomerOrder[]): PaymentSummary[] {
  const paidOrders = orders.filter((order) => order.paymentStatus === "Paid" && order.status !== "Cancelled");

  return paymentOptions.map((option) => {
    const optionOrders = paidOrders.filter((order) => order.paymentMethod === option.value);

    return {
      value: option.value,
      label: option.label,
      count: optionOrders.length,
      total: sumOrders(optionOrders),
    };
  });
}

function getPaymentChangeCopy(order: CustomerOrder) {
  if (
    order.requestedPaymentMethod &&
    order.requestedPaymentMethod !== "Undefined" &&
    order.requestedPaymentMethod !== order.paymentMethod
  ) {
    return `Alterado de ${formatPaymentMethod(order.requestedPaymentMethod)} para ${formatPaymentMethod(order.paymentMethod)} no caixa.`;
  }

  return null;
}

function getOrderFlowLabel(order: CustomerOrder) {
  if (order.status === "Delivered") {
    return "Concluido";
  }

  if (order.status === "Ready") {
    return "Pronto";
  }

  if (order.status === "InKitchen") {
    return "Em preparo";
  }

  return "Novo";
}

function getOrderPrintCheckLabel(order: CustomerOrder) {
  return order.printStatus === "Printed" ? "Impresso" : "Ainda nao impresso";
}

function getOrderPrintCheckCopy(order: CustomerOrder) {
  if (order.printStatus === "Printed") {
    return order.printedAtUtc ? `Impresso em ${formatDateTime(order.printedAtUtc)}.` : "Pedido marcado como impresso.";
  }

  return "Esse pedido ainda nao foi impresso.";
}

function buildCashColumns(orders: CustomerOrder[]): CashColumn[] {
  const activeOrders = sortOrdersByOldestFirst(orders.filter((order) => order.status !== "Cancelled"));

  return [
    {
      key: "pending",
      title: "A cobrar",
      items: activeOrders.filter((order) => order.paymentStatus !== "Paid"),
    },
    {
      key: "paid",
      title: "Pagos",
      items: activeOrders.filter((order) => order.paymentStatus === "Paid"),
    },
  ];
}

export function CashModule({ token, onUnauthorized }: { token: string; onUnauthorized: AsyncVoid }) {
  const [orders, setOrders] = useState<CustomerOrder[]>([]);
  const [selectedPaymentMethods, setSelectedPaymentMethods] = useState<Record<string, string>>({});
  const [loading, setLoading] = useState(true);
  const [processingOrderId, setProcessingOrderId] = useState("");
  const [removingPaidBatch, setRemovingPaidBatch] = useState(false);
  const [removingTodayFlow, setRemovingTodayFlow] = useState(false);
  const [exportingDailyReport, setExportingDailyReport] = useState(false);
  const [errorMessage, setErrorMessage] = useState("");
  const [successMessage, setSuccessMessage] = useState("");

  async function loadOrders() {
    setLoading(true);

    try {
      const response = await getOrders(token);
      setOrders(response);
      setSelectedPaymentMethods(
        response.reduce<Record<string, string>>((result, order) => {
          result[order.id] = order.paymentMethod === "Undefined" ? "" : order.paymentMethod;
          return result;
        }, {}),
      );
      setErrorMessage("");
    } catch (error) {
      await handleApiError(error, onUnauthorized, setErrorMessage, "Nao foi possivel carregar o caixa.");
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    void loadOrders();
  }, [token]);

  async function handlePaymentUpdate(order: CustomerOrder, paymentStatus: string) {
    const selectedPaymentMethod =
      selectedPaymentMethods[order.id] || (order.paymentMethod !== "Undefined" ? order.paymentMethod : "");

    if (paymentStatus === "Paid" && !selectedPaymentMethod) {
      setErrorMessage("Selecione Pix, Credito, Debito ou Dinheiro antes de marcar como pago.");
      return;
    }

    try {
      setProcessingOrderId(order.id);
      setSuccessMessage("");
      const updatedOrder = await updateOrderPayment(
        token,
        order.id,
        paymentStatus,
        paymentStatus === "Paid" ? selectedPaymentMethod : undefined,
      );

      setOrders((currentValue) => currentValue.map((currentOrder) => (currentOrder.id === updatedOrder.id ? updatedOrder : currentOrder)));
      setSelectedPaymentMethods((currentValue) => ({
        ...currentValue,
        [updatedOrder.id]: updatedOrder.paymentMethod === "Undefined" ? "" : updatedOrder.paymentMethod,
      }));
      setErrorMessage("");
      setSuccessMessage(paymentStatus === "Paid" ? "Pedido marcado como pago." : "Pedido voltou para a cobranca.");
    } catch (error) {
      await handleApiError(error, onUnauthorized, setErrorMessage, "Nao foi possivel atualizar o caixa.");
    } finally {
      setProcessingOrderId("");
    }
  }

  async function handleDeletePaidOrder(orderId: string) {
    const password = window.prompt("Digite a senha do owner para apagar este pedido pago.");

    if (!password) {
      return;
    }

    const confirmed = window.confirm("Apagar este pedido pago do caixa?");

    if (!confirmed) {
      return;
    }

    try {
      setProcessingOrderId(orderId);
      setSuccessMessage("");
      await deletePaidOrder(token, orderId, password);
      setOrders((currentValue) => currentValue.filter((order) => order.id !== orderId));
      setSelectedPaymentMethods((currentValue) => {
        const nextValue = { ...currentValue };
        delete nextValue[orderId];
        return nextValue;
      });
      setErrorMessage("");
      setSuccessMessage("Pedido pago apagado.");
    } catch (error) {
      await handleApiError(error, onUnauthorized, setErrorMessage, "Nao foi possivel apagar o pedido pago.");
    } finally {
      setProcessingOrderId("");
    }
  }

  async function handleDeleteUnpaidOrder(orderId: string) {
    const confirmed = window.confirm("Apagar este pedido antes do pagamento?");

    if (!confirmed) {
      return;
    }

    try {
      setProcessingOrderId(orderId);
      setSuccessMessage("");
      await deleteOrder(token, orderId);
      setOrders((currentValue) => currentValue.filter((order) => order.id !== orderId));
      setSelectedPaymentMethods((currentValue) => {
        const nextValue = { ...currentValue };
        delete nextValue[orderId];
        return nextValue;
      });
      setErrorMessage("");
      setSuccessMessage("Pedido apagado.");
    } catch (error) {
      await handleApiError(error, onUnauthorized, setErrorMessage, "Nao foi possivel apagar o pedido.");
    } finally {
      setProcessingOrderId("");
    }
  }

  async function handleDeleteAllPaidOrders() {
    const password = window.prompt("Digite a senha do owner para apagar todos os pedidos pagos.");

    if (!password) {
      return;
    }

    const confirmed = window.confirm("Apagar todos os pedidos ja pagos do caixa?");

    if (!confirmed) {
      return;
    }

    try {
      const paidOrderIds = orders.filter((order) => order.paymentStatus === "Paid").map((order) => order.id);
      setRemovingPaidBatch(true);
      setSuccessMessage("");
      await deleteAllPaidOrders(token, password);
      setOrders((currentValue) => currentValue.filter((order) => order.paymentStatus !== "Paid"));
      setSelectedPaymentMethods((currentValue) => {
        const nextValue = { ...currentValue };

        Object.keys(nextValue).forEach((orderId) => {
          if (paidOrderIds.includes(orderId)) {
            delete nextValue[orderId];
          }
        });

        return nextValue;
      });
      setErrorMessage("");
      setSuccessMessage("Pedidos pagos apagados.");
    } catch (error) {
      await handleApiError(error, onUnauthorized, setErrorMessage, "Nao foi possivel apagar os pedidos pagos.");
    } finally {
      setRemovingPaidBatch(false);
    }
  }

  async function handleDownloadDailyReport() {
    try {
      setExportingDailyReport(true);
      setSuccessMessage("");
      const file = await downloadDailyCashReportPdf(token);
      const downloadUrl = URL.createObjectURL(file.blob);
      const anchor = document.createElement("a");
      anchor.href = downloadUrl;
      anchor.download = file.fileName;
      document.body.appendChild(anchor);
      anchor.click();
      anchor.remove();
      URL.revokeObjectURL(downloadUrl);
      setErrorMessage("");
      setSuccessMessage("Relatorio diario exportado em PDF.");
    } catch (error) {
      await handleApiError(error, onUnauthorized, setErrorMessage, "Nao foi possivel exportar o relatorio do dia.");
    } finally {
      setExportingDailyReport(false);
    }
  }

  async function handleDeleteTodayFlow() {
    const password = window.prompt("Digite a senha do owner para apagar todos os pedidos ativos da operacao.");

    if (!password) {
      return;
    }

    const confirmed = window.confirm(
      "Apagar todos os pedidos ativos em todo o sistema, incluindo cozinha, caixa, impressao e status atuais?"
    );

    if (!confirmed) {
      return;
    }

    try {
        setRemovingTodayFlow(true);
        setSuccessMessage("");
        await deleteTodayOrderFlow(token, password);
        await loadOrders();
        setErrorMessage("");
        setSuccessMessage("Pedidos ativos da operacao apagados em todo o sistema.");
      } catch (error) {
        await handleApiError(error, onUnauthorized, setErrorMessage, "Nao foi possivel apagar os pedidos atuais da operacao.");
      } finally {
        setRemovingTodayFlow(false);
      }
  }

  const columns = useMemo(() => buildCashColumns(orders), [orders]);
  const pendingColumn = columns.find((column) => column.key === "pending");
  const paidColumn = columns.find((column) => column.key === "paid");
  const pendingCount = pendingColumn?.items.length ?? 0;
  const paidCount = paidColumn?.items.length ?? 0;
  const pendingTotal = sumOrders(pendingColumn?.items ?? []);
  const paidTotal = sumOrders(paidColumn?.items ?? []);
  const paymentSummaries = useMemo(() => buildPaymentSummaries(orders), [orders]);

  return (
    <section className="menu-workspace">
      {successMessage ? <p className="module-feedback success">{successMessage}</p> : null}
      {errorMessage ? <p className="module-feedback error">{errorMessage}</p> : null}

      <section className="surface-card module-list-card">
        <div className="module-section-head cash-header compact-order-panel-head">
          <div className="cash-header-copy">
            <h2>Caixa</h2>
            <div className="cash-summary-pills">
              <span className="cash-summary-pill">
                <small>A cobrar</small>
                <strong>{pendingCount}</strong>
                <em>{formatCurrency(pendingTotal)}</em>
              </span>
              <span className="cash-summary-pill">
                <small>Pagos</small>
                <strong>{paidCount}</strong>
                <em>{formatCurrency(paidTotal)}</em>
              </span>
            </div>
          </div>

          <div className="toolbar-actions compact table-card-actions module-action-row">
            <button
              className="ghost-link button-link module-action-button"
              type="button"
              disabled={exportingDailyReport}
              onClick={() => void handleDownloadDailyReport()}
            >
              {exportingDailyReport ? "Exportando PDF..." : "Exportar relatorio"}
            </button>

            {paidCount > 0 ? (
              <button
                className="ghost-link button-link destructive-link module-action-button"
                type="button"
                disabled={removingPaidBatch}
                onClick={() => void handleDeleteAllPaidOrders()}
              >
                {removingPaidBatch ? "Apagando pagos..." : "Apagar pagos"}
              </button>
            ) : null}

            <button
              className="ghost-link button-link destructive-link module-action-button"
              type="button"
              disabled={removingTodayFlow}
              onClick={() => void handleDeleteTodayFlow()}
            >
              {removingTodayFlow ? "Apagando operacao..." : "Apagar pedidos atuais"}
            </button>
          </div>
        </div>

        <div className="cash-method-grid">
          {paymentSummaries.map((summary) => (
            <article key={summary.value} className="cash-method-card">
              <div className="cash-method-head">
                <strong>{summary.label}</strong>
                <span>{summary.count} pagos</span>
              </div>
              <strong className="cash-method-total">{formatCurrency(summary.total)}</strong>
            </article>
          ))}
        </div>

        {loading ? (
          <p className="loading-state">Carregando caixa...</p>
        ) : orders.length === 0 ? (
          <div className="module-empty-state">
            <strong>Nenhum pedido.</strong>
          </div>
        ) : (
          <div className="kitchen-board cash-board">
            {columns.map((column) => (
              <section key={column.key} className="kitchen-column">
                <div className="module-section-head kitchen-column-head compact-order-column-head">
                  <strong>{column.title}</strong>
                  <strong>{column.items.length}</strong>
                </div>

                {column.items.length === 0 ? (
                  <div className="module-empty-state compact-empty-state">
                    <p>Sem pedidos.</p>
                  </div>
                ) : (
                  <div className="module-card-list">
                    {column.items.map((order) => (
                      <article
                        key={order.id}
                        className={`module-entity-card interactive-card kitchen-order-card cash-order-card compact-order-card ${
                          order.paymentStatus === "Paid" ? "cash-order-card-paid" : "cash-order-card-pending"
                        }`}
                      >
                        <div className="entity-head">
                          <div>
                            <h3>Pedido #{order.number}</h3>
                            <p>
                              {order.tableName} · {order.items.length} item(ns)
                            </p>
                          </div>
                          <span className={`status-chip ${order.paymentStatus === "Paid" ? "ready" : "pending"} cash-payment-chip`}>
                            {formatPaymentStatus(order.paymentStatus)}
                          </span>
                        </div>

                        <div className="entity-meta-grid">
                          <span>{formatCurrency(order.totalAmount)}</span>
                          <span>{formatPaymentMethod(order.paymentMethod)}</span>
                          <span>{formatDateTime(order.submittedAtUtc)}</span>
                          <span>{getOrderFlowLabel(order)}</span>
                          <span>{getOrderPrintCheckLabel(order)}</span>
                        </div>

                        <OrderItemsCompact items={order.items} />

                        {order.paymentStatus === "Paid" ? (
                          <div className="module-empty-state compact-empty-state cash-payment-state-card">
                            <strong>{formatPaymentMethod(order.paymentMethod)}</strong>
                            <p>{order.paidAtUtc ? `Pago em ${formatDateTime(order.paidAtUtc)}` : "Pago no caixa."}</p>
                            {getPaymentChangeCopy(order) ? <p>{getPaymentChangeCopy(order)}</p> : null}
                          </div>
                        ) : (
                          <div className="field-group cash-payment-field">
                            <label>Forma de pagamento no caixa</label>
                            <div className="choice-pill-row cash-choice-row">
                              {paymentOptions.map((option) => (
                                <button
                                  key={option.value}
                                  className={`ghost-link button-link choice-pill cash-choice-pill ${selectedPaymentMethods[order.id] === option.value ? "is-selected-filter" : ""}`}
                                  type="button"
                                  onClick={() =>
                                    setSelectedPaymentMethods((currentValue) => ({
                                      ...currentValue,
                                      [order.id]: option.value,
                                    }))
                                  }
                                >
                                  {option.label}
                                </button>
                              ))}
                            </div>
                          </div>
                        )}

                        <div className="module-empty-state compact-empty-state cash-payment-state-card">
                          <strong>{getOrderPrintCheckLabel(order)}</strong>
                          <p>{getOrderPrintCheckCopy(order)}</p>
                        </div>

                        <div className="toolbar-actions compact table-card-actions module-action-row order-card-actions">
                          {order.paymentStatus !== "Paid" ? (
                            <>
                              <button
                                className="ghost-link button-link module-action-button module-action-button-primary"
                                type="button"
                                disabled={processingOrderId === order.id}
                                onClick={() => void handlePaymentUpdate(order, "Paid")}
                              >
                                {processingOrderId === order.id ? "Salvando..." : "Pago"}
                              </button>
                              <button
                                className="ghost-link button-link destructive-link module-action-button"
                                type="button"
                                disabled={processingOrderId === order.id}
                                onClick={() => void handleDeleteUnpaidOrder(order.id)}
                              >
                                {processingOrderId === order.id ? "Apagando..." : "Excluir"}
                              </button>
                            </>
                          ) : (
                            <button
                              className="ghost-link button-link module-action-button"
                              type="button"
                              disabled={processingOrderId === order.id}
                              onClick={() => void handlePaymentUpdate(order, "Pending")}
                            >
                              {processingOrderId === order.id ? "Salvando..." : "Cobrar"}
                            </button>
                          )}

                          {order.paymentStatus === "Paid" ? (
                            <button
                              className="ghost-link button-link destructive-link module-action-button"
                              type="button"
                              disabled={processingOrderId === order.id}
                              onClick={() => void handleDeletePaidOrder(order.id)}
                            >
                              {processingOrderId === order.id ? "Apagando..." : "Excluir pago"}
                            </button>
                          ) : null}
                        </div>
                      </article>
                    ))}
                  </div>
                )}
              </section>
            ))}
          </div>
        )}
      </section>
    </section>
  );
}
