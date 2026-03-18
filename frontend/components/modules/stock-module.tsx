"use client";

import { FormEvent, useEffect, useState } from "react";
import { createStockItem, getStockItems, updateStockItem, type StockItem } from "@/lib/api";
import {
  emptyStockDraft,
  handleApiError,
  type AsyncVoid,
  type StockDraft,
} from "@/components/modules/module-utils";

export function StockModule({ token, onUnauthorized }: { token: string; onUnauthorized: AsyncVoid }) {
  const [items, setItems] = useState<StockItem[]>([]);
  const [drafts, setDrafts] = useState<Record<string, StockDraft>>({});
  const [createDraft, setCreateDraft] = useState<StockDraft>(emptyStockDraft());
  const [loading, setLoading] = useState(true);
  const [errorMessage, setErrorMessage] = useState("");
  const [successMessage, setSuccessMessage] = useState("");

  async function loadStock() {
    setLoading(true);

    try {
      const response = await getStockItems(token);
      setItems(response);
      setDrafts(
        Object.fromEntries(
          response.map((item) => [
            item.id,
            {
              name: item.name,
              category: item.category,
              unit: item.unit,
              currentQuantity: String(item.currentQuantity),
              minimumQuantity: String(item.minimumQuantity),
            },
          ]),
        ),
      );
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

  async function handleCreate(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
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
      setSuccessMessage("Item criado no estoque.");
      await loadStock();
    } catch (error) {
      await handleApiError(error, onUnauthorized, setErrorMessage, "Nao foi possivel criar o item.");
    }
  }

  async function handleSave(itemId: string) {
    const draft = drafts[itemId];

    try {
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
    }
  }

  return (
    <section className="module-body-grid">
      <section className="surface-card module-form-card">
        <span className="eyebrow">Novo item</span>
        <h2>Adicionar insumo</h2>
        <form className="module-form" onSubmit={handleCreate}>
          <div className="module-inline-grid">
            <div className="field-group">
              <label>Nome</label>
              <input
                value={createDraft.name}
                onChange={(event) => setCreateDraft((current) => ({ ...current, name: event.target.value }))}
              />
            </div>
            <div className="field-group">
              <label>Categoria</label>
              <input
                value={createDraft.category}
                onChange={(event) => setCreateDraft((current) => ({ ...current, category: event.target.value }))}
              />
            </div>
          </div>

          <div className="module-inline-grid triple">
            <div className="field-group">
              <label>Unidade</label>
              <input
                value={createDraft.unit}
                onChange={(event) => setCreateDraft((current) => ({ ...current, unit: event.target.value }))}
              />
            </div>
            <div className="field-group">
              <label>Atual</label>
              <input
                type="number"
                step="0.01"
                value={createDraft.currentQuantity}
                onChange={(event) => setCreateDraft((current) => ({ ...current, currentQuantity: event.target.value }))}
              />
            </div>
            <div className="field-group">
              <label>Minimo</label>
              <input
                type="number"
                step="0.01"
                value={createDraft.minimumQuantity}
                onChange={(event) => setCreateDraft((current) => ({ ...current, minimumQuantity: event.target.value }))}
              />
            </div>
          </div>

          <button className="primary-link button-link" type="submit">
            Criar item
          </button>
        </form>

        {successMessage ? <p className="module-feedback success">{successMessage}</p> : null}
        {errorMessage ? <p className="module-feedback error">{errorMessage}</p> : null}
      </section>

      <section className="surface-card module-list-card">
        <div className="module-section-head">
          <span className="eyebrow">Estoque</span>
          <strong>{items.length} itens cadastrados</strong>
        </div>

        {loading ? (
          <p className="loading-state">Carregando estoque...</p>
        ) : items.length === 0 ? (
          <div className="module-empty-state">
            <strong>Nenhum insumo cadastrado.</strong>
            <p>Crie os itens principais da operacao para acompanhar reposicao e nivel minimo.</p>
          </div>
        ) : (
          <div className="module-card-list">
            {items.map((item) => (
              <article key={item.id} className="module-entity-card interactive-card">
                <div className="entity-head">
                  <div>
                    <h3>{item.name}</h3>
                    <p>{item.category}</p>
                  </div>
                  <span className={`status-chip ${item.isLowStock ? "warning" : "available"}`}>
                    {item.isLowStock ? "Reposicao" : "Regular"}
                  </span>
                </div>

                <div className="module-inline-grid triple">
                  <div className="field-group">
                    <label>Atual</label>
                    <input
                      type="number"
                      step="0.01"
                      value={drafts[item.id]?.currentQuantity ?? ""}
                      onChange={(event) =>
                        setDrafts((current) => ({
                          ...current,
                          [item.id]: { ...current[item.id], currentQuantity: event.target.value },
                        }))
                      }
                    />
                  </div>
                  <div className="field-group">
                    <label>Minimo</label>
                    <input
                      type="number"
                      step="0.01"
                      value={drafts[item.id]?.minimumQuantity ?? ""}
                      onChange={(event) =>
                        setDrafts((current) => ({
                          ...current,
                          [item.id]: { ...current[item.id], minimumQuantity: event.target.value },
                        }))
                      }
                    />
                  </div>
                  <div className="field-group">
                    <label>Unidade</label>
                    <input
                      value={drafts[item.id]?.unit ?? ""}
                      onChange={(event) =>
                        setDrafts((current) => ({
                          ...current,
                          [item.id]: { ...current[item.id], unit: event.target.value },
                        }))
                      }
                    />
                  </div>
                </div>

                <div className="toolbar-actions">
                  <button className="ghost-link button-link" type="button" onClick={() => void handleSave(item.id)}>
                    Salvar ajuste
                  </button>
                </div>
              </article>
            ))}
          </div>
        )}
      </section>
    </section>
  );
}
