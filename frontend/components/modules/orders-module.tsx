"use client";

import { useEffect, useMemo, useState } from "react";
import {
  deleteOrder,
  getOrders,
  updateOrderStatus,
  type CustomerOrder,
} from "@/lib/api";
import {
  formatCurrency,
  formatDateTime,
  handleApiError,
  type AsyncVoid,
} from "@/components/modules/module-utils";

type KitchenColumn = {
  key: string;
  title: string;
  items: CustomerOrder[];
};

function buildKitchenColumns(orders: CustomerOrder[]): KitchenColumn[] {
  return [
    {
      key: "pending",
      title: "Aguardando",
      items: orders.filter((order) => order.status === "Pending"),
    },
    {
      key: "inkitchen",
      title: "Em preparo",
      items: orders.filter((order) => order.status === "InKitchen"),
    },
    {
      key: "ready",
      title: "Prontos",
      items: orders.filter((order) => order.status === "Ready"),
    },
    {
      key: "closed",
      title: "Finalizados",
      items: orders.filter((order) => order.status === "Delivered" || order.status === "Cancelled"),
    },
  ];
}

export function OrdersModule({ token, onUnauthorized }: { token: string; onUnauthorized: AsyncVoid }) {
  const [orders, setOrders] = useState<CustomerOrder[]>([]);
  const [loading, setLoading] = useState(true);
  const [processingOrderId, setProcessingOrderId] = useState("");
  const [errorMessage, setErrorMessage] = useState("");

  async function loadOrders() {
    setLoading(true);

    try {
      const response = await getOrders(token);
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

  async function handleDeleteCancelledOrder(orderId: string) {
    const confirmed = window.confirm("Remover este pedido cancelado?");

    if (!confirmed) {
      return;
    }

    try {
      setProcessingOrderId(orderId);
      await deleteOrder(token, orderId);
      setOrders((currentValue) => currentValue.filter((order) => order.id !== orderId));
      setErrorMessage("");
    } catch (error) {
      await handleApiError(error, onUnauthorized, setErrorMessage, "Nao foi possivel remover o pedido cancelado.");
    } finally {
      setProcessingOrderId("");
    }
  }

  const columns = useMemo(() => buildKitchenColumns(orders), [orders]);

  return (
    <section className="menu-workspace">
      {errorMessage ? <p className="module-feedback error">{errorMessage}</p> : null}

      <section className="surface-card module-list-card">
        <div className="module-section-head">
          <span className="eyebrow">Cozinha</span>
          <strong>{orders.length} pedidos</strong>
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
                <div className="module-section-head kitchen-column-head">
                  <span className="eyebrow">{column.title}</span>
                  <strong>{column.items.length}</strong>
                </div>

                {column.items.length === 0 ? (
                  <div className="module-empty-state compact-empty-state">
                    <p>Sem pedidos.</p>
                  </div>
                ) : (
                  <div className="module-card-list">
                    {column.items.map((order) => (
                      <article key={order.id} className="module-entity-card interactive-card kitchen-order-card">
                        <div className="entity-head">
                          <div>
                            <h3>Pedido #{order.number}</h3>
                            <p>{order.tableName}</p>
                          </div>
                          <span className={`status-chip ${order.status.toLowerCase()}`}>{order.status}</span>
                        </div>

                        <div className="entity-meta-grid">
                          {order.customerName ? <span>{order.customerName}</span> : null}
                          <span>{formatDateTime(order.submittedAtUtc)}</span>
                          <span>{formatCurrency(order.totalAmount)}</span>
                        </div>

                        <div className="item-line-list">
                          {order.items.map((item) => (
                            <p key={item.id}>
                              {item.quantity}x {item.name} - {formatCurrency(item.totalPrice)}
                            </p>
                          ))}
                        </div>

                        {order.notes ? (
                          <div className="module-empty-state compact-empty-state">
                            <strong>Observacao</strong>
                            <p>{order.notes}</p>
                          </div>
                        ) : null}

                        <div className="toolbar-actions compact table-card-actions">
                          {order.status === "Pending" ? (
                            <button
                              className="ghost-link button-link"
                              type="button"
                              disabled={processingOrderId === order.id}
                              onClick={() => void handleStatusUpdate(order.id, "InKitchen")}
                            >
                              Iniciar preparo
                            </button>
                          ) : null}

                          {order.status === "InKitchen" ? (
                            <button
                              className="ghost-link button-link"
                              type="button"
                              disabled={processingOrderId === order.id}
                              onClick={() => void handleStatusUpdate(order.id, "Ready")}
                            >
                              Marcar pronto
                            </button>
                          ) : null}

                          {order.status === "Ready" ? (
                            <button
                              className="ghost-link button-link"
                              type="button"
                              disabled={processingOrderId === order.id}
                              onClick={() => void handleStatusUpdate(order.id, "Delivered")}
                            >
                              Concluir
                            </button>
                          ) : null}

                          {order.status !== "Delivered" && order.status !== "Cancelled" ? (
                            <button
                              className="ghost-link button-link admin-danger-button"
                              type="button"
                              disabled={processingOrderId === order.id}
                              onClick={() => void handleStatusUpdate(order.id, "Cancelled")}
                            >
                              Cancelar
                            </button>
                          ) : null}

                          {order.status === "Cancelled" ? (
                            <button
                              className="ghost-link button-link destructive-link"
                              type="button"
                              disabled={processingOrderId === order.id}
                              onClick={() => void handleDeleteCancelledOrder(order.id)}
                            >
                              {processingOrderId === order.id ? "Removendo..." : "Remover cancelado"}
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
