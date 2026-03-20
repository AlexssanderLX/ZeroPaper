"use client";

import type { Dispatch, SetStateAction } from "react";
import { ApiError, type OrderItemInput } from "@/lib/api";

export type AsyncVoid = () => Promise<void>;

export type OrderDraftItem = {
  name: string;
  quantity: string;
  unitPrice: string;
  notes: string;
};

export type StockDraft = {
  name: string;
  category: string;
  unit: string;
  currentQuantity: string;
  minimumQuantity: string;
};

export function emptyOrderDraftItem(): OrderDraftItem {
  return {
    name: "",
    quantity: "1",
    unitPrice: "0",
    notes: "",
  };
}

export function emptyStockDraft(): StockDraft {
  return {
    name: "",
    category: "",
    unit: "",
    currentQuantity: "0",
    minimumQuantity: "0",
  };
}

export async function handleApiError(
  error: unknown,
  onUnauthorized: AsyncVoid,
  setErrorMessage: Dispatch<SetStateAction<string>>,
  fallbackMessage: string,
) {
  if (error instanceof ApiError && error.status === 401) {
    await onUnauthorized();
    return;
  }

  if (error instanceof Error && error.message) {
    setErrorMessage(error.message);
    return;
  }

  setErrorMessage(fallbackMessage);
}

export function formatCurrency(value: number) {
  return new Intl.NumberFormat("pt-BR", {
    style: "currency",
    currency: "BRL",
  }).format(value);
}

export function formatDateTime(value: string) {
  return new Intl.DateTimeFormat("pt-BR", {
    dateStyle: "short",
    timeStyle: "short",
  }).format(new Date(value));
}

export function buildOrderPayload(items: OrderDraftItem[]): OrderItemInput[] {
  return items
    .filter((item) => item.name.trim() !== "")
    .map((item) => ({
      name: item.name.trim(),
      quantity: Number(item.quantity),
      unitPrice: Number(item.unitPrice),
      notes: item.notes.trim() || undefined,
    }))
    .filter((item) => item.quantity > 0);
}
