"use client";

import { useEffect, useMemo, useState } from "react";
import Link from "next/link";
import {
  ApiError,
  adjustOrderValue,
  getOrders,
  requeuePrintOrder,
  updateOrderPayment,
  updateOrderStatus,
  type CustomerOrder,
} from "@/lib/api";
import { useAppSession } from "@/components/app-session-provider";
import {
  formatCurrency,
  formatDateTime,
  formatPaymentMethod,
  formatPaymentStatus,
} from "@/components/modules/module-utils";
import { OrderItemsCompact } from "@/components/modules/order-items-compact";
import { WorkspaceShell } from "@/components/workspace-shell";

type FlowArea = "todo" | "ready" | "finished";
type AdjustmentDraft = {
  order: CustomerOrder;
  finalAmount: string;
  note: string;
};

const statusLabels: Record<string, string> = {
  Pending: "Recebido",
  InKitchen: "Producao",
  Ready: "Pronto",
  Delivered: "Finalizado",
  Cancelled: "Cancelado",
};

function sortByOldestFirst(orders: CustomerOrder[]) {
  return [...orders].sort(
    (leftOrder, rightOrder) => new Date(leftOrder.submittedAtUtc).getTime() - new Date(rightOrder.submittedAtUtc).getTime(),
  );
}

function renderOrderTitle(order: CustomerOrder) {
  if (order.fulfillmentType === "Pickup") {
    return order.customerName || "Retirada";
  }

  if (order.isDeliveryOrder) {
    return order.customerName || "Delivery";
  }

  if (order.salesAgentId && order.salesAgentName) {
    return `Pedido de ${order.salesAgentName}`;
  }

  return order.tableName || "Pedido local";
}

function renderOrderItems(order: CustomerOrder) {
  if (order.items.length === 0) {
    return "Sem itens";
  }

  return order.items.map((item) => `${item.quantity}x ${item.name}`).join(", ");
}

function renderServiceType(order: CustomerOrder) {
  if (order.fulfillmentType === "Pickup") {
    return "Retirada";
  }

  if (order.isDeliveryOrder) {
    return "Entrega";
  }

  if (order.salesAgentId) {
    return "Vendedor";
  }

  return order.tableName?.toLocaleLowerCase("pt-BR").includes("retirada") ? "Retirada" : "Local";
}

