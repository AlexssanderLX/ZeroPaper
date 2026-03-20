"use client";

import { FormEvent, useEffect, useState } from "react";
import { createStockItem, getStockItems, updateStockItem, type StockItem } from "@/lib/api";
import {
  emptyStockDraft,
  handleApiError,
  type AsyncVoid,
  type StockDraft,
} from "@/components/modules/module-utils";

function buildStockDraftMap(items: StockItem[]) {
  return Object.fromEntries(
    items.map((item) => [
      item.id,
      {
        name: item.name,
        category: item.category,
        unit: item.unit,
        currentQuantity: String(item.currentQuantity),
        minimumQuantity: String(item.minimumQuantity),
      },
    ]),
  ) as Record<string, StockDraft>;
}

export function StockModule({ token, onUnauthorized }: { token: string; onUnauthorized: AsyncVoid }) {
  const [items, setItems] = useState<StockItem[]>([]);
  const [drafts, setDrafts] = useState<Record<string, StockDraft>>({});
  const [createDraft, setCreateDraft] = useState<StockDraft>(emptyStockDraft());
  const [loading, setLoading] = useState(true);
  const [isSavingCreate, setIsSavingCreate] = useState(false);
  const [savingItemId, setSavingItemId] = useState("");
  const [errorMessage, setErrorMessage] = useState("");
  const [successMessage, setSuccessMessage] = useState("");

  async function loadStock() {
    setLoading(true);

    try {
      const response = await getStockItems(token);
      setItems(response);
      setDrafts(buildStockDraftMap(response));
      setErrorMessage("");
    } catch (error) {
      await handleApiError(error, onUnauthorized, setErrorMessage, "Nao foi possivel carregar o estoque.");
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    void loadStock();
  }, [token]);

  function updateCreateDraft(field: keyof StockDraft, value: string) {
    setCreateDraft((current) => ({
      ...current,
      [field]: value,
    }));
  }

  function updateItemDraft(itemId: string, field: keyof StockDraft, value: string) {
    setDrafts((current) => ({
      ...current,
      [itemId]: {
        ...current[itemId],
        [field]: value,
      },
    }));
  }

  async function handleCreate(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setIsSavingCreate(true);
    setSuccessMessage("");

    try {
      await createStockItem(token, {
        name: createDraft.name,
        category: createDraft.category,
        unit: createDraft.unit,
        currentQuantity: Number(createDraft.currentQuantity),
        minimumQuantity: Number(createDraft.minimumQuantity),
      });

      setCreateDraft(emptyStockDraft());
      setSuccessMessage("Item criado.");
      await loadStock();
    } catch (error) {
      await handleApiError(error, onUnauthorized, setErrorMessage, "Nao foi possivel criar o item.");
    } finally {
      setIsSavingCreate(false);
    }
  }

  async function handleSave(itemId: string) {
    const draft = drafts[itemId];

    try {
      setSavingItemId(itemId);
      setSuccessMessage("");
      await updateStockItem(token, itemId, {
        name: draft.name,
        category: draft.category,
        unit: draft.unit,
        currentQuantity: Number(draft.currentQuantity),
        minimumQuantity: Number(draft.minimumQuantity),
      });

      setSuccessMessage("Estoque atualizado.");
      await loadStock();
    } catch (error) {
      await handleApiError(error, onUnauthorized, setErrorMessage, "Nao foi possivel atualizar o item.");
    } finally {
      setSavingItemId("");
    }
  }

  return (
    <section className="menu-workspace stock-workspace">
      <section className="surface-card module-form-card stock-create-card">
        <span className="eyebrow">Novo item</span>
        <h2>Adicionar ao estoque</h2>

        <form className="module-form" onSubmit={handleCreate}>
          <div className="module-inline-grid">
            <div className="field-group">
              <label htmlFor="stockName">Nome</label>
              <input
                id="stockName"
                value={createDraft.name}
                onChange={(event) => updateCreateDraft("name", event.target.value)}
                placeholder="Cebola roxa"
              />
            </div>

            <div className="field-group">
              <label htmlFor="stockCategory">Categoria</label>
              <input
                id="stockCategory"
                value={createDraft.category}
                onChange={(event) => updateCreateDraft("category", event.target.value)}
                placeholder="Hortifruti"
              />
            </div>
          </div>

          <div className="module-inline-grid triple">
            <div className="field-group">
              <label htmlFor="stockUnit">Unidade</label>
              <input
                id="stockUnit"
                value={createDraft.unit}
                onChange={(event) => updateCreateDraft("unit", event.target.value)}
                placeholder="kg"
              />
            </div>

            <div className="field-group">
              <label htmlFor="stockCurrentQuantity">Atual</label>
              <input
                id="stockCurrentQuantity"
                type="number"
                min="0"
                step="0.01"
                value={createDraft.currentQuantity}
                onChange={(event) => updateCreateDraft("currentQuantity", event.target.value)}
              />
            </div>

            <div className="field-group">
              <label htmlFor="stockMinimumQuantity">Minimo</label>
              <input
                id="stockMinimumQuantity"
                type="number"
                min="0"
                step="0.01"
                value={createDraft.minimumQuantity}
                onChange={(event) => updateCreateDraft("minimumQuantity", event.target.value)}
              />
            </div>
          </div>

          <button className="primary-link button-link" type="submit" disabled={isSavingCreate}>
            {isSavingCreate ? "Salvando..." : "Adicionar item"}
          </button>
        </form>
      </section>

      {successMessage ? <p className="module-feedback success">{successMessage}</p> : null}
      {errorMessage ? <p className="module-feedback error">{errorMessage}</p> : null}

      <section className="surface-card module-list-card">
        <div className="module-section-head">
          <span className="eyebrow">Estoque</span>
          <strong>{items.length} itens</strong>
        </div>

        {loading ? (
          <p className="loading-state">Carregando estoque...</p>
        ) : items.length === 0 ? (
          <div className="module-empty-state">
            <strong>Nenhum item.</strong>
          </div>
        ) : (
          <div className="menu-category-stack stock-item-stack">
            {items.map((item) => {
              const draft = drafts[item.id];

              return (
                <article key={item.id} className="module-entity-card interactive-card stock-item-card">
                  <div className="entity-head">
                    <div>
                      <h3>{draft?.name || item.name}</h3>
                      <p>{draft?.category || item.category}</p>
                    </div>

                    <span className={`status-chip ${item.isLowStock ? "warning" : "available"}`}>
                      {item.isLowStock ? "Reposicao" : "Regular"}
                    </span>
                  </div>

                  <div className="module-inline-grid">
                    <div className="field-group">
                      <label>Nome</label>
                      <input
                        value={draft?.name ?? ""}
                        onChange={(event) => updateItemDraft(item.id, "name", event.target.value)}
                      />
                    </div>

                    <div className="field-group">
                      <label>Categoria</label>
                      <input
                        value={draft?.category ?? ""}
                        onChange={(event) => updateItemDraft(item.id, "category", event.target.value)}
                      />
                    </div>
                  </div>

                  <div className="module-inline-grid triple">
                    <div className="field-group">
                      <label>Atual</label>
                      <input
                        type="number"
                        min="0"
                        step="0.01"
                        value={draft?.currentQuantity ?? ""}
                        onChange={(event) => updateItemDraft(item.id, "currentQuantity", event.target.value)}
                      />
                    </div>

                    <div className="field-group">
                      <label>Minimo</label>
                      <input
                        type="number"
                        min="0"
                        step="0.01"
                        value={draft?.minimumQuantity ?? ""}
                        onChange={(event) => updateItemDraft(item.id, "minimumQuantity", event.target.value)}
                      />
                    </div>

                    <div className="field-group">
                      <label>Unidade</label>
                      <input
                        value={draft?.unit ?? ""}
                        onChange={(event) => updateItemDraft(item.id, "unit", event.target.value)}
                      />
                    </div>
                  </div>

                  <div className="toolbar-actions compact table-card-actions">
                    <button
                      className="ghost-link button-link"
                      type="button"
                      disabled={savingItemId === item.id}
                      onClick={() => void handleSave(item.id)}
                    >
                      {savingItemId === item.id ? "Salvando..." : "Salvar"}
                    </button>
                  </div>
                </article>
              );
            })}
          </div>
        )}
      </section>
    </section>
  );
}
