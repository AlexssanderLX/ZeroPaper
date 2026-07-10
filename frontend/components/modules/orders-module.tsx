"use client";

import { useEffect, useMemo, useState } from "react";
import Link from "next/link";
import {
  getOrders,
  requeuePrintOrder,
  updateOrderPayment,
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
import { CustomerProfilePanel } from "@/components/modules/customer-profile-panel";

type KitchenColumn = {
  key: OrdersModuleSection;
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

function buildDeliverySummary(order: CustomerOrder) {
  if (!order.isDeliveryOrder) {
    return [];
  }

  const lines = [];

  if (order.customerName) {
    lines.push(`Cliente: ${order.customerName}`);
  }

  if (order.deliveryPhone) {
    lines.push(`Telefone: ${order.deliveryPhone}`);
  }

  if (order.fulfillmentType === "Pickup") {
    lines.push("Retirada: no local");
    return lines;
  }

  if (order.deliveryAddress || order.deliveryNumber) {
    let address = [order.deliveryAddress, order.deliveryNumber].filter(Boolean).join(", ");

    if (order.deliveryComplement) {
      address = `${address} (${order.deliveryComplement})`;
    }

    lines.push(`Entrega: ${address}`);
  }

  return lines;
}

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
      subtitle: "Aguardando saida da cozinha",
      items: sortedOrders.filter((order) => order.status === "Ready"),
    },
  ];
}

function renderOrderSubtitle(order: CustomerOrder) {
  if (order.fulfillmentType === "Pickup") {
    return `${order.items.length} item(ns) para retirada`;
  }

  if (order.isDeliveryOrder) {
    return `${order.items.length} item(ns) para entrega`;
  }

  return `${order.tableName} - ${order.items.length} item(ns)`;
}

function renderOrderTitle(order: CustomerOrder) {
  if (!order.isDeliveryOrder) {
    if (order.salesAgentId && order.salesAgentName) {
      return `Pedido de ${order.salesAgentName}`;
    }
    return order.tableName || `Pedido #${order.number}`;
  }

  if (order.fulfillmentType === "Pickup") {
    return order.customerName ? `Retirada de ${order.customerName}` : "Retirada";
  }

  return order.customerName ? `Delivery de ${order.customerName}` : "Delivery";
}

function normalizeOrderSearch(value: string) {
  return value
    .toLocaleLowerCase("pt-BR")
    .normalize("NFD")
    .replace(/[̀-ͯ]/g, "")
    .trim();
}

function orderMatchesSearch(order: CustomerOrder, searchTerm: string) {
  const normalizedSearch = normalizeOrderSearch(searchTerm);

  if (!normalizedSearch) {
    return true;
  }

  const searchableText = [
    renderOrderTitle(order),
    renderOrderSubtitle(order),
    order.tableName,
    order.customerName,
    order.deliveryPhone,
    `pedido ${order.number}`,
    `#${order.number}`,
    formatPaymentMethod(order.paymentMethod),
    formatPaymentStatus(order.paymentStatus),
  ]
    .filter(Boolean)
    .join(" ");

  return normalizeOrderSearch(searchableText).includes(normalizedSearch);
}

export type OrdersModuleSection = "todo" | "ready";

