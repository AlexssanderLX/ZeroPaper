"use client";

import Link from "next/link";
import { FormEvent, useEffect, useMemo, useState } from "react";
import {
  createMenuAdditionalGroup,
  createMenuCategory,
  createMenuItem,
  deleteMenuAdditionalGroup,
  deleteMenuCategory,
  deleteMenuItem,
  getMenuAdditionals,
  getMenuCategoryItems,
  getMenuCategorySummaries,
  getMenuItem,
  updateMenuAdditionalGroup,
  updateMenuCategory,
  updateMenuItem,
  updateMenuItemStatus,
  uploadMenuCategoryImage,
  uploadMenuItemImage,
  type MenuAdditionalCatalogGroup,
  type MenuCategory,
  type MenuCategorySummary,
  type MenuItem,
  type MenuItemAdditionalGroup,
} from "@/lib/api";
import { handleApiError, type AsyncVoid } from "@/components/modules/module-utils";

export type MenuModuleSection = "items" | "additionals";

type AdditionalOptionDraft = {
  id: string;
  catalogOptionId?: string | null;
  name: string;
  price: string;
};

type AdditionalGroupDraft = {
  id: string;
  catalogGroupId?: string | null;
  name: string;
  allowMultiple: boolean;
  maxAdditionalSelections: string;
  options: AdditionalOptionDraft[];
};

type ItemEditorState = {
  mode: "create" | "edit";
  id: string;
  categoryId: string;
  name: string;
  description: string;
  accentLabel: string;
  price: string;
  maxAdditionalSelections: string;
  isActive: boolean;
  storedImageUrl: string;
  previewImageUrl: string;
  imageFile: File | null;
  additionalGroups: AdditionalGroupDraft[];
};

type CategoryEditorState = {
  mode: "create" | "edit";
  id: string;
  name: string;
  storedImageUrl: string;
  previewImageUrl: string;
  imageFile: File | null;
};

type AdditionalEditorState = {
  mode: "create" | "edit";
  id: string;
  name: string;
  allowMultiple: boolean;
  maxAdditionalSelections: string;
  options: AdditionalOptionDraft[];
};

const currencyFormatter = new Intl.NumberFormat("pt-BR", {
  style: "currency",
  currency: "BRL",
});

const imageTypes = new Set(["image/jpeg", "image/png", "image/webp"]);

function createDraftId() {
  if (typeof crypto !== "undefined" && typeof crypto.randomUUID === "function") {
    return crypto.randomUUID();
  }

  return `draft-${Math.random().toString(36).slice(2, 11)}`;
}

function formatCurrency(value?: number | null) {
  return currencyFormatter.format(value ?? 0);
}

function parsePrice(value: string) {
  const normalized = value.replace(/\./g, "").replace(",", ".").trim();
  const parsed = Number(normalized || "0");
  return Number.isFinite(parsed) ? Math.max(0, parsed) : 0;
}

function parseOptionalAdditionalLimit(value: string) {
  if (!value.trim()) {
    return null;
  }

  return Math.max(0, Math.min(100, Number.parseInt(value, 10) || 0));
}

function normalizeMenuImageUrlForPayload(value?: string | null) {
  if (!value) {
    return undefined;
  }

  const normalizedValue = value.trim();

  try {
    const parsed = normalizedValue.startsWith("http://") || normalizedValue.startsWith("https://")
      ? new URL(normalizedValue)
      : new URL(normalizedValue, "https://zeropaper.local");

    if (parsed.pathname.startsWith("/media/uploads/")) {
      return parsed.pathname.replace("/media", "");
    }

    if (parsed.pathname.startsWith("/uploads/")) {
      return parsed.pathname;
    }

    return normalizedValue;
  } catch {
    if (normalizedValue.startsWith("/media/uploads/")) {
      return normalizedValue.replace("/media", "").split("?")[0];
    }

    if (normalizedValue.startsWith("/uploads/")) {
      return normalizedValue.split("?")[0];
    }

    return normalizedValue;
  }
}

function validateImageFile(file: File) {
  if (!imageTypes.has(file.type)) {
    return "Use JPG, PNG ou WEBP.";
  }

  if (file.size > 5 * 1024 * 1024) {
    return "A imagem precisa ter ate 5 MB.";
  }

  return "";
}

function clearPreviewUrl(url: string) {
  if (url.startsWith("blob:")) {
    URL.revokeObjectURL(url);
  }
}

function createOptionDraft(name = "", price = "0", catalogOptionId?: string | null): AdditionalOptionDraft {
  return {
    id: createDraftId(),
    catalogOptionId: catalogOptionId ?? null,
    name,
    price,
  };
}

function createCatalogGroupDraftFromCatalog(group: MenuAdditionalCatalogGroup): AdditionalGroupDraft {
  return {
    id: createDraftId(),
    catalogGroupId: group.id,
    name: group.name,
    allowMultiple: group.allowMultiple,
    maxAdditionalSelections: group.maxAdditionalSelections?.toString() ?? "",
    options: group.options.map((option) => createOptionDraft(option.name, option.price.toString(), option.id)),
  };
}

function mapAdditionalGroupsToDrafts(item: MenuItem): AdditionalGroupDraft[] {
  return item.additionalGroups.map((group) => ({
    id: createDraftId(),
    catalogGroupId: group.sourceMenuAdditionalCatalogGroupId ?? null,
    name: group.name,
    allowMultiple: group.allowMultiple,
    maxAdditionalSelections: group.maxAdditionalSelections?.toString() ?? "",
    options: group.options.map((option) =>
      createOptionDraft(option.name, option.price.toString(), option.sourceMenuAdditionalCatalogOptionId ?? null),
    ),
  }));
}

function normalizeAdditionalGroupsForPayload(groups: AdditionalGroupDraft[]) {
  return groups
    .map((group) => {
      const options = group.options
        .map((option) => ({
          catalogOptionId: option.catalogOptionId ?? null,
          name: option.name.trim(),
          price: parsePrice(option.price),
        }))
        .filter((option) => option.name.length > 0);

      return {
        catalogGroupId: group.catalogGroupId ?? null,
        name: group.name.trim(),
        allowMultiple: group.allowMultiple,
        maxAdditionalSelections: parseOptionalAdditionalLimit(group.maxAdditionalSelections),
        options,
      };
    })
    .filter((group) => group.name.length > 0 && group.options.length > 0);
}

function createEmptyItemEditor(categoryId: string): ItemEditorState {
  return {
    mode: "create",
    id: "",
    categoryId,
    name: "",
    description: "",
    accentLabel: "",
    price: "0",
    maxAdditionalSelections: "",
    isActive: true,
    storedImageUrl: "",
    previewImageUrl: "",
    imageFile: null,
    additionalGroups: [],
  };
}

