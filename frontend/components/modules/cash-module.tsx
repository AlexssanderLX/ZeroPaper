"use client";

import { useEffect, useMemo, useState } from "react";
import { deletePaidOrder, getOrders, updateOrderPayment, type CustomerOrder } from "@/lib/api";
import {
  formatCurrency,
  formatDateTime,
  formatPaymentMethod,
  formatPaymentStatus,
  handleApiError,
  type AsyncVoid,
} from "@/components/modules/module-utils";

type CashColumn = {
  key: string;
  title: string;
  items: CustomerOrder[];
};

const paymentOptions = [
  { value: "Pix", label: "Pix" },
  { value: "Credit", label: "Credito" },
  { value: "Debit", label: "Debito" },
];

function buildCashColumns(orders: CustomerOrder[]): CashColumn[] {
  const activeOrders = orders
    .filter((order) => order.status !== "Cancelled")
    .sort((leftOrder, rightOrder) => new Date(rightOrder.submittedAtUtc).getTime() - new Date(leftOrder.submittedAtUtc).getTime());

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
  const [errorMessage, setErrorMessage] = useState("");

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
      setErrorMessage("Selecione Pix, Credito ou Debito antes de marcar como pago.");
      return;
    }

    try {
      setProcessingOrderId(order.id);
      const updatedOrder = await updateOrderPayment(
        token,
        order.id,
        paymentStatus,
        paymentStatus === "Paid" ? selectedPaymentMethod : undefined,
      );
      setOrders((currentValue) => currentValue.map((order) => (order.id === updatedOrder.id ? updatedOrder : order)));
      setSelectedPaymentMethods((currentValue) => ({
        ...currentValue,
        [updatedOrder.id]: updatedOrder.paymentMethod === "Undefined" ? "" : updatedOrder.paymentMethod,
      }));
      setErrorMessage("");
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
      await deletePaidOrder(token, orderId, password);
      setOrders((currentValue) => currentValue.filter((order) => order.id !== orderId));
      setSelectedPaymentMethods((currentValue) => {
        const nextValue = { ...currentValue };
        delete nextValue[orderId];
        return nextValue;
      });
      setErrorMessage("");
    } catch (error) {
      await handleApiError(error, onUnauthorized, setErrorMessage, "Nao foi possivel apagar o pedido pago.");
    } finally {
      setProcessingOrderId("");
    }
  }

  const columns = useMemo(() => buildCashColumns(orders), [orders]);
  const pendingCount = columns.find((column) => column.key === "pending")?.items.length ?? 0;

  return (
    <section className="menu-workspace">
      {errorMessage ? <p className="module-feedback error">{errorMessage}</p> : null}

      <section className="surface-card module-list-card">
        <div className="module-section-head">
          <h2>Caixa</h2>
          <strong>{pendingCount} a cobrar</strong>
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
                <div className="module-section-head kitchen-column-head">
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
                      <article key={order.id} className="module-entity-card interactive-card kitchen-order-card">
                        <div className="entity-head">
                          <div>
                            <h3>Pedido #{order.number}</h3>
                            <p>{order.tableName}</p>
                          </div>
                          <span className={`status-chip ${order.paymentStatus === "Paid" ? "ready" : "pending"}`}>
                            {formatPaymentStatus(order.paymentStatus)}
                          </span>
                        </div>

                        <div className="entity-meta-grid">
                          <span>{formatCurrency(order.totalAmount)}</span>
                          <span>{formatPaymentMethod(order.paymentMethod)}</span>
                          <span>{formatDateTime(order.submittedAtUtc)}</span>
                          <span>{order.status === "Delivered" ? "Concluido" : order.status === "Ready" ? "Pronto" : order.status === "InKitchen" ? "Em preparo" : "Novo"}</span>
                        </div>

                        {order.paymentStatus === "Paid" && order.paidAtUtc ? (
                          <div className="module-empty-state compact-empty-state">
                            <strong>Pago em</strong>
                            <p>{formatDateTime(order.paidAtUtc)}</p>
                          </div>
                        ) : null}

                        <div className="field-group">
                          <label>Forma de pagamento no caixa</label>
                          <div className="choice-pill-row">
                            {paymentOptions.map((option) => (
                              <button
                                key={option.value}
                                className={`ghost-link button-link choice-pill ${selectedPaymentMethods[order.id] === option.value ? "is-selected-filter" : ""}`}
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

                        <div className="toolbar-actions compact table-card-actions">
                          {order.paymentStatus !== "Paid" ? (
                            <button
                              className="ghost-link button-link"
                              type="button"
                              disabled={processingOrderId === order.id}
                              onClick={() => void handlePaymentUpdate(order, "Paid")}
                            >
                              {processingOrderId === order.id ? "Salvando..." : "Marcar pago"}
                            </button>
                          ) : (
                            <button
                              className="ghost-link button-link"
                              type="button"
                              disabled={processingOrderId === order.id}
                              onClick={() => void handlePaymentUpdate(order, "Pending")}
                            >
                              {processingOrderId === order.id ? "Salvando..." : "Voltar para cobrar"}
                            </button>
                          )}

                          {order.paymentStatus === "Paid" ? (
                            <button
                              className="ghost-link button-link destructive-link"
                              type="button"
                              disabled={processingOrderId === order.id}
                              onClick={() => void handleDeletePaidOrder(order.id)}
                            >
                              {processingOrderId === order.id ? "Apagando..." : "Apagar pedido"}
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