export function OrdersModule({
  token,
  onUnauthorized,
  section,
}: {
  token: string;
  onUnauthorized: AsyncVoid;
  section?: OrdersModuleSection;
}) {
  const [orders, setOrders] = useState<CustomerOrder[]>([]);
  const [loading, setLoading] = useState(true);
  const [processingOrderId, setProcessingOrderId] = useState("");
  const [processingPaymentOrderId, setProcessingPaymentOrderId] = useState("");
  const [processingBatchKey, setProcessingBatchKey] = useState("");
  const [printingOrderId, setPrintingOrderId] = useState("");
  const [errorMessage, setErrorMessage] = useState("");
  const [activeSection, setActiveSection] = useState<OrdersModuleSection>(section ?? "todo");
  const [searchTerm, setSearchTerm] = useState("");
  const [selectedOrderId, setSelectedOrderId] = useState("");

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
  }, [token, section]);

  async function handleStatusUpdate(orderId: string, status: string, password?: string) {
    try {
      setProcessingOrderId(orderId);
      await updateOrderStatus(token, orderId, status, password);
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
      );
      await loadOrders();
    } catch (error) {
      await handleApiError(error, onUnauthorized, setErrorMessage, "Nao foi possivel atualizar os pedidos desta etapa.");
    } finally {
      setProcessingBatchKey("");
    }
  }

  async function handlePaymentUpdate(order: CustomerOrder, paymentStatus: string) {
    try {
      setProcessingPaymentOrderId(order.id);
      const updated = await updateOrderPayment(token, order.id, paymentStatus);
      setOrders((current) => current.map((o) => (o.id === updated.id ? updated : o)));
      setErrorMessage("");
    } catch (error) {
      await handleApiError(error, onUnauthorized, setErrorMessage, "Nao foi possivel atualizar o pagamento.");
    } finally {
      setProcessingPaymentOrderId("");
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
      await handleApiError(error, onUnauthorized, setErrorMessage, "Nao foi possivel imprimir os pedidos desta etapa.");
    } finally {
      setProcessingBatchKey("");
    }
  }

  async function handleMarkColumnReady(column: KitchenColumn) {
    const readyCandidateOrders = column.items.filter((order) => order.status === "Pending" || order.status === "InKitchen");

    if (readyCandidateOrders.length === 0) {
      return;
    }

    const confirmed = window.confirm("Marcar todos os pedidos de A fazer como prontos?");

    if (!confirmed) {
      return;
    }

    try {
      setProcessingBatchKey(`${column.key}:ready`);
      await updateOrdersStatusBatch(
        token,
        readyCandidateOrders.map((order) => order.id),
        "Ready",
      );
      await loadOrders();
    } catch (error) {
      await handleApiError(error, onUnauthorized, setErrorMessage, "Nao foi possivel marcar todos como prontos.");
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
  const isOverview = !section;
  const currentSection = section ?? activeSection;
  const activeColumn = columns.find((column) => column.key === currentSection) ?? columns[0];
  const activeColumnProcessing = processingBatchKey.startsWith(activeColumn.key);
  const visibleItems = activeColumn.items.filter((order) => orderMatchesSearch(order, searchTerm));
  const hasSearch = normalizeOrderSearch(searchTerm).length > 0;
  const panelTitle = "Cozinha";

  function renderOrderModal() {
    const order = orders.find((currentOrder) => currentOrder.id === selectedOrderId);

    if (!order) {
      return null;
    }

    const isProcessing = processingOrderId === order.id;
    const isProcessingPayment = processingPaymentOrderId === order.id;
    const isPrinting = printingOrderId === order.id;
    const isPaid = order.paymentStatus === "Paid";
    const deliveryLines = buildDeliverySummary(order);
    const canCook = order.status === "Pending" || order.status === "InKitchen";
    const canEdit = order.status === "Pending" || order.status === "InKitchen" || order.status === "Ready";

    const eyebrow = order.isDeliveryOrder
      ? order.fulfillmentType === "Pickup" ? "Retirada" : "Delivery"
      : order.salesAgentId ? "Vendedor" : order.tableName || "Local";

    return (
      <div className="cash-order-modal-backdrop" role="presentation" onClick={() => setSelectedOrderId("")}>
        <section
          className="surface-card cash-order-modal"
          role="dialog"
          aria-modal="true"
          aria-label={renderOrderTitle(order)}
          onClick={(event) => event.stopPropagation()}
        >
          <div className="cash-modal-head">
            <div className="cash-modal-head-copy">
              <span className="eyebrow">{eyebrow}</span>
              <h2>{renderOrderTitle(order)}</h2>
              <p>Pedido #{order.number} · {formatDateTime(order.submittedAtUtc)}</p>
            </div>
            <div className="cash-modal-head-right">
              <strong className="cash-modal-total">{formatCurrency(order.totalAmount)}</strong>
              <span className={`status-chip ${order.status.toLowerCase()} cash-payment-chip`}>
                {statusLabels[order.status] ?? order.status}
              </span>
            </div>
            <button className="cash-modal-close" type="button" onClick={() => setSelectedOrderId("")} aria-label="Fechar">
              ×
            </button>
          </div>

          <div className="entity-meta-grid">
            <span>{order.items.length} item(ns)</span>
            <span>{formatPaymentMethod(order.paymentMethod)}</span>
            <span className={isPaid ? "meta-tag-paid" : ""}>{formatPaymentStatus(order.paymentStatus)}</span>
            <span>{printStatusLabels[order.printStatus] ?? order.printStatus}</span>
            {order.salesAgentName ? <span>Vendedor: {order.salesAgentName}</span> : null}
            {order.customerName ? <span>{order.customerName}</span> : null}
            {order.isEdited ? <span>Editado</span> : null}
          </div>

          <OrderItemsCompact items={order.items} />

          {deliveryLines.length > 0 ? (
            <div className="module-empty-state compact-empty-state cash-payment-state-card">
              <strong>Entrega</strong>
              {deliveryLines.map((line) => (
                <p key={line}>{line}</p>
              ))}
            </div>
          ) : null}

          {order.notes ? (
            <div className="module-empty-state compact-empty-state cash-payment-state-card">
              <strong>Observacao</strong>
              <p>{order.notes}</p>
            </div>
          ) : null}

          {order.deliveryPhone ? (
            <CustomerProfilePanel token={token} phoneNumber={order.deliveryPhone} onUnauthorized={onUnauthorized} />
          ) : null}

          {/* Ações de pagamento */}
          <div className="cash-modal-primary-actions">
            {isPaid ? (
              <button
                className="ghost-link button-link module-action-button cash-modal-action-secondary"
                type="button"
                disabled={isProcessingPayment}
                onClick={() => void handlePaymentUpdate(order, "Pending")}
              >
                {isProcessingPayment ? "Salvando..." : "Voltar a cobrar"}
              </button>
            ) : (
              <button
                className="ghost-link button-link module-action-button module-action-button-primary cash-modal-action-primary"
                type="button"
                disabled={isProcessingPayment}
                onClick={() => void handlePaymentUpdate(order, "Paid")}
              >
                {isProcessingPayment ? "Salvando..." : "Pago"}
              </button>
            )}
          </div>

          {/* Ações de cozinha */}
          <div className="toolbar-actions compact table-card-actions module-action-row order-card-actions">
            {canCook ? (
              <button
                className="ghost-link button-link module-action-button"
                type="button"
                disabled={isPrinting}
                onClick={() => void handlePrintOrder(order.id)}
              >
                {isPrinting ? "Enviando..." : order.printStatus === "Printed" ? "Reimprimir" : "Imprimir"}
              </button>
            ) : null}

            {canCook ? (
              <button
                className="ghost-link button-link module-action-button module-action-button-primary"
                type="button"
                disabled={isProcessing}
                onClick={() => void handleStatusUpdate(order.id, "Ready")}
              >
                Marcar pronto
              </button>
            ) : null}

            {order.status === "Ready" ? (
              <button
                className="ghost-link button-link module-action-button"
                type="button"
                disabled={isProcessing}
                onClick={() => void handleStatusUpdate(order.id, "InKitchen")}
              >
                Voltar para a fazer
              </button>
            ) : null}

            {canEdit ? (
              <Link className="ghost-link button-link module-action-button" href={`/app/pedidos/editar/${order.id}`}>
                Editar
              </Link>
            ) : null}

            {order.status !== "Cancelled" ? (
              <button
                className="ghost-link button-link destructive-link module-action-button kitchen-cancel-order-button"
                type="button"
                disabled={isProcessing}
                onClick={() => {
                  const confirmed = window.confirm(
                    `Tem certeza que deseja cancelar o pedido "${renderOrderTitle(order)}"? Esta acao nao pode ser desfeita.`,
                  );

                  if (!confirmed) {
                    return;
                  }

                  void handleStatusUpdate(order.id, "Cancelled");
                  setSelectedOrderId("");
                }}
              >
                Cancelar pedido
              </button>
            ) : null}
          </div>
        </section>
      </div>
    );
  }

  return (
    <section className="menu-workspace">
      {errorMessage ? <p className="module-feedback error">{errorMessage}</p> : null}

      <section className="surface-card module-list-card">
        <div className="module-section-head compact-order-panel-head">
          <h2>{panelTitle}</h2>
        </div>

        {loading ? (
          <p className="loading-state">Carregando pedidos...</p>
        ) : orders.length === 0 ? (
          <div className="module-empty-state">
            <strong>Nenhum pedido.</strong>
          </div>
        ) : (
          <div className="kitchen-shell">
            <section className="surface-card kitchen-focus-panel">
              {isOverview ? (
                <nav className="owner-flow-tabs cash-flow-tabs" aria-label="Trocar etapa da cozinha">
                  {columns.map((column) => (
                    <div key={column.key} className="owner-flow-tab-item">
                      <button
                        className={currentSection === column.key ? "owner-flow-tab-button is-active" : "owner-flow-tab-button"}
                        type="button"
                        aria-pressed={currentSection === column.key}
                        onClick={() => setActiveSection(column.key)}
                      >
                        <span>{column.title}</span>
                        <strong>{column.items.length}</strong>
                      </button>
                    </div>
                  ))}
                </nav>
              ) : null}

              <div className="module-section-head kitchen-column-head kitchen-column-toolbar compact-order-column-head">
                <div className="kitchen-column-copy">
                  <span className="eyebrow">{activeColumn.title}</span>
                  <strong>{activeColumn.items.length}</strong>
                  <small>{activeColumn.subtitle}</small>
                </div>

                {activeColumn.items.length > 0 ? (
                  <div className="kitchen-batch-actions">
                    {activeColumn.batchLabel ? (
                      <button
                        className={`ghost-link button-link module-action-button kitchen-batch-button ${activeColumn.requiresOwnerPassword ? "" : "module-action-button-primary"}`}
                        type="button"
                        disabled={activeColumnProcessing}
                        onClick={() => void handleColumnAction(activeColumn)}
                      >
                        {processingBatchKey === activeColumn.key ? "Atualizando..." : activeColumn.batchLabel}
                      </button>
                    ) : null}

                    {activeColumn.key === "todo" ? (
                      <button
                        className="ghost-link button-link module-action-button kitchen-batch-button module-action-button-primary"
                        type="button"
                        disabled={activeColumnProcessing}
                        onClick={() => void handleMarkColumnReady(activeColumn)}
                      >
                        {processingBatchKey === `${activeColumn.key}:ready` ? "Atualizando..." : "Marcar todos prontos"}
                      </button>
                    ) : null}
                  </div>
                ) : null}
              </div>

              <div className="field-group cash-search-field">
                <label htmlFor="kitchenOrderSearch">Buscar nesta lista</label>
                <input
                  id="kitchenOrderSearch"
                  type="search"
                  value={searchTerm}
                  onChange={(event) => setSearchTerm(event.target.value)}
                  placeholder="Ex.: Davi, Mesa 2, telefone..."
                />
              </div>

              {visibleItems.length === 0 ? (
                <div className="module-empty-state compact-empty-state">
                  <p>{hasSearch ? "Nenhum pedido encontrado." : "Sem pedidos nesta etapa."}</p>
                </div>
              ) : (
                <div className="cash-single-list kitchen-single-list">
                  {visibleItems.map((order) => (
                    <article
                      key={order.id}
                      className="cash-compact-card"
                      role="button"
                      tabIndex={0}
                      onClick={() => setSelectedOrderId(order.id)}
                      onKeyDown={(event) => {
                        if (event.key === "Enter" || event.key === " ") {
                          event.preventDefault();
                          setSelectedOrderId(order.id);
                        }
                      }}
                    >
                      <div className="cash-compact-head">
                        <div className="cash-compact-copy">
                          <h3>{renderOrderTitle(order)}</h3>
                          <p>{renderOrderSubtitle(order)}</p>
                        </div>
                        <strong className="cash-compact-amount">{formatCurrency(order.totalAmount)}</strong>
                      </div>
                      <div className="entity-meta-grid cash-compact-meta">
                        <span>{statusLabels[order.status] ?? order.status}</span>
                        <span>{formatDateTime(order.submittedAtUtc)}</span>
                        <span>{printStatusLabels[order.printStatus] ?? order.printStatus}</span>
                        {order.isDeliveryOrder ? (
                          <span>{order.fulfillmentType === "Pickup" ? "Retirada" : "Delivery"}</span>
                        ) : (
                          <span>Local</span>
                        )}
                        {order.isEdited ? <span>Editado</span> : null}
                      </div>
                    </article>
                  ))}
                </div>
              )}
            </section>
          </div>
        )}
      </section>

      {renderOrderModal()}
    </section>
  );
}
