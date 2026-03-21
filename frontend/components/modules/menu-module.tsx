"use client";

import { FormEvent, useEffect, useRef, useState } from "react";
import {
  createMenuCategory,
  createMenuItem,
  deleteMenuCategory,
  deleteMenuItem,
  getMenu,
  updateMenuCategory,
  updateMenuItem,
  updateMenuItemStatus,
  uploadMenuItemImage,
  type MenuCategory,
  type MenuItem,
} from "@/lib/api";
import { handleApiError, type AsyncVoid } from "@/components/modules/module-utils";

export function MenuModule({ token, onUnauthorized }: { token: string; onUnauthorized: AsyncVoid }) {
  const [categories, setCategories] = useState<MenuCategory[]>([]);
  const [selectedCategoryId, setSelectedCategoryId] = useState("");
  const [editingCategoryId, setEditingCategoryId] = useState("");
  const [editingItemId, setEditingItemId] = useState("");
  const [categoryName, setCategoryName] = useState("");
  const [itemName, setItemName] = useState("");
  const [itemDescription, setItemDescription] = useState("");
  const [itemAccentLabel, setItemAccentLabel] = useState("");
  const [itemImageFile, setItemImageFile] = useState<File | null>(null);
  const [itemImagePreviewUrl, setItemImagePreviewUrl] = useState("");
  const [itemStoredImageUrl, setItemStoredImageUrl] = useState("");
  const [itemPrice, setItemPrice] = useState("0");
  const [fileInputKey, setFileInputKey] = useState(0);
  const [loading, setLoading] = useState(true);
  const [isSavingCategory, setIsSavingCategory] = useState(false);
  const [isSavingItem, setIsSavingItem] = useState(false);
  const [deletingCategoryId, setDeletingCategoryId] = useState("");
  const [updatingItemId, setUpdatingItemId] = useState("");
  const [deletingItemId, setDeletingItemId] = useState("");
  const [errorMessage, setErrorMessage] = useState("");
  const [successMessage, setSuccessMessage] = useState("");
  const categoryEditorRef = useRef<HTMLElement | null>(null);
  const itemEditorRef = useRef<HTMLElement | null>(null);

  function removeItemFromState(menuItemId: string) {
    setCategories((currentValue) =>
      currentValue.map((category) => ({
        ...category,
        items: category.items.filter((item) => item.id !== menuItemId),
      })),
    );
  }

  function removeCategoryFromState(categoryId: string) {
    let nextSelectedCategoryId = "";

    setCategories((currentValue) => {
      const filteredCategories = currentValue.filter((category) => category.id !== categoryId);
      nextSelectedCategoryId = filteredCategories[0]?.id ?? "";
      return filteredCategories;
    });

    setSelectedCategoryId((currentValue) => (currentValue === categoryId ? nextSelectedCategoryId : currentValue));
  }

  function replaceCategoryInState(nextCategory: MenuCategory) {
    setCategories((currentValue) =>
      currentValue.map((category) =>
        category.id === nextCategory.id
          ? {
              ...category,
              ...nextCategory,
              items: category.items,
            }
          : category,
      ),
    );
  }

  function replaceItemInState(nextItem: MenuItem) {
    setCategories((currentValue) =>
      currentValue.map((category) =>
        category.id !== nextItem.categoryId
          ? category
          : {
              ...category,
              items: category.items.map((item) => (item.id === nextItem.id ? nextItem : item)),
            },
      ),
    );
  }

  function upsertItemInState(nextItem: MenuItem, previousCategoryId?: string) {
    setCategories((currentValue) =>
      currentValue.map((category) => {
        if (category.id === nextItem.categoryId) {
          const hasItem = category.items.some((item) => item.id === nextItem.id);

          return {
            ...category,
            items: hasItem
              ? category.items.map((item) => (item.id === nextItem.id ? nextItem : item))
              : [...category.items, nextItem],
          };
        }

        if (previousCategoryId && category.id === previousCategoryId && previousCategoryId !== nextItem.categoryId) {
          return {
            ...category,
            items: category.items.filter((item) => item.id !== nextItem.id),
          };
        }

        return category;
      }),
    );
  }

  function appendItemToState(nextItem: MenuItem) {
    setCategories((currentValue) =>
      currentValue.map((category) =>
        category.id !== nextItem.categoryId
          ? category
          : {
              ...category,
              items: [...category.items, nextItem],
            },
      ),
    );
  }

  function resetCategoryEditor() {
    setEditingCategoryId("");
    setCategoryName("");
  }

  function clearPreviewUrl(url: string) {
    if (url.startsWith("blob:")) {
      URL.revokeObjectURL(url);
    }
  }

  function resetItemEditor() {
    setEditingItemId("");
    setItemName("");
    setItemDescription("");
    setItemAccentLabel("");
    setItemStoredImageUrl("");
    clearPreviewUrl(itemImagePreviewUrl);
    setItemImagePreviewUrl("");
    setItemImageFile(null);
    setItemPrice("0");
    setFileInputKey((currentValue) => currentValue + 1);
  }

  function scrollToEditor(ref: { current: HTMLElement | null }) {
    requestAnimationFrame(() => {
      ref.current?.scrollIntoView({
        behavior: "smooth",
        block: "start",
      });
    });
  }

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

  useEffect(() => {
    return () => {
      if (itemImagePreviewUrl) {
        clearPreviewUrl(itemImagePreviewUrl);
      }
    };
  }, [itemImagePreviewUrl]);

  function handleImageSelection(file: File | null) {
    clearPreviewUrl(itemImagePreviewUrl);

    setItemImageFile(file);
    setItemImagePreviewUrl(file ? URL.createObjectURL(file) : "");
    if (file) {
      setItemStoredImageUrl("");
    }
  }

  async function handleSaveCategory(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setIsSavingCategory(true);
    setSuccessMessage("");

    try {
      if (editingCategoryId) {
        const updatedCategory = await updateMenuCategory(token, editingCategoryId, {
          name: categoryName,
        });

        replaceCategoryInState(updatedCategory);
        setSelectedCategoryId(updatedCategory.id);
        setSuccessMessage("Categoria atualizada.");
      } else {
        const createdCategory = await createMenuCategory(token, {
          name: categoryName,
        });

        setCategories((currentValue) => [...currentValue, { ...createdCategory, items: createdCategory.items ?? [] }]);
        setSelectedCategoryId(createdCategory.id);
        setSuccessMessage("Categoria criada.");
      }

      resetCategoryEditor();
      setErrorMessage("");
    } catch (error) {
      await handleApiError(
        error,
        onUnauthorized,
        setErrorMessage,
        editingCategoryId ? "Nao foi possivel atualizar a categoria." : "Nao foi possivel criar a categoria.",
      );
    } finally {
      setIsSavingCategory(false);
    }
  }

  async function handleSaveItem(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    if (!selectedCategoryId) {
      setErrorMessage("Crie ou selecione uma categoria antes de adicionar itens.");
      return;
    }

    setIsSavingItem(true);
    setSuccessMessage("");

    try {
      let imageUrl = itemStoredImageUrl || undefined;

      if (itemImageFile) {
        const uploadResponse = await uploadMenuItemImage(token, itemImageFile);
        imageUrl = uploadResponse.imageUrl;
      }

      if (editingItemId) {
        const previousCategoryId =
          categories.find((category) => category.items.some((item) => item.id === editingItemId))?.id ?? selectedCategoryId;

        const updatedItem = await updateMenuItem(token, editingItemId, {
          categoryId: selectedCategoryId,
          name: itemName,
          description: itemDescription || undefined,
          accentLabel: itemAccentLabel || undefined,
          imageUrl,
          price: Number(itemPrice),
        });

        upsertItemInState(updatedItem, previousCategoryId);
        setSelectedCategoryId(updatedItem.categoryId);
        setSuccessMessage("Produto atualizado.");
      } else {
        const createdItem = await createMenuItem(token, {
          categoryId: selectedCategoryId,
          name: itemName,
          description: itemDescription || undefined,
          accentLabel: itemAccentLabel || undefined,
          imageUrl,
          price: Number(itemPrice),
        });

        appendItemToState(createdItem);
        setSuccessMessage("Item adicionado ao cardapio.");
      }

      resetItemEditor();
      setErrorMessage("");
    } catch (error) {
      await handleApiError(
        error,
        onUnauthorized,
        setErrorMessage,
        editingItemId ? "Nao foi possivel atualizar o produto." : "Nao foi possivel adicionar o item.",
      );
    } finally {
      setIsSavingItem(false);
    }
  }

  async function handleToggleAvailability(item: MenuItem) {
    setUpdatingItemId(item.id);
    setSuccessMessage("");

    try {
      const updatedItem = await updateMenuItemStatus(token, item.id, !item.isActive);
      replaceItemInState(updatedItem);
      setSuccessMessage(item.isActive ? "Item ocultado do cardapio." : "Item liberado no cardapio.");
      setErrorMessage("");
    } catch (error) {
      await handleApiError(error, onUnauthorized, setErrorMessage, "Nao foi possivel atualizar o item.");
    } finally {
      setUpdatingItemId("");
    }
  }

  async function handleDeleteCategory(category: MenuCategory) {
    const confirmed = window.confirm(`Apagar a categoria "${category.name}" e remover os itens dela do cardapio?`);

    if (!confirmed) {
      return;
    }

    setDeletingCategoryId(category.id);
    setSuccessMessage("");

    try {
      await deleteMenuCategory(token, category.id);
      removeCategoryFromState(category.id);
      if (editingCategoryId === category.id) {
        resetCategoryEditor();
      }
      setSuccessMessage("Categoria apagada.");
      setErrorMessage("");
    } catch (error) {
      await handleApiError(error, onUnauthorized, setErrorMessage, "Nao foi possivel apagar a categoria.");
    } finally {
      setDeletingCategoryId("");
    }
  }

  async function handleDeleteItem(item: MenuItem) {
    const confirmed = window.confirm(`Apagar o item "${item.name}" do cardapio?`);

    if (!confirmed) {
      return;
    }

    setDeletingItemId(item.id);
    setSuccessMessage("");

    try {
      await deleteMenuItem(token, item.id);
      removeItemFromState(item.id);
      if (editingItemId === item.id) {
        resetItemEditor();
      }
      setSuccessMessage("Item apagado.");
      setErrorMessage("");
    } catch (error) {
      await handleApiError(error, onUnauthorized, setErrorMessage, "Nao foi possivel apagar o item.");
    } finally {
      setDeletingItemId("");
    }
  }

  function handleEditCategory(category: MenuCategory) {
    setEditingCategoryId(category.id);
    setCategoryName(category.name);
    setSelectedCategoryId(category.id);
    setSuccessMessage("");
    setErrorMessage("");
    scrollToEditor(categoryEditorRef);
  }

  function handleEditItem(item: MenuItem) {
    setEditingItemId(item.id);
    setSelectedCategoryId(item.categoryId);
    setItemName(item.name);
    setItemDescription(item.description ?? "");
    setItemAccentLabel(item.accentLabel ?? "");
    setItemStoredImageUrl(item.imageUrl ?? "");
    clearPreviewUrl(itemImagePreviewUrl);
    setItemImagePreviewUrl(item.imageUrl ?? "");
    setItemImageFile(null);
    setItemPrice(item.price.toString());
    setFileInputKey((currentValue) => currentValue + 1);
    setSuccessMessage("");
    setErrorMessage("");
    scrollToEditor(itemEditorRef);
  }

  return (
    <section className="menu-workspace">
      <section className="menu-builder-grid">
        <section ref={categoryEditorRef} className="surface-card module-form-card menu-category-form-card">
          <div className="module-section-head">
            <h2>{editingCategoryId ? "Editar categoria" : "Categorias"}</h2>
            <strong>{editingCategoryId ? "Edicao ativa" : categories.length}</strong>
          </div>

          <form className="module-form" onSubmit={handleSaveCategory}>
            <div className="field-group">
              <label htmlFor="categoryName">Nome da categoria</label>
              <input
                id="categoryName"
                value={categoryName}
                onChange={(event) => setCategoryName(event.target.value)}
                placeholder="Hamburgueres"
              />
            </div>

            <div className="toolbar-actions menu-category-form-actions">
              {editingCategoryId ? (
                <button className="ghost-link button-link" type="button" onClick={resetCategoryEditor}>
                  Cancelar edicao
                </button>
              ) : null}
              <button className="primary-link button-link" type="submit" disabled={isSavingCategory}>
                {isSavingCategory
                  ? editingCategoryId
                    ? "Salvando..."
                    : "Criando..."
                  : editingCategoryId
                    ? "Salvar categoria"
                    : "Criar categoria"}
              </button>
            </div>
          </form>
        </section>

        <section ref={itemEditorRef} className="surface-card module-form-card menu-item-form-card">
          <div className="module-section-head">
            <h2>{editingItemId ? "Editar produto" : "Novo item"}</h2>
            <strong>
              {editingItemId ? "Edicao ativa" : selectedCategoryId ? "Pronto" : "Selecione uma categoria"}
            </strong>
          </div>

          <form className="module-form" onSubmit={handleSaveItem}>
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

            <div className="field-group">
              <label htmlFor="itemImageFile">Foto do prato</label>
              <input
                key={fileInputKey}
                id="itemImageFile"
                type="file"
                accept="image/png,image/jpeg,image/webp"
                onChange={(event) => handleImageSelection(event.target.files?.[0] ?? null)}
              />
            </div>

            {itemImagePreviewUrl ? (
              <div className="menu-upload-preview">
                <img src={itemImagePreviewUrl} alt="Preview do prato" />
              </div>
            ) : null}

            <div className="toolbar-actions menu-category-form-actions">
              {editingItemId ? (
                <button className="ghost-link button-link" type="button" onClick={resetItemEditor}>
                  Cancelar edicao
                </button>
              ) : null}
              <button className="primary-link button-link" type="submit" disabled={isSavingItem}>
                {isSavingItem
                  ? editingItemId
                    ? "Salvando..."
                    : "Adicionando..."
                  : editingItemId
                    ? "Salvar produto"
                    : "Adicionar item"}
              </button>
            </div>
          </form>
        </section>
      </section>

      {successMessage ? <p className="module-feedback success">{successMessage}</p> : null}
      {errorMessage ? <p className="module-feedback error">{errorMessage}</p> : null}

      <section className="surface-card module-list-card">
        <div className="module-section-head">
          <h2>Cardapio</h2>
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
              <article key={category.id} className="module-entity-card interactive-card menu-category-panel">
                <div className="menu-category-head">
                  <div className="menu-category-copy">
                    <h3>{category.name}</h3>
                    <p>{category.items.length} itens</p>
                  </div>

                  <div className="toolbar-actions compact menu-category-actions">
                    <button
                      className={`ghost-link button-link ${editingCategoryId === category.id ? "is-selected-filter" : ""}`}
                      type="button"
                      onClick={() => handleEditCategory(category)}
                    >
                      {editingCategoryId === category.id ? "Editando" : "Editar"}
                    </button>
                    <button
                      className="ghost-link button-link destructive-link"
                      type="button"
                      disabled={deletingCategoryId === category.id}
                      onClick={() => void handleDeleteCategory(category)}
                    >
                      {deletingCategoryId === category.id ? "Apagando..." : "Apagar categoria"}
                    </button>
                  </div>
                </div>

                {category.items.length === 0 ? (
                  <div className="module-empty-state compact-empty-state">
                    <p>Sem itens.</p>
                  </div>
                ) : (
                  <div className="menu-item-stack">
                    {category.items.map((item) => (
                      <div
                        key={item.id}
                        className={`menu-item-row menu-item-rich-row ${item.isActive ? "" : "is-inactive"}`}
                      >
                        {item.imageUrl ? (
                          <img className="menu-item-image" src={item.imageUrl} alt={item.name} loading="lazy" />
                        ) : (
                          <div className="menu-item-image menu-item-image-placeholder" aria-hidden="true">
                            <span>{item.name.slice(0, 1)}</span>
                          </div>
                        )}

                        <div className="menu-item-copy">
                          <div className="menu-item-text">
                            <strong>{item.name}</strong>
                            {item.description ? <p>{item.description}</p> : <p>Sem descricao</p>}
                          </div>

                          <div className="menu-item-meta">
                            {item.accentLabel ? <span className="status-chip pending">{item.accentLabel}</span> : null}
                            <span className={`status-chip ${item.isActive ? "available" : "inactive"}`}>
                              {item.isActive ? "Disponivel" : "Oculto"}
                            </span>
                            <strong>{item.price.toLocaleString("pt-BR", { style: "currency", currency: "BRL" })}</strong>
                          </div>
                        </div>

                        <div className="toolbar-actions compact menu-item-actions">
                          <span className="menu-item-activity">
                            {deletingItemId === item.id ? "Removendo..." : updatingItemId === item.id ? "Atualizando..." : ""}
                          </span>
                          <button
                            className={`ghost-link button-link ${editingItemId === item.id ? "is-selected-filter" : ""}`}
                            type="button"
                            onClick={() => handleEditItem(item)}
                          >
                            {editingItemId === item.id ? "Editando" : "Editar"}
                          </button>
                          <button
                            className="ghost-link button-link"
                            type="button"
                            disabled={updatingItemId === item.id}
                            onClick={() => void handleToggleAvailability(item)}
                          >
                            {updatingItemId === item.id ? "Salvando..." : item.isActive ? "Ocultar" : "Disponibilizar"}
                          </button>
                          <button
                            className="ghost-link button-link destructive-link"
                            type="button"
                            disabled={deletingItemId === item.id}
                            onClick={() => void handleDeleteItem(item)}
                          >
                            {deletingItemId === item.id ? "Apagando..." : "Apagar item"}
                          </button>
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
