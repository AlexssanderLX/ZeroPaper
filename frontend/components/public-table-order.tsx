"use client";

import { FormEvent, useEffect, useState } from "react";
import { BrandMark } from "@/components/brand-mark";
import {
  createPublicOrder,
  getPublicTable,
  type CustomerOrder,
  type MenuCategory,
  type PublicTableView,
} from "@/lib/api";
import { formatCurrency, formatPaymentMethod, handleApiError } from "@/components/modules/module-utils";

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
  const [paymentMethod, setPaymentMethod] = useState("Pix");
  const [selectionState, setSelectionState] = useState<MenuSelectionState>({});
  const [createdOrder, setCreatedOrder] = useState<CustomerOrder | null>(null);
  const [loading, setLoading] = useState(true);
  const [isSaving, setIsSaving] = useState(false);
  const [errorMessage, setErrorMessage] = useState("");
  const visibleCategories = (table?.menu ?? []).filter((category) => category.items.length > 0);

  async function loadTable() {
    setLoading(true);

    try {
      const response = await getPublicTable(publicCode);
      setTable(response);
      setExpandedCategoryId("");
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

  useEffect(() => {
    if (!createdOrder) {
      return;
    }

    const timeoutId = window.setTimeout(() => {
      resetForNewOrder();
    }, 12000);

    return () => window.clearTimeout(timeoutId);
  }, [createdOrder, visibleCategories]);

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
  const totalUnits = cartItems.reduce((sum, item) => sum + item.quantity, 0);

  function handleExpandCategory(categoryId: string) {
    setExpandedCategoryId(categoryId);
  }

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

  function resetForNewOrder() {
    setCreatedOrder(null);
    setOrderNotes("");
    setPaymentMethod("Pix");
    setSelectionState({});
    setExpandedCategoryId("");
    setErrorMessage("");
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
        paymentMethod,
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
        <div className="brand-lockup compact public-brand-lockup">
          <BrandMark small variant="full" />
          <div className="brand-copy">
            <span className="eyebrow">ZeroPaper</span>
            <strong className="public-restaurant-name">{table?.restaurantName || "Carregando..."}</strong>
          </div>
        </div>

        {loading ? (
          <p className="loading-state">Abrindo mesa...</p>
        ) : (
          <>
            <div className="public-table-header">
              <span className="eyebrow">Mesa</span>
              <h1 className="public-title">{table?.tableName}</h1>
            </div>

            {createdOrder ? (
              <section className="surface-card public-success-card public-order-complete">
                <span className="eyebrow">Pedido enviado</span>
                <h2>Obrigado.</h2>
                <p>Pedido #{createdOrder.number} enviado para a cozinha.</p>

                <div className="public-success-summary-grid">
                  <div className="public-success-stat">
                    <span>Total</span>
                    <strong>{formatCurrency(createdOrder.totalAmount)}</strong>
                  </div>
                  <div className="public-success-stat">
                    <span>Pagamento</span>
                    <strong>{formatPaymentMethod(createdOrder.paymentMethod)}</strong>
                  </div>
                </div>

                <p className="public-cash-note">Acerto no caixa.</p>
                <p>Deseja fazer um novo pedido?</p>
                <p className="field-hint">Essa confirmacao fecha sozinha em alguns segundos.</p>

                <div className="toolbar-actions public-success-actions">
                  <button className="primary-link button-link" type="button" onClick={resetForNewOrder}>
                    Fazer novo pedido
                  </button>
                </div>
              </section>
            ) : visibleCategories.length ? (
              <form className="public-menu-layout" onSubmit={handleSubmit}>
                <div className="public-menu-main">
                  <div className="public-category-stack">
                    {visibleCategories.map((category) => (
                      <section
                        key={category.id}
                        className={`public-category-details ${expandedCategoryId === category.id ? "is-open" : ""}`}
                      >
                        <button
                          className="public-category-header-button"
                          type="button"
                          onClick={() => handleExpandCategory(category.id)}
                          aria-expanded={expandedCategoryId === category.id}
                        >
                          <div className="public-category-summary">
                            <div>
                              <strong>{category.name}</strong>
                              <p>{category.items.length} itens</p>
                            </div>
                            <div className="public-category-summary-meta">
                              <span className="public-category-summary-price">
                                {category.items.length > 0
                                  ? `a partir de ${formatCurrency(Math.min(...category.items.map((item) => item.price)))}`
                                  : "sem itens"}
                              </span>
                              <span className="public-category-summary-arrow" aria-hidden="true">
                                {expandedCategoryId === category.id ? "−" : "+"}
                              </span>
                            </div>
                          </div>
                        </button>

                        {expandedCategoryId === category.id ? (
                          <div className="public-category-content">
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
                          </div>
                        ) : null}
                      </section>
                    ))}
                  </div>
                </div>

                <aside className="surface-card public-cart-card">
                  <div className="module-section-head">
                    <span className="eyebrow">Pedido</span>
                    <strong>{totalUnits > 0 ? `${totalUnits} itens` : "Nenhum item"}</strong>
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

                  <div className="field-group">
                    <label>Forma de pagamento</label>
                    <div className="choice-pill-row">
                      {[
                        { value: "Pix", label: "Pix" },
                        { value: "Credit", label: "Credito" },
                        { value: "Debit", label: "Debito" },
                      ].map((option) => (
                        <button
                          key={option.value}
                          className={`ghost-link button-link choice-pill ${paymentMethod === option.value ? "is-selected-filter" : ""}`}
                          type="button"
                          onClick={() => setPaymentMethod(option.value)}
                        >
                          {option.label}
                        </button>
                      ))}
                    </div>
                  </div>

                  <p className="field-hint public-cash-note">Acerto no caixa.</p>

                  <div className="public-cart-submit-row">
                    <div className="public-cart-total-box">
                      <span>Total</span>
                      <strong>{formatCurrency(totalAmount)}</strong>
                    </div>

                    <button className="primary-link button-link" type="submit" disabled={isSaving}>
                      {isSaving ? "Enviando..." : "Enviar pedido"}
                    </button>
                  </div>
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
      </section>
    </main>
  );
}
