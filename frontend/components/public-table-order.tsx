"use client";

import { FormEvent, useEffect, useState } from "react";
import Link from "next/link";
import { createPublicOrder, getPublicTable, type CustomerOrder, type PublicTableView } from "@/lib/api";
import {
  buildOrderPayload,
  emptyOrderDraftItem,
  formatCurrency,
  handleApiError,
  type OrderDraftItem,
} from "@/components/modules/module-utils";

export function PublicTableOrder({ publicCode }: { publicCode: string }) {
  const [table, setTable] = useState<PublicTableView | null>(null);
  const [customerName, setCustomerName] = useState("");
  const [notes, setNotes] = useState("");
  const [items, setItems] = useState<OrderDraftItem[]>([emptyOrderDraftItem()]);
  const [createdOrder, setCreatedOrder] = useState<CustomerOrder | null>(null);
  const [loading, setLoading] = useState(true);
  const [isSaving, setIsSaving] = useState(false);
  const [errorMessage, setErrorMessage] = useState("");

  async function loadTable() {
    setLoading(true);

    try {
      setTable(await getPublicTable(publicCode));
      setErrorMessage("");
    } catch (error) {
      await handleApiError(error, async () => undefined, setErrorMessage, "Nao foi possivel abrir a mesa.");
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    void loadTable();
  }, [publicCode]);

  function updateItem(index: number, field: keyof OrderDraftItem, value: string) {
    setItems((current) =>
      current.map((item, itemIndex) => (itemIndex === index ? { ...item, [field]: value } : item)),
    );
  }

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setIsSaving(true);

    try {
      const response = await createPublicOrder(publicCode, {
        customerName: customerName.trim() || undefined,
        notes: notes.trim() || undefined,
        items: buildOrderPayload(items),
      });

      setCreatedOrder(response);
      setCustomerName("");
      setNotes("");
      setItems([emptyOrderDraftItem()]);
      setErrorMessage("");
    } catch (error) {
      await handleApiError(error, async () => undefined, setErrorMessage, "Nao foi possivel enviar o pedido.");
    } finally {
      setIsSaving(false);
    }
  }

  return (
    <main className="page-shell public-shell">
      <section className="surface-card public-card ambient-panel">
        <div className="brand-lockup compact">
          <div className="brand-mark small" aria-hidden="true">
            <span>Z</span>
            <span>P</span>
          </div>
          <div className="brand-copy">
            <span className="eyebrow">ZeroPaper</span>
            <strong>{table?.restaurantName || "Carregando..."}</strong>
          </div>
        </div>

        {loading ? (
          <p className="loading-state">Abrindo mesa...</p>
        ) : (
          <>
            <h1 className="public-title">{table?.tableName}</h1>
            <p className="body-copy">Envie o pedido direto para a operacao da casa.</p>

            <form className="module-form" onSubmit={handleSubmit}>
              <div className="module-inline-grid">
                <div className="field-group">
                  <label>Nome</label>
                  <input value={customerName} onChange={(event) => setCustomerName(event.target.value)} placeholder="Seu nome" />
                </div>
                <div className="field-group">
                  <label>Observacoes</label>
                  <input value={notes} onChange={(event) => setNotes(event.target.value)} placeholder="Sem gelo, bem passado..." />
                </div>
              </div>

              <div className="dynamic-item-stack">
                {items.map((item, index) => (
                  <div key={`public-item-${index}`} className="dynamic-item-card">
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
                  </div>
                ))}
              </div>

              <div className="toolbar-actions">
                <button className="ghost-link button-link" type="button" onClick={() => setItems((current) => [...current, emptyOrderDraftItem()])}>
                  Adicionar item
                </button>
                <button className="primary-link button-link" type="submit" disabled={isSaving}>
                  {isSaving ? "Enviando..." : "Enviar pedido"}
                </button>
              </div>
            </form>
          </>
        )}

        {errorMessage ? <p className="module-feedback error">{errorMessage}</p> : null}

        {createdOrder ? (
          <section className="surface-card public-success-card">
            <span className="eyebrow">Pedido enviado</span>
            <h2>Pedido #{createdOrder.number}</h2>
            <p>{createdOrder.items.length} itens enviados para a operacao.</p>
            <p>{formatCurrency(createdOrder.totalAmount)}</p>
            <Link className="ghost-link" href={`/q/${publicCode}`}>
              Fazer novo pedido
            </Link>
          </section>
        ) : null}
      </section>
    </main>
  );
}
