// Daily sales report client — isolated from lib/api.ts on purpose so this
// feature can be built in parallel without touching files the backend agent
// may also be editing. Only the public ApiError type is reused from lib/api.
//
// Contract source (owned by the backend / Codex):
//   GET /api/workspace/reports/sales/{date}   date format: yyyy-MM-dd
//
// Do NOT change the contract here. If the backend response evolves, update the
// types below to match — never the other way around.

import { ApiError } from "@/lib/api";

export { ApiError };

const CONFIGURED_API_BASE_URL =
  process.env.NEXT_PUBLIC_API_BASE_URL ?? "http://localhost:5097";

// --- Mock toggle -----------------------------------------------------------
// While the endpoint is still being finished by the backend agent, set
// NEXT_PUBLIC_REPORTS_USE_MOCK=true to render against the local fixture.
// Flip it back to false (or remove it) to hit the real API. That is the only
// switch required — no fake endpoint is ever created.
export const USE_MOCK_DAILY_SALES_REPORT =
  process.env.NEXT_PUBLIC_REPORTS_USE_MOCK === "true";

// --- Contract types --------------------------------------------------------
// Mirrors the documented response shape. The exact shape of each payment
// method entry was not specified in the contract, so it is modelled
// defensively and normalized after fetching.
export type DailySalesPaymentMethod = {
  method: string;
  label: string;
  amount: number;
  count: number;
  percent: number;
};

export type DailySalesReport = {
  referenceDate: string;
  ordersSubmittedCount: number;
  paidOrdersCount: number;
  pendingOrdersCount: number;
  cancelledOrdersCount: number;
  totalSalesAmount: number;
  paidAmount: number;
  pendingAmount: number;
  cancelledAmount: number;
  discountAmount: number;
  surchargeAmount: number;
  deliveryFreightAmount: number;
  averageTicket: number;
  paymentMethods: DailySalesPaymentMethod[];
  hasDetailedData: boolean;
  detailExpiresAtUtc: string | null;
};

// Raw shape as it may arrive over the wire, before normalization. Kept loose so
// small backend naming differences do not crash the UI.
type RawPaymentMethod = {
  method?: string;
  label?: string;
  name?: string;
  amount?: number;
  total?: number;
  count?: number;
  percent?: number;
};

type RawDailySalesReport = Partial<Omit<DailySalesReport, "paymentMethods">> & {
  paymentMethods?: RawPaymentMethod[] | null;
};

// --- Helpers ---------------------------------------------------------------
function getApiBaseUrl() {
  if (typeof window !== "undefined") {
    const hostname = window.location.hostname;

    if (hostname === "localhost" || hostname === "127.0.0.1") {
      try {
        return new URL(CONFIGURED_API_BASE_URL).origin;
      } catch {
        return CONFIGURED_API_BASE_URL;
      }
    }

    return "";
  }

  try {
    return new URL(CONFIGURED_API_BASE_URL).origin;
  } catch {
    return CONFIGURED_API_BASE_URL;
  }
}

const PAYMENT_LABELS: Record<string, string> = {
  Pix: "Pix",
  Credit: "Credito",
  Debit: "Debito",
  Cash: "Dinheiro",
  Undefined: "Nao definido",
};

function toNumber(value: unknown): number {
  return typeof value === "number" && Number.isFinite(value) ? value : 0;
}

function labelForMethod(method: string, explicit?: string): string {
  if (explicit && explicit.trim()) {
    return explicit;
  }

  return PAYMENT_LABELS[method] ?? method;
}

function normalizePaymentMethods(raw?: RawPaymentMethod[] | null): DailySalesPaymentMethod[] {
  if (!Array.isArray(raw)) {
    return [];
  }

  const methods = raw.map((entry) => {
    const method = entry.method ?? entry.name ?? "Undefined";
    return {
      method,
      label: labelForMethod(method, entry.label),
      amount: toNumber(entry.amount ?? entry.total),
      count: toNumber(entry.count),
      percent: toNumber(entry.percent),
    };
  });

  // If the backend did not provide percentages, derive them from the amounts.
  const hasPercent = methods.some((entry) => entry.percent > 0);

  if (!hasPercent) {
    const total = methods.reduce((sum, entry) => sum + entry.amount, 0);

    if (total > 0) {
      return methods.map((entry) => ({
        ...entry,
        percent: Math.round((entry.amount / total) * 100),
      }));
    }
  }

  return methods;
}

