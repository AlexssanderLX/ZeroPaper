"use client";

import { useEffect, useState } from "react";
import { getOrders, updateOrderStatus, type CustomerOrder } from "@/lib/api";
import {
  handleApiError,
  type AsyncVoid,
} from "@/components/modules/module-utils";

export function KitchenModule({ token, onUnauthorized }: { token: string; onUnauthorized: AsyncVoid }) {
  const [orders, setOrders] = useState<CustomerOrder[]>([]);
  const [loading, setLoading] = useState(true);
  const [errorMessage, setErrorMessage] = useState("");

  async function loadKitchen() {
    setLoading(true);

    try {
      setOrders(await getOrders(token, true));
      setErrorMessage("");
    } catch (error) {
      await handleApiError(error, onUnauthorized, setErrorMessage, "Nao foi possivel carregar a fila da cozinha.");
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    void loadKitchen();
  }, [token]);

  async function advanceOrder(order: CustomerOrder) {
    const nextStatus =
      order.status === "Pending"
        ? "InKitchen"
        : order.status === "InKitchen"
          ? "Ready"
          : "Delivered";

    try {
      await updateOrderStatus(token, order.id, nextStatus);
      await loadKitchen();
    } catch (error) {
      await handleApiError(error, onUnauthorized, setErrorMessage, "Nao foi possivel atualizar a fila da cozinha.");
    }
  }

  return (
    <section className="surface-card module-list-card">
      <div className="module-section-head">
        <span className="eyebrow">Cozinha</span>
        <strong>{orders.length} pedidos em fila</strong>
      </div>

      {errorMessage ? <p className="module-feedback error">{errorMessage}</p> : null}

      {loading ? (
        <p className="loading-state">Carregando fila...</p>
      ) : orders.length === 0 ? (
        <div className="module-empty-state">
          <strong>Nada na fila agora.</strong>
          <p>Assim que um pedido entrar na operacao ou for enviado para cozinha, ele aparece aqui.</p>
        </div>
      ) : (
        <div className="module-card-list">
          {orders.map((order) => (
            <article key={order.id} className="module-entity-card interactive-card">
              <div className="entity-head">
                <div>
                  <h3>Pedido #{order.number}</h3>
                  <p>{order.tableName}</p>
                </div>
                <span className={`status-chip ${order.status.toLowerCase()}`}>{order.status}</span>
              </div>

              <div className="item-line-list">
                {order.items.map((item) => (
                  <p key={item.id}>
                    {item.quantity}x {item.name}
                  </p>
                ))}
              </div>

              <div className="toolbar-actions">
                <button className="primary-link button-link" type="button" onClick={() => void advanceOrder(order)}>
                  {order.status === "Pending"
                    ? "Iniciar preparo"
                    : order.status === "InKitchen"
                      ? "Marcar pronto"
                      : "Fechar pedido"}
                </button>
              </div>
            </article>
          ))}
        </div>
      )}
    </section>
  );
}
