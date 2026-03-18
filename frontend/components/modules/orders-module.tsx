"use client";

import { FormEvent, useEffect, useState } from "react";
import {
  createOrder,
  getOrders,
  getTables,
  updateOrderStatus,
  type CustomerOrder,
  type DiningTable,
} from "@/lib/api";
import {
  buildOrderPayload,
  emptyOrderDraftItem,
  formatCurrency,
  formatDateTime,
  handleApiError,
  type AsyncVoid,
  type OrderDraftItem,
} from "@/components/modules/module-utils";

export function OrdersModule({ token, onUnauthorized }: { token: string; onUnauthorized: AsyncVoid }) {
  const [tables, setTables] = useState<DiningTable[]>([]);
  const [orders, setOrders] = useState<CustomerOrder[]>([]);
  const [tableId, setTableId] = useState("");
  const [customerName, setCustomerName] = useState("");
  const [notes, setNotes] = useState("");
  const [items, setItems] = useState<OrderDraftItem[]>([emptyOrderDraftItem()]);
  const [loading, setLoading] = useState(true);
  const [isSaving, setIsSaving] = useState(false);
  const [errorMessage, setErrorMessage] = useState("");
  const [successMessage, setSuccessMessage] = useState("");

  async function loadData() {
    setLoading(true);

    try {
      const [tablesResponse, ordersResponse] = await Promise.all([getTables(token), getOrders(token)]);
      setTables(tablesResponse);
      setOrders(ordersResponse);
      setTableId((current) => current || tablesResponse[0]?.id || "");
      setErrorMessage("");
    } catch (error) {
      await handleApiError(error, onUnauthorized, setErrorMessage, "Nao foi possivel carregar os pedidos.");
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    void loadData();
  }, [token]);

  function updateItem(index: number, field: keyof OrderDraftItem, value: string) {
    setItems((current) =>
      current.map((item, itemIndex) => (itemIndex === index ? { ...item, [field]: value } : item)),
    );
  }

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setIsSaving(true);
    setSuccessMessage("");

    try {
      await createOrder(token, {
        tableId,
        customerName: customerName.trim() || undefined,
        notes: notes.trim() || undefined,
        items: buildOrderPayload(items),
      });

      setCustomerName("");
      setNotes("");
      setItems([emptyOrderDraftItem()]);
      setSuccessMessage("Pedido enviado para a operacao.");
      await loadData();
    } catch (error) {
      await handleApiError(error, onUnauthorized, setErrorMessage, "Nao foi possivel criar o pedido.");
    } finally {
      setIsSaving(false);
    }
  }

  async function handleStatusUpdate(orderId: string, status: string) {
    try {
      await updateOrderStatus(token, orderId, status);
      await loadData();
    } catch (error) {
      await handleApiError(error, onUnauthorized, setErrorMessage, "Nao foi possivel atualizar o pedido.");
    }
  }

  return (
    <section className="module-body-grid">
      <section className="surface-card module-form-card">
        <span className="eyebrow">Novo pedido</span>
        <h2>Lancar pedido da unidade</h2>
        <form className="module-form" onSubmit={handleSubmit}>
          <div className="field-group">
            <label htmlFor="orderTable">Mesa</label>
            <select id="orderTable" value={tableId} onChange={(event) => setTableId(event.target.value)} disabled={tables.length === 0}>
              {tables.length === 0 ? (
                <option value="">Crie uma mesa primeiro</option>
              ) : (
                tables.map((table) => (
                  <option key={table.id} value={table.id}>
                    {table.name}
                  </option>
                ))
              )}
            </select>
          </div>

          <div className="module-inline-grid">
            <div className="field-group">
              <label htmlFor="orderCustomer">Cliente</label>
              <input
                id="orderCustomer"
                value={customerName}
                onChange={(event) => setCustomerName(event.target.value)}
                placeholder="Nome do cliente"
              />
            </div>

            <div className="field-group">
              <label htmlFor="orderNotes">Observacoes</label>
              <input
                id="orderNotes"
                value={notes}
                onChange={(event) => setNotes(event.target.value)}
                placeholder="Sem cebola, mesa ao fundo..."
              />
            </div>
          </div>

          <div className="dynamic-item-stack">
            {items.map((item, index) => (
              <div key={`order-item-${index}`} className="dynamic-item-card">
                <div className="module-inline-grid triple">
                  <div className="field-group">
                    <label>Item</label>
                    <input value={item.name} onChange={(event) => updateItem(index, "name", event.target.value)} />
                  </div>

                  <div className="field-group">
                    <label>Qtd.</label>
                    <input
                      type="number"
                      min="1"
                      step="1"
                      value={item.quantity}
                      onChange={(event) => updateItem(index, "quantity", event.target.value)}
                    />
                  </div>

                  <div className="field-group">
                    <label>Valor</label>
                    <input
                      type="number"
                      min="0"
                      step="0.01"
                      value={item.unitPrice}
                      onChange={(event) => updateItem(index, "unitPrice", event.target.value)}
                    />
                  </div>
                </div>

                <div className="field-group">
                  <label>Observacao do item</label>
                  <input value={item.notes} onChange={(event) => updateItem(index, "notes", event.target.value)} />
                </div>
              </div>
            ))}
          </div>

          <div className="toolbar-actions">
            <button className="ghost-link button-link" type="button" onClick={() => setItems((current) => [...current, emptyOrderDraftItem()])}>
              Adicionar item
            </button>
            <button className="primary-link button-link" type="submit" disabled={isSaving || tables.length === 0}>
              {isSaving ? "Salvando..." : "Criar pedido"}
            </button>
          </div>
        </form>

        {successMessage ? <p className="module-feedback success">{successMessage}</p> : null}
        {errorMessage ? <p className="module-feedback error">{errorMessage}</p> : null}
      </section>

      <section className="surface-card module-list-card">
        <div className="module-section-head">
          <span className="eyebrow">Pedidos do dia</span>
          <strong>{orders.length} registrados</strong>
        </div>

        {loading ? (
          <p className="loading-state">Carregando pedidos...</p>
        ) : orders.length === 0 ? (
          <div className="module-empty-state">
            <strong>Nenhum pedido criado.</strong>
            <p>Os pedidos da unidade e os pedidos enviados por QR vao aparecer aqui.</p>
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

                <div className="entity-meta-grid">
                  <span>{order.customerName || "Sem nome"}</span>
                  <span>{formatDateTime(order.submittedAtUtc)}</span>
                  <span>{formatCurrency(order.totalAmount)}</span>
                </div>

                <div className="item-line-list">
                  {order.items.map((item) => {
                    const itemLabel = `${item.quantity}x ${item.name} - ${formatCurrency(item.totalPrice)}`;
                    return <p key={item.id}>{itemLabel}</p>;
                  })}
                </div>

                <div className="toolbar-actions">
                  {order.status === "Pending" ? (
                    <button className="ghost-link button-link" type="button" onClick={() => void handleStatusUpdate(order.id, "InKitchen")}>
                      Enviar para cozinha
                    </button>
                  ) : null}
                  {order.status !== "Delivered" && order.status !== "Cancelled" ? (
                    <button className="ghost-link button-link" type="button" onClick={() => void handleStatusUpdate(order.id, "Cancelled")}>
                      Cancelar
                    </button>
                  ) : null}
                </div>
              </article>
            ))}
          </div>
        )}
      </section>
    </section>
  );
}
