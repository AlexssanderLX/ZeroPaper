"use client";

import { FormEvent, useEffect, useMemo, useRef, useState, type WheelEvent } from "react";
import { PublicCustomerProfilePanel } from "@/components/public-customer-profile-panel";
import {
  ApiError,
  createPublicMercadoPagoCheckout,
  createPublicOrder,
  createPublicSellerLinkOrder,
  createPublicWaiterCall,
  getPublicDeliveryCustomerProfile,
  getPublicMenuItem,
  getPublicTable,
  quotePublicDeliveryFreight,
  updateOrder,
  validatePublicCoupon,
  type CustomerOrder,
  type CouponValidation,
  type DeliveryFreightQuote,
  type MenuCategory,
  type MenuItem,
  type MenuItemAdditionalGroup,
  type PublicTableView,
} from "@/lib/api";
import { formatCurrency, formatDateTime, formatPaymentMethod, handleApiError } from "@/components/modules/module-utils";
import type { AsyncVoid } from "@/components/modules/module-utils";
import { Banknote, Check, CheckCircle2, CreditCard, MapPin, Menu, MessageSquare, Phone, Plus, QrCode, Search, ShoppingCart, Smartphone, Store, Tag, Truck, User } from "lucide-react";

type PublicCartLine = {
  id: string;
  menuItemId: string;
  quantity: number;
  notes: string;
  additionalOptionIds: string[];
};

type MenuLookupEntry = {
  item: MenuItem;
  categoryName: string;
};

type ResolvedCartLine = {
  id: string;
  line: PublicCartLine;
  item: MenuItem;
  categoryName: string;
  selectedAdditionals: Array<{
    id: string;
    groupName: string;
    optionName: string;
    price: number;
    count: number;
  }>;
  totalPrice: number;
};

type FulfillmentChoice = "delivery" | "pickup";
type OrderStage = "menu" | "cart" | "checkout" | "profile";

type PublicTableOrderProps = {
  publicCode: string;
  sellerCode?: string;
  editOrder?: CustomerOrder | null;
  editToken?: string;
  editBackHref?: string;
  onEditUnauthorized?: AsyncVoid;
  sellerName?: string;
};

function createLocalId() {
  if (typeof crypto !== "undefined" && typeof crypto.randomUUID === "function") {
    return crypto.randomUUID();
  }

  return `line-${Math.random().toString(36).slice(2, 11)}`;
}

function createCartLine(menuItemId: string, quantity = 1, notes = "", additionalOptionIds: string[] = []): PublicCartLine {
  return {
    id: createLocalId(),
    menuItemId,
    quantity,
    notes,
    additionalOptionIds,
  };
}

function buildMenuSelectionPayload(cartLines: PublicCartLine[], menuLookup: Map<string, MenuLookupEntry>) {
  return cartLines
    .filter((line) => line.quantity > 0)
    .map((line) => {
      const resolvedEntry = menuLookup.get(line.menuItemId);

      if (!resolvedEntry) {
        return null;
      }

      return {
        menuItemId: line.menuItemId,
        quantity: line.quantity,
        notes: line.notes.trim() || undefined,
        additionalOptionIds: normalizeSelectedAdditionalIdsForItem(resolvedEntry.item, line.additionalOptionIds),
      };
    })
    .filter((selection): selection is {
      menuItemId: string;
      quantity: number;
      notes: string | undefined;
      additionalOptionIds: string[];
    } => Boolean(selection));
}

function buildMenuLookup(menu: MenuCategory[]) {
  const lookup = new Map<string, MenuLookupEntry>();

  menu.forEach((category) => {
    category.items.forEach((item) => {
      lookup.set(item.id, {
        item,
        categoryName: category.name,
      });
    });
  });

  return lookup;
}

function resolveLineAdditionals(item: MenuItem, additionalOptionIds: string[]) {
  const additionals = additionalOptionIds.reduce<ResolvedCartLine["selectedAdditionals"]>((result, optionId) => {
    for (const group of item.additionalGroups) {
      const option = group.options.find((currentOption) => currentOption.id === optionId);

      if (option) {
        const existingSelection = result.find((selection) => selection.id === option.id);

        if (existingSelection) {
          existingSelection.count += 1;
          break;
        }

        result.push({
          id: option.id,
          groupName: group.name,
          optionName: option.name,
          price: option.price,
          count: 1,
        });
        break;
      }
    }

    return result;
  }, []);

  return additionals;
}

function calculateLineTotal(item: MenuItem, line: PublicCartLine) {
  const additionalsTotal = resolveLineAdditionals(item, line.additionalOptionIds).reduce(
    (sum, selection) => sum + selection.price,
    0,
  );

  return (item.price + additionalsTotal) * line.quantity;
}

function getConfiguredAdditionalLimit(item: MenuItem) {
  return item.maxAdditionalSelections ?? null;
}

function getConfiguredGroupAdditionalLimit(group: MenuItemAdditionalGroup) {
  return group.maxAdditionalSelections ?? (group.allowMultiple ? null : 1);
}

function itemAcceptsSelectableAdditionals(item: MenuItem) {
  return getConfiguredAdditionalLimit(item) !== 0 &&
    (item.hasAdditionalOptions ||
      item.additionalGroups.some((group) => getConfiguredGroupAdditionalLimit(group) !== 0 && group.options.length > 0));
}

function getMenuItemStartingPrice(item: MenuItem) {
  return item.startingPrice ?? item.price;
}

function formatMenuItemPrice(item: MenuItem) {
  const startingPrice = getMenuItemStartingPrice(item);
  return startingPrice > item.price ? `A partir de ${formatCurrency(startingPrice)}` : formatCurrency(item.price);
}

function requiresPricedChoice(item: MenuItem) {
  return item.price <= 0 && getMenuItemStartingPrice(item) > 0;
}

function hasPricedChoice(item: MenuItem, optionIds: string[]) {
  if (!requiresPricedChoice(item)) {
    return true;
  }

  return item.additionalGroups.some((group) =>
    group.options.some((option) => option.price > 0 && optionIds.includes(option.id)),
  );
}

function normalizeSelectedAdditionalIdsForItem(item: MenuItem, optionIds: string[]) {
  const itemLimit = getConfiguredAdditionalLimit(item);
  const selectedOptionIds: string[] = [];
  const selectedGroupCounts = new Map<string, number>();
  const optionGroupLookup = new Map<string, MenuItemAdditionalGroup>();

  item.additionalGroups.forEach((group) => {
    if (getConfiguredGroupAdditionalLimit(group) === 0) {
      return;
    }

    group.options.forEach((option) => {
      optionGroupLookup.set(option.id, group);
    });
  });

  if (itemLimit === 0) {
    return selectedOptionIds;
  }

  for (const optionId of optionIds) {
    const group = optionGroupLookup.get(optionId);
    if (!group) {
      continue;
    }

    if (selectedOptionIds.includes(optionId)) {
      continue;
    }

    const groupLimit = getConfiguredGroupAdditionalLimit(group);

    if (itemLimit !== null && selectedOptionIds.length >= itemLimit) {
      continue;
    }

    if (groupLimit !== null && (selectedGroupCounts.get(group.id) ?? 0) >= groupLimit) {
      continue;
    }

    selectedOptionIds.push(optionId);
    selectedGroupCounts.set(group.id, (selectedGroupCounts.get(group.id) ?? 0) + 1);
  }

  return selectedOptionIds;
}

function countSelectedOption(optionIds: string[], optionId: string) {
  return optionIds.filter((currentOptionId) => currentOptionId === optionId).length;
}

function canAddAdditionalOption(item: MenuItem, optionIds: string[], optionId: string) {
  const normalizedCurrent = normalizeSelectedAdditionalIdsForItem(item, optionIds);
  const normalizedNext = normalizeSelectedAdditionalIdsForItem(item, [...optionIds, optionId]);

  return normalizedNext.length > normalizedCurrent.length;
}

function matchesSearchTerm(text: string | null | undefined, searchTerm: string) {
  if (!searchTerm.trim()) {
    return true;
  }

  const normalize = (value: string) =>
    value
      .normalize("NFD")
      .replace(/[\u0300-\u036f]/g, "")
      .toLowerCase();

  return normalize(text ?? "").includes(normalize(searchTerm));
}

function getDeliveryFieldValue(value?: string | null) {
  return value ?? "";
}

function normalizeDeliveryPostalCode(value: string) {
  const digits = value.replace(/\D/g, "").slice(0, 8);
  return digits.length > 5 ? `${digits.slice(0, 5)}-${digits.slice(5)}` : digits;
}

function parseCurrencyInput(value: string) {
  const digitsOnly = value.replace(/[^\d]/g, "");

  if (!digitsOnly) {
    return 0;
  }

  if (value.includes(",") || value.includes(".")) {
    return Number(digitsOnly) / 100;
  }

  return Number(digitsOnly);
}

function hasValidDeliveryPostalCode(value: string) {
  return value.replace(/\D/g, "").length === 8;
}

function formatFreightQuoteDetails(quote: DeliveryFreightQuote | null) {
  if (!quote?.isAvailable) {
    return quote?.message ?? "Informe o CEP para o sistema estimar a taxa de entrega.";
  }

  if (quote.freightAmount > 0) {
    return "Taxa de entrega calculada para o CEP informado.";
  }

  return "Nenhuma taxa de entrega foi adicionada para este CEP.";
}

function formatComplementLabel(selection: ResolvedCartLine["selectedAdditionals"][number]) {
  const groupName = selection.groupName.trim();
  const optionName = selection.optionName.trim();

  if (!groupName || groupName.toLocaleLowerCase("pt-BR") === optionName.toLocaleLowerCase("pt-BR")) {
    return optionName;
  }

  return `${groupName}: ${optionName}`;
}

function formatCartComplement(selection: ResolvedCartLine["selectedAdditionals"][number], quantity: number) {
  const priceLabel = selection.price > 0
    ? ` (${formatCurrency(selection.price)} por un.)`
    : "";

  return `${formatComplementLabel(selection)} na linha${quantity > 1 ? ` dos ${quantity}` : ""}${priceLabel}`;
}

