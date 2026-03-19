"use client";

import { FormEvent, useEffect, useMemo, useState } from "react";
import {
  createMenuCategory,
  createMenuItem,
  getMenu,
  type MenuCategory,
} from "@/lib/api";
import { handleApiError, type AsyncVoid } from "@/components/modules/module-utils";

export function MenuModule({ token, onUnauthorized }: { token: string; onUnauthorized: AsyncVoid }) {
  const [categories, setCategories] = useState<MenuCategory[]>([]);
  const [selectedCategoryId, setSelectedCategoryId] = useState("");
  const [categoryName, setCategoryName] = useState("");
  const [itemName, setItemName] = useState("");
  const [itemDescription, setItemDescription] = useState("");
  const [itemAccentLabel, setItemAccentLabel] = useState("");
  const [itemPrice, setItemPrice] = useState("0");
  const [loading, setLoading] = useState(true);
  const [isSavingCategory, setIsSavingCategory] = useState(false);
  const [isSavingItem, setIsSavingItem] = useState(false);
  const [errorMessage, setErrorMessage] = useState("");
  const [successMessage, setSuccessMessage] = useState("");

  async function loadMenu() {
    setLoading(true);

    try {
      const response = await getMenu(token);
      setCategories(response);
      setSelectedCategoryId((currentValue) => {
        if (currentValue && response.some((item) => item.id === currentValue)) {
          return currentValue;
        }

        return response[0]?.id ?? "";
      });
      setErrorMessage("");
    } catch (error) {
      await handleApiError(error, onUnauthorized, setErrorMessage, "Nao foi possivel carregar o cardapio.");
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    void loadMenu();
  }, [token]);

  const selectedCategory = useMemo(
    () => categories.find((category) => category.id === selectedCategoryId) ?? categories[0] ?? null,
    [categories, selectedCategoryId],
  );

  async function handleCreateCategory(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setIsSavingCategory(true);
    setSuccessMessage("");

    try {
      const createdCategory = await createMenuCategory(token, {
        name: categoryName,
      });

      setCategoryName("");
      setSelectedCategoryId(createdCategory.id);
      setSuccessMessage("Categoria criada.");
      await loadMenu();
    } catch (error) {
      await handleApiError(error, onUnauthorized, setErrorMessage, "Nao foi possivel criar a categoria.");
    } finally {
      setIsSavingCategory(false);
    }
  }

  async function handleCreateItem(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    if (!selectedCategoryId) {
      setErrorMessage("Crie ou selecione uma categoria antes de adicionar itens.");
      return;
    }

    setIsSavingItem(true);
    setSuccessMessage("");

    try {
      await createMenuItem(token, {
        categoryId: selectedCategoryId,
        name: itemName,
        description: itemDescription || undefined,
        accentLabel: itemAccentLabel || undefined,
        price: Number(itemPrice),
      });

      setItemName("");
      setItemDescription("");
      setItemAccentLabel("");
      setItemPrice("0");
      setSuccessMessage("Item adicionado ao cardapio.");
      await loadMenu();
    } catch (error) {
      await handleApiError(error, onUnauthorized, setErrorMessage, "Nao foi possivel adicionar o item.");
    } finally {
      setIsSavingItem(false);
    }
  }

  return (
    <section className="menu-workspace">
      <section className="menu-builder-grid">
        <section className="surface-card module-form-card">
          <span className="eyebrow">Categorias</span>
          <h2>Organize o cardapio</h2>
          <form className="module-form" onSubmit={handleCreateCategory}>
            <div className="field-group">
              <label htmlFor="categoryName">Nome da categoria</label>
              <input
                id="categoryName"
                value={categoryName}
                onChange={(event) => setCategoryName(event.target.value)}
                placeholder="Hamburgueres"
              />
            </div>

            <button className="primary-link button-link" type="submit" disabled={isSavingCategory}>
              {isSavingCategory ? "Criando..." : "Criar categoria"}
            </button>
          </form>
        </section>

        <section className="surface-card module-form-card">
          <span className="eyebrow">Itens</span>
          <h2>Adicionar ao cardapio</h2>
          <form className="module-form" onSubmit={handleCreateItem}>
            <div className="field-group">
              <label htmlFor="itemCategory">Categoria</label>
              <select
                id="itemCategory"
                value={selectedCategoryId}
                onChange={(event) => setSelectedCategoryId(event.target.value)}
              >
                <option value="">Selecione</option>
                {categories.map((category) => (
                  <option key={category.id} value={category.id}>
                    {category.name}
                  </option>
                ))}
              </select>
            </div>

            <div className="field-group">
              <label htmlFor="itemName">Nome do item</label>
              <input id="itemName" value={itemName} onChange={(event) => setItemName(event.target.value)} placeholder="Classic Burger" />
            </div>

            <div className="field-group">
              <label htmlFor="itemDescription">Descricao</label>
              <input
                id="itemDescription"
                value={itemDescription}
                onChange={(event) => setItemDescription(event.target.value)}
                placeholder="Pao brioche, burger e queijo"
              />
            </div>

            <div className="module-inline-grid">
              <div className="field-group">
                <label htmlFor="itemAccentLabel">Selo</label>
                <input
                  id="itemAccentLabel"
                  value={itemAccentLabel}
                  onChange={(event) => setItemAccentLabel(event.target.value)}
                  placeholder="Mais pedido"
                />
              </div>
              <div className="field-group">
                <label htmlFor="itemPrice">Preco</label>
                <input
                  id="itemPrice"
                  type="number"
                  min="0"
                  step="0.01"
                  value={itemPrice}
                  onChange={(event) => setItemPrice(event.target.value)}
                />
              </div>
            </div>

            <button className="primary-link button-link" type="submit" disabled={isSavingItem}>
              {isSavingItem ? "Adicionando..." : "Adicionar item"}
            </button>
          </form>
        </section>
      </section>

      {successMessage ? <p className="module-feedback success">{successMessage}</p> : null}
      {errorMessage ? <p className="module-feedback error">{errorMessage}</p> : null}

      <section className="surface-card module-list-card">
        <div className="module-section-head">
          <span className="eyebrow">Cardapio</span>
          <strong>{categories.length} categorias</strong>
        </div>

        {loading ? (
          <p className="loading-state">Carregando cardapio...</p>
        ) : categories.length === 0 ? (
          <div className="module-empty-state">
            <strong>Nenhuma categoria criada.</strong>
          </div>
        ) : (
          <div className="menu-category-stack">
            {categories.map((category) => (
              <article key={category.id} className={`module-entity-card interactive-card ${selectedCategory?.id === category.id ? "is-selected" : ""}`}>
                <div className="entity-head">
                  <div>
                    <h3>{category.name}</h3>
                    <p>{category.items.length} itens</p>
                  </div>
                  <button className="ghost-link button-link" type="button" onClick={() => setSelectedCategoryId(category.id)}>
                    Selecionar
                  </button>
                </div>

                {category.items.length === 0 ? (
                  <div className="module-empty-state compact-empty-state">
                    <p>Sem itens.</p>
                  </div>
                ) : (
                  <div className="menu-item-stack">
                    {category.items.map((item) => (
                      <div key={item.id} className="menu-item-row">
                        <div>
                          <strong>{item.name}</strong>
                          <p>{item.description || "Sem descricao"}</p>
                        </div>
                        <div className="menu-item-meta">
                          {item.accentLabel ? <span className="status-chip pending">{item.accentLabel}</span> : null}
                          <strong>{item.price.toLocaleString("pt-BR", { style: "currency", currency: "BRL" })}</strong>
                        </div>
                      </div>
                    ))}
                  </div>
                )}
              </article>
            ))}
          </div>
        )}
      </section>
    </section>
  );
}
