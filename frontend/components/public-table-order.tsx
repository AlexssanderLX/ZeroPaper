"use client";

import { FormEvent, useEffect, useState } from "react";
import Link from "next/link";
import { BrandMark } from "@/components/brand-mark";
import {
  createPublicOrder,
  getPublicTable,
  type CustomerOrder,
  type MenuCategory,
  type PublicTableView,
} from "@/lib/api";
import { formatCurrency, handleApiError } from "@/components/modules/module-utils";

type MenuSelectionState = Record<string, number>;

function buildSelectionPayload(selectionState: MenuSelectionState) {
  return Object.entries(selectionState)
    .filter(([, quantity]) => quantity > 0)
    .map(([menuItemId, value]) => ({
      menuItemId,
      quantity: value,
    }));
}

export function PublicTableOrder({ publicCode }: { publicCode: string }) {
  const [table, setTable] = useState<PublicTableView | null>(null);
  const [expandedCategoryId, setExpandedCategoryId] = useState("");
  const [orderNotes, setOrderNotes] = useState("");
  const [selectionState, setSelectionState] = useState<MenuSelectionState>({});
  const [createdOrder, setCreatedOrder] = useState<CustomerOrder | null>(null);
  const [loading, setLoading] = useState(true);
  const [isSaving, setIsSaving] = useState(false);
  const [errorMessage, setErrorMessage] = useState("");

  async function loadTable() {
    setLoading(true);

    try {
      const response = await getPublicTable(publicCode);
      setTable(response);
      setExpandedCategoryId(response.menu[0]?.id ?? "");
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

  const cartItems = (() => {
    const items = new Map<string, { item: MenuCategory["items"][number]; categoryName: string }>();

    table?.menu.forEach((category) => {
      category.items.forEach((item) => {
        items.set(item.id, { item, categoryName: category.name });
      });
    });

    return Object.entries(selectionState)
      .filter(([, value]) => value > 0)
      .map(([menuItemId, value]) => {
        const resolved = items.get(menuItemId);

        if (!resolved) {
          return null;
        }

        return {
          menuItemId,
          categoryName: resolved.categoryName,
          item: resolved.item,
          quantity: value,
          totalPrice: resolved.item.price * value,
        };
      })
      .filter(Boolean) as Array<{
      menuItemId: string;
      categoryName: string;
      item: MenuCategory["items"][number];
      quantity: number;
      totalPrice: number;
    }>;
  })();

  const totalAmount = cartItems.reduce((sum, item) => sum + item.totalPrice, 0);

  function updateItemQuantity(menuItemId: string, nextQuantity: number) {
    setSelectionState((currentValue) => {
      if (nextQuantity <= 0) {
        const nextState = { ...currentValue };
        delete nextState[menuItemId];
        return nextState;
      }

      return {
        ...currentValue,
        [menuItemId]: nextQuantity,
      };
    });
  }

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    const menuSelections = buildSelectionPayload(selectionState);

    if (menuSelections.length === 0) {
      setErrorMessage("Escolha pelo menos um item para enviar o pedido.");
      return;
    }

    setIsSaving(true);

    try {
      const response = await createPublicOrder(publicCode, {
        notes: orderNotes.trim() || undefined,
        menuSelections,
      });

      setCreatedOrder(response);
      setOrderNotes("");
      setSelectionState({});
      setErrorMessage("");
    } catch (error) {
      await handleApiError(error, async () => undefined, setErrorMessage, "Nao foi possivel enviar o pedido.");
    } finally {
      setIsSaving(false);
    }
  }

  return (
    <main className="page-shell public-shell">
      <section className="surface-card public-card ambient-panel public-menu-card">
        <div className="brand-lockup compact">
          <BrandMark small />
          <div className="brand-copy">
            <span className="eyebrow">ZeroPaper</span>
            <strong>{table?.restaurantName || "Carregando..."}</strong>
          </div>
        </div>

        {loading ? (
          <p className="loading-state">Abrindo mesa...</p>
        ) : (
          <>
            <div className="public-menu-header">
              <div>
                <h1 className="public-title">{table?.tableName}</h1>
              </div>

              <div className="public-menu-total">
                <span>Total</span>
                <strong>{formatCurrency(totalAmount)}</strong>
              </div>
            </div>

            {table?.menu.length ? (
              <form className="public-menu-layout" onSubmit={handleSubmit}>
                <div className="public-menu-main">
                  <div className="public-category-stack">
                    {table.menu.map((category) => (
                      <details
                        key={category.id}
                        className="public-category-details"
                        open={expandedCategoryId === category.id}
                      >
                        <summary
                          className="public-category-summary"
                          onClick={() =>
                            setExpandedCategoryId((currentValue) => (currentValue === category.id ? "" : category.id))
                          }
                        >
                          <div>
                            <strong>{category.name}</strong>
                            <p>{category.items.length} opcoes</p>
                          </div>
                          <span className="public-category-summary-price">
                            {category.items.length > 0
                              ? `a partir de ${formatCurrency(Math.min(...category.items.map((item) => item.price)))}`
                              : "sem itens"}
                          </span>
                        </summary>

                        <div className="public-product-grid">
                          {category.items.map((item) => {
                            const quantity = selectionState[item.id] ?? 0;

                            return (
                              <article key={item.id} className={`public-product-card ${quantity > 0 ? "is-selected" : ""}`}>
                                {item.imageUrl ? (
                                  <img className="public-product-image" src={item.imageUrl} alt={item.name} loading="lazy" />
                                ) : null}

                                <div className="public-product-top">
                                  <div>
                                    {item.accentLabel ? <span className="eyebrow">{item.accentLabel}</span> : null}
                                    <h2>{item.name}</h2>
                                    {item.description ? <p>{item.description}</p> : null}
                                  </div>
                                  <strong>{formatCurrency(item.price)}</strong>
                                </div>

                                <div className="public-product-actions">
                                  <button className="ghost-link button-link" type="button" onClick={() => updateItemQuantity(item.id, Math.max(0, quantity - 1))}>
                                    -
                                  </button>
                                  <span>{quantity}</span>
                                  <button className="ghost-link button-link" type="button" onClick={() => updateItemQuantity(item.id, quantity + 1)}>
                                    +
                                  </button>
                                </div>
                              </article>
                            );
                          })}
                        </div>
                      </details>
                    ))}
                  </div>
                </div>

                <aside className="surface-card public-cart-card">
                  <div className="module-section-head">
                    <span className="eyebrow">Pedido</span>
                    <strong>{cartItems.length} itens</strong>
                  </div>

                  {cartItems.length === 0 ? (
                    <div className="module-empty-state compact-empty-state">
                      <p>Nenhum item.</p>
                    </div>
                  ) : (
                    <div className="public-cart-stack">
                      {cartItems.map((entry) => (
                        <div key={entry.menuItemId} className="public-cart-row">
                          <div>
                            <strong>{entry.item.name}</strong>
                            <p>{entry.quantity}x {formatCurrency(entry.item.price)}</p>
                          </div>
                          <strong>{formatCurrency(entry.totalPrice)}</strong>
                        </div>
                      ))}
                    </div>
                  )}

                  <div className="field-group">
                    <label htmlFor="orderNotes">Observacoes do pedido</label>
                    <input
                      id="orderNotes"
                      value={orderNotes}
                      onChange={(event) => setOrderNotes(event.target.value)}
                      placeholder="Algo geral para a cozinha ou atendimento"
                    />
                  </div>

                  <button className="primary-link button-link" type="submit" disabled={isSaving}>
                    {isSaving ? "Enviando..." : "Enviar pedido"}
                  </button>
                </aside>
              </form>
            ) : (
              <div className="module-empty-state">
                <strong>Cardapio indisponivel.</strong>
              </div>
            )}
          </>
        )}

        {errorMessage ? <p className="module-feedback error">{errorMessage}</p> : null}

        {createdOrder ? (
          <section className="surface-card public-success-card">
            <span className="eyebrow">Pedido enviado</span>
            <h2>Pedido #{createdOrder.number}</h2>
            <p>{createdOrder.items.length} itens</p>
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