function createEmptyCategoryEditor(): CategoryEditorState {
  return {
    mode: "create",
    id: "",
    name: "",
    storedImageUrl: "",
    previewImageUrl: "",
    imageFile: null,
  };
}

function createEmptyAdditionalEditor(): AdditionalEditorState {
  return {
    mode: "create",
    id: "",
    name: "",
    allowMultiple: false,
    maxAdditionalSelections: "",
    options: [createOptionDraft()],
  };
}

function getCatalogComplementSummary(group: MenuAdditionalCatalogGroup) {
  if (group.options.length === 0) {
    return "Sem opcoes";
  }

  if (group.options.length === 1) {
    return `${group.options[0].name} - ${formatCurrency(group.options[0].price)}`;
  }

  const minPrice = Math.min(...group.options.map((option) => option.price));
  return `${group.options.length} opcoes desde ${formatCurrency(minPrice)}`;
}

function formatItemPrice(item: MenuItem) {
  const startingPrice = item.startingPrice ?? item.price;
  return startingPrice > item.price ? `A partir de ${formatCurrency(startingPrice)}` : formatCurrency(item.price);
}

export function MenuModule({
  token,
  onUnauthorized,
  section,
}: {
  token: string;
  onUnauthorized: AsyncVoid;
  section: MenuModuleSection;
}) {
  const [categories, setCategories] = useState<MenuCategorySummary[]>([]);
  const [categoryItems, setCategoryItems] = useState<Record<string, MenuCategory>>({});
  const [selectedCategoryId, setSelectedCategoryId] = useState("");
  const [additionals, setAdditionals] = useState<MenuAdditionalCatalogGroup[]>([]);
  const [loadingCategories, setLoadingCategories] = useState(section === "items");
  const [loadingCategoryId, setLoadingCategoryId] = useState("");
  const [loadingAdditionals, setLoadingAdditionals] = useState(section === "additionals");
  const [additionalsLoaded, setAdditionalsLoaded] = useState(false);
  const [categoryEditor, setCategoryEditor] = useState<CategoryEditorState | null>(null);
  const [itemEditor, setItemEditor] = useState<ItemEditorState | null>(null);
  const [additionalEditor, setAdditionalEditor] = useState<AdditionalEditorState | null>(null);
  const [itemEditorDirty, setItemEditorDirty] = useState(false);
  const [savingCategory, setSavingCategory] = useState(false);
  const [savingItem, setSavingItem] = useState(false);
  const [savingAdditional, setSavingAdditional] = useState(false);
  const [updatingItemId, setUpdatingItemId] = useState("");
  const [deletingId, setDeletingId] = useState("");
  const [feedback, setFeedback] = useState("");
  const [errorMessage, setErrorMessage] = useState("");
  const [categoryFileInputKey, setCategoryFileInputKey] = useState(0);
  const [itemFileInputKey, setItemFileInputKey] = useState(0);

  const selectedCategory = selectedCategoryId ? categoryItems[selectedCategoryId] : null;
  const selectedCategorySummary = categories.find((category) => category.id === selectedCategoryId) ?? null;

  const menuTotals = useMemo(() => {
    return categories.reduce(
      (acc, category) => ({
        totalItems: acc.totalItems + category.totalItems,
        activeItems: acc.activeItems + category.activeItems,
        hiddenItems: acc.hiddenItems + category.hiddenItems,
        missingImages: acc.missingImages + category.itemsWithoutImage,
      }),
      { totalItems: 0, activeItems: 0, hiddenItems: 0, missingImages: 0 },
    );
  }, [categories]);

  const sortedAdditionals = useMemo(
    () => [...additionals].sort((left, right) => left.displayOrder - right.displayOrder || left.name.localeCompare(right.name)),
    [additionals],
  );

  useEffect(() => {
    if (section === "items") {
      void loadCategories();
      return;
    }

    void loadAdditionals();
  }, [section, token]);

  useEffect(() => {
    return () => {
      if (itemEditor?.previewImageUrl) {
        clearPreviewUrl(itemEditor.previewImageUrl);
      }

      if (categoryEditor?.previewImageUrl) {
        clearPreviewUrl(categoryEditor.previewImageUrl);
      }
    };
  }, [itemEditor?.previewImageUrl, categoryEditor?.previewImageUrl]);

  async function loadCategories(selectFirst = false) {
    setLoadingCategories(true);

    try {
      const response = await getMenuCategorySummaries(token);
      setCategories(response);
      setErrorMessage("");

      if (selectFirst && !selectedCategoryId) {
        const firstCategoryId = response[0]?.id ?? "";
        if (firstCategoryId) {
          await openCategory(firstCategoryId, true);
        }
      }
    } catch (error) {
      await handleApiError(error, onUnauthorized, setErrorMessage, "Nao foi possivel carregar as categorias.");
    } finally {
      setLoadingCategories(false);
    }
  }

  async function loadAdditionals() {
    setLoadingAdditionals(true);

    try {
      const response = await getMenuAdditionals(token);
      setAdditionals(response);
      setAdditionalsLoaded(true);
      setErrorMessage("");
      return response;
    } catch (error) {
      await handleApiError(error, onUnauthorized, setErrorMessage, "Nao foi possivel carregar os complementos.");
      return null;
    } finally {
      setLoadingAdditionals(false);
    }
  }

  async function ensureAdditionalsLoaded() {
    if (additionalsLoaded) {
      return additionals;
    }

    return await loadAdditionals();
  }

  async function openCategory(categoryId: string, force = false) {
    setSelectedCategoryId(categoryId);

    if (!force && categoryItems[categoryId]) {
      return categoryItems[categoryId];
    }

    setLoadingCategoryId(categoryId);

    try {
      const response = await getMenuCategoryItems(token, categoryId);
      setCategoryItems((currentValue) => ({ ...currentValue, [categoryId]: response }));
      setErrorMessage("");
      return response;
    } catch (error) {
      await handleApiError(error, onUnauthorized, setErrorMessage, "Nao foi possivel carregar os produtos desta categoria.");
      return null;
    } finally {
      setLoadingCategoryId("");
    }
  }

  async function refreshOpenCategory(categoryId = selectedCategoryId) {
    if (!categoryId) {
      return;
    }

    await openCategory(categoryId, true);
  }

  function markItemEditorDirty() {
    setItemEditorDirty(true);
  }

  function updateItemEditor(patch: Partial<ItemEditorState>, dirty = true) {
    setItemEditor((currentValue) => (currentValue ? { ...currentValue, ...patch } : currentValue));
    if (dirty) {
      markItemEditorDirty();
    }
  }

  function closeItemEditor() {
    if (itemEditorDirty && !window.confirm("Descartar alteracoes deste produto?")) {
      return;
    }

    dismissItemEditor();
  }

  function dismissItemEditor() {
    if (itemEditor?.previewImageUrl) {
      clearPreviewUrl(itemEditor.previewImageUrl);
    }

    setItemEditor(null);
    setItemEditorDirty(false);
    setItemFileInputKey((currentValue) => currentValue + 1);
  }

  function closeCategoryEditor() {
    if (categoryEditor?.previewImageUrl) {
      clearPreviewUrl(categoryEditor.previewImageUrl);
    }

    setCategoryEditor(null);
    setCategoryFileInputKey((currentValue) => currentValue + 1);
  }

  function openCreateCategory() {
    setCategoryEditor(createEmptyCategoryEditor());
    setFeedback("");
    setErrorMessage("");
  }

  function openEditCategory(category: MenuCategorySummary) {
    setCategoryEditor({
      mode: "edit",
      id: category.id,
      name: category.name,
      storedImageUrl: category.imageUrl ?? "",
      previewImageUrl: "",
      imageFile: null,
    });
    setFeedback("");
    setErrorMessage("");
  }

  async function openCreateItem() {
    const categoryId = selectedCategoryId || categories[0]?.id || "";
    if (!categoryId) {
      setErrorMessage("Crie uma categoria antes de cadastrar produtos.");
      return;
    }

    await ensureAdditionalsLoaded();
    setItemEditor(createEmptyItemEditor(categoryId));
    setItemEditorDirty(false);
    setFeedback("");
    setErrorMessage("");
  }

  async function openEditItem(item: MenuItem) {
    setUpdatingItemId(item.id);

    try {
      const [details] = await Promise.all([
        getMenuItem(token, item.id),
        ensureAdditionalsLoaded(),
      ]);

      setItemEditor({
        mode: "edit",
        id: details.id,
        categoryId: details.categoryId,
        name: details.name,
        description: details.description ?? "",
        accentLabel: details.accentLabel ?? "",
        price: details.price.toString(),
        maxAdditionalSelections: details.maxAdditionalSelections?.toString() ?? "",
        isActive: details.isActive,
        storedImageUrl: details.imageUrl ?? "",
        previewImageUrl: "",
        imageFile: null,
        additionalGroups: mapAdditionalGroupsToDrafts(details),
      });
      setItemEditorDirty(false);
      setFeedback("");
      setErrorMessage("");
    } catch (error) {
      await handleApiError(error, onUnauthorized, setErrorMessage, "Nao foi possivel abrir este produto.");
    } finally {
      setUpdatingItemId("");
    }
  }

  function openCreateAdditional() {
    setAdditionalEditor(createEmptyAdditionalEditor());
    setFeedback("");
    setErrorMessage("");
  }

  function openEditAdditional(group: MenuAdditionalCatalogGroup) {
    setAdditionalEditor({
      mode: "edit",
      id: group.id,
      name: group.name,
      allowMultiple: group.allowMultiple,
      maxAdditionalSelections: group.maxAdditionalSelections?.toString() ?? "",
      options: group.options.length
        ? group.options.map((option) => createOptionDraft(option.name, option.price.toString()))
        : [createOptionDraft()],
    });
    setFeedback("");
    setErrorMessage("");
  }

  function handleCategoryImageSelection(file: File | null) {
    if (!categoryEditor) {
      return;
    }

    clearPreviewUrl(categoryEditor.previewImageUrl);

    if (!file) {
      setCategoryEditor({ ...categoryEditor, imageFile: null, previewImageUrl: "" });
      return;
    }

    const error = validateImageFile(file);
    if (error) {
      setErrorMessage(error);
      setCategoryEditor({ ...categoryEditor, imageFile: null, previewImageUrl: "" });
      return;
    }

    setCategoryEditor({
      ...categoryEditor,
      imageFile: file,
      previewImageUrl: URL.createObjectURL(file),
    });
  }

  function handleItemImageSelection(file: File | null) {
    if (!itemEditor) {
      return;
    }

    clearPreviewUrl(itemEditor.previewImageUrl);

    if (!file) {
      updateItemEditor({ imageFile: null, previewImageUrl: "" });
      return;
    }

    const error = validateImageFile(file);
    if (error) {
      setErrorMessage(error);
      updateItemEditor({ imageFile: null, previewImageUrl: "" }, false);
      return;
    }

    updateItemEditor({
      imageFile: file,
      previewImageUrl: URL.createObjectURL(file),
      storedImageUrl: "",
    });
  }

  async function handleSaveCategory(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    if (!categoryEditor) {
      return;
    }

    if (!categoryEditor.name.trim()) {
      setErrorMessage("Informe o nome da categoria.");
      return;
    }

    setSavingCategory(true);

    try {
      let imageUrl = categoryEditor.storedImageUrl;

      if (categoryEditor.imageFile) {
        const uploadResult = await uploadMenuCategoryImage(token, categoryEditor.imageFile);
        imageUrl = uploadResult.imageUrl;
      }

      const payload = {
        name: categoryEditor.name.trim(),
        imageUrl: normalizeMenuImageUrlForPayload(imageUrl) ?? null,
      };

      const savedCategory = categoryEditor.mode === "edit"
        ? await updateMenuCategory(token, categoryEditor.id, payload)
        : await createMenuCategory(token, payload);

      await loadCategories();

      if (categoryEditor.mode === "create") {
        await openCategory(savedCategory.id, true);
      } else {
        await refreshOpenCategory(savedCategory.id);
      }

      setFeedback(categoryEditor.mode === "edit" ? "Categoria salva." : "Categoria criada.");
      closeCategoryEditor();
    } catch (error) {
      await handleApiError(error, onUnauthorized, setErrorMessage, "Nao foi possivel salvar a categoria.");
    } finally {
      setSavingCategory(false);
    }
  }

  async function handleDeleteCategory(category: MenuCategorySummary) {
    const confirmed = window.confirm(
      `Apagar a categoria "${category.name}" e todos os produtos dela? Essa acao nao pode ser desfeita.`,
    );

    if (!confirmed) {
      return;
    }

    setDeletingId(category.id);

    try {
      await deleteMenuCategory(token, category.id);
      setCategoryItems((currentValue) => {
        const nextValue = { ...currentValue };
        delete nextValue[category.id];
        return nextValue;
      });
      setSelectedCategoryId((currentValue) => (currentValue === category.id ? "" : currentValue));
      await loadCategories();
      setFeedback("Categoria apagada.");
    } catch (error) {
      await handleApiError(error, onUnauthorized, setErrorMessage, "Nao foi possivel apagar a categoria.");
    } finally {
      setDeletingId("");
    }
  }

  async function handleSaveItem(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    if (!itemEditor) {
      return;
    }

    if (!itemEditor.categoryId) {
      setErrorMessage("Escolha a categoria do produto.");
      return;
    }

    if (!itemEditor.name.trim()) {
      setErrorMessage("Informe o nome do produto.");
      return;
    }

    setSavingItem(true);

    try {
      let imageUrl = itemEditor.storedImageUrl;

      if (itemEditor.imageFile) {
        const uploadResult = await uploadMenuItemImage(token, itemEditor.imageFile);
        imageUrl = uploadResult.imageUrl;
      }

      const payload = {
        categoryId: itemEditor.categoryId,
        name: itemEditor.name.trim(),
        description: itemEditor.description.trim(),
        accentLabel: itemEditor.accentLabel.trim(),
        imageUrl: normalizeMenuImageUrlForPayload(imageUrl),
        price: parsePrice(itemEditor.price),
        maxAdditionalSelections: parseOptionalAdditionalLimit(itemEditor.maxAdditionalSelections),
        additionalGroups: normalizeAdditionalGroupsForPayload(itemEditor.additionalGroups),
      };

      const previousCategoryId = itemEditor.mode === "edit"
        ? selectedCategory?.items.find((item) => item.id === itemEditor.id)?.categoryId ?? itemEditor.categoryId
        : itemEditor.categoryId;

      const savedItem = itemEditor.mode === "edit"
        ? await updateMenuItem(token, itemEditor.id, payload)
        : await createMenuItem(token, payload);

      if (itemEditor.mode === "edit" && savedItem.isActive !== itemEditor.isActive) {
        await updateMenuItemStatus(token, savedItem.id, itemEditor.isActive);
      } else if (itemEditor.mode === "create" && !itemEditor.isActive) {
        await updateMenuItemStatus(token, savedItem.id, false);
      }

      await loadCategories();
      await Promise.all([
        refreshOpenCategory(savedItem.categoryId),
        previousCategoryId !== savedItem.categoryId ? refreshOpenCategory(previousCategoryId) : Promise.resolve(),
      ]);

      setSelectedCategoryId(savedItem.categoryId);
      setFeedback(itemEditor.mode === "edit" ? "Produto salvo." : "Produto criado.");
      setItemEditorDirty(false);
      dismissItemEditor();
    } catch (error) {
      await handleApiError(error, onUnauthorized, setErrorMessage, "Nao foi possivel salvar o produto.");
    } finally {
      setSavingItem(false);
    }
  }

  async function handleToggleItemStatus(item: MenuItem) {
    setUpdatingItemId(item.id);

    try {
      await updateMenuItemStatus(token, item.id, !item.isActive);
      await Promise.all([loadCategories(), refreshOpenCategory(item.categoryId)]);
      setFeedback(item.isActive ? "Produto ocultado." : "Produto exibido.");
    } catch (error) {
      await handleApiError(error, onUnauthorized, setErrorMessage, "Nao foi possivel alterar a visibilidade.");
    } finally {
      setUpdatingItemId("");
    }
  }

  async function handleDeleteItem(item: MenuItem) {
    if (!window.confirm(`Apagar o produto "${item.name}"?`)) {
      return;
    }

    setDeletingId(item.id);

    try {
      const categoryToRefresh = itemEditor?.id === item.id && selectedCategoryId ? selectedCategoryId : item.categoryId;
      await deleteMenuItem(token, item.id);
      await Promise.all([loadCategories(), refreshOpenCategory(categoryToRefresh)]);
      setFeedback("Produto apagado.");
      if (itemEditor?.id === item.id) {
        dismissItemEditor();
      }
    } catch (error) {
      await handleApiError(error, onUnauthorized, setErrorMessage, "Nao foi possivel apagar o produto.");
    } finally {
      setDeletingId("");
    }
  }

  async function handleSaveAdditional(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    if (!additionalEditor) {
      return;
    }

    const options = additionalEditor.options
      .map((option) => ({ name: option.name.trim(), price: parsePrice(option.price) }))
      .filter((option) => option.name.length > 0);

    if (!additionalEditor.name.trim()) {
      setErrorMessage("Informe o nome do complemento.");
      return;
    }

    if (options.length === 0) {
      setErrorMessage("Adicione pelo menos uma opcao.");
      return;
    }

    setSavingAdditional(true);

    try {
      const payload = {
        name: additionalEditor.name.trim(),
        allowMultiple: additionalEditor.allowMultiple,
        maxAdditionalSelections: parseOptionalAdditionalLimit(additionalEditor.maxAdditionalSelections),
        options,
      };

      if (additionalEditor.mode === "edit") {
        await updateMenuAdditionalGroup(token, additionalEditor.id, payload);
      } else {
        await createMenuAdditionalGroup(token, payload);
      }

      await loadAdditionals();
      setAdditionalEditor(null);
      setFeedback(additionalEditor.mode === "edit" ? "Complemento salvo." : "Complemento criado.");
    } catch (error) {
      await handleApiError(error, onUnauthorized, setErrorMessage, "Nao foi possivel salvar o complemento.");
    } finally {
      setSavingAdditional(false);
    }
  }

  async function handleDeleteAdditional(group: MenuAdditionalCatalogGroup) {
    const confirmed = window.confirm(
      `Apagar "${group.name}"? Produtos que usam este complemento perdem esse vinculo.`,
    );

    if (!confirmed) {
      return;
    }

    setDeletingId(group.id);

    try {
      await deleteMenuAdditionalGroup(token, group.id);
      await loadAdditionals();
      setFeedback("Complemento apagado.");
    } catch (error) {
      await handleApiError(error, onUnauthorized, setErrorMessage, "Nao foi possivel apagar o complemento.");
    } finally {
      setDeletingId("");
    }
  }

  function attachCatalogToItemEditor(group: MenuAdditionalCatalogGroup) {
    if (!itemEditor) {
      return;
    }

    if (itemEditor.additionalGroups.some((item) => item.catalogGroupId === group.id)) {
      return;
    }

    updateItemEditor({
      additionalGroups: [...itemEditor.additionalGroups, createCatalogGroupDraftFromCatalog(group)],
    });
  }

  function removeAdditionalFromItemEditor(groupId: string) {
    if (!itemEditor) {
      return;
    }

    updateItemEditor({
      additionalGroups: itemEditor.additionalGroups.filter((group) => group.id !== groupId),
    });
  }

  function updateAdditionalEditorOption(optionId: string, patch: Partial<AdditionalOptionDraft>) {
    setAdditionalEditor((currentValue) => {
      if (!currentValue) {
        return currentValue;
      }

      return {
        ...currentValue,
        options: currentValue.options.map((option) => (option.id === optionId ? { ...option, ...patch } : option)),
      };
    });
  }

  // ─── render helpers ────────────────────────────────────────────────────────

  function renderFeedback() {
    if (!feedback && !errorMessage) return null;
    return (
      <div className="mnu-feedback-stack" aria-live="polite">
        {feedback ? <div className="mnu-feedback mnu-feedback--ok">{feedback}</div> : null}
        {errorMessage ? <div className="mnu-feedback mnu-feedback--err">{errorMessage}</div> : null}
      </div>
    );
  }

  function renderThumb(src?: string | null, label = "Imagem") {
    if (!src) {
      return (
        <div className="mnu-thumb mnu-thumb--empty" aria-hidden="true">
          <span>Foto</span>
        </div>
      );
    }
    return <img className="mnu-thumb" src={src} alt={label} loading="lazy" />;
  }

  // ─── items section ─────────────────────────────────────────────────────────

  function renderItemsSection() {
    return (
      <section className="owner-menu-shell">
        <div className="mnu-top">
          <div>
            <span className="eyebrow">Cardapio</span>
            <h2>Categorias</h2>
          </div>
          <div className="mnu-top-actions">
            <Link className="ghost-link button-link" href="/app/cardapio/complementos">
              Complementos
            </Link>
            <button className="primary-link button-link" type="button" onClick={openCreateCategory}>
              + Categoria
            </button>
          </div>
        </div>

        <div className="mnu-stats" aria-label="Resumo do cardapio">
          <div className="mnu-stat">
            <b>{categories.length}</b>
            <span>categorias</span>
          </div>
          <div className="mnu-stat">
            <b>{menuTotals.totalItems}</b>
            <span>produtos</span>
          </div>
          <div className="mnu-stat mnu-stat--ok">
            <b>{menuTotals.activeItems}</b>
            <span>visiveis</span>
          </div>
          {menuTotals.hiddenItems > 0 && (
            <div className="mnu-stat mnu-stat--dim">
              <b>{menuTotals.hiddenItems}</b>
              <span>ocultos</span>
            </div>
          )}
          {menuTotals.missingImages > 0 && (
            <div className="mnu-stat mnu-stat--warn">
              <b>{menuTotals.missingImages}</b>
              <span>sem foto</span>
            </div>
          )}
        </div>

        {renderFeedback()}

        {loadingCategories ? (
          <p className="loading-state">Carregando categorias...</p>
        ) : categories.length === 0 ? (
          <div className="mnu-empty surface-card">
            <strong>Nenhuma categoria criada</strong>
            <p>Crie a primeira categoria para liberar o cadastro dos produtos.</p>
            <button className="primary-link button-link" type="button" onClick={openCreateCategory}>
              Criar categoria
            </button>
          </div>
        ) : (
          <div className="mnu-cat-grid">
            {categories.map((cat) => {
              const isSelected = cat.id === selectedCategoryId;
              return (
                <article
                  key={cat.id}
                  className={`mnu-cat-card surface-card${isSelected ? " is-open" : ""}`}
                >
                  <button
                    className="mnu-cat-main"
                    type="button"
                    onClick={() => void openCategory(cat.id)}
                    aria-label={`Abrir categoria ${cat.name}`}
                  >
                    <div className="mnu-cat-img-wrap">
                      {cat.imageUrl ? (
                        <img className="mnu-cat-img" src={cat.imageUrl} alt="" loading="lazy" />
                      ) : (
                        <div className="mnu-cat-img mnu-cat-img--placeholder" aria-hidden="true">
                          <span>{cat.name.slice(0, 2).toUpperCase()}</span>
                        </div>
                      )}
                    </div>
                    <div className="mnu-cat-info">
                      <strong className="mnu-cat-name">{cat.name}</strong>
                      <span className="mnu-cat-sub">
                        {cat.totalItems} produto{cat.totalItems !== 1 ? "s" : ""}
                      </span>
                      <div className="mnu-cat-tags">
                        {cat.activeItems > 0 && (
                          <span className="mnu-tag mnu-tag--ok">{cat.activeItems} vis.</span>
                        )}
                        {cat.hiddenItems > 0 && (
                          <span className="mnu-tag mnu-tag--dim">{cat.hiddenItems} ocult.</span>
                        )}
                        {cat.itemsWithAdditionals > 0 && (
                          <span className="mnu-tag mnu-tag--accent">extras</span>
                        )}
                      </div>
                    </div>
                    <svg
                      className="mnu-chevron"
                      viewBox="0 0 24 24"
                      fill="none"
                      stroke="currentColor"
                      strokeWidth="2.5"
                      aria-hidden="true"
                    >
                      <path d="M9 18l6-6-6-6" />
                    </svg>
                  </button>
                  <div className="mnu-cat-row-actions">
                    <button
                      className="ghost-link button-link"
                      type="button"
                      onClick={() => openEditCategory(cat)}
                    >
                      Editar
                    </button>
                    <button
                      className="ghost-link button-link destructive-link"
                      type="button"
                      disabled={deletingId === cat.id}
                      onClick={() => void handleDeleteCategory(cat)}
                    >
                      {deletingId === cat.id ? "..." : "Apagar"}
                    </button>
                  </div>
                </article>
              );
            })}
          </div>
        )}

        {renderCategoryProductsPanel()}
      </section>
    );
  }

  // ─── category products panel (bottom sheet) ────────────────────────────────

  function renderCategoryProductsPanel() {
    if (!selectedCategoryId) return null;
    const summary = selectedCategorySummary;

    return (
      <div
        className="mnu-sheet-backdrop"
        role="presentation"
        onClick={() => setSelectedCategoryId("")}
      >
        <section
          className="mnu-sheet surface-card"
          role="dialog"
          aria-modal="true"
          aria-label={summary ? `Produtos de ${summary.name}` : "Produtos"}
          onClick={(e) => e.stopPropagation()}
        >
          <div className="mnu-sheet-handle" aria-hidden="true" />

          <div className="mnu-sheet-header">
            <div className="mnu-sheet-header-copy">
              <span className="eyebrow">Categoria aberta</span>
              <h3>{summary?.name ?? selectedCategory?.name ?? "Categoria"}</h3>
              <span className="mnu-sheet-count">
                {selectedCategory?.items.length ?? summary?.totalItems ?? 0} produto(s)
                {summary?.hiddenItems ? ` · ${summary.hiddenItems} oculto(s)` : ""}
              </span>
            </div>
            <div className="mnu-sheet-header-actions">
              <button
                className="primary-link button-link"
                type="button"
                onClick={() => void openCreateItem()}
              >
                + Produto
              </button>
              {summary && (
                <button
                  className="ghost-link button-link"
                  type="button"
                  onClick={() => openEditCategory(summary)}
                >
                  Editar
                </button>
              )}
              <button
                className="ghost-link button-link"
                type="button"
                onClick={() => setSelectedCategoryId("")}
              >
                Fechar
              </button>
            </div>
          </div>

          <div className="mnu-sheet-body">
            {loadingCategoryId === selectedCategoryId ? (
              <p className="loading-state">Carregando produtos...</p>
            ) : selectedCategory ? (
              selectedCategory.items.length === 0 ? (
                <div className="mnu-empty mnu-empty--inline">
                  <strong>Categoria vazia</strong>
                  <p>Cadastre o primeiro produto desta categoria.</p>
                </div>
              ) : (
                <div className="mnu-product-list">
                  {selectedCategory.items.map((item) => (
                    <article
                      key={item.id}
                      className={`mnu-product-card${item.isActive ? "" : " is-hidden"}`}
                    >
                      <button
                        className="mnu-product-main"
                        type="button"
                        disabled={updatingItemId === item.id}
                        onClick={() => void openEditItem(item)}
                      >
                        <div className="mnu-product-img-wrap">
                          {item.imageUrl ? (
                            <img
                              className="mnu-product-img"
                              src={item.imageUrl}
                              alt={item.name}
                              loading="lazy"
                            />
                          ) : (
                            <div className="mnu-product-img mnu-product-img--empty" aria-hidden="true" />
                          )}
                        </div>
                        <div className="mnu-product-info">
                          <div className="mnu-product-title-row">
                            <strong className="mnu-product-name">{item.name}</strong>
                            <span
                              className={`mnu-status-dot${item.isActive ? " is-active" : ""}`}
                              aria-label={item.isActive ? "Visivel" : "Oculto"}
                            />
                          </div>
                          <span className="mnu-product-price">
                            {item.isActive ? formatItemPrice(item) : "Oculto do cardapio"}
                          </span>
                          {item.description && item.isActive && (
                            <span className="mnu-product-desc">{item.description}</span>
                          )}
                        </div>
                      </button>
                      <button
                        className={`mnu-vis-toggle${item.isActive ? " is-active" : ""}`}
                        type="button"
                        title={item.isActive ? "Ocultar" : "Exibir"}
                        aria-label={item.isActive ? "Ocultar produto" : "Exibir produto"}
                        disabled={updatingItemId === item.id}
                        onClick={() => void handleToggleItemStatus(item)}
                      >
                        <span className="mnu-vis-track">
                          <span className="mnu-vis-thumb" />
                        </span>
                      </button>
                    </article>
                  ))}
                </div>
              )
            ) : (
              <div className="mnu-empty mnu-empty--inline">
                <strong>Erro ao carregar</strong>
                <button
                  className="ghost-link button-link"
                  type="button"
                  onClick={() => void refreshOpenCategory()}
                >
                  Tentar novamente
                </button>
              </div>
            )}
          </div>

          {summary && (
            <div className="mnu-sheet-footer">
              <button
                className="ghost-link button-link destructive-link"
                type="button"
                disabled={deletingId === summary.id}
                onClick={() => void handleDeleteCategory(summary)}
              >
                {deletingId === summary.id ? "Apagando..." : "Apagar categoria"}
              </button>
            </div>
          )}
        </section>
      </div>
    );
  }

  // ─── additionals section ───────────────────────────────────────────────────

  function renderAdditionalsSection() {
    return (
      <section className="owner-menu-shell">
        <div className="mnu-top">
          <div>
            <span className="eyebrow">Complementos</span>
            <h2>Adicionais</h2>
          </div>
          <div className="mnu-top-actions">
            <Link className="ghost-link button-link" href="/app/cardapio">
              Categorias
            </Link>
            <button className="primary-link button-link" type="button" onClick={openCreateAdditional}>
              + Adicional
            </button>
          </div>
        </div>

        {renderFeedback()}

        {loadingAdditionals ? (
          <p className="loading-state">Carregando complementos...</p>
        ) : sortedAdditionals.length === 0 ? (
          <div className="mnu-empty surface-card">
            <strong>Nenhum adicional cadastrado</strong>
            <p>Crie adicionais como bacon, borda, molhos ou acompanhamentos e vincule nos produtos.</p>
            <button className="primary-link button-link" type="button" onClick={openCreateAdditional}>
              Criar adicional
            </button>
          </div>
        ) : (
          <div className="mnu-add-grid">
            {sortedAdditionals.map((group) => (
              <article key={group.id} className="mnu-add-card surface-card">
                <div className="mnu-add-head">
                  <div className="mnu-add-head-copy">
                    <strong className="mnu-add-name">{group.name}</strong>
                    <span className="mnu-add-sub">{getCatalogComplementSummary(group)}</span>
                  </div>
                  <span className={`mnu-tag${(group.linkedItemCount ?? 0) > 0 ? " mnu-tag--accent" : " mnu-tag--dim"}`}>
                    {group.linkedItemCount ?? 0} prod.
                  </span>
                </div>

                <div className="mnu-add-options">
                  {group.options.slice(0, 6).map((opt) => (
                    <span key={opt.id} className="mnu-add-chip">
                      {opt.name}
                      {opt.price > 0 ? ` +${formatCurrency(opt.price)}` : ""}
                    </span>
                  ))}
                  {group.options.length > 6 && (
                    <span className="mnu-add-chip mnu-add-chip--more">
                      +{group.options.length - 6}
                    </span>
                  )}
                </div>

                {group.linkedItemNames?.length ? (
                  <p className="mnu-add-used">
                    Usado em: {group.linkedItemNames.join(", ")}
                    {(group.linkedItemCount ?? 0) > group.linkedItemNames.length ? " e outros" : ""}
                  </p>
                ) : null}

                <div className="mnu-add-actions">
                  <button
                    className="ghost-link button-link"
                    type="button"
                    onClick={() => openEditAdditional(group)}
                  >
                    Editar
                  </button>
                  <button
                    className="ghost-link button-link destructive-link"
                    type="button"
                    disabled={deletingId === group.id}
                    onClick={() => void handleDeleteAdditional(group)}
                  >
                    {deletingId === group.id ? "Apagando..." : "Apagar"}
                  </button>
                </div>
              </article>
            ))}
          </div>
        )}
      </section>
    );
  }

  // ─── category editor dialog ────────────────────────────────────────────────

  function renderCategoryEditor() {
    if (!categoryEditor) return null;
    const preview = categoryEditor.previewImageUrl || categoryEditor.storedImageUrl;

    return (
      <div
        className="mnu-backdrop"
        role="presentation"
        onClick={closeCategoryEditor}
      >
        <form
          className="mnu-dialog surface-card"
          role="dialog"
          aria-modal="true"
          aria-label={categoryEditor.mode === "edit" ? "Editar categoria" : "Nova categoria"}
          onClick={(e) => e.stopPropagation()}
          onSubmit={handleSaveCategory}
        >
          <div className="mnu-dialog-handle" aria-hidden="true" />
          <div className="mnu-dialog-header">
            <div>
              <span className="eyebrow">
                {categoryEditor.mode === "edit" ? "Editar" : "Nova"} categoria
              </span>
              <h3>{categoryEditor.mode === "edit" ? categoryEditor.name : "Categoria"}</h3>
            </div>
            <button
              className="ghost-link button-link"
              type="button"
              onClick={closeCategoryEditor}
            >
              Fechar
            </button>
          </div>

          <div className="mnu-dialog-body">
            <div className="mnu-field-group">
              <label htmlFor="categoryName">Nome da categoria</label>
              <input
                id="categoryName"
                value={categoryEditor.name}
                onChange={(e) => setCategoryEditor({ ...categoryEditor, name: e.target.value })}
                placeholder="Ex.: Pasteis, Bebidas, Sobremesas..."
              />
            </div>
            <div className="mnu-image-field">
              <div className="mnu-image-preview">
                {renderThumb(preview, categoryEditor.name || "Categoria")}
              </div>
              <div className="mnu-image-right">
                <label htmlFor="categoryImage">Foto da categoria</label>
                <input
                  key={categoryFileInputKey}
                  id="categoryImage"
                  type="file"
                  accept="image/jpeg,image/png,image/webp"
                  onChange={(e) => handleCategoryImageSelection(e.target.files?.[0] ?? null)}
                />
                <p className="mnu-hint">JPG, PNG ou WEBP · max 5 MB</p>
              </div>
            </div>
          </div>

          <div className="mnu-dialog-footer">
            <button
              className="ghost-link button-link"
              type="button"
              onClick={closeCategoryEditor}
            >
              Cancelar
            </button>
            <button className="primary-link button-link" type="submit" disabled={savingCategory}>
              {savingCategory ? "Salvando..." : "Salvar"}
            </button>
          </div>
        </form>
      </div>
    );
  }

  // ─── item editor dialog ────────────────────────────────────────────────────

  function renderItemEditor() {
    if (!itemEditor) return null;
    const preview = itemEditor.previewImageUrl || itemEditor.storedImageUrl;
    const attachedCatalogIds = itemEditor.additionalGroups
      .map((g) => g.catalogGroupId)
      .filter(Boolean);
    const availableCatalogs = sortedAdditionals.filter((g) => !attachedCatalogIds.includes(g.id));

    return (
      <div
        className="mnu-backdrop"
        role="presentation"
        onClick={closeItemEditor}
      >
        <form
          className="mnu-dialog mnu-dialog--wide surface-card"
          role="dialog"
          aria-modal="true"
          aria-label={itemEditor.mode === "edit" ? "Editar produto" : "Novo produto"}
          onClick={(e) => e.stopPropagation()}
          onSubmit={handleSaveItem}
        >
          <div className="mnu-dialog-handle" aria-hidden="true" />
          <div className="mnu-dialog-header">
            <div>
              <span className="eyebrow">
                {itemEditor.mode === "edit" ? "Editar" : "Novo"} produto
              </span>
              <h3>{itemEditor.name || "Produto"}</h3>
            </div>
            <button
              className="ghost-link button-link"
              type="button"
              onClick={closeItemEditor}
            >
              Fechar
            </button>
          </div>

          <div className="mnu-dialog-body">
            <div className="mnu-form-grid">
              <div className="mnu-field-group">
                <label htmlFor="itemCategory">Categoria</label>
                <select
                  id="itemCategory"
                  value={itemEditor.categoryId}
                  onChange={(e) => updateItemEditor({ categoryId: e.target.value })}
                >
                  {categories.map((cat) => (
                    <option key={cat.id} value={cat.id}>
                      {cat.name}
                    </option>
                  ))}
                </select>
              </div>
              <div className="mnu-field-group">
                <label htmlFor="itemName">Nome</label>
                <input
                  id="itemName"
                  value={itemEditor.name}
                  onChange={(e) => updateItemEditor({ name: e.target.value })}
                  placeholder="Ex.: Pastel de carne"
                />
              </div>
              <div className="mnu-field-group mnu-col-2">
                <label htmlFor="itemDescription">Descricao</label>
                <textarea
                  id="itemDescription"
                  value={itemEditor.description}
                  onChange={(e) => updateItemEditor({ description: e.target.value })}
                  placeholder="Ingredientes, tamanho, observacao..."
                  rows={2}
                />
              </div>
              <div className="mnu-field-group">
                <label htmlFor="itemPrice">Preco (R$)</label>
                <input
                  id="itemPrice"
                  inputMode="decimal"
                  value={itemEditor.price}
                  onChange={(e) => updateItemEditor({ price: e.target.value })}
                  placeholder="0,00"
                />
              </div>
              <div className="mnu-field-group">
                <label htmlFor="itemAccent">
                  Selo{" "}
                  <span className="mnu-optional">opcional</span>
                </label>
                <input
                  id="itemAccent"
                  value={itemEditor.accentLabel}
                  onChange={(e) => updateItemEditor({ accentLabel: e.target.value })}
                  placeholder="Ex.: Mais pedido"
                />
              </div>
              <div className="mnu-field-group">
                <label htmlFor="itemMaxAdditionalSelections">
                  Max. adicionais{" "}
                  <span className="mnu-optional">opcional</span>
                </label>
                <input
                  id="itemMaxAdditionalSelections"
                  inputMode="numeric"
                  value={itemEditor.maxAdditionalSelections}
                  onChange={(e) => updateItemEditor({ maxAdditionalSelections: e.target.value })}
                  placeholder="Vazio = sem limite"
                />
              </div>
              <label className="mnu-toggle-card mnu-col-2">
                <input
                  type="checkbox"
                  checked={itemEditor.isActive}
                  onChange={(e) => updateItemEditor({ isActive: e.target.checked })}
                />
                <span className="mnu-toggle-label">
                  {itemEditor.isActive ? "Produto visivel" : "Produto oculto"}
                </span>
              </label>
            </div>

            <div className="mnu-image-field">
              <div className="mnu-image-preview">
                {renderThumb(preview, itemEditor.name || "Produto")}
              </div>
              <div className="mnu-image-right">
                <label htmlFor="itemImage">Foto do produto</label>
                <input
                  key={itemFileInputKey}
                  id="itemImage"
                  type="file"
                  accept="image/jpeg,image/png,image/webp"
                  onChange={(e) => handleItemImageSelection(e.target.files?.[0] ?? null)}
                />
                <p className="mnu-hint">JPG, PNG ou WEBP · max 5 MB</p>
              </div>
            </div>

            <section className="mnu-inner-section">
              <div className="mnu-inner-header">
                <div>
                  <span className="eyebrow">Adicionais vinculados</span>
                  <h4>Complementos</h4>
                </div>
                <Link className="ghost-link button-link" href="/app/cardapio/complementos">
                  Gerenciar
                </Link>
              </div>

              {itemEditor.additionalGroups.length === 0 ? (
                <p className="mnu-hint mnu-hint--center">Nenhum complemento vinculado.</p>
              ) : (
                <div className="mnu-linked-list">
                  {itemEditor.additionalGroups.map((group) => (
                    <div key={group.id} className="mnu-linked-card">
                      <div>
                        <strong>{group.name}</strong>
                        <span>
                          {group.options.length} opcao(oes)
                          {group.maxAdditionalSelections ? ` · max ${group.maxAdditionalSelections}` : ""}
                        </span>
                      </div>
                      <button
                        className="ghost-link button-link destructive-link"
                        type="button"
                        onClick={() => removeAdditionalFromItemEditor(group.id)}
                      >
                        Remover
                      </button>
                    </div>
                  ))}
                </div>
              )}

              {availableCatalogs.length > 0 && (
                <div className="mnu-catalog-picker">
                  {availableCatalogs.map((group) => (
                    <button
                      key={group.id}
                      className="ghost-link button-link"
                      type="button"
                      onClick={() => attachCatalogToItemEditor(group)}
                    >
                      + {group.name}
                    </button>
                  ))}
                </div>
              )}
            </section>
          </div>

          <div className="mnu-dialog-footer">
            {itemEditor.mode === "edit" && (
              <button
                className="ghost-link button-link destructive-link"
                type="button"
                disabled={deletingId === itemEditor.id || savingItem}
                onClick={() =>
                  void handleDeleteItem({
                    id: itemEditor.id,
                    categoryId: itemEditor.categoryId,
                    name: itemEditor.name,
                  } as MenuItem)
                }
              >
                {deletingId === itemEditor.id ? "Apagando..." : "Apagar produto"}
              </button>
            )}
            <button className="ghost-link button-link" type="button" onClick={closeItemEditor}>
              Cancelar
            </button>
            <button className="primary-link button-link" type="submit" disabled={savingItem}>
              {savingItem ? "Salvando..." : "Salvar produto"}
            </button>
          </div>
        </form>
      </div>
    );
  }

  // ─── additional editor dialog ──────────────────────────────────────────────

  function renderAdditionalEditor() {
    if (!additionalEditor) return null;

    return (
      <div
        className="mnu-backdrop"
        role="presentation"
        onClick={() => setAdditionalEditor(null)}
      >
        <form
          className="mnu-dialog surface-card"
          role="dialog"
          aria-modal="true"
          aria-label={additionalEditor.mode === "edit" ? "Editar adicional" : "Novo adicional"}
          onClick={(e) => e.stopPropagation()}
          onSubmit={handleSaveAdditional}
        >
          <div className="mnu-dialog-handle" aria-hidden="true" />
          <div className="mnu-dialog-header">
            <div>
              <span className="eyebrow">
                {additionalEditor.mode === "edit" ? "Editar" : "Novo"} adicional
              </span>
              <h3>{additionalEditor.name || "Adicional"}</h3>
            </div>
            <button
              className="ghost-link button-link"
              type="button"
              onClick={() => setAdditionalEditor(null)}
            >
              Fechar
            </button>
          </div>

          <div className="mnu-dialog-body">
            <div className="mnu-form-grid">
              <div className="mnu-field-group">
                <label htmlFor="additionalName">Nome do grupo</label>
                <input
                  id="additionalName"
                  value={additionalEditor.name}
                  onChange={(e) =>
                    setAdditionalEditor({ ...additionalEditor, name: e.target.value })
                  }
                  placeholder="Ex.: Bacon, Borda, Molho..."
                />
              </div>
              <div className="mnu-field-group">
                <label htmlFor="additionalLimit">
                  Max. escolhas{" "}
                  <span className="mnu-optional">opcional</span>
                </label>
                <input
                  id="additionalLimit"
                  inputMode="numeric"
                  value={additionalEditor.maxAdditionalSelections}
                  onChange={(e) =>
                    setAdditionalEditor({
                      ...additionalEditor,
                      maxAdditionalSelections: e.target.value,
                    })
                  }
                  placeholder="Vazio = sem limite"
                />
              </div>
              <label className="mnu-toggle-card mnu-col-2">
                <input
                  type="checkbox"
                  checked={additionalEditor.allowMultiple}
                  onChange={(e) =>
                    setAdditionalEditor({ ...additionalEditor, allowMultiple: e.target.checked })
                  }
                />
                <span className="mnu-toggle-label">Permitir multiplas opcoes</span>
              </label>
            </div>

            <section className="mnu-inner-section">
              <div className="mnu-inner-header">
                <div>
                  <span className="eyebrow">Opcoes</span>
                  <h4>Valores do adicional</h4>
                </div>
                <button
                  className="ghost-link button-link"
                  type="button"
                  onClick={() =>
                    setAdditionalEditor({
                      ...additionalEditor,
                      options: [...additionalEditor.options, createOptionDraft()],
                    })
                  }
                >
                  + Opcao
                </button>
              </div>

              <div className="mnu-option-list">
                {additionalEditor.options.map((opt) => (
                  <div key={opt.id} className="mnu-option-row">
                    <div className="mnu-field-group">
                      <label>Nome</label>
                      <input
                        value={opt.name}
                        onChange={(e) =>
                          updateAdditionalEditorOption(opt.id, { name: e.target.value })
                        }
                        placeholder="Ex.: Cheddar"
                      />
                    </div>
                    <div className="mnu-field-group">
                      <label>Preco</label>
                      <input
                        inputMode="decimal"
                        value={opt.price}
                        onChange={(e) =>
                          updateAdditionalEditorOption(opt.id, { price: e.target.value })
                        }
                        placeholder="0,00"
                      />
                    </div>
                    <button
                      className="ghost-link button-link destructive-link mnu-option-remove"
                      type="button"
                      onClick={() =>
                        setAdditionalEditor({
                          ...additionalEditor,
                          options: additionalEditor.options.filter((o) => o.id !== opt.id),
                        })
                      }
                    >
                      Remover
                    </button>
                  </div>
                ))}
              </div>
            </section>
          </div>

          <div className="mnu-dialog-footer">
            <button
              className="ghost-link button-link"
              type="button"
              onClick={() => setAdditionalEditor(null)}
            >
              Cancelar
            </button>
            <button
              className="primary-link button-link"
              type="submit"
              disabled={savingAdditional}
            >
              {savingAdditional ? "Salvando..." : "Salvar"}
            </button>
          </div>
        </form>
      </div>
    );
  }

  // ─── render ────────────────────────────────────────────────────────────────

  return (
    <>
      {section === "items" ? renderItemsSection() : renderAdditionalsSection()}
      {renderCategoryEditor()}
      {renderItemEditor()}
      {renderAdditionalEditor()}
    </>
  );
}