export function PublicTableOrder({
  publicCode,
  sellerCode,
  editOrder = null,
  editToken,
  editBackHref,
  onEditUnauthorized,
  sellerName,
}: PublicTableOrderProps) {
  const [table, setTable] = useState<PublicTableView | null>(null);
  const [customerName, setCustomerName] = useState("");
  const [deliveryPhone, setDeliveryPhone] = useState("");
  const [deliveryAddress, setDeliveryAddress] = useState("");
  const [deliveryNumber, setDeliveryNumber] = useState("");
  const [deliveryComplement, setDeliveryComplement] = useState("");
  const [deliveryPostalCode, setDeliveryPostalCode] = useState("");
  const [fulfillmentType, setFulfillmentType] = useState<FulfillmentChoice>("delivery");
  const [freightQuote, setFreightQuote] = useState<DeliveryFreightQuote | null>(null);
  const [isQuotingFreight, setIsQuotingFreight] = useState(false);
  const [orderNotes, setOrderNotes] = useState("");
  const [paymentMethod, setPaymentMethod] = useState("Undefined");
  const [wantsOnlinePayment, setWantsOnlinePayment] = useState(false);
  const [couponCode, setCouponCode] = useState("");
  const [couponValidation, setCouponValidation] = useState<CouponValidation | null>(null);
  const [isValidatingCoupon, setIsValidatingCoupon] = useState(false);
  const [needsCashChange, setNeedsCashChange] = useState(false);
  const [cashChangeAmount, setCashChangeAmount] = useState("");
  const [cartLines, setCartLines] = useState<PublicCartLine[]>([]);
  const [activeMenuItemId, setActiveMenuItemId] = useState<string | null>(null);
  const [menuItemDetails, setMenuItemDetails] = useState<Record<string, MenuItem>>({});
  const [loadingMenuItemId, setLoadingMenuItemId] = useState("");
  const [editingCartLineId, setEditingCartLineId] = useState<string | null>(null);
  const [detailQuantity, setDetailQuantity] = useState(1);
  const [detailNotes, setDetailNotes] = useState("");
  const [detailSelectedOptionIds, setDetailSelectedOptionIds] = useState<string[]>([]);
  const [brokenImageIds, setBrokenImageIds] = useState<Record<string, true>>({});
  const [createdOrder, setCreatedOrder] = useState<CustomerOrder | null>(null);
  const [isStartingOnlinePayment, setIsStartingOnlinePayment] = useState(false);
  const [deliveryCustomerToken, setDeliveryCustomerToken] = useState<string | null>(null);
  const [deliveryCustomerMessage, setDeliveryCustomerMessage] = useState("");
  const [isLoadingDeliveryCustomer, setIsLoadingDeliveryCustomer] = useState(false);
  const [orderStage, setOrderStage] = useState<OrderStage>("menu");
  const [loading, setLoading] = useState(true);
  const [isSaving, setIsSaving] = useState(false);
  const [isCallingWaiter, setIsCallingWaiter] = useState(false);
  const [waiterMessage, setWaiterMessage] = useState("");
  const [errorMessage, setErrorMessage] = useState("");
  const [activeCategoryId, setActiveCategoryId] = useState<string | null>(null);
  const [menuSearchTerm, setMenuSearchTerm] = useState("");
  const categoryTabsRef = useRef<HTMLDivElement | null>(null);
  const hydratedEditOrderId = useRef<string | null>(null);
  const visibleCategories = useMemo(() => (table?.menu ?? []).filter((category) => category.items.length > 0), [table]);
  const isOwnerEditMode = Boolean(editOrder && editToken);
  const isDeliveryChannel = table?.isDeliveryChannel ?? false;
  const isPickupFlow = isDeliveryChannel && fulfillmentType === "pickup";
  const isDeliveryFlow = isDeliveryChannel && fulfillmentType === "delivery";
  const isOrderingClosed = isOwnerEditMode ? false : table ? !table.isOrderingAvailable : false;
  const isCartStage = orderStage === "cart";
  const isCheckoutStage = orderStage === "checkout";
  const isProfileStage = orderStage === "profile";
  const activeCategory = visibleCategories.find((category) => category.id === activeCategoryId) ?? visibleCategories[0] ?? null;
  const activeCategoryIndex = activeCategory ? visibleCategories.findIndex((category) => category.id === activeCategory.id) : -1;
  const restaurantLogoUrl = table?.restaurantLogoUrl ?? null;
  const canOpenCustomerProfile = Boolean(deliveryCustomerToken && !isOwnerEditMode);
  const restaurantInitial = (table?.restaurantName || "Z").trim().slice(0, 1).toUpperCase();
  const normalizedMenuSearchTerm = menuSearchTerm.trim();
  const displayedMenuEntries = useMemo(() => {
    if (!activeCategory) {
      return [];
    }

    return activeCategory.items.filter((item) => {
      if (!normalizedMenuSearchTerm) {
        return true;
      }

      return matchesSearchTerm(item.name, normalizedMenuSearchTerm) ||
        matchesSearchTerm(item.description, normalizedMenuSearchTerm) ||
        matchesSearchTerm(activeCategory.name, normalizedMenuSearchTerm);
    });
  }, [activeCategory, normalizedMenuSearchTerm]);

  const menuLookup = useMemo(() => {
    const lookup = buildMenuLookup(table?.menu ?? []);

    Object.entries(menuItemDetails).forEach(([itemId, item]) => {
      const currentEntry = lookup.get(itemId);
      if (currentEntry) {
        lookup.set(itemId, { ...currentEntry, item });
      }
    });

    return lookup;
  }, [menuItemDetails, table]);

  const cartItems = useMemo(() => {
    return cartLines
      .map<ResolvedCartLine | null>((line) => {
        const resolvedEntry = menuLookup.get(line.menuItemId);

        if (!resolvedEntry) {
          return null;
        }

        const selectedAdditionals = resolveLineAdditionals(resolvedEntry.item, line.additionalOptionIds);

        return {
          id: line.id,
          line,
          item: resolvedEntry.item,
          categoryName: resolvedEntry.categoryName,
          selectedAdditionals,
          totalPrice: calculateLineTotal(resolvedEntry.item, line),
        };
      })
      .filter((line): line is ResolvedCartLine => Boolean(line));
  }, [cartLines, menuLookup]);

  const cartQuantityByMenuItem = useMemo(() => {
    return cartLines.reduce<Record<string, number>>((result, line) => {
      result[line.menuItemId] = (result[line.menuItemId] ?? 0) + line.quantity;
      return result;
    }, {});
  }, [cartLines]);

  const totalAmount = cartItems.reduce((sum, item) => sum + item.totalPrice, 0);
  const couponDiscountAmount = couponValidation?.isValid ? couponValidation.discountAmount : 0;
  const discountedItemsTotal = Math.max(0, totalAmount - couponDiscountAmount);
  const freightAmount = isDeliveryFlow && freightQuote?.isAvailable ? freightQuote.freightAmount : 0;
  const checkoutTotalAmount = discountedItemsTotal + freightAmount;
  const hasPhoneFromDeliveryLink = Boolean(deliveryCustomerToken && deliveryPhone.trim());
  const paymentOptions = isDeliveryFlow
    ? [
        { value: "Pix", label: "Pix" },
        { value: "Credit", label: "Credito" },
        { value: "Debit", label: "Debito" },
        { value: "Cash", label: "Dinheiro" },
      ]
    : [
        { value: "Undefined", label: "Escolher no caixa" },
        { value: "Pix", label: "Pix" },
        { value: "Credit", label: "Credito" },
        { value: "Debit", label: "Debito" },
        { value: "Cash", label: "Dinheiro" },
      ];
  const totalUnits = cartItems.reduce((sum, item) => sum + item.line.quantity, 0);
  const isCashPayment = paymentMethod === "Cash" && !wantsOnlinePayment;
  const canUseOnlinePayment = !isOwnerEditMode && isDeliveryChannel && Boolean(table?.isOnlinePaymentAvailable);
  const cashChangeQuickAmounts = ["R$ 20,00", "R$ 50,00", "R$ 100,00", "R$ 200,00"];
  const activeMenuItemEntry = activeMenuItemId ? menuLookup.get(activeMenuItemId) ?? null : null;
  const activeItemAcceptsAdditionals = activeMenuItemEntry
    ? itemAcceptsSelectableAdditionals(activeMenuItemEntry.item)
    : false;
  const detailPreviewLine = activeMenuItemEntry
    ? {
        id: editingCartLineId ?? "preview",
        menuItemId: activeMenuItemEntry.item.id,
        quantity: detailQuantity,
        notes: detailNotes,
        additionalOptionIds: detailSelectedOptionIds,
      }
    : null;
  const detailPreviewTotal = activeMenuItemEntry && detailPreviewLine
    ? calculateLineTotal(activeMenuItemEntry.item, detailPreviewLine)
    : 0;

  async function loadTable() {
    setLoading(true);

    try {
      const response = await getPublicTable(publicCode);
      setTable(response);
      setBrokenImageIds({});
      setErrorMessage("");
    } catch (error) {
      await handleApiError(error, async () => undefined, setErrorMessage, "Nao foi possivel abrir este canal de pedido.");
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    void loadTable();
  }, [publicCode]);

  useEffect(() => {
    if (!visibleCategories.length) {
      setActiveCategoryId(null);
      return;
    }

    if (!activeCategoryId || !visibleCategories.some((category) => category.id === activeCategoryId)) {
      setActiveCategoryId(visibleCategories[0].id);
    }
  }, [activeCategoryId, visibleCategories]);

  useEffect(() => {
    if (!createdOrder || isDeliveryChannel || isOwnerEditMode) {
      return;
    }

    const timeoutId = window.setTimeout(() => {
      resetForNewOrder();
    }, 12000);

    return () => window.clearTimeout(timeoutId);
  }, [createdOrder, isDeliveryChannel, isOwnerEditMode]);

  useEffect(() => {
    if (!createdOrder) {
      return;
    }

    window.scrollTo({ top: 0, behavior: "smooth" });
    document.querySelector(".public-app-card")?.scrollTo({ top: 0, behavior: "smooth" });
  }, [createdOrder]);

  useEffect(() => {
    if (typeof window === "undefined") {
      return;
    }

    if (isOwnerEditMode) {
      setDeliveryCustomerToken(null);
      return;
    }

    const searchParams = new URLSearchParams(window.location.search);
    const nextDeliveryCustomerToken = searchParams.get("cliente");
    if (searchParams.has("editar")) {
      searchParams.delete("editar");
      window.history.replaceState({}, "", `${window.location.pathname}${searchParams.toString() ? `?${searchParams}` : ""}`);
    }
    setDeliveryCustomerToken(nextDeliveryCustomerToken?.trim() || null);
  }, [isOwnerEditMode]);

  useEffect(() => {
    if (!table || !editOrder || hydratedEditOrderId.current === editOrder.id) {
      return;
    }

    hydratedEditOrderId.current = editOrder.id;
    setCreatedOrder(null);
    setCustomerName(editOrder.customerName ?? "");
    setDeliveryPhone(editOrder.deliveryPhone ?? "");
    setDeliveryAddress(editOrder.fulfillmentType === "Pickup" ? "" : editOrder.deliveryAddress ?? "");
    setDeliveryNumber(editOrder.deliveryNumber ?? "");
    setDeliveryComplement(editOrder.deliveryComplement ?? "");
    setDeliveryPostalCode(normalizeDeliveryPostalCode(getDeliveryFieldValue(editOrder.deliveryPostalCode)));
    setFulfillmentType(editOrder.fulfillmentType === "Pickup" ? "pickup" : "delivery");
    setOrderNotes(editOrder.notes ?? "");
    setPaymentMethod(
      editOrder.requestedPaymentMethod && editOrder.requestedPaymentMethod !== "Undefined"
        ? editOrder.requestedPaymentMethod
        : editOrder.paymentMethod || "Undefined",
    );
    setFreightQuote(
      editOrder.fulfillmentType === "Delivery"
        ? {
            isEnabled: true,
            isConfigured: true,
            isAvailable: true,
            isTestMode: false,
            provider: editOrder.deliveryFreightProvider ?? "",
            originPostalCode: null,
            destinationPostalCode: editOrder.deliveryPostalCode ?? null,
            distanceKm: editOrder.deliveryDistanceKm ?? null,
            baseFee: 0,
            baseDistanceKm: 0,
            chargedDistanceKm: editOrder.deliveryDistanceKm ?? 0,
            pricePerKm: 0,
            freightAmount: editOrder.deliveryFreightAmount ?? 0,
            totalWithFreight: editOrder.totalAmount,
            fromCache: false,
            message: "Taxa atual do pedido.",
          }
        : null,
    );

    const nextCartLines = editOrder.items
      .filter((item) => item.menuItemId)
      .map((item) =>
        createCartLine(
          item.menuItemId!,
          Number(item.quantity || 1),
          item.notes ?? "",
          item.additionalSelections
            .map((selection) => selection.sourceMenuItemAdditionalOptionId)
            .filter((optionId): optionId is string => Boolean(optionId)),
        ),
      );
    setCartLines(nextCartLines);
    setOrderStage("checkout");
    resetComposer();
    setErrorMessage("");

    const missingMenuItemIds = Array.from(new Set(nextCartLines.map((line) => line.menuItemId)))
      .filter((menuItemId) => !menuLookup.get(menuItemId)?.item.additionalGroups.length);

    void Promise.all(
      missingMenuItemIds.map(async (menuItemId) => {
        try {
          const details = await getPublicMenuItem(publicCode, menuItemId);
          setMenuItemDetails((currentValue) => ({ ...currentValue, [menuItemId]: details }));
        } catch {
          // The base menu still lets the owner save items without loaded details.
        }
      }),
    );

    window.requestAnimationFrame(() => window.scrollTo({ top: 0, behavior: "smooth" }));
  }, [editOrder, menuLookup, publicCode, table]);

  useEffect(() => {
    if (!table || !isDeliveryChannel || !deliveryCustomerToken || isOwnerEditMode) {
      return;
    }

    let ignore = false;

    async function loadDeliveryCustomerProfile() {
      try {
        setIsLoadingDeliveryCustomer(true);
        const profile = await getPublicDeliveryCustomerProfile(publicCode, deliveryCustomerToken!);

        if (ignore) {
          return;
        }

        if (profile.deliveryPhone) {
          setDeliveryPhone((currentValue) => currentValue || profile.deliveryPhone || "");
        }

        if (!profile.found) {
          setDeliveryCustomerMessage(profile.message ?? "");
          return;
        }

        setCustomerName((currentValue) => currentValue || profile.customerName || "");
        setDeliveryAddress((currentValue) => currentValue || profile.deliveryAddress || "");
        setDeliveryNumber((currentValue) => currentValue || profile.deliveryNumber || "");
        setDeliveryComplement((currentValue) => currentValue || profile.deliveryComplement || "");
        setDeliveryPostalCode((currentValue) =>
          currentValue || normalizeDeliveryPostalCode(getDeliveryFieldValue(profile.deliveryPostalCode)),
        );
        setDeliveryCustomerMessage(
          profile.message ??
            "Encontramos os dados do seu ultimo pedido neste numero. Confira a entrega e altere se precisar.",
        );
      } catch {
        if (!ignore) {
          setDeliveryCustomerMessage("");
        }
      } finally {
        if (!ignore) {
          setIsLoadingDeliveryCustomer(false);
        }
      }
    }

    void loadDeliveryCustomerProfile();

    return () => {
      ignore = true;
    };
  }, [table, isDeliveryChannel, deliveryCustomerToken, publicCode, isOwnerEditMode]);

  useEffect(() => {
    if (!isDeliveryFlow || isOrderingClosed || !isCheckoutStage || totalAmount <= 0) {
      setFreightQuote(null);
      setIsQuotingFreight(false);
      return;
    }

    const normalizedPostalCode = deliveryPostalCode.replace(/\D/g, "");

    if (normalizedPostalCode.length !== 8) {
      setFreightQuote(null);
      setIsQuotingFreight(false);
      return;
    }

    let ignore = false;
    setIsQuotingFreight(true);

    const timeoutId = window.setTimeout(() => {
      quotePublicDeliveryFreight(publicCode, {
        destinationPostalCode: normalizedPostalCode,
        subtotal: totalAmount,
      })
        .then((quote) => {
          if (!ignore) {
            setFreightQuote(quote);
          }
        })
        .catch(() => {
          if (!ignore) {
            setFreightQuote({
              isEnabled: false,
              isConfigured: false,
              isAvailable: false,
              isTestMode: false,
              provider: "",
              originPostalCode: null,
              destinationPostalCode: normalizedPostalCode,
              distanceKm: null,
              baseFee: 0,
              baseDistanceKm: 0,
              chargedDistanceKm: 0,
              pricePerKm: 0,
              freightAmount: 0,
              totalWithFreight: totalAmount,
              fromCache: false,
              message: "Nao foi possivel calcular o frete agora. A unidade confirma a entrega pelo atendimento.",
            });
          }
        })
        .finally(() => {
          if (!ignore) {
            setIsQuotingFreight(false);
          }
        });
    }, 700);

    return () => {
      ignore = true;
      window.clearTimeout(timeoutId);
    };
  }, [deliveryPostalCode, isCheckoutStage, isDeliveryFlow, isOrderingClosed, publicCode, totalAmount]);

  useEffect(() => {
    if (!isPickupFlow) {
      return;
    }

    setFreightQuote(null);
    setIsQuotingFreight(false);
  }, [isPickupFlow]);

  useEffect(() => {
    if (totalUnits === 0 && orderStage !== "menu" && orderStage !== "profile") {
      setOrderStage("menu");
    }
  }, [orderStage, totalUnits]);

  useEffect(() => {
    if (paymentMethod === "Cash") {
      return;
    }

    setNeedsCashChange(false);
    setCashChangeAmount("");
  }, [paymentMethod]);

  useEffect(() => {
    if (canUseOnlinePayment) {
      return;
    }

    setWantsOnlinePayment(false);
  }, [canUseOnlinePayment]);

  useEffect(() => {
    if (!couponValidation) {
      return;
    }

    if (couponValidation.code !== couponCode.trim().toUpperCase()) {
      setCouponValidation(null);
      return;
    }

    if (Math.abs((couponValidation.finalSubtotal + couponValidation.discountAmount) - totalAmount) > 0.01) {
      setCouponValidation(null);
    }
  }, [couponCode, couponValidation, totalAmount]);

  useEffect(() => {
    if (!activeMenuItemEntry || typeof document === "undefined") {
      return;
    }

    const previousOverflow = document.body.style.overflow;
    document.body.style.overflow = "hidden";

    return () => {
      document.body.style.overflow = previousOverflow;
    };
  }, [activeMenuItemEntry]);

  function clearEditUrl() {
    if (typeof window !== "undefined") {
      const targetUrl = new URL(window.location.href);
      targetUrl.searchParams.delete("editar");
      window.history.replaceState({}, "", `${targetUrl.pathname}${targetUrl.search}`);
    }
  }

  function resetComposer() {
    setActiveMenuItemId(null);
    setEditingCartLineId(null);
    setDetailQuantity(1);
    setDetailNotes("");
    setDetailSelectedOptionIds([]);
  }

  async function openComposerForNewItem(menuItemId: string) {
    if (loadingMenuItemId === menuItemId) {
      return;
    }

    const menuItem = menuLookup.get(menuItemId)?.item;

    if (menuItem?.hasAdditionalOptions && menuItem.additionalGroups.length === 0) {
      try {
        setLoadingMenuItemId(menuItemId);
        const details = await getPublicMenuItem(publicCode, menuItemId);
        setMenuItemDetails((currentValue) => ({ ...currentValue, [menuItemId]: details }));
      } catch {
        setErrorMessage("Nao foi possivel abrir os adicionais deste item agora. Tente novamente.");
        return;
      } finally {
        setLoadingMenuItemId("");
      }
    }

    setActiveMenuItemId(menuItemId);
    setEditingCartLineId(null);
    setDetailQuantity(1);
    setDetailNotes("");
    setDetailSelectedOptionIds([]);
  }

  function openComposerForExistingLine(lineId: string) {
    const line = cartLines.find((currentLine) => currentLine.id === lineId);
    const resolvedEntry = line ? menuLookup.get(line.menuItemId) : null;

    if (!line || !resolvedEntry) {
      return;
    }

    setActiveMenuItemId(line.menuItemId);
    setEditingCartLineId(line.id);
    setDetailQuantity(line.quantity);
    setDetailNotes(line.notes);
    setDetailSelectedOptionIds(normalizeSelectedAdditionalIdsForItem(resolvedEntry.item, line.additionalOptionIds));
  }

  function removeCartLine(lineId: string) {
    setCartLines((currentValue) => currentValue.filter((line) => line.id !== lineId));

    if (editingCartLineId === lineId) {
      resetComposer();
    }
  }

  function changeCartLineQuantity(lineId: string, delta: 1 | -1) {
    setCartLines((currentValue) =>
      currentValue.flatMap((line) => {
        if (line.id !== lineId) {
          return [line];
        }

        const nextQuantity = Math.max(0, line.quantity + delta);
        return nextQuantity > 0 ? [{ ...line, quantity: nextQuantity }] : [];
      }),
    );
  }

  function setDetailQuantitySafely(nextQuantity: number) {
    setDetailQuantity(Math.max(1, Math.floor(nextQuantity)));
  }

  function changeAdditionalOptionCount(optionId: string, delta: 1 | -1) {
    if (!activeMenuItemEntry) {
      return;
    }

    setDetailSelectedOptionIds((currentValue) => {
      if (delta < 0) {
        return currentValue.filter((currentOptionId) => currentOptionId !== optionId);
      }

      if (currentValue.includes(optionId)) {
        return currentValue;
      }

      return normalizeSelectedAdditionalIdsForItem(
        activeMenuItemEntry.item,
        [...currentValue, optionId],
      );
    });
  }

  function saveComposer() {
    if (!activeMenuItemEntry) {
      return;
    }

    const normalizedAdditionalOptionIds = normalizeSelectedAdditionalIdsForItem(
      activeMenuItemEntry.item,
      detailSelectedOptionIds,
    );

    if (!hasPricedChoice(activeMenuItemEntry.item, normalizedAdditionalOptionIds)) {
      setErrorMessage("Escolha uma variacao antes de adicionar este item.");
      return;
    }

    const normalizedLine: PublicCartLine = {
      id: editingCartLineId ?? createLocalId(),
      menuItemId: activeMenuItemEntry.item.id,
      quantity: Math.max(1, detailQuantity),
      notes: detailNotes,
      additionalOptionIds: normalizedAdditionalOptionIds,
    };

    setCartLines((currentValue) => {
      if (editingCartLineId) {
        return currentValue.map((line) => (line.id === editingCartLineId ? normalizedLine : line));
      }

      return [...currentValue, normalizedLine];
    });

    resetComposer();
  }

  function resetForNewOrder() {
    setCreatedOrder(null);
    setOrderStage("menu");
    setCustomerName("");
    setDeliveryPhone("");
    setDeliveryAddress("");
    setDeliveryNumber("");
    setDeliveryComplement("");
    setDeliveryPostalCode("");
    setFulfillmentType("delivery");
    setFreightQuote(null);
    setIsQuotingFreight(false);
    setOrderNotes("");
    setPaymentMethod("Undefined");
    setWantsOnlinePayment(false);
    setNeedsCashChange(false);
    setCashChangeAmount("");
    setCartLines([]);
    setWaiterMessage("");
    setErrorMessage("");
    resetComposer();
    clearEditUrl();
  }

  function goToCheckout() {
    if (isOrderingClosed) {
      setErrorMessage(table?.orderingUnavailableMessage || "A unidade esta fora do horario de atendimento e nao recebe pedidos agora.");
      return;
    }

    const menuSelections = buildMenuSelectionPayload(cartLines, menuLookup);

    if (menuSelections.length === 0) {
      setErrorMessage("Escolha pelo menos um item para continuar.");
      return;
    }

    resetComposer();
    setErrorMessage("");
    setOrderStage("checkout");
    window.scrollTo({ top: 0, behavior: "smooth" });
  }

  function goToCart() {
    if (totalUnits === 0) {
      setErrorMessage("Escolha pelo menos um item para ver o pedido.");
      setOrderStage("menu");
      return;
    }

    setErrorMessage("");
    setOrderStage("cart");
    window.scrollTo({ top: 0, behavior: "smooth" });
  }

  function goBackToMenu() {
    setErrorMessage("");
    setOrderStage("menu");
    window.scrollTo({ top: 0, behavior: "smooth" });
  }

  function buildOrderNotesForSubmit() {
    const notes = orderNotes.trim();

    if (!isCashPayment) {
      return notes || undefined;
    }

    const cashChangeNote = needsCashChange
      ? cashChangeAmount.trim()
        ? `Troco para: ${cashChangeAmount.trim()}.`
        : "Precisa de troco."
      : "Troco: nao precisa.";

    return [notes, cashChangeNote].filter(Boolean).join("\n") || undefined;
  }

  function getCategorySelectedQuantity(category: MenuCategory) {
    return category.items.reduce((sum, item) => sum + (cartQuantityByMenuItem[item.id] ?? 0), 0);
  }

  function getCategoryMinPrice(category: MenuCategory) {
    return category.items.length > 0 ? Math.min(...category.items.map((item) => item.price)) : 0;
  }

  function selectCategory(categoryId: string) {
    setActiveCategoryId(categoryId);

    window.requestAnimationFrame(() => {
      const activeTab = Array.from(categoryTabsRef.current?.children ?? [])
        .find((element) => element.getAttribute("data-category-id") === categoryId);
      activeTab?.scrollIntoView({
        behavior: "smooth",
        block: "nearest",
        inline: "center",
      });
      document.getElementById("public-product-scroll")?.scrollTo({
        top: 0,
        behavior: "smooth",
      });
    });
  }

  function handleCategoryTabsWheel(event: WheelEvent<HTMLDivElement>) {
    const tabs = event.currentTarget;
    if (tabs.scrollWidth <= tabs.clientWidth) {
      return;
    }

    const scrollDelta = Math.abs(event.deltaX) > Math.abs(event.deltaY) ? event.deltaX : event.deltaY;
    if (!scrollDelta) {
      return;
    }

    event.preventDefault();
    tabs.scrollLeft += scrollDelta;
  }

  function moveCategory(direction: "previous" | "next") {
    if (activeCategoryIndex < 0) {
      return;
    }

    const nextIndex = direction === "next" ? activeCategoryIndex + 1 : activeCategoryIndex - 1;
    const nextCategory = visibleCategories[nextIndex];
    if (nextCategory) {
      selectCategory(nextCategory.id);
    }
  }

  async function handleCallWaiter() {
    if (isDeliveryChannel) {
      return;
    }

    setWaiterMessage("Chamando atendente...");
    setIsCallingWaiter(true);

    try {
      await createPublicWaiterCall(publicCode);
      setWaiterMessage("Atendente chamado. Aguarde um instante.");
      setErrorMessage("");
    } catch (error) {
      setWaiterMessage("");
      await handleApiError(error, async () => undefined, setErrorMessage, "Nao foi possivel chamar o atendente.");
    } finally {
      setIsCallingWaiter(false);
    }
  }

  async function handleValidateCoupon() {
    const normalizedCode = couponCode.trim();
    if (!normalizedCode) {
      setCouponValidation(null);
      setErrorMessage("Digite o codigo do cupom.");
      return;
    }

    if (totalAmount <= 0) {
      setCouponValidation(null);
      setErrorMessage("Adicione itens antes de aplicar um cupom.");
      return;
    }

    setIsValidatingCoupon(true);
    try {
      const validation = await validatePublicCoupon(publicCode, {
        code: normalizedCode,
        subtotal: totalAmount,
      });
      setCouponValidation(validation);
      setErrorMessage(validation.isValid ? "" : validation.message || "Cupom invalido.");
    } catch (error) {
      setCouponValidation(null);
      await handleApiError(error, async () => undefined, setErrorMessage, "Nao foi possivel validar o cupom.");
    } finally {
      setIsValidatingCoupon(false);
    }
  }

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    const menuSelections = buildMenuSelectionPayload(cartLines, menuLookup);

    if (isOrderingClosed) {
      setErrorMessage(table?.orderingUnavailableMessage || "A unidade esta fora do horario de atendimento e nao recebe pedidos agora.");
      return;
    }

    if (menuSelections.length === 0) {
      setErrorMessage("Escolha pelo menos um item para enviar o pedido.");
      return;
    }

    if (!isOwnerEditMode && couponCode.trim() && !couponValidation?.isValid) {
      setErrorMessage("Aplique um cupom valido ou limpe o campo antes de enviar.");
      return;
    }

    if (isDeliveryChannel) {
      if (!customerName.trim()) {
        setErrorMessage(isPickupFlow ? "Informe o nome para retirada." : "Informe o nome para a entrega.");
        return;
      }

      if (!deliveryPhone.trim()) {
        setErrorMessage("Abra pelo link do WhatsApp ou informe um telefone para contato.");
        return;
      }

      if (isDeliveryFlow && paymentMethod === "Undefined" && !wantsOnlinePayment) {
        setErrorMessage("Escolha a forma de pagamento antes de enviar a entrega.");
        return;
      }

      if (isDeliveryFlow && !hasValidDeliveryPostalCode(deliveryPostalCode)) {
        setErrorMessage("Informe um CEP valido para calcular a entrega.");
        return;
      }

      if (isDeliveryFlow && !deliveryAddress.trim()) {
        setErrorMessage("Informe o endereco da entrega.");
        return;
      }

      if (isDeliveryFlow && !deliveryNumber.trim()) {
        setErrorMessage("Informe o numero do endereco.");
        return;
      }
    }

    if (isOwnerEditMode) {
      const confirmed = window.confirm("Confirmar alteracoes deste pedido?");

      if (!confirmed) {
        return;
      }
    }

    setIsSaving(true);

    try {
      const orderPayload = {
        customerName: isDeliveryChannel ? customerName.trim() : customerName.trim() || undefined,
        notes: buildOrderNotesForSubmit(),
        deliveryPhone: isDeliveryChannel ? deliveryPhone.trim() : undefined,
        deliveryAddress: isDeliveryFlow ? deliveryAddress.trim() : undefined,
        deliveryNumber: isDeliveryFlow ? deliveryNumber.trim() : undefined,
        deliveryComplement: isDeliveryFlow ? deliveryComplement.trim() || undefined : undefined,
        deliveryPostalCode: isDeliveryFlow ? deliveryPostalCode.replace(/\D/g, "") : undefined,
        fulfillmentType: isPickupFlow ? "Pickup" : isDeliveryFlow ? "Delivery" : "Local",
        paymentMethod: wantsOnlinePayment ? "Undefined" : paymentMethod,
        couponCode: !isOwnerEditMode && couponValidation?.isValid ? couponValidation.code : undefined,
        menuSelections,
      };
      const response = isOwnerEditMode && editOrder && editToken
        ? await updateOrder(editToken, editOrder.id, {
            ...orderPayload,
            items: [],
          })
        : sellerCode
          ? await createPublicSellerLinkOrder(sellerCode, publicCode, orderPayload)
          : await createPublicOrder(publicCode, orderPayload);

      setCreatedOrder(response);

      if (!isOwnerEditMode && wantsOnlinePayment) {
        await handleStartOnlinePayment(response);
        return;
      }

      if (!isDeliveryChannel && !isOwnerEditMode) {
        setCustomerName("");
        setDeliveryPhone("");
        setDeliveryAddress("");
        setDeliveryNumber("");
        setDeliveryComplement("");
        setDeliveryPostalCode("");
        setFreightQuote(null);
        setIsQuotingFreight(false);
        setOrderNotes("");
        setNeedsCashChange(false);
        setCashChangeAmount("");
        setCouponCode("");
        setCouponValidation(null);
        setWantsOnlinePayment(false);
        setCartLines([]);
      }

      resetComposer();
      setErrorMessage("");
    } catch (error) {
      if (error instanceof ApiError && error.status === 409) {
        setErrorMessage(error.message || "A unidade esta fora do horario de atendimento e nao recebe pedidos agora.");
      } else if (isOwnerEditMode) {
        await handleApiError(
          error,
          onEditUnauthorized ?? (async () => undefined),
          setErrorMessage,
          "Nao foi possivel salvar as alteracoes do pedido.",
        );
      } else {
        await handleApiError(error, async () => undefined, setErrorMessage, "Nao foi possivel enviar o pedido.");
      }
    } finally {
      setIsSaving(false);
    }
  }

  async function handleStartOnlinePayment(order = createdOrder) {
    if (!order) {
      return;
    }

    try {
      setIsStartingOnlinePayment(true);
      setErrorMessage("");
      const response = await createPublicMercadoPagoCheckout(publicCode, order.id);
      if (!response.available || !response.initPoint) {
        setErrorMessage(response.message || "Pagamento online indisponivel para este pedido.");
        return;
      }

      window.location.href = response.initPoint;
    } catch (error) {
      await handleApiError(error, async () => undefined, setErrorMessage, "Nao foi possivel abrir o pagamento online.");
    } finally {
      setIsStartingOnlinePayment(false);
    }
  }

  const isPublicMessageState = isOrderingClosed || Boolean(createdOrder);
  const createdOrderIsPickup = createdOrder?.fulfillmentType === "Pickup";
  const pickupEstimateLabel = table?.pickupEstimatedMinutes
    ? `Tempo estimado para retirada: ${table.pickupEstimatedMinutes} minutos.`
    : "";
  const deliveryEstimateLabel = table?.deliveryEstimatedMinutes
    ? `Tempo estimado para entrega: ${table.deliveryEstimatedMinutes} minutos.`
    : "";
  const activeFulfillmentEstimate = isDeliveryFlow ? deliveryEstimateLabel : isPickupFlow ? pickupEstimateLabel : "";

  return (
    <main className="page-shell public-shell">
      <section
        className={`surface-card public-card ambient-panel public-menu-card public-app-card ${
          isPublicMessageState ? "is-message-state" : ""
        }`}
      >
        <div className="public-app-header">
          <div className="public-store-lockup">
            <div className="public-store-logo" aria-hidden="true">
              {restaurantLogoUrl ? <img src={restaurantLogoUrl} alt="" /> : <span>{restaurantInitial}</span>}
            </div>
            <div className="public-store-copy">
              <strong className="public-restaurant-name">{table?.restaurantName || "Carregando..."}</strong>
              <span>{isDeliveryChannel ? "Delivery e retirada" : sellerName ? `Vendedor · ${sellerName}` : table?.tableName || "Mesa"}</span>
            </div>
          </div>

          <span className="public-zp-wordmark">ZeroPaper</span>
        </div>

        {loading ? (
          <p className="loading-state">{isDeliveryChannel ? "Abrindo delivery..." : "Abrindo mesa..."}</p>
        ) : (
          <>
            {isOwnerEditMode && editOrder ? (
              <div className="public-edit-mode-banner">
                <div>
                  <span className="eyebrow">Editando pedido</span>
                  <strong>Pedido #{editOrder.number}</strong>
                </div>
                <p>Horario original preservado: {formatDateTime(editOrder.submittedAtUtc)}</p>
              </div>
            ) : null}

            <div className="public-table-header">
              <span className="eyebrow">
                {isOwnerEditMode
                  ? "Edicao"
                  : isDeliveryChannel
                  ? isOrderingClosed
                    ? "Fora do horario"
                    : createdOrder
                      ? "Pedido enviado"
                      : "Delivery"
                  : "Mesa"}
              </span>
              <h1 className="public-title">
                {isOwnerEditMode && editOrder
                  ? `Editando pedido #${editOrder.number}`
                  : isDeliveryChannel
                  ? isOrderingClosed
                      ? "Atendimento fechado agora"
                      : createdOrder
                      ? "Pedido enviado com sucesso"
                      : "Pedido da unidade"
                  : table?.tableName}
              </h1>
              {isOwnerEditMode ? (
                <p className="public-title-support">
                  Use o cardapio normal para alterar itens, adicionais, observacoes e pagamento. Ao salvar, o pedido existente sera atualizado.
                </p>
              ) : isDeliveryChannel ? (
                <p className="public-title-support">
                  {isOrderingClosed
                      ? "A unidade esta fora do horario configurado. O link fica protegido e nao recebe pedidos ate o atendimento reabrir."
                    : createdOrder
                      ? "O pedido foi enviado para a unidade e ja entrou no fluxo operacional. Se precisar corrigir algo, fale com a unidade pelo atendimento."
                      : "Monte o pedido, escolha entrega ou retirada e finalize com os dados necessarios."}
                </p>
              ) : !createdOrder ? (
                <p className="public-title-support">
                  Escolha os itens com calma e siga para uma etapa separada para revisar tudo antes de enviar o pedido para a unidade.
                </p>
              ) : null}
            </div>

            {isOrderingClosed ? (
              <section className="surface-card public-success-card public-order-complete">
                <span className="eyebrow">Fora do horario</span>
                <h2>A unidade nao recebe pedidos agora.</h2>
                <p>
                  {table?.orderingUnavailableMessage ||
                    "O sistema de pedidos fica fechado fora do horario de atendimento configurado pela unidade."}
                </p>
                {table?.serviceStartTime && table?.serviceEndTime ? (
                  <p className="field-hint">
                    Atendimento para pedidos: {table.serviceStartTime} as {table.serviceEndTime}.
                  </p>
                ) : null}
                <p className="field-hint">
                  Voce pode voltar neste link quando o atendimento reabrir para montar e enviar o pedido com seguranca.
                </p>
              </section>
            ) : createdOrder ? (
              <section className="surface-card public-success-card public-order-complete">
                <span className="eyebrow">{isOwnerEditMode ? "Pedido atualizado" : "Pedido enviado"}</span>
                <h2>{isOwnerEditMode ? "Alteracoes salvas." : isDeliveryChannel ? "Tudo certo por aqui." : "Obrigado."}</h2>
                <p>
                  {isOwnerEditMode
                    ? `Pedido #${createdOrder.number} atualizado sem mudar o horario original.`
                    : isDeliveryChannel
                    ? createdOrderIsPickup
                      ? "Seu pedido para retirada foi enviado para a unidade."
                      : "Seu delivery foi enviado para a unidade."
                    : `Pedido #${createdOrder.number} enviado para a unidade.`}
                </p>

                <div className="public-success-summary-grid">
                  <div className="public-success-stat">
                    <span>Total</span>
                    <strong>{formatCurrency(createdOrder.totalAmount)}</strong>
                  </div>
                  <div className="public-success-stat">
                    <span>Pagamento</span>
                    <strong>{formatPaymentMethod(createdOrder.paymentMethod)}</strong>
                  </div>
                  <div className="public-success-stat">
                    <span>Enviado em</span>
                    <strong>{formatDateTime(createdOrder.submittedAtUtc)}</strong>
                  </div>
                </div>

                {createdOrder.deliveryAssistantMessage
                  ? createdOrder.deliveryAssistantMessage.split("\n\n").map((paragraph) => (
                      <p key={paragraph} className="field-hint">
                        {paragraph}
                      </p>
                    ))
                  : <p>Deseja fazer um novo pedido?</p>}
                {!isDeliveryChannel && !isOwnerEditMode ? <p className="field-hint">Essa confirmacao fecha sozinha em alguns segundos.</p> : null}
                {isDeliveryChannel && createdOrder.publicDeliveryCustomerUrl ? (
                  <p className="field-hint">
                    Nos proximos pedidos, use o link pessoal enviado no WhatsApp para abrir o delivery com seus dados preenchidos.
                  </p>
                ) : null}

                <div className="toolbar-actions public-success-actions">
                  {!isDeliveryChannel ? (
                    <button className="ghost-link button-link" type="button" onClick={() => void handleCallWaiter()} disabled={isCallingWaiter}>
                      {isCallingWaiter ? "Chamando..." : "Chamar atendente"}
                    </button>
                  ) : null}
                  {isDeliveryChannel && createdOrder.publicDeliveryCustomerUrl ? (
                    <a className="ghost-link button-link" href={createdOrder.publicDeliveryCustomerUrl}>
                      Meu link de delivery
                    </a>
                  ) : null}
                  {isOwnerEditMode && editBackHref ? (
                    <a className="primary-link button-link" href={editBackHref}>
                      Voltar aos pedidos
                    </a>
                  ) : (
                    <button className="ghost-link button-link" type="button" onClick={resetForNewOrder}>
                      Fazer novo pedido
                    </button>
                  )}
                </div>
              </section>
            ) : visibleCategories.length ? (
              <form
                className={`${orderStage === "menu" ? "public-menu-stage-layout" : "public-checkout-layout"} public-order-stage public-order-stage-${orderStage}`}
                onSubmit={handleSubmit}
              >
                <nav className="pnf-bar" aria-label="Navegacao do pedido">
                  <button
                    className={`pnf-tab ${orderStage === "menu" ? "is-active" : ""}`}
                    type="button"
                    aria-current={orderStage === "menu" ? "page" : undefined}
                    onClick={goBackToMenu}
                  >
                    <Menu size={20} strokeWidth={1.75} />
                    <span>Cardapio</span>
                  </button>

                  <button
                    className={`pnf-tab ${isCartStage ? "is-active" : ""}`}
                    type="button"
                    aria-current={isCartStage ? "page" : undefined}
                    onClick={goToCart}
                    disabled={totalUnits === 0}
                  >
                    <ShoppingCart size={20} strokeWidth={1.75} />
                    <span>Carrinho</span>
                    {totalUnits > 0 ? <strong className="pnf-badge">{totalUnits}</strong> : null}
                  </button>

                  <button
                    className={`pnf-tab ${isCheckoutStage ? "is-active" : ""}`}
                    type="button"
                    aria-current={isCheckoutStage ? "page" : undefined}
                    onClick={goToCheckout}
                    disabled={totalUnits === 0 || isOrderingClosed}
                  >
                    <CheckCircle2 size={20} strokeWidth={1.75} />
                    <span>Finalizar</span>
                  </button>

                  {canOpenCustomerProfile ? (
                    <button
                      className={`pnf-tab ${isProfileStage ? "is-active" : ""}`}
                      type="button"
                      aria-current={isProfileStage ? "page" : undefined}
                      onClick={() => setOrderStage("profile")}
                    >
                      <User size={20} strokeWidth={1.75} />
                      <span>Perfil</span>
                    </button>
                  ) : null}

                  <div className="pnf-total">
                    <span>Total</span>
                    <strong>{formatCurrency(isCheckoutStage ? (isDeliveryChannel ? checkoutTotalAmount : discountedItemsTotal) : totalAmount)}</strong>
                  </div>

                  {sellerName ? (
                    <div className="pnf-seller-chip" aria-label={`Pedido via ${sellerName}`}>
                      <span className="pnf-seller-label">via</span>
                      <span className="pnf-seller-name">{sellerName}</span>
                    </div>
                  ) : null}
                </nav>

                <div className="public-menu-main">
                  {!isProfileStage ? (
                  <section className="surface-card public-delivery-stage-card">
                    <div className="public-delivery-stage-head">
                      <div>
                        <span className="eyebrow">Etapas do pedido</span>
                        <h2>
                          {isCheckoutStage
                            ? isDeliveryChannel
                              ? "Finalize seu pedido"
                              : "Confirme o envio"
                            : isCartStage
                              ? "Revise seu pedido"
                              : "Monte seu pedido"}
                        </h2>
                        <p>
                          {isCheckoutStage
                            ? isDeliveryChannel
                              ? "Informe entrega ou retirada, pagamento, observacoes e confirme o total."
                              : "Defina pagamento, observacoes e envie para a unidade."
                            : isCartStage
                              ? "Confira quantidades, adicionais e observacoes antes de finalizar."
                              : isDeliveryChannel
                                ? "Escolha os itens. Depois voce confere o pedido e finaliza."
                                : "Escolha os itens. Depois voce confere o pedido e envia para a unidade."}
                        </p>
                      </div>

                      <div className="public-delivery-stage-pills">
                        <span className={`status-chip neutral ${orderStage === "menu" ? "is-selected-filter" : ""}`}>1. Cardapio</span>
                        <span className={`status-chip neutral ${isCartStage ? "is-selected-filter" : ""}`}>2. Pedido</span>
                        <span className={`status-chip neutral ${isCheckoutStage ? "is-selected-filter" : ""}`}>3. Finalizar</span>
                      </div>
                    </div>
                  </section>
                  ) : null}

                  {isProfileStage && canOpenCustomerProfile ? (
                    <PublicCustomerProfilePanel code={deliveryCustomerToken!} onClose={() => setOrderStage("menu")} asPage />
                  ) : isCartStage ? (
                    <section className="surface-card public-delivery-review-card">
                      <div className="pcc-header">
                        <div>
                          <span className="eyebrow">Seu pedido</span>
                          <h2>Revise os itens</h2>
                        </div>
                        <div className="pcc-header-count">
                          <span>{totalUnits} {totalUnits === 1 ? "item" : "itens"}</span>
                          <strong>{formatCurrency(totalAmount)}</strong>
                        </div>
                      </div>

                      {cartItems.length === 0 ? (
                        <div className="module-empty-state compact-empty-state">
                          <ShoppingCart size={28} strokeWidth={1.5} />
                          <p>Nenhum item no pedido.</p>
                        </div>
                      ) : (
                        <div className="pcc-items">
                          {cartItems.map((entry) => (
                            <div key={entry.id} className="pcc-item">
                              {entry.item.imageUrl && !brokenImageIds[entry.item.id] ? (
                                <img
                                  className="pcc-thumb"
                                  src={entry.item.imageUrl}
                                  alt=""
                                  loading="lazy"
                                  onError={() =>
                                    setBrokenImageIds((v) => ({ ...v, [entry.item.id]: true }))
                                  }
                                />
                              ) : (
                                <div className="pcc-thumb-placeholder" aria-hidden="true">
                                  {entry.item.name.slice(0, 1)}
                                </div>
                              )}

                              <div className="pcc-body">
                                <div className="pcc-name-row">
                                  <strong>{entry.item.name}</strong>
                                  <span className="pcc-item-total">{formatCurrency(entry.totalPrice)}</span>
                                </div>

                                <p className="pcc-unit-price">{entry.line.quantity}x &middot; {formatCurrency(entry.item.price)} un.</p>

                                {entry.selectedAdditionals.length > 0 ? (
                                  <div className="pcc-chips">
                                    {entry.selectedAdditionals.map((sel) => (
                                      <span key={`${entry.id}-${sel.id}`} className="pcc-chip">
                                        {formatComplementLabel(sel)}
                                        {sel.price > 0 ? (
                                          <span className="pcc-chip-price">+{formatCurrency(sel.price * entry.line.quantity)}</span>
                                        ) : null}
                                      </span>
                                    ))}
                                  </div>
                                ) : null}

                                {entry.line.notes.trim() ? (
                                  <p className="pcc-note">
                                    <MessageSquare size={11} strokeWidth={2} />
                                    {entry.line.notes.trim()}
                                  </p>
                                ) : null}

                                <div className="pcc-foot">
                                  <div className="pcc-stepper" aria-label={`Quantidade de ${entry.item.name}`}>
                                    <button type="button" onClick={() => changeCartLineQuantity(entry.id, -1)}>−</button>
                                    <span className="pcc-stepper-qty">{entry.line.quantity}</span>
                                    <button type="button" onClick={() => changeCartLineQuantity(entry.id, 1)}>+</button>
                                  </div>
                                  <div className="pcc-actions">
                                    <button className="pcc-action-btn" type="button" onClick={() => openComposerForExistingLine(entry.id)}>
                                      Editar
                                    </button>
                                    <button className="pcc-action-btn is-remove" type="button" onClick={() => removeCartLine(entry.id)}>
                                      Remover
                                    </button>
                                  </div>
                                </div>
                              </div>
                            </div>
                          ))}
                        </div>
                      )}

                      {cartItems.length > 0 ? (
                        <div className="pcc-summary">
                          <div className="pcc-subtotal">
                            <span>Subtotal</span>
                            <strong>{formatCurrency(totalAmount)}</strong>
                          </div>
                          <button className="pcc-checkout-btn" type="button" onClick={goToCheckout} disabled={isOrderingClosed}>
                            <span>Finalizar pedido</span>
                            <div className="pcc-checkout-btn-right">
                              <strong>{formatCurrency(totalAmount)}</strong>
                              <CheckCircle2 size={18} strokeWidth={2} />
                            </div>
                          </button>
                        </div>
                      ) : null}
                    </section>
                  ) : !isCheckoutStage ? (
                    <div className="public-category-browser">
                      <section className="pmc-search-bar" aria-label="Buscar no cardapio">
                        <div className="pmc-search-field">
                          <Search size={16} strokeWidth={2.2} aria-hidden="true" className="pmc-search-icon" />
                          <input
                            id="publicMenuSearch"
                            value={menuSearchTerm}
                            onChange={(event) => setMenuSearchTerm(event.target.value)}
                            placeholder="Buscar no cardapio..."
                            type="search"
                            aria-label="Buscar no cardapio"
                          />
                        </div>
                      </section>

                      <section className="surface-card public-category-selector">
                        <div className="pmc-selector-head">
                          <div>
                            <span className="eyebrow">Cardapio</span>
                            <h2>Categorias</h2>
                          </div>
                          <span className="pmc-category-count">{visibleCategories.length}</span>
                        </div>

                        <div
                          ref={categoryTabsRef}
                          className="public-category-tabs"
                          role="tablist"
                          aria-label="Categorias do cardapio"
                          onWheel={handleCategoryTabsWheel}
                        >
                          {visibleCategories.map((category) => {
                            const selectedQuantity = getCategorySelectedQuantity(category);
                            const isActive = activeCategory?.id === category.id;

                            return (
                              <button
                                key={category.id}
                                className={`public-category-tab ${isActive ? "is-active" : ""}`}
                                type="button"
                                role="tab"
                                aria-selected={isActive}
                                data-category-id={category.id}
                                onClick={() => selectCategory(category.id)}
                              >
                                {category.imageUrl ? (
                                  <img className="public-category-tab-image" src={category.imageUrl} alt="" loading="lazy" />
                                ) : null}
                                <span>{category.name}</span>
                                {selectedQuantity > 0 ? <strong>{selectedQuantity}</strong> : null}
                              </button>
                            );
                          })}
                        </div>
                      </section>

                      {activeCategory ? (
                        <section id="public-category-view" className="public-category-details is-open public-category-focus-card">
                          <div className="public-category-content">
                            <div className="public-product-grid compact-public-product-grid">
                              {displayedMenuEntries.length === 0 ? (
                                <div className="module-empty-state compact-empty-state public-menu-search-empty">
                                  <strong>Nenhum item encontrado.</strong>
                                  <p>Tente outro termo ou escolha outra categoria.</p>
                                </div>
                              ) : null}

                              {displayedMenuEntries.map((item) => {
                                const quantity = cartQuantityByMenuItem[item.id] ?? 0;
                                const additionalLimit = getConfiguredAdditionalLimit(item);
                                const canChooseAdditionals = itemAcceptsSelectableAdditionals(item);
                                const visualImageUrl = item.imageUrl ?? activeCategory.imageUrl;

                                return (
                                  <article
                                    key={item.id}
                                    className={`public-product-card compact-public-product-card ${quantity > 0 ? "is-selected" : ""}`}
                                    role="button"
                                    tabIndex={0}
                                    onClick={() => void openComposerForNewItem(item.id)}
                                    onKeyDown={(event) => {
                                      if (event.key === "Enter" || event.key === " ") {
                                        event.preventDefault();
                                        void openComposerForNewItem(item.id);
                                      }
                                    }}
                                  >
                                    {visualImageUrl && !brokenImageIds[item.id] ? (
                                      <img
                                        className="public-product-image compact-public-product-image"
                                        src={visualImageUrl}
                                        alt={item.name}
                                        loading="lazy"
                                        onError={() =>
                                          setBrokenImageIds((v) => ({ ...v, [item.id]: true }))
                                        }
                                      />
                                    ) : (
                                      <div className="public-product-image compact-public-product-image public-product-image-placeholder pmc-item-placeholder" aria-hidden="true">
                                        <span>{item.name.slice(0, 1)}</span>
                                      </div>
                                    )}

                                    <div className="public-product-copy compact-public-product-copy">
                                      <div className="public-product-top compact-public-product-top">
                                        <div>
                                          {item.accentLabel ? <span className="eyebrow compact-product-eyebrow">{item.accentLabel}</span> : null}
                                          <h2>{item.name}</h2>
                                          {item.description ? <p>{item.description}</p> : null}
                                        </div>
                                        <strong>{formatMenuItemPrice(item)}</strong>
                                      </div>

                                      {quantity > 0 ? (
                                        <div className="pmc-product-footer">
                                          <span className="pmc-qty-badge">
                                            <Plus size={11} strokeWidth={2.5} />
                                            {quantity} no pedido
                                          </span>
                                        </div>
                                      ) : null}
                                    </div>
                                  </article>
                                );
                              })}
                            </div>
                          </div>
                        </section>
                      ) : null}
                    </div>
                  ) : null}
                </div>

                {isCheckoutStage ? (
                <aside className="public-checkout-v2">
                  {isDeliveryChannel ? (
                    <div className="pco-fulfillment-strip">
                      <button
                        type="button"
                        className={`pco-fulfillment-btn${isDeliveryFlow ? " is-active" : ""}`}
                        onClick={() => setFulfillmentType("delivery")}
                      >
                        <Truck size={22} strokeWidth={1.75} />
                        <span>Entrega</span>
                        {deliveryEstimateLabel ? <small>{deliveryEstimateLabel}</small> : null}
                      </button>
                      <button
                        type="button"
                        className={`pco-fulfillment-btn${isPickupFlow ? " is-active" : ""}`}
                        onClick={() => setFulfillmentType("pickup")}
                      >
                        <Store size={22} strokeWidth={1.75} />
                        <span>Retirada</span>
                        {pickupEstimateLabel ? <small>{pickupEstimateLabel}</small> : null}
                      </button>
                    </div>
                  ) : null}

                  {isDeliveryChannel ? (
                    <div className="pco-fields-group">
                      {isLoadingDeliveryCustomer ? (
                        <p className="module-feedback success compact-feedback">Buscando seus dados...</p>
                      ) : deliveryCustomerMessage ? (
                        <p className="module-feedback success compact-feedback">{deliveryCustomerMessage}</p>
                      ) : null}
                      <div className="pco-field-row">
                        <User size={16} strokeWidth={1.75} className="pco-field-icon" />
                        <input
                          id="deliveryCustomerName"
                          value={customerName}
                          onChange={(event) => setCustomerName(event.target.value)}
                          placeholder={isPickupFlow ? "Nome para retirada" : "Nome para entrega"}
                          className="pco-input"
                        />
                      </div>
                      {hasPhoneFromDeliveryLink ? (
                        <div className="pco-field-row pco-locked-row">
                          <Phone size={16} strokeWidth={1.75} className="pco-field-icon" />
                          <span>WhatsApp ···· {deliveryPhone.replace(/\D/g, "").slice(-4)}</span>
                        </div>
                      ) : (
                        <div className="pco-field-row">
                          <Phone size={16} strokeWidth={1.75} className="pco-field-icon" />
                          <input
                            id="deliveryPhone"
                            value={deliveryPhone}
                            onChange={(event) => setDeliveryPhone(event.target.value)}
                            placeholder="WhatsApp ou telefone"
                            inputMode="tel"
                            className="pco-input"
                          />
                        </div>
                      )}
                    </div>
                  ) : null}

                  {isDeliveryFlow ? (
                    <div className="pco-fields-group">
                      <div className="pco-field-row">
                        <MapPin size={16} strokeWidth={1.75} className="pco-field-icon" />
                        <input
                          id="deliveryPostalCode"
                          value={deliveryPostalCode}
                          onChange={(event) => setDeliveryPostalCode(normalizeDeliveryPostalCode(event.target.value))}
                          placeholder="CEP 00000-000"
                          inputMode="numeric"
                          autoComplete="postal-code"
                          className="pco-input pco-input-cep"
                        />
                      </div>
                      <div className="pco-field-row pco-field-indent">
                        <input
                          id="deliveryAddress"
                          value={deliveryAddress}
                          onChange={(event) => setDeliveryAddress(event.target.value)}
                          placeholder="Rua e bairro"
                          className="pco-input"
                        />
                      </div>
                      <div className="pco-field-row pco-field-indent pco-address-pair">
                        <input
                          id="deliveryNumber"
                          value={deliveryNumber}
                          onChange={(event) => setDeliveryNumber(event.target.value)}
                          placeholder="Numero"
                          className="pco-input"
                        />
                        <input
                          id="deliveryComplement"
                          value={deliveryComplement}
                          onChange={(event) => setDeliveryComplement(event.target.value)}
                          placeholder="Complemento"
                          className="pco-input"
                        />
                      </div>
                      <div className={`pco-freight-chip${freightQuote?.isAvailable ? " is-ready" : ""}`}>
                        <Truck size={14} strokeWidth={1.75} />
                        <span>
                          {isQuotingFreight
                            ? "Calculando frete..."
                            : freightQuote?.isAvailable
                              ? `Frete: ${formatCurrency(freightQuote.freightAmount)}`
                              : "Frete calculado apos CEP completo"}
                        </span>
                        {freightQuote?.isAvailable ? (
                          <strong>Total: {formatCurrency(checkoutTotalAmount)}</strong>
                        ) : null}
                      </div>
                    </div>
                  ) : isPickupFlow ? (
                    <div className="pco-freight-chip is-ready">
                      <Store size={14} strokeWidth={1.75} />
                      <span>{pickupEstimateLabel || "Sem frete — retirada no local"}</span>
                    </div>
                  ) : null}

                  <div className="pco-field-row pco-field-row-standalone">
                    <MessageSquare size={16} strokeWidth={1.75} className="pco-field-icon" />
                    <input
                      id="orderNotes"
                      value={orderNotes}
                      onChange={(event) => setOrderNotes(event.target.value)}
                      placeholder="Observacoes (opcional)"
                      className="pco-input"
                    />
                  </div>

                  {!isOwnerEditMode ? (
                    <div className="pco-coupon-row">
                      <Tag size={16} strokeWidth={1.75} className="pco-field-icon" />
                      <input
                        id="couponCode"
                        value={couponCode}
                        onChange={(event) => setCouponCode(event.target.value.toUpperCase())}
                        placeholder="Cupom"
                        autoComplete="off"
                        className="pco-input"
                      />
                      <button
                        className="pco-coupon-apply ghost-link button-link"
                        type="button"
                        disabled={isValidatingCoupon || totalAmount <= 0 || !couponCode.trim()}
                        onClick={() => void handleValidateCoupon()}
                      >
                        {isValidatingCoupon ? "..." : couponValidation?.isValid ? <Check size={14} /> : "Aplicar"}
                      </button>
                      {couponValidation?.message ? (
                        <span className={`module-feedback ${couponValidation.isValid ? "success" : "error"} compact-feedback pco-coupon-msg`}>
                          {couponValidation.message}
                        </span>
                      ) : null}
                    </div>
                  ) : null}

                  <div className="pco-section-head">
                    <CreditCard size={15} strokeWidth={1.75} />
                    <span>Pagamento</span>
                    {wantsOnlinePayment ? <span className="pco-online-badge">Online</span> : null}
                  </div>
                  <div className="pco-payment-grid">
                    {canUseOnlinePayment ? (
                      <button
                        type="button"
                        className={`pco-payment-btn${wantsOnlinePayment ? " is-active" : ""}`}
                        onClick={() => {
                          setWantsOnlinePayment(true);
                          setPaymentMethod("Pix");
                          setNeedsCashChange(false);
                          setCashChangeAmount("");
                        }}
                      >
                        <Smartphone size={20} strokeWidth={1.5} />
                        <span>Online</span>
                      </button>
                    ) : null}
                    {paymentOptions.map((option) => (
                      <button
                        key={option.value}
                        type="button"
                        className={`pco-payment-btn${!wantsOnlinePayment && paymentMethod === option.value ? " is-active" : ""}`}
                        onClick={() => {
                          setWantsOnlinePayment(false);
                          setPaymentMethod(option.value);
                        }}
                      >
                        {option.value === "Cash" ? (
                          <Banknote size={20} strokeWidth={1.5} />
                        ) : option.value === "Pix" ? (
                          <QrCode size={20} strokeWidth={1.5} />
                        ) : (
                          <CreditCard size={20} strokeWidth={1.5} />
                        )}
                        <span>{option.label}</span>
                      </button>
                    ))}
                  </div>

                  {isCashPayment ? (
                    <div className="pco-change-panel">
                      <div className="pco-change-head">
                        <Banknote size={15} strokeWidth={1.75} />
                        <span>Precisa de troco?</span>
                        <div className="pco-change-toggle">
                          <button
                            className={`ghost-link button-link choice-pill${!needsCashChange ? " is-selected-filter" : ""}`}
                            type="button"
                            onClick={() => { setNeedsCashChange(false); setCashChangeAmount(""); }}
                          >Nao</button>
                          <button
                            className={`ghost-link button-link choice-pill${needsCashChange ? " is-selected-filter" : ""}`}
                            type="button"
                            onClick={() => setNeedsCashChange(true)}
                          >Sim</button>
                        </div>
                      </div>
                      {needsCashChange ? (
                        <div className="pco-change-body">
                          <input
                            id="cashChangeAmount"
                            value={cashChangeAmount}
                            onChange={(event) => setCashChangeAmount(event.target.value)}
                            onBlur={() => {
                              const parsedAmount = parseCurrencyInput(cashChangeAmount);
                              if (parsedAmount > 0) setCashChangeAmount(formatCurrency(parsedAmount));
                            }}
                            placeholder="Troco para quanto?"
                            inputMode="decimal"
                            className="pco-input"
                          />
                          <div className="public-cash-note-chips">
                            {cashChangeQuickAmounts.map((amount) => (
                              <button
                                key={amount}
                                className={`ghost-link button-link choice-pill${cashChangeAmount === amount ? " is-selected-filter" : ""}`}
                                type="button"
                                onClick={() => setCashChangeAmount(amount)}
                              >
                                {amount.replace(",00", "")}
                              </button>
                            ))}
                          </div>
                        </div>
                      ) : null}
                    </div>
                  ) : null}

                  {!isDeliveryChannel && waiterMessage ? <p className="module-feedback success">{waiterMessage}</p> : null}

                  <div className="pco-submit-bar">
                    <div className="pco-submit-total">
                      <span>Total</span>
                      <strong>{formatCurrency(isDeliveryChannel ? checkoutTotalAmount : discountedItemsTotal)}</strong>
                      {couponDiscountAmount > 0 ? <small>-{formatCurrency(couponDiscountAmount)}</small> : null}
                    </div>
                    <div className="pco-submit-actions">
                      {!isDeliveryChannel ? (
                        <button className="ghost-link button-link" type="button" disabled={isCallingWaiter} onClick={() => void handleCallWaiter()}>
                          {isCallingWaiter ? "Chamando..." : "Chamar atendente"}
                        </button>
                      ) : null}
                      <button className="primary-link button-link" type="submit" disabled={isSaving || totalUnits === 0 || isOrderingClosed}>
                        {isSaving
                          ? wantsOnlinePayment ? "Abrindo pagamento..." : "Salvando..."
                          : isOrderingClosed ? "Fora do horario"
                          : isOwnerEditMode ? "Salvar alteracoes"
                          : wantsOnlinePayment ? "Enviar e pagar" : "Enviar pedido"}
                      </button>
                    </div>
                  </div>
                </aside>
                ) : null}
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

      {activeMenuItemEntry ? (
        <div className="public-item-detail-backdrop" onClick={resetComposer}>
          <section className="surface-card public-item-detail-modal public-composer-v2" onClick={(event) => event.stopPropagation()}>

            {/* Header: back button + name/description + thumbnail */}
            <div className="public-composer-header">
              <button className="public-composer-back" type="button" onClick={resetComposer} aria-label="Fechar">
                ←
              </button>
              <div className="public-composer-title-block">
                <span className="eyebrow">{activeMenuItemEntry.categoryName}</span>
                <h2>{activeMenuItemEntry.item.name}</h2>
                {activeMenuItemEntry.item.description ? (
                  <p>{activeMenuItemEntry.item.description}</p>
                ) : null}
              </div>
              {activeMenuItemEntry.item.imageUrl && !brokenImageIds[activeMenuItemEntry.item.id] ? (
                <img
                  className="public-composer-thumb"
                  src={activeMenuItemEntry.item.imageUrl}
                  alt=""
                  loading="lazy"
                  onError={() =>
                    setBrokenImageIds((currentValue) => ({
                      ...currentValue,
                      [activeMenuItemEntry.item.id]: true,
                    }))
                  }
                />
              ) : null}
            </div>

            {/* Scrollable body */}
            <div className="public-item-detail-scroll">

              {/* Quantity stepper + running total in one row */}
              <div className="public-composer-qty-row">
                <div className="public-product-actions compact-public-product-actions public-composer-stepper">
                  <button className="ghost-link button-link" type="button" onClick={() => setDetailQuantitySafely(detailQuantity - 1)}>
                    -
                  </button>
                  <span>{detailQuantity}</span>
                  <button className="ghost-link button-link" type="button" onClick={() => setDetailQuantitySafely(detailQuantity + 1)}>
                    +
                  </button>
                </div>
                <div className="public-composer-qty-label">
                  <span>Quantidade</span>
                  <strong>{detailQuantity} {detailQuantity === 1 ? "unidade" : "unidades"}</strong>
                </div>
                <div className="public-composer-running-total">
                  <span>Total da linha</span>
                  <strong>{formatCurrency(detailPreviewTotal)}</strong>
                </div>
              </div>

              {activeItemAcceptsAdditionals && detailQuantity > 1 ? (
                <p className="public-composer-line-note">
                  Adicionais aplicados a linha inteira dos {detailQuantity} itens.
                </p>
              ) : null}

              {/* Notes */}
              <div className="field-group public-item-detail-notes">
                <label htmlFor="detailNotes">Observacoes deste item</label>
                <textarea
                  id="detailNotes"
                  className="public-line-notes"
                  value={detailNotes}
                  onChange={(event) => setDetailNotes(event.target.value)}
                  placeholder="Ex.: sem cebola, ponto da carne, caprichar no molho..."
                />
              </div>

              {/* Additionals */}
              {activeItemAcceptsAdditionals ? (
                <div className="pic-addon-stack">
                  {activeMenuItemEntry.item.additionalGroups
                    .filter((group) => getConfiguredGroupAdditionalLimit(group) !== 0 && group.options.length > 0)
                    .map((group) => {
                      const groupLimit = getConfiguredGroupAdditionalLimit(group);

                      return (
                        <div key={group.id} className="pic-addon-group">
                          {group.options.length > 1 ? (
                            <p className="pic-addon-hint">
                              {groupLimit === null
                                ? group.allowMultiple
                                  ? "Multipla escolha"
                                  : "Opcional"
                                : groupLimit === 1
                                  ? "Escolha 1 opcao"
                                  : `Ate ${groupLimit} opcoes`}
                            </p>
                          ) : null}

                          <div className="pic-addon-list">
                            {group.options.map((option) => {
                              const selectedCount = countSelectedOption(detailSelectedOptionIds, option.id);
                              const isSelected = selectedCount > 0;
                              const canAdd = canAddAdditionalOption(
                                activeMenuItemEntry.item,
                                detailSelectedOptionIds,
                                option.id,
                              );

                              return (
                                <button
                                  key={option.id}
                                  className={`pic-addon-row${isSelected ? " is-on" : ""}${!isSelected && !canAdd ? " is-blocked" : ""}`}
                                  type="button"
                                  disabled={!isSelected && !canAdd}
                                  onClick={() => changeAdditionalOptionCount(option.id, isSelected ? -1 : 1)}
                                  aria-pressed={isSelected}
                                >
                                  <span className="pic-addon-check" aria-hidden="true">
                                    {isSelected ? <Check size={14} strokeWidth={2.5} /> : null}
                                  </span>
                                  <span className="pic-addon-copy">
                                    <strong>{option.name}</strong>
                                    <span>
                                      {isSelected
                                        ? detailQuantity > 1
                                          ? `Nos ${detailQuantity} itens`
                                          : "Adicionado"
                                        : option.price > 0
                                          ? `${formatCurrency(option.price)} por un.`
                                          : "Sem custo adicional"}
                                    </span>
                                  </span>
                                  {option.price > 0 ? (
                                    <em className="pic-addon-price">
                                      {formatCurrency(option.price * (isSelected ? detailQuantity : 1))}
                                    </em>
                                  ) : null}
                                </button>
                              );
                            })}
                          </div>
                        </div>
                      );
                    })}
                </div>
              ) : null}
            </div>

            {/* Sticky CTA with price embedded */}
            <div className="public-item-detail-sticky-action">
              <button
                className="primary-link button-link public-composer-cta"
                type="button"
                onClick={saveComposer}
                disabled={!hasPricedChoice(activeMenuItemEntry.item, detailSelectedOptionIds)}
              >
                <span>{editingCartLineId ? "Salvar item" : "Adicionar ao pedido"}</span>
              </button>
            </div>
          </section>
        </div>
      ) : null}

    </main>
  );
}