export function normalizeDailySalesReport(
  raw: RawDailySalesReport,
  fallbackDate: string,
): DailySalesReport {
  return {
    referenceDate: raw.referenceDate ?? fallbackDate,
    ordersSubmittedCount: toNumber(raw.ordersSubmittedCount),
    paidOrdersCount: toNumber(raw.paidOrdersCount),
    pendingOrdersCount: toNumber(raw.pendingOrdersCount),
    cancelledOrdersCount: toNumber(raw.cancelledOrdersCount),
    totalSalesAmount: toNumber(raw.totalSalesAmount),
    paidAmount: toNumber(raw.paidAmount),
    pendingAmount: toNumber(raw.pendingAmount),
    cancelledAmount: toNumber(raw.cancelledAmount),
    discountAmount: toNumber(raw.discountAmount),
    surchargeAmount: toNumber(raw.surchargeAmount),
    deliveryFreightAmount: toNumber(raw.deliveryFreightAmount),
    averageTicket: toNumber(raw.averageTicket),
    paymentMethods: normalizePaymentMethods(raw.paymentMethods),
    hasDetailedData: Boolean(raw.hasDetailedData),
    detailExpiresAtUtc: raw.detailExpiresAtUtc ?? null,
  };
}

// --- Date helpers ----------------------------------------------------------
export function toReportDateParam(date: Date): string {
  // yyyy-MM-dd in the operation timezone (America/Sao_Paulo) to match how the
  // rest of the workspace reasons about "the day".
  const parts = new Intl.DateTimeFormat("en-CA", {
    year: "numeric",
    month: "2-digit",
    day: "2-digit",
    timeZone: "America/Sao_Paulo",
  }).format(date);

  return parts; // en-CA already yields yyyy-MM-dd
}

export function todayReportDateParam(): string {
  return toReportDateParam(new Date());
}

export function yesterdayReportDateParam(): string {
  const d = new Date();
  d.setDate(d.getDate() - 1);
  return toReportDateParam(d);
}

const DATE_PARAM_PATTERN = /^\d{4}-\d{2}-\d{2}$/;

export function isValidReportDateParam(value: string): boolean {
  return DATE_PARAM_PATTERN.test(value);
}

// --- Fetch -----------------------------------------------------------------
export async function getDailySalesReport(
  token: string,
  date: string,
): Promise<DailySalesReport> {
  if (!isValidReportDateParam(date)) {
    throw new ApiError("Data invalida para o relatorio.", 400);
  }

  if (USE_MOCK_DAILY_SALES_REPORT) {
    return buildMockDailySalesReport(date);
  }

  const response = await fetch(
    `${getApiBaseUrl()}/api/workspace/reports/sales/${date}`,
    {
      method: "GET",
      headers: {
        Accept: "application/json",
        Authorization: `Bearer ${token}`,
      },
      cache: "no-store",
    },
  );

  if (!response.ok) {
    let message = "Nao foi possivel carregar o relatorio do dia.";

    try {
      const body = (await response.json()) as {
        detail?: string;
        title?: string;
        message?: string;
      };
      message = body.detail || body.message || body.title || message;
    } catch {
      message = response.statusText || message;
    }

    throw new ApiError(message, response.status);
  }

  const raw = (await response.json()) as RawDailySalesReport;
  return normalizeDailySalesReport(raw, date);
}

// --- Local fixture ---------------------------------------------------------
// Isolated mock used only while the real endpoint is unavailable. Deterministic
// per date so the UI is stable during development.
export function buildMockDailySalesReport(date: string): DailySalesReport {
  const paymentMethods = normalizePaymentMethods([
    { method: "Pix", amount: 842.5, count: 18 },
    { method: "Credit", amount: 432.0, count: 11 },
    { method: "Debit", amount: 318.9, count: 7 },
    { method: "Cash", amount: 196.0, count: 5 },
  ]);

  return normalizeDailySalesReport(
    {
      referenceDate: date,
      ordersSubmittedCount: 46,
      paidOrdersCount: 41,
      pendingOrdersCount: 3,
      cancelledOrdersCount: 2,
      totalSalesAmount: 1987.4,
      paidAmount: 1789.4,
      pendingAmount: 142.0,
      cancelledAmount: 56.0,
      discountAmount: 73.5,
      surchargeAmount: 12.0,
      deliveryFreightAmount: 128.0,
      averageTicket: 43.64,
      paymentMethods,
      hasDetailedData: true,
      detailExpiresAtUtc: new Date(Date.now() + 36 * 60 * 60 * 1000).toISOString(),
    },
    date,
  );
}
