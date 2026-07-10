"use client";

import { useEffect, useMemo, useState } from "react";
import { getOrders, type CustomerOrder } from "@/lib/api";
import {
  formatCurrency,
  formatDateTime,
  formatPaymentMethod,
  formatPaymentStatus,
  handleApiError,
  type AsyncVoid,
} from "@/components/modules/module-utils";
import { OrderItemsCompact } from "@/components/modules/order-items-compact";

function sortByNewestFirst(orders: CustomerOrder[]) {
  return [...orders].sort(
    (leftOrder, rightOrder) => new Date(rightOrder.submittedAtUtc).getTime() - new Date(leftOrder.submittedAtUtc).getTime(),
  );
}

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

function renderOrderTitle(order: CustomerOrder) {
  if (order.fulfillmentType === "Pickup") {
    return order.customerName ? `Retirada de ${order.customerName}` : "Retirada";
  }

  if (order.isDeliveryOrder) {
    return order.customerName ? `Delivery de ${order.customerName}` : "Delivery";
  }

  return order.tableName || "Pedido local";
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

export function FinishedOrdersModule({
  token,
  onUnauthorized,
}: {
  token: string;
  onUnauthorized: AsyncVoid;
}) {
  const [orders, setOrders] = useState<CustomerOrder[]>([]);
  const [loading, setLoading] = useState(true);
  const [errorMessage, setErrorMessage] = useState("");

  useEffect(() => {
    let isMounted = true;

    void (async () => {
      try {
        const response = await getOrders(token);

        if (!isMounted) {
          return;
        }

        setOrders(response);
        setErrorMessage("");
      } catch (error) {
        if (!isMounted) {
          return;
        }

        await handleApiError(error, onUnauthorized, setErrorMessage, "Nao foi possivel carregar os pedidos finalizados.");
      } finally {
        if (isMounted) {
          setLoading(false);
        }
      }
    })();

    return () => {
      isMounted = false;
    };
  }, [onUnauthorized, token]);

  const finishedOrders = useMemo(
    () => sortByNewestFirst(orders.filter((order) => order.status === "Delivered")),
    [orders],
  );
  const finishedTotal = useMemo(
    () => finishedOrders.reduce((total, order) => total + order.totalAmount, 0),
    [finishedOrders],
  );

  return (
    <section className="menu-workspace">
      {errorMessage ? <p className="module-feedback error">{errorMessage}</p> : null}

      <section className="surface-card module-list-card">
        <div className="module-section-head compact-order-panel-head">
          <div>
            <h2>Pedidos finalizados</h2>
            <p>Consulta rapida dos pedidos encerrados. Esta tela nao devolve pedido para a cozinha.</p>
          </div>
          <div className="cash-summary-pills order-summary-pills">
            <span className="cash-summary-pill">
              <small>Finalizados</small>
              <strong>{finishedOrders.length}</strong>
              <em>{formatCurrency(finishedTotal)}</em>
            </span>
          </div>
        </div>

        {loading ? (
          <p className="loading-state">Carregando finalizados...</p>
        ) : finishedOrders.length === 0 ? (
          <div className="module-empty-state compact-empty-state">
            <strong>Nenhum pedido finalizado agora.</strong>
            <p>Quando o pedido for encerrado pelo fluxo de Meus pedidos, ele aparece aqui apenas para conferencia.</p>
          </div>
        ) : (
          <div className="module-card-list kitchen-single-list">
            {finishedOrders.map((order) => (
              <article key={order.id} className="module-entity-card interactive-card kitchen-order-card compact-order-card">
                <div className="entity-head">
                  <div>
                    <h3>{renderOrderTitle(order)}</h3>
                    <p>{renderOrderSubtitle(order)}</p>
                  </div>
                  <span className="status-chip ready">Finalizado</span>
                </div>

                <div className="entity-meta-grid">
                  <span>{formatDateTime(order.submittedAtUtc)}</span>
                  <span>{formatCurrency(order.totalAmount)}</span>
                  <span>{formatPaymentMethod(order.paymentMethod)}</span>
                  <span>{formatPaymentStatus(order.paymentStatus)}</span>
                  {order.isDeliveryOrder ? <span>Delivery</span> : null}
                  {order.customerName ? <span>{order.customerName}</span> : null}
                </div>

                <OrderItemsCompact items={order.items} />

                {buildDeliverySummary(order).length > 0 ? (
                  <div className="module-empty-state compact-empty-state">
                    <strong>Entrega</strong>
                    {buildDeliverySummary(order).map((line) => (
                      <p key={line}>{line}</p>
                    ))}
                  </div>
                ) : null}

                <div className="module-empty-state compact-empty-state finished-order-readonly">
                  <strong>Consulta</strong>
                  <p>Pedido encerrado. Se precisar corrigir operacao, trate pelo fluxo administrativo adequado, nao pela cozinha.</p>
                </div>
              </article>
            ))}
          </div>
        )}
      </section>
    </section>
  );
}