export function OwnerLobby() {
  const { session } = useAppSession();
  const [orders, setOrders] = useState<CustomerOrder[]>([]);
  const [loadingOperation, setLoadingOperation] = useState(true);
  const [processingOrderId, setProcessingOrderId] = useState("");
  const [processingPaymentOrderId, setProcessingPaymentOrderId] = useState("");
  const [printingOrderId, setPrintingOrderId] = useState("");
  const [errorMessage, setErrorMessage] = useState("");
  const [successMessage, setSuccessMessage] = useState("");
  const [activeArea, setActiveArea] = useState<FlowArea>("todo");
  const [showAllFinished, setShowAllFinished] = useState(false);
  const [selectedOrder, setSelectedOrder] = useState<CustomerOrder | null>(null);
  const [adjustmentDraft, setAdjustmentDraft] = useState<AdjustmentDraft | null>(null);

  useEffect(() => {
    let isMounted = true;
    let isRefreshing = false;

    async function refreshOrders(showLoading: boolean) {
      if (isRefreshing) {
        return;
      }

      isRefreshing = true;
      if (showLoading) {
        setLoadingOperation(true);
      }

      try {
        const ordersResponse = await getOrders(session.token);

        if (!isMounted) {
          return;
        }

        setOrders(ordersResponse);
        setErrorMessage("");
      } catch (error) {
        if (!isMounted) {
          return;
        }

        if (error instanceof ApiError && error.status === 401) {
          return;
        }

        setErrorMessage("Nao foi possivel carregar a visao geral agora.");
      } finally {
        isRefreshing = false;
        if (isMounted) {
          setLoadingOperation(false);
        }
      }
    }

    void refreshOrders(true);

    const intervalId = window.setInterval(() => {
      if (!document.hidden) {
        void refreshOrders(false);
      }
    }, 5000);

    const handleVisibilityChange = () => {
      if (!document.hidden) {
        void refreshOrders(false);
      }
    };
    document.addEventListener("visibilitychange", handleVisibilityChange);

    return () => {
      isMounted = false;
      window.clearInterval(intervalId);
      document.removeEventListener("visibilitychange", handleVisibilityChange);
    };
  }, [session.token]);

  const activeOrders = useMemo(() => orders.filter((order) => order.status !== "Cancelled"), [orders]);
  const todoOrders = useMemo(
    () => sortByOldestFirst(activeOrders.filter((order) => order.status === "Pending" || order.status === "InKitchen")),
    [activeOrders],
  );
  const readyOrders = useMemo(
    () => sortByOldestFirst(activeOrders.filter((order) => order.status === "Ready")),
    [activeOrders],
  );
  const finishedOrders = useMemo(
    () => sortByOldestFirst(activeOrders.filter((order) => order.status === "Delivered")),
    [activeOrders],
  );

  async function reloadOperation() {
    const ordersResponse = await getOrders(session.token);

    setOrders(ordersResponse);
  }

  function parseMoneyInput(value: string) {
    const parsedValue = Number(value.trim().replace(",", "."));
    return Number.isFinite(parsedValue) ? parsedValue : Number.NaN;
  }

  function canEditOrder(order: CustomerOrder) {
    return order.status === "Pending" || order.status === "InKitchen" || order.status === "Ready";
  }

  function handleAdjustOrderValue(order: CustomerOrder) {
    setAdjustmentDraft({
      order,
      finalAmount: order.totalAmount.toLocaleString("pt-BR", { minimumFractionDigits: 2, maximumFractionDigits: 2 }),
      note: order.priceAdjustmentNote ?? "",
    });
    setSelectedOrder(null);
  }

  async function handleConfirmAdjustOrderValue() {
    if (!adjustmentDraft) {
      return;
    }

    const finalAmount = parseMoneyInput(adjustmentDraft.finalAmount);

    if (!Number.isFinite(finalAmount) || finalAmount < 0) {
      setErrorMessage("Informe um valor final valido.");
      return;
    }

    const confirmed = window.confirm(`Confirmar alteracao do valor deste pedido para ${formatCurrency(finalAmount)}?`);

    if (!confirmed) {
      return;
    }

    try {
      const order = adjustmentDraft.order;
      setProcessingOrderId(order.id);
      const updatedOrder = await adjustOrderValue(session.token, order.id, {
        finalAmount,
        discountAmount: 0,
        surchargeAmount: 0,
        note: adjustmentDraft.note,
      });

      setOrders((currentOrders) =>
        currentOrders.map((currentOrder) => (currentOrder.id === updatedOrder.id ? updatedOrder : currentOrder)),
      );
      setSelectedOrder((currentOrder) => (currentOrder?.id === updatedOrder.id ? updatedOrder : currentOrder));
      setAdjustmentDraft(null);
      setErrorMessage("");
    } catch (error) {
      if (error instanceof ApiError && error.status === 401) {
        return;
      }

      setErrorMessage(error instanceof Error && error.message ? error.message : "Nao foi possivel ajustar o valor do pedido.");
    } finally {
      setProcessingOrderId("");
    }
  }

  async function handlePaymentUpdate(order: CustomerOrder, paymentStatus: string) {
    try {
      setProcessingPaymentOrderId(order.id);
      const updated = await updateOrderPayment(session.token, order.id, paymentStatus);
      setOrders((current) => current.map((o) => (o.id === updated.id ? updated : o)));
      setSelectedOrder((current) => (current?.id === updated.id ? updated : current));
      setErrorMessage("");
    } catch (error) {
      if (error instanceof ApiError && error.status === 401) return;
      setErrorMessage(error instanceof Error && error.message ? error.message : "Nao foi possivel atualizar o pagamento.");
    } finally {
      setProcessingPaymentOrderId("");
    }
  }

  async function handleStatusUpdate(orderId: string, status: string, password?: string) {
    try {
      setProcessingOrderId(orderId);
      await updateOrderStatus(session.token, orderId, status, password);
      await reloadOperation();
      setErrorMessage("");
    } catch (error) {
      if (error instanceof ApiError && error.status === 401) {
        return;
      }

      setErrorMessage(error instanceof Error && error.message ? error.message : "Nao foi possivel atualizar o pedido.");
    } finally {
      setProcessingOrderId("");
    }
  }

  async function handlePrintOrder(orderId: string) {
    try {
      setPrintingOrderId(orderId);
      setSuccessMessage("");
      await requeuePrintOrder(session.token, orderId);
      await reloadOperation();
      setSuccessMessage("Pedido enviado para impressao. Se o agente estiver online, ele imprime em instantes.");
      setErrorMessage("");
    } catch (error) {
      if (error instanceof ApiError && error.status === 401) {
        return;
      }

      setErrorMessage(
        error instanceof Error && error.message ? error.message : "Nao foi possivel enviar o pedido para impressao.",
      );
    } finally {
      setPrintingOrderId("");
    }
  }

  function renderOrderDetailModal() {
    if (!selectedOrder) {
      return null;
    }

    const isPickup = selectedOrder.fulfillmentType === "Pickup";
    const deliveryLines = selectedOrder.isDeliveryOrder
      ? [
          selectedOrder.customerName,
          selectedOrder.deliveryPhone,
          !isPickup && [selectedOrder.deliveryAddress, selectedOrder.deliveryNumber].filter(Boolean).join(", "),
          !isPickup && selectedOrder.deliveryComplement,
          !isPickup && selectedOrder.deliveryPostalCode,
          !isPickup && selectedOrder.deliveryFreightAmount > 0 && `Frete: ${formatCurrency(selectedOrder.deliveryFreightAmount)}`,
          !isPickup && selectedOrder.deliveryDistanceKm && `${selectedOrder.deliveryDistanceKm.toLocaleString("pt-BR")} km`,
        ].filter(Boolean as unknown as <T>(x: T | false | null | undefined) => x is T)
      : [];

    return (
      <div className="owner-order-modal-backdrop" role="presentation" onClick={() => setSelectedOrder(null)}>
        <section
          className="surface-card owner-order-modal owner-order-modal-v2"
          role="dialog"
          aria-modal="true"
          aria-labelledby="owner-order-modal-title"
          onClick={(event) => event.stopPropagation()}
        >
          {/* Header */}
          <div className="owner-modal-v2-head">
            <div className="owner-modal-v2-title">
              <span className="eyebrow">{renderServiceType(selectedOrder)}</span>
              <h2 id="owner-order-modal-title">{renderOrderTitle(selectedOrder)}</h2>
              <p>Pedido #{selectedOrder.number} · {formatDateTime(selectedOrder.submittedAtUtc)}</p>
            </div>
            <div className="owner-modal-v2-aside">
              <strong className="owner-modal-v2-total">{formatCurrency(selectedOrder.totalAmount)}</strong>
              <span className={`status-chip ${selectedOrder.status.toLowerCase()} owner-modal-status-chip`}>
                {statusLabels[selectedOrder.status] ?? selectedOrder.status}
              </span>
            </div>
            <button className="owner-modal-v2-close" type="button" onClick={() => setSelectedOrder(null)} aria-label="Fechar">×</button>
          </div>

          <div className="owner-modal-v2-toolbar">
            <div className="owner-modal-v2-meta entity-meta-grid">
              <span>{formatPaymentMethod(selectedOrder.paymentMethod)}</span>
              <span>{formatPaymentStatus(selectedOrder.paymentStatus)}</span>
              {selectedOrder.salesAgentName ? <span>Vendedor: {selectedOrder.salesAgentName}</span> : null}
              {selectedOrder.requestedPaymentMethod && selectedOrder.requestedPaymentMethod !== selectedOrder.paymentMethod ? (
                <span>Solicitado: {formatPaymentMethod(selectedOrder.requestedPaymentMethod)}</span>
              ) : null}
              {selectedOrder.hasPriceAdjustment ? <span>Valor ajustado</span> : null}
              {selectedOrder.isEdited ? <span>Editado</span> : null}
            </div>

            {/* Ações de pagamento */}
            <div className="cash-modal-primary-actions">
              {selectedOrder.paymentStatus === "Paid" ? (
                <button
                  className="ghost-link button-link module-action-button cash-modal-action-secondary"
                  type="button"
                  disabled={processingPaymentOrderId === selectedOrder.id}
                  onClick={() => void handlePaymentUpdate(selectedOrder, "Pending")}
                >
                  {processingPaymentOrderId === selectedOrder.id ? "Salvando..." : "Voltar a cobrar"}
                </button>
              ) : (
                <button
                  className="ghost-link button-link module-action-button module-action-button-primary cash-modal-action-primary"
                  type="button"
                  disabled={processingPaymentOrderId === selectedOrder.id}
                  onClick={() => void handlePaymentUpdate(selectedOrder, "Paid")}
                >
                  {processingPaymentOrderId === selectedOrder.id ? "Salvando..." : "Pago"}
                </button>
              )}
            </div>

            <div className="owner-modal-v2-actions">
              <button
                className="ghost-link button-link owner-order-quick-action owner-order-quick-action-primary"
                type="button"
                disabled={printingOrderId === selectedOrder.id}
                onClick={() => void handlePrintOrder(selectedOrder.id)}
              >
                {printingOrderId === selectedOrder.id ? "Enviando..." : "Imprimir pedido"}
              </button>
              <button
                className="ghost-link button-link owner-order-quick-action"
                type="button"
                disabled={processingOrderId === selectedOrder.id}
                onClick={() => handleAdjustOrderValue(selectedOrder)}
              >
                Ajustar valor
              </button>
              {canEditOrder(selectedOrder) ? (
                <Link
                  className="ghost-link button-link owner-order-quick-action owner-order-quick-action-primary"
                  href={`/app/pedidos/editar/${selectedOrder.id}`}
                >
                  Editar pedido
                </Link>
              ) : null}
            </div>
          </div>

          <div className="owner-modal-v2-body">

          {/* Items — primary focus */}
          <div className="owner-modal-v2-items">
            <span className="owner-modal-v2-section-label">Itens do pedido</span>
            <OrderItemsCompact items={selectedOrder.items} />
          </div>

          {/* Notes */}
          {selectedOrder.notes ? (
            <div className="owner-order-detail-note">
              <span>Observacao</span>
              <p>{selectedOrder.notes}</p>
            </div>
          ) : null}

          {/* Price adjustment note */}
          {selectedOrder.hasPriceAdjustment ? (
            <div className="owner-order-detail-note owner-order-adjustment-note">
              <span>Valor alterado</span>
              <p>Original: {formatCurrency(selectedOrder.originalTotalAmount)}. Final: {formatCurrency(selectedOrder.totalAmount)}.</p>
              {selectedOrder.priceAdjustmentNote ? <p>{selectedOrder.priceAdjustmentNote}</p> : null}
            </div>
          ) : null}

          {/* Delivery / pickup info */}
          {deliveryLines.length > 0 ? (
            <div className="owner-order-detail-note">
              <span>{isPickup ? "Retirada" : "Entrega"}</span>
              {deliveryLines.map((line) => (
                <p key={String(line)}>{String(line)}</p>
              ))}
            </div>
          ) : null}

          {/* Print error */}
          {selectedOrder.printLastError ? (
            <div className="owner-order-detail-note">
              <span>Erro de impressao</span>
              <p>{selectedOrder.printLastError}</p>
            </div>
          ) : null}
          </div>
        </section>
      </div>
    );
  }

  function renderAdjustmentModal() {
    if (!adjustmentDraft) {
      return null;
    }

    return (
      <div className="cash-adjust-modal-backdrop owner-lobby-adjust-modal-backdrop" role="presentation">
        <section className="surface-card cash-adjust-modal" role="dialog" aria-modal="true" aria-label="Ajustar valor do pedido">
          <div className="module-section-head compact-order-panel-head">
            <div>
              <span className="eyebrow">Ajustar valor</span>
              <h2>{renderOrderTitle(adjustmentDraft.order)}</h2>
              <p>Atual: {formatCurrency(adjustmentDraft.order.totalAmount)}</p>
            </div>
            <button className="ghost-link button-link module-action-button" type="button" onClick={() => setAdjustmentDraft(null)}>
              Fechar
            </button>
          </div>

          <div className="field-group">
            <label htmlFor="ownerLobbyFinalAmount">Valor final do pedido</label>
            <input
              id="ownerLobbyFinalAmount"
              inputMode="decimal"
              value={adjustmentDraft.finalAmount}
              onChange={(event) =>
                setAdjustmentDraft((currentValue) =>
                  currentValue ? { ...currentValue, finalAmount: event.target.value } : currentValue,
                )
              }
              autoFocus
            />
            <p className="field-hint">O sistema calcula desconto ou acrescimo automaticamente.</p>
          </div>

          <div className="field-group">
            <label htmlFor="ownerLobbyAdjustmentNote">Observacao opcional</label>
            <input
              id="ownerLobbyAdjustmentNote"
              value={adjustmentDraft.note}
              onChange={(event) =>
                setAdjustmentDraft((currentValue) =>
                  currentValue ? { ...currentValue, note: event.target.value } : currentValue,
                )
              }
              placeholder="Ex.: desconto combinado"
            />
          </div>

          <div className="toolbar-actions compact table-card-actions module-action-row order-card-actions">
            <button className="ghost-link button-link module-action-button" type="button" onClick={() => setAdjustmentDraft(null)}>
              Cancelar
            </button>
            <button
              className="ghost-link button-link module-action-button module-action-button-primary"
              type="button"
              disabled={processingOrderId === adjustmentDraft.order.id}
              onClick={() => void handleConfirmAdjustOrderValue()}
            >
              {processingOrderId === adjustmentDraft.order.id ? "Salvando..." : "Confirmar ajuste"}
            </button>
          </div>
        </section>
      </div>
    );
  }

  function renderOperationalOrder(order: CustomerOrder, area: "todo" | "ready" | "finished") {
    return (
      <article
        key={`${area}-${order.id}`}
        className={`owner-flow-order-card status-${order.status.toLowerCase()}`}
        role="button"
        tabIndex={0}
        onClick={() => setSelectedOrder(order)}
        onKeyDown={(event) => {
          if (event.key === "Enter" || event.key === " ") {
            event.preventDefault();
            setSelectedOrder(order);
          }
        }}
      >
        <div className="owner-flow-order-head">
          <div>
            <strong>{renderOrderTitle(order)}</strong>
            <span>{renderOrderItems(order)}</span>
          </div>
          <em>{formatCurrency(order.totalAmount)}</em>
        </div>

        <div className="owner-flow-order-tags">
          <span>{statusLabels[order.status] ?? order.status}</span>
          <span>{renderServiceType(order)}</span>
          <span>{formatPaymentStatus(order.paymentStatus)}</span>
          <span>{formatDateTime(order.submittedAtUtc)}</span>
        </div>

        {area === "todo" ? (
          <div className="owner-flow-action-row">
            <button
              className="primary-link button-link owner-flow-action"
              type="button"
              disabled={processingOrderId === order.id}
              onClick={(event) => {
                event.stopPropagation();
                void handleStatusUpdate(order.id, "Ready");
              }}
            >
              {processingOrderId === order.id ? "Atualizando..." : "Pronto"}
            </button>
            {order.paymentStatus !== "Paid" ? (
              <button
                className="ghost-link button-link owner-flow-action"
                type="button"
                disabled={processingPaymentOrderId === order.id}
                onClick={(event) => {
                  event.stopPropagation();
                  void handlePaymentUpdate(order, "Paid");
                }}
              >
                {processingPaymentOrderId === order.id ? "Salvando..." : "Pago"}
              </button>
            ) : (
              <button
                className="ghost-link button-link owner-flow-action"
                type="button"
                disabled={processingPaymentOrderId === order.id}
                onClick={(event) => {
                  event.stopPropagation();
                  void handlePaymentUpdate(order, "Pending");
                }}
              >
                {processingPaymentOrderId === order.id ? "Salvando..." : "A cobrar"}
              </button>
            )}
          </div>
        ) : null}

        {area === "ready" ? (
          <div className="owner-flow-action-row">
            <button
              className="ghost-link button-link owner-flow-action"
              type="button"
              disabled={processingOrderId === order.id}
              onClick={(event) => {
                event.stopPropagation();
                void handleStatusUpdate(order.id, "InKitchen");
              }}
            >
              {processingOrderId === order.id ? "Atualizando..." : "Voltar"}
            </button>
            <button
              className="primary-link button-link owner-flow-action"
              type="button"
              disabled={processingOrderId === order.id}
              onClick={(event) => {
                event.stopPropagation();
                void handleStatusUpdate(order.id, "Delivered");
              }}
            >
              {processingOrderId === order.id ? "Atualizando..." : "Finalizar"}
            </button>
            {order.paymentStatus !== "Paid" ? (
              <button
                className="ghost-link button-link owner-flow-action"
                type="button"
                disabled={processingPaymentOrderId === order.id}
                onClick={(event) => {
                  event.stopPropagation();
                  void handlePaymentUpdate(order, "Paid");
                }}
              >
                {processingPaymentOrderId === order.id ? "Salvando..." : "Pago"}
              </button>
            ) : (
              <button
                className="ghost-link button-link owner-flow-action"
                type="button"
                disabled={processingPaymentOrderId === order.id}
                onClick={(event) => {
                  event.stopPropagation();
                  void handlePaymentUpdate(order, "Pending");
                }}
              >
                {processingPaymentOrderId === order.id ? "Salvando..." : "A cobrar"}
              </button>
            )}
          </div>
        ) : null}

        {area === "finished" ? <span className="owner-flow-readonly-note">Somente conferencia</span> : null}

        <button
          className="ghost-link button-link owner-flow-action owner-flow-print"
          type="button"
          disabled={printingOrderId === order.id}
          onClick={(event) => {
            event.stopPropagation();
            void handlePrintOrder(order.id);
          }}
        >
          {printingOrderId === order.id ? "Enviando..." : "Imprimir"}
        </button>
      </article>
    );
  }

  function renderOperationalColumn({
    title,
    subtitle,
    orders,
    area,
  }: {
    title: string;
    subtitle: string;
    orders: CustomerOrder[];
    area: FlowArea;
  }) {
    const shouldCollapseFinished = area === "finished" && !showAllFinished;
    const visibleOrders = shouldCollapseFinished ? orders.slice(0, 3) : orders;
    const hiddenOrders = Math.max(0, orders.length - visibleOrders.length);

    return (
      <section className={`surface-card owner-flow-column owner-flow-column-${area} ${activeArea === area ? "is-active" : ""}`}>
        <div className="owner-flow-column-head">
          <div>
            <span className="eyebrow">{title}</span>
            <p>{subtitle}</p>
          </div>
          <strong>{orders.length}</strong>
        </div>

        {loadingOperation ? (
          <p className="loading-state">Carregando...</p>
        ) : visibleOrders.length === 0 ? (
          <div className="owner-flow-empty">
            <strong>Nada aqui agora.</strong>
            <span>Quando chegar pedido, ele aparece nesta coluna.</span>
          </div>
        ) : (
          <div className="owner-flow-order-list">
            {visibleOrders.map((order) => renderOperationalOrder(order, area))}
            {area === "finished" && hiddenOrders > 0 ? (
              <button className="owner-flow-more" type="button" onClick={() => setShowAllFinished(true)}>
                Mostrar mais {hiddenOrders} finalizados
              </button>
            ) : null}
            {area === "finished" && showAllFinished && orders.length > 3 ? (
              <button className="owner-flow-more" type="button" onClick={() => setShowAllFinished(false)}>
                Recolher finalizados
              </button>
            ) : null}
          </div>
        )}
        {area === "finished" ? (
          <Link className="ghost-link button-link owner-flow-view-all owner-flow-finished-link" href="/app/finalizados">
            Ver pedidos finalizados
          </Link>
        ) : null}
      </section>
    );
  }

  const flowTabs: Array<{ area: FlowArea; title: string; count: number }> = [
    { area: "todo", title: "A fazer", count: todoOrders.length },
    { area: "ready", title: "Prontos", count: readyOrders.length },
    { area: "finished", title: "Finalizados", count: finishedOrders.length },
  ];

  return (
    <WorkspaceShell showAlertCard>
      <section className="owner-lobby-main">
          {errorMessage ? <p className="module-feedback error">{errorMessage}</p> : null}
          {successMessage ? <p className="module-feedback success">{successMessage}</p> : null}

          <nav className="owner-flow-tabs" aria-label="Trocar etapa dos pedidos">
            {flowTabs.map((tab) => (
              <div key={tab.area} className="owner-flow-tab-item">
                <button
                  className={activeArea === tab.area ? "owner-flow-tab-button is-active" : "owner-flow-tab-button"}
                  type="button"
                  aria-pressed={activeArea === tab.area}
                  onClick={() => setActiveArea(tab.area)}
                >
                  <span>{tab.title}</span>
                  <strong>{tab.count}</strong>
                </button>
              </div>
            ))}
          </nav>

          <section className="owner-flow-board" aria-label="Fluxo principal de pedidos e caixa">
            {renderOperationalColumn({
              title: "A fazer",
              subtitle: "Novos e em producao.",
              orders: todoOrders,
              area: "todo",
            })}

            {renderOperationalColumn({
              title: "Prontos",
              subtitle: "Pedidos aguardando saida.",
              orders: readyOrders,
              area: "ready",
            })}

            {renderOperationalColumn({
              title: "Finalizados",
              subtitle: "Pedidos entregues ou retirados.",
              orders: finishedOrders,
              area: "finished",
            })}
          </section>
      </section>
      {renderOrderDetailModal()}
      {renderAdjustmentModal()}
    </WorkspaceShell>
  );
}
