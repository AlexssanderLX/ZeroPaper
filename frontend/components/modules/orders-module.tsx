"use client";

import { useEffect, useMemo, useState } from "react";
import {
  getOrders,
  requeuePrintOrder,
  updateOrderStatus,
  updateOrdersStatusBatch,
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

type KitchenColumn = {
  key: string;
  title: string;
  subtitle: string;
  items: CustomerOrder[];
  batchLabel?: string;
  batchAction?: "print" | "status";
  batchStatus?: string;
  requiresOwnerPassword?: boolean;
};

const statusLabels: Record<string, string> = {
  Pending: "Aguardando",
  InKitchen: "Em preparo",
  Ready: "Pronto",
  Delivered: "Saiu da cozinha",
  Cancelled: "Cancelado",
};

const printStatusLabels: Record<string, string> = {
  Pending: "Falta imprimir",
  Processing: "Em impressao",
  Printed: "Impresso",
  Failed: "Falhou",
  Disabled: "Pausado",
};

function buildKitchenColumns(orders: CustomerOrder[]): KitchenColumn[] {
  const sortedOrders = [...orders].sort(
    (leftOrder, rightOrder) => new Date(leftOrder.submittedAtUtc).getTime() - new Date(rightOrder.submittedAtUtc).getTime(),
  );

  return [
    {
      key: "todo",
      title: "A fazer",
      subtitle: "Novos e em preparo",
      items: sortedOrders.filter((order) => order.status === "Pending" || order.status === "InKitchen"),
      batchLabel: "Imprimir a fazer",
      batchAction: "print",
    },
    {
      key: "ready",
      title: "Prontos",
      subtitle: "Ja podem sair da cozinha",
      items: sortedOrders.filter((order) => order.status === "Ready"),
      batchLabel: "Retirar todos",
      batchAction: "status",
      batchStatus: "Delivered",
      requiresOwnerPassword: true,
    },
  ];
}

export function OrdersModule({ token, onUnauthorized }: { token: string; onUnauthorized: AsyncVoid }) {
  const [orders, setOrders] = useState<CustomerOrder[]>([]);
  const [loading, setLoading] = useState(true);
  const [processingOrderId, setProcessingOrderId] = useState("");
  const [processingBatchKey, setProcessingBatchKey] = useState("");
  const [printingOrderId, setPrintingOrderId] = useState("");
  const [errorMessage, setErrorMessage] = useState("");

  async function loadOrders() {
    setLoading(true);

    try {
      const response = await getOrders(token, true);
      setOrders(response);
      setErrorMessage("");
    } catch (error) {
      await handleApiError(error, onUnauthorized, setErrorMessage, "Nao foi possivel carregar a cozinha.");
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    void loadOrders();
  }, [token]);

  function requestOwnerPassword(actionLabel: string) {
    return window.prompt(`Digite a senha do owner para ${actionLabel}.`)?.trim() || "";
  }

  async function handleStatusUpdate(orderId: string, status: string) {
    try {
      setProcessingOrderId(orderId);
      await updateOrderStatus(token, orderId, status);
      await loadOrders();
    } catch (error) {
      await handleApiError(error, onUnauthorized, setErrorMessage, "Nao foi possivel atualizar o pedido.");
    } finally {
      setProcessingOrderId("");
    }
  }

  async function handleBatchStatusUpdate(column: KitchenColumn) {
    if (column.batchAction !== "status" || !column.batchStatus || column.items.length === 0) {
      return;
    }

    const password = column.requiresOwnerPassword
      ? requestOwnerPassword(
          column.batchStatus === "Ready"
            ? "marcar todos os pedidos como prontos"
            : "encerrar todos os pedidos desta coluna",
        )
      : "";

    if (column.requiresOwnerPassword && !password) {
      return;
    }

    const confirmed = window.confirm(`${column.batchLabel}?`);

    if (!confirmed) {
      return;
    }

    try {
      setProcessingBatchKey(column.key);
      await updateOrdersStatusBatch(
        token,
        column.items.map((order) => order.id),
        column.batchStatus,
        password || undefined,
      );
      await loadOrders();
    } catch (error) {
      await handleApiError(error, onUnauthorized, setErrorMessage, "Nao foi possivel atualizar os pedidos da coluna.");
    } finally {
      setProcessingBatchKey("");
    }
  }

  async function handlePrintOrder(orderId: string) {
    try {
      setPrintingOrderId(orderId);
      await requeuePrintOrder(token, orderId);
      await loadOrders();
    } catch (error) {
      await handleApiError(error, onUnauthorized, setErrorMessage, "Nao foi possivel enviar esse pedido para impressao.");
    } finally {
      setPrintingOrderId("");
    }
  }

  async function handleBatchPrint(column: KitchenColumn) {
    if (column.batchAction !== "print" || column.items.length === 0) {
      return;
    }

    const confirmed = window.confirm("Imprimir todos os pedidos de A fazer?");

    if (!confirmed) {
      return;
    }

    try {
      setProcessingBatchKey(column.key);

      for (const order of column.items) {
        await requeuePrintOrder(token, order.id);
      }

      await loadOrders();
    } catch (error) {
      await handleApiError(error, onUnauthorized, setErrorMessage, "Nao foi possivel imprimir os pedidos desta coluna.");
    } finally {
      setProcessingBatchKey("");
    }
  }

  async function handleColumnAction(column: KitchenColumn) {
    if (column.batchAction === "print") {
      await handleBatchPrint(column);
      return;
    }

    await handleBatchStatusUpdate(column);
  }

  const columns = useMemo(() => buildKitchenColumns(orders), [orders]);
  const activeKitchenCount = columns.reduce((total, column) => total + column.items.length, 0);

  return (
    <section className="menu-workspace">
      {errorMessage ? <p className="module-feedback error">{errorMessage}</p> : null}

      <section className="surface-card module-list-card">
        <div className="module-section-head compact-order-panel-head">
          <h2>Pedidos para a cozinha</h2>
          <div className="cash-summary-pills order-summary-pills">
            <span className="cash-summary-pill">
              <small>Em processo</small>
              <strong>{activeKitchenCount}</strong>
            </span>
          </div>
        </div>

        {loading ? (
          <p className="loading-state">Carregando pedidos...</p>
        ) : orders.length === 0 ? (
          <div className="module-empty-state">
            <strong>Nenhum pedido.</strong>
          </div>
        ) : (
          <div className="kitchen-board">
            {columns.map((column) => (
              <section key={column.key} className="kitchen-column">
                <div className="module-section-head kitchen-column-head kitchen-column-toolbar compact-order-column-head">
                  <div className="kitchen-column-copy">
                    <span className="eyebrow">{column.title}</span>
                    <strong>{column.items.length}</strong>
                    <small>{column.subtitle}</small>
                  </div>

                  {column.batchLabel && column.items.length > 0 ? (
                    <button
                      className={`ghost-link button-link module-action-button kitchen-batch-button ${column.requiresOwnerPassword ? "" : "module-action-button-primary"}`}
                      type="button"
                      disabled={processingBatchKey === column.key}
                      onClick={() => void handleColumnAction(column)}
                    >
                      {processingBatchKey === column.key ? "Atualizando..." : column.batchLabel}
                    </button>
                  ) : null}
                </div>

                {column.items.length === 0 ? (
                  <div className="module-empty-state compact-empty-state">
                    <p>Sem pedidos.</p>
                  </div>
                ) : (
                  <div className="module-card-list">
                    {column.items.map((order) => (
                      <article key={order.id} className="module-entity-card interactive-card kitchen-order-card compact-order-card">
                        <div className="entity-head">
                          <div>
                            <h3>Pedido #{order.number}</h3>
                            <p>
                              {order.tableName} · {order.items.length} item(ns)
                            </p>
                          </div>
                          <span className={`status-chip ${order.status.toLowerCase()}`}>{statusLabels[order.status] ?? order.status}</span>
                        </div>

                        <div className="entity-meta-grid">
                          <span>{formatDateTime(order.submittedAtUtc)}</span>
                          <span>{formatCurrency(order.totalAmount)}</span>
                          <span>{formatPaymentMethod(order.paymentMethod)}</span>
                          <span>{formatPaymentStatus(order.paymentStatus)}</span>
                          <span>{printStatusLabels[order.printStatus] ?? order.printStatus}</span>
                          {order.customerName ? <span>{order.customerName}</span> : null}
                        </div>

                        <OrderItemsCompact items={order.items} />

                        {order.notes ? (
                          <div className="module-empty-state compact-empty-state">
                            <strong>Observacao</strong>
                            <p>{order.notes}</p>
                          </div>
                        ) : null}

                        <div className="toolbar-actions compact table-card-actions module-action-row order-card-actions">
                          {(order.status === "Pending" || order.status === "InKitchen") ? (
                            <button
                              className="ghost-link button-link module-action-button"
                              type="button"
                              disabled={printingOrderId === order.id}
                              onClick={() => void handlePrintOrder(order.id)}
                            >
                              {printingOrderId === order.id ? "Enviando..." : order.printStatus === "Printed" ? "Reimprimir" : "Imprimir"}
                            </button>
                          ) : null}

                          {order.status === "Pending" ? (
                            <button
                              className="ghost-link button-link module-action-button module-action-button-primary"
                              type="button"
                              disabled={processingOrderId === order.id}
                              onClick={() => void handleStatusUpdate(order.id, "InKitchen")}
                            >
                              Iniciar
                            </button>
                          ) : null}

                          {order.status === "InKitchen" ? (
                            <button
                              className="ghost-link button-link module-action-button module-action-button-primary"
                              type="button"
                              disabled={processingOrderId === order.id}
                              onClick={() => void handleStatusUpdate(order.id, "Ready")}
                            >
                              Pronto
                            </button>
                          ) : null}

                          {order.status === "Ready" ? (
                            <button
                              className="ghost-link button-link module-action-button module-action-button-primary"
                              type="button"
                              disabled={processingOrderId === order.id}
                              onClick={() => void handleStatusUpdate(order.id, "Delivered")}
                            >
                              Retirar
                            </button>
                          ) : null}

                          {order.status !== "Delivered" && order.status !== "Cancelled" ? (
                            <button
                              className="ghost-link button-link destructive-link module-action-button"
                              type="button"
                              disabled={processingOrderId === order.id}
                              onClick={() => void handleStatusUpdate(order.id, "Cancelled")}
                            >
                              Cancelar
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
