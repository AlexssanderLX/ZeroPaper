const CONFIGURED_API_BASE_URL = process.env.NEXT_PUBLIC_API_BASE_URL ?? "http://localhost:5097";
export const APP_BASE_URL = process.env.NEXT_PUBLIC_APP_BASE_URL ?? "";
const ASSET_VERSION = "20260322-1133";

type RequestOptions = {
  method?: "GET" | "POST" | "PUT" | "PATCH" | "DELETE";
  body?: unknown;
  token?: string;
};

type FileDownloadResult = {
  blob: Blob;
  fileName: string;
};

export class ApiError extends Error {
  status: number;

  constructor(message: string, status: number) {
    super(message);
    this.name = "ApiError";
    this.status = status;
  }
}

export type LoginPayload = {
  email: string;
  password: string;
  profile?: "restaurant" | "admin";
};

export type LoginResult = {
  token: string;
  expiresAtUtc: string;
  email: string;
  ownerName: string;
  role: string;
  restaurantName: string;
};

export type WorkspaceOverview = {
  activeTables: number;
  openOrders: number;
  publishedMenuItems: number;
  totalMenuItems: number;
  totalStockItems: number;
  lowStockItems: number;
  pendingPayments: number;
  pendingPrints: number;
  printedPrints: number;
  failedPrints: number;
};

export type DiningTable = {
  id: string;
  name: string;
  internalCode: string;
  seats: number;
  status: string;
  openOrderCount: number;
  publicCode: string;
  accessUrl: string;
  alertSoundUrl?: string | null;
  hasCustomAlertSound: boolean;
};

export type MenuItem = {
  id: string;
  categoryId: string;
  name: string;
  description?: string | null;
  accentLabel?: string | null;
  imageUrl?: string | null;
  price: number;
  displayOrder: number;
  isActive: boolean;
};

export type MenuCategory = {
  id: string;
  name: string;
  displayOrder: number;
  items: MenuItem[];
};

export type OrderItemInput = {
  name: string;
  quantity: number;
  unitPrice: number;
  notes?: string;
};

export type MenuOrderSelectionInput = {
  menuItemId: string;
  quantity: number;
  notes?: string;
};

export type OrderItem = {
  id: string;
  name: string;
  categoryName?: string | null;
  imageUrl?: string | null;
  quantity: number;
  unitPrice: number;
  totalPrice: number;
  notes?: string | null;
};

export type CustomerOrder = {
  id: string;
  number: number;
  tableId: string;
  tableName: string;
  status: string;
  paymentMethod: string;
  requestedPaymentMethod: string;
  paymentStatus: string;
  printStatus: string;
  customerName?: string | null;
  notes?: string | null;
  totalAmount: number;
  submittedAtUtc: string;
  paidAtUtc?: string | null;
  printedAtUtc?: string | null;
  printAttempts: number;
  printLastError?: string | null;
  printAgentName?: string | null;
  printPrinterName?: string | null;
  items: OrderItem[];
};

export type StockItem = {
  id: string;
  name: string;
  category: string;
  unit: string;
  currentQuantity: number;
  minimumQuantity: number;
  isLowStock: boolean;
};

export type UploadMenuItemImageResult = {
  imageUrl: string;
};

export type AlertSettings = {
  enableOrderAlerts: boolean;
  enableWaiterCallAlerts: boolean;
  soundUrl?: string | null;
  hasCustomSound: boolean;
  volumePercent: number;
  playbackSeconds: number;
};

export type PrintOrderSummary = {
  id: string;
  number: number;
  tableName: string;
  status: string;
  printStatus: string;
  totalAmount: number;
  submittedAtUtc: string;
  printedAtUtc?: string | null;
  printAttempts: number;
  printLastError?: string | null;
};

export type PrintingSettings = {
  enableAutomaticPrinting: boolean;
  paperProfile: string;
  ordersPerPage: number;
  hasAgentKey: boolean;
  agentOnline: boolean;
  agentName?: string | null;
  printerName?: string | null;
  lastSeenAtUtc?: string | null;
  pendingJobs: number;
  failedJobs: number;
  printedJobs: number;
  downloadUrl: string;
  recentOrders: PrintOrderSummary[];
};

export type RotatePrintingAgentKeyResult = {
  agentKey: string;
  printing: PrintingSettings;
};

export type UploadAlertSoundResult = {
  alerts: AlertSettings;
};

export type UploadTableAlertSoundResult = {
  table: DiningTable;
};

export type CompanySettings = {
  legalName: string;
  tradeName: string;
  accessSlug: string;
  contactEmail?: string | null;
  contactPhone?: string | null;
  alerts: AlertSettings;
};

export type PublicTableView = {
  restaurantName: string;
  tableName: string;
  accessCode: string;
  menu: MenuCategory[];
};

export type WaiterCall = {
  id: string;
  tableId: string;
  tableName: string;
  tableAlertSoundUrl?: string | null;
  requestedAtUtc: string;
  resolvedAtUtc?: string | null;
};

export type WorkspaceAlertsSignal = {
  pendingWaiterCalls: number;
  latestWaiterCallAtUtc?: string | null;
  latestWaiterCallTableName?: string | null;
  latestWaiterCallTableSoundUrl?: string | null;
  latestOrderAtUtc?: string | null;
};

export type RestaurantSignupPayload = {
  restaurantName: string;
  legalName: string;
  ownerName: string;
  ownerEmail: string;
  accessCode: string;
  ownerPassword: string;
  contactPhone?: string;
  planName: string;
  monthlyPrice: number;
  maxUsers: number;
};

export type RestaurantSignupResult = {
  tenantIdentifier: string;
  accessSlug: string;
  accessUrl: string;
  ownerEmail: string;
  planName: string;
};

export type SignupCode = {
  id: string;
  label: string;
  boundEmail?: string | null;
  allowedPlanName?: string | null;
  allowedMaxUsers?: number | null;
  expiresAtUtc: string;
  maxUses: number;
  usedCount: number;
  isActive: boolean;
  createdAtUtc: string;
};

export type CreateSignupCodePayload = {
  label: string;
  boundEmail?: string;
  allowedPlanName?: string;
  allowedMaxUsers?: number;
};

export type CreateSignupCodeResult = SignupCode & {
  rawCode: string;
};

export type AdminUser = {
  id: string;
  fullName: string;
  email: string;
  role: string;
  restaurantName: string;
  isActive: boolean;
  isCompanyActive: boolean;
  hasActiveSession: boolean;
  isOnlineNow: boolean;
  activeSessionCount: number;
  lastLoginAtUtc?: string | null;
  lastSeenAtUtc?: string | null;
};

export type AccessRequestPayload = {
  restaurantName: string;
  legalName?: string;
  ownerName: string;
  ownerEmail: string;
  contactPhone?: string;
  cityRegion?: string;
  notes?: string;
};

export type AccessRequestResult = {
  accepted: boolean;
  message: string;
};

export type PasswordResetRequestPayload = {
  email: string;
};

export type PasswordResetRequestResult = {
  accepted: boolean;
  message: string;
};

export type ConfirmPasswordPayload = {
  password: string;
};

export type ConfirmPasswordResult = {
  confirmed: boolean;
};

export type ResetPasswordPayload = {
  token: string;
  newPassword: string;
};

async function apiRequest<T>(path: string, options: RequestOptions = {}): Promise<T> {
  const apiBaseUrl = getApiBaseUrl();
  const headers = new Headers({
    Accept: "application/json",
  });

  if (options.body !== undefined) {
    headers.set("Content-Type", "application/json");
  }

  if (options.token) {
    headers.set("Authorization", `Bearer ${options.token}`);
  }

  const response = await fetch(`${apiBaseUrl}${path}`, {
    method: options.method ?? "GET",
    headers,
    body: options.body === undefined ? undefined : JSON.stringify(options.body),
    cache: "no-store",
  });

  if (!response.ok) {
    let message = "Nao foi possivel concluir a requisicao.";

    try {
      const errorBody = (await response.json()) as { detail?: string; title?: string };
      message = errorBody.detail || errorBody.title || message;
    } catch {
      message = response.statusText || message;
    }

    throw new ApiError(message, response.status);
  }

  if (response.status === 204) {
    return undefined as T;
  }

  return (await response.json()) as T;
}

async function apiFormRequest<T>(path: string, body: FormData, token?: string): Promise<T> {
  const apiBaseUrl = getApiBaseUrl();
  const headers = new Headers({
    Accept: "application/json",
  });

  if (token) {
    headers.set("Authorization", `Bearer ${token}`);
  }

  const response = await fetch(`${apiBaseUrl}${path}`, {
    method: "POST",
    headers,
    body,
    cache: "no-store",
  });

  if (!response.ok) {
    let message = "Nao foi possivel concluir a requisicao.";

    try {
      const errorBody = (await response.json()) as { detail?: string; title?: string };
      message = errorBody.detail || errorBody.title || message;
    } catch {
      message = response.statusText || message;
    }

    throw new ApiError(message, response.status);
  }

  return (await response.json()) as T;
}

async function apiFileRequest(path: string, token: string): Promise<FileDownloadResult> {
  const apiBaseUrl = getApiBaseUrl();
  const response = await fetch(`${apiBaseUrl}${path}`, {
    method: "GET",
    headers: {
      Accept: "application/pdf",
      Authorization: `Bearer ${token}`,
    },
    cache: "no-store",
  });

  if (!response.ok) {
    let message = "Nao foi possivel concluir a requisicao.";

    try {
      const errorBody = (await response.json()) as { detail?: string; title?: string };
      message = errorBody.detail || errorBody.title || message;
    } catch {
      message = response.statusText || message;
    }

    throw new ApiError(message, response.status);
  }

  const contentDisposition = response.headers.get("Content-Disposition") ?? "";
  const fileNameMatch =
    contentDisposition.match(/filename\*=UTF-8''([^;]+)/i) ??
    contentDisposition.match(/filename="?([^"]+)"?/i);

  return {
    blob: await response.blob(),
    fileName: fileNameMatch?.[1] ? decodeURIComponent(fileNameMatch[1]) : "arquivo.pdf",
  };
}

function resolveApiAssetUrl(url?: string | null) {
  if (!url) {
    return url ?? undefined;
  }

  if (url.startsWith("/media/uploads/")) {
    return appendAssetVersion(url);
  }

  if (url.startsWith("/")) {
    return appendAssetVersion(`/media${url}`);
  }

  if (url.startsWith("http://") || url.startsWith("https://")) {
    try {
      const parsed = new URL(url);

        if (parsed.pathname.startsWith("/uploads/")) {
          return appendAssetVersion(`/media${parsed.pathname}${parsed.search}`);
        }

        if (parsed.pathname.startsWith("/media/uploads/")) {
          return appendAssetVersion(`${parsed.pathname}${parsed.search}`);
        }

        return url;
    } catch {
      return url;
    }
  }

  return `/media/${url}`;
}

function appendAssetVersion(url: string) {
  const separator = url.includes("?") ? "&" : "?";
  return `${url}${separator}v=${ASSET_VERSION}`;
}

function getApiBaseUrl() {
  if (typeof window !== "undefined") {
    return "";
  }

  try {
    const configuredUrl = new URL(CONFIGURED_API_BASE_URL);

    return configuredUrl.origin;
  } catch {
    return CONFIGURED_API_BASE_URL;
  }
}

function normalizeMenuItem(item: MenuItem): MenuItem {
  return {
    ...item,
    imageUrl: resolveApiAssetUrl(item.imageUrl),
  };
}

function normalizeOrderItem(item: OrderItem): OrderItem {
  return {
    ...item,
    imageUrl: resolveApiAssetUrl(item.imageUrl),
  };
}

function normalizeCustomerOrder(order: CustomerOrder): CustomerOrder {
  return {
    ...order,
    items: order.items.map(normalizeOrderItem),
  };
}

function normalizeDiningTable(table: DiningTable): DiningTable {
  return {
    ...table,
    alertSoundUrl: resolveApiAssetUrl(table.alertSoundUrl),
  };
}

function normalizeWaiterCall(waiterCall: WaiterCall): WaiterCall {
  return {
    ...waiterCall,
    tableAlertSoundUrl: resolveApiAssetUrl(waiterCall.tableAlertSoundUrl),
  };
}

function normalizeWorkspaceAlertsSignal(signal: WorkspaceAlertsSignal): WorkspaceAlertsSignal {
  return {
    ...signal,
    latestWaiterCallTableSoundUrl: resolveApiAssetUrl(signal.latestWaiterCallTableSoundUrl),
  };
}

function normalizeMenuCategories(categories: MenuCategory[]) {
  return categories.map((category) => ({
    ...category,
    items: category.items.map(normalizeMenuItem),
  }));
}

function normalizeAlertSettings(alerts: AlertSettings): AlertSettings {
  return {
    ...alerts,
    soundUrl: resolveApiAssetUrl(alerts.soundUrl),
    volumePercent: Number.isFinite(alerts.volumePercent) ? alerts.volumePercent : 100,
    playbackSeconds: Number.isFinite(alerts.playbackSeconds) ? alerts.playbackSeconds : 6,
  };
}

export function loginPortal(payload: LoginPayload) {
  return apiRequest<LoginResult>("/api/auth/login", {
    method: "POST",
    body: payload,
  });
}

export function createRestaurantSignup(payload: RestaurantSignupPayload) {
  return apiRequest<RestaurantSignupResult>("/api/onboarding/restaurants", {
    method: "POST",
    body: payload,
  });
}

export function getSignupCodes(token: string) {
  return apiRequest<SignupCode[]>("/api/admin/signup-codes", { token });
}

export function createSignupCode(token: string, payload: CreateSignupCodePayload) {
  return apiRequest<CreateSignupCodeResult>("/api/admin/signup-codes", {
    method: "POST",
    token,
    body: payload,
  });
}

export function getAdminUsers(token: string) {
  return apiRequest<AdminUser[]>("/api/admin/users", { token });
}

export function reactivateAdminUser(token: string, userId: string) {
  return apiRequest<AdminUser>(`/api/admin/users/${userId}/reactivate`, {
    method: "PATCH",
    token,
  });
}

export function deactivateAdminUser(token: string, userId: string) {
  return apiRequest<AdminUser>(`/api/admin/users/${userId}/deactivate`, {
    method: "PATCH",
    token,
  });
}

export function deleteAdminUser(token: string, userId: string) {
  return apiRequest<void>(`/api/admin/users/${userId}`, {
    method: "DELETE",
    token,
  });
}

export function createAccessRequest(payload: AccessRequestPayload) {
  return apiRequest<AccessRequestResult>("/api/public/access-requests", {
    method: "POST",
    body: payload,
  });
}

export function requestPasswordReset(payload: PasswordResetRequestPayload) {
  return apiRequest<PasswordResetRequestResult>("/api/auth/password/request-reset", {
    method: "POST",
    body: payload,
  });
}

export function confirmCurrentPassword(token: string, payload: ConfirmPasswordPayload) {
  return apiRequest<ConfirmPasswordResult>("/api/auth/confirm-password", {
    method: "POST",
    token,
    body: payload,
  });
}

export function resetPassword(payload: ResetPasswordPayload) {
  return apiRequest<void>("/api/auth/password/reset", {
    method: "POST",
    body: payload,
  });
}

export function logoutPortal(token: string) {
  return apiRequest<void>("/api/auth/logout", {
    method: "POST",
    token,
  });
}

export function getWorkspaceOverview(token: string) {
  return apiRequest<WorkspaceOverview>("/api/workspace/overview", { token });
}

export function getTables(token: string) {
  return apiRequest<DiningTable[]>("/api/workspace/tables", { token }).then((response) => response.map(normalizeDiningTable));
}

export function getMenu(token: string) {
  return apiRequest<MenuCategory[]>("/api/workspace/menu", { token }).then(normalizeMenuCategories);
}

export function createMenuCategory(token: string, payload: { name: string }) {
  return apiRequest<MenuCategory>("/api/workspace/menu/categories", {
    method: "POST",
    token,
    body: payload,
  });
}

export function updateMenuCategory(token: string, categoryId: string, payload: { name: string }) {
  return apiRequest<MenuCategory>(`/api/workspace/menu/categories/${categoryId}`, {
    method: "PUT",
    token,
    body: payload,
  });
}

export function createMenuItem(
  token: string,
  payload: {
    categoryId: string;
    name: string;
    description?: string;
    accentLabel?: string;
    imageUrl?: string;
    price: number;
  },
) {
  return apiRequest<MenuItem>("/api/workspace/menu/items", {
    method: "POST",
    token,
    body: payload,
  }).then(normalizeMenuItem);
}

export function updateMenuItem(
  token: string,
  menuItemId: string,
  payload: {
    categoryId: string;
    name: string;
    description?: string;
    accentLabel?: string;
    imageUrl?: string;
    price: number;
  },
) {
  return apiRequest<MenuItem>(`/api/workspace/menu/items/${menuItemId}`, {
    method: "PUT",
    token,
    body: payload,
  }).then(normalizeMenuItem);
}

export function updateMenuItemStatus(token: string, menuItemId: string, isActive: boolean) {
  return apiRequest<MenuItem>(`/api/workspace/menu/items/${menuItemId}/status`, {
    method: "PATCH",
    token,
    body: { isActive },
  }).then(normalizeMenuItem);
}

export function uploadMenuItemImage(token: string, file: File) {
  const formData = new FormData();
  formData.append("file", file);

  return apiFormRequest<UploadMenuItemImageResult>("/api/workspace/menu/images", formData, token)
    .then((response) => ({
      ...response,
      imageUrl: resolveApiAssetUrl(response.imageUrl) ?? "",
    }));
}

export function deleteMenuCategory(token: string, categoryId: string) {
  return apiRequest<void>(`/api/workspace/menu/categories/${categoryId}`, {
    method: "DELETE",
    token,
  });
}

export function deleteMenuItem(token: string, menuItemId: string) {
  return apiRequest<void>(`/api/workspace/menu/items/${menuItemId}`, {
    method: "DELETE",
    token,
  });
}

export function createTable(token: string, payload: { name: string; seats: number }) {
  return apiRequest<DiningTable>("/api/workspace/tables", {
    method: "POST",
    token,
    body: payload,
  }).then(normalizeDiningTable);
}

export function updateTable(token: string, tableId: string, payload: { name: string; seats: number }) {
  return apiRequest<DiningTable>(`/api/workspace/tables/${tableId}`, {
    method: "PUT",
    token,
    body: payload,
  }).then(normalizeDiningTable);
}

export function uploadTableAlertSound(token: string, tableId: string, file: File) {
  const formData = new FormData();
  formData.append("file", file);

  return apiFormRequest<UploadTableAlertSoundResult>(`/api/workspace/tables/${tableId}/alert-sound`, formData, token).then((response) => ({
    ...response,
    table: normalizeDiningTable(response.table),
  }));
}

export function deleteTableAlertSound(token: string, tableId: string) {
  return apiRequest<DiningTable>(`/api/workspace/tables/${tableId}/alert-sound`, {
    method: "DELETE",
    token,
  }).then(normalizeDiningTable);
}

export function getOrders(token: string, kitchenOnly = false) {
  return apiRequest<CustomerOrder[]>(`/api/workspace/orders?kitchenOnly=${kitchenOnly}`, { token }).then((response) =>
    response.map(normalizeCustomerOrder),
  );
}

export function updateOrderStatus(token: string, orderId: string, status: string, password?: string) {
  return apiRequest<CustomerOrder>(`/api/workspace/orders/${orderId}/status`, {
    method: "PATCH",
    token,
    body: { status, password },
  }).then(normalizeCustomerOrder);
}

export function updateOrdersStatusBatch(token: string, orderIds: string[], status: string, password?: string) {
  return apiRequest<void>("/api/workspace/orders/batch-status", {
    method: "POST",
    token,
    body: { orderIds, status, password },
  });
}

export function updateOrderPayment(token: string, orderId: string, paymentStatus: string, paymentMethod?: string) {
  return apiRequest<CustomerOrder>(`/api/workspace/orders/${orderId}/payment`, {
    method: "PATCH",
    token,
    body: { paymentStatus, paymentMethod },
  }).then(normalizeCustomerOrder);
}

export function deleteOrder(token: string, orderId: string) {
  return apiRequest<void>(`/api/workspace/orders/${orderId}`, {
    method: "DELETE",
    token,
  });
}

export function deletePaidOrder(token: string, orderId: string, password: string) {
  return apiRequest<void>(`/api/workspace/orders/${orderId}/delete-paid`, {
    method: "POST",
    token,
    body: { password },
  });
}

export function deleteAllPaidOrders(token: string, password: string) {
  return apiRequest<void>("/api/workspace/orders/delete-paid-all", {
    method: "POST",
    token,
    body: { password },
  });
}

export function deleteClosedOrders(token: string, orderIds: string[], password?: string) {
  return apiRequest<void>("/api/workspace/orders/delete-closed", {
    method: "POST",
    token,
    body: { orderIds, password },
  });
}

export function deleteTodayOrderFlow(token: string, password: string) {
  return apiRequest<void>("/api/workspace/orders/delete-today-flow", {
    method: "POST",
    token,
    body: { password },
  });
}

export function downloadDailyCashReportPdf(token: string) {
  return apiFileRequest("/api/workspace/orders/daily-report", token);
}

export function getStockItems(token: string) {
  return apiRequest<StockItem[]>("/api/workspace/stock", { token });
}

export function createStockItem(
  token: string,
  payload: {
    name: string;
    category: string;
    unit: string;
    currentQuantity: number;
    minimumQuantity: number;
  },
) {
  return apiRequest<StockItem>("/api/workspace/stock", {
    method: "POST",
    token,
    body: payload,
  });
}

export function updateStockItem(
  token: string,
  stockItemId: string,
  payload: {
    name: string;
    category: string;
    unit: string;
    currentQuantity: number;
    minimumQuantity: number;
  },
) {
  return apiRequest<StockItem>(`/api/workspace/stock/${stockItemId}`, {
    method: "PUT",
    token,
    body: payload,
  });
}

export function getCompanySettings(token: string) {
  return apiRequest<CompanySettings>("/api/workspace/settings", { token }).then((response) => ({
    ...response,
    alerts: normalizeAlertSettings(response.alerts),
  }));
}

export function updateCompanySettings(
  token: string,
  payload: {
    legalName: string;
    tradeName: string;
    contactEmail?: string;
    contactPhone?: string;
  },
) {
  return apiRequest<CompanySettings>("/api/workspace/settings", {
    method: "PUT",
    token,
    body: payload,
  }).then((response) => ({
    ...response,
    alerts: normalizeAlertSettings(response.alerts),
  }));
}

export function updateAlertSettings(
  token: string,
  payload: {
    enableOrderAlerts: boolean;
    enableWaiterCallAlerts: boolean;
    volumePercent: number;
    playbackSeconds: number;
  },
) {
  return apiRequest<AlertSettings>("/api/workspace/settings/alerts", {
    method: "PATCH",
    token,
    body: payload,
  }).then(normalizeAlertSettings);
}

export function uploadAlertSound(token: string, file: File) {
  const formData = new FormData();
  formData.append("file", file);

  return apiFormRequest<UploadAlertSoundResult>("/api/workspace/settings/alerts/sound", formData, token).then((response) => ({
    ...response,
    alerts: normalizeAlertSettings(response.alerts),
  }));
}

export function deleteAlertSound(token: string) {
  return apiRequest<AlertSettings>("/api/workspace/settings/alerts/sound", {
    method: "DELETE",
    token,
  }).then(normalizeAlertSettings);
}

export function getPrintingSettings(token: string) {
  return apiRequest<PrintingSettings>("/api/workspace/printing", { token });
}

export function updatePrintingSettings(
  token: string,
  payload: { enableAutomaticPrinting: boolean; paperProfile: string; ordersPerPage: number },
) {
  return apiRequest<PrintingSettings>("/api/workspace/printing", {
    method: "PATCH",
    token,
    body: payload,
  });
}

export function rotatePrintingAgentKey(token: string) {
  return apiRequest<RotatePrintingAgentKeyResult>("/api/workspace/printing/agent-key", {
    method: "POST",
    token,
  });
}

export function requeuePrintOrder(token: string, orderId: string) {
  return apiRequest<void>(`/api/workspace/printing/orders/${orderId}/requeue`, {
    method: "POST",
    token,
  });
}

export function getPublicTable(publicCode: string) {
  return apiRequest<PublicTableView>(`/api/public/tables/${publicCode}`)
    .then((response) => ({
      ...response,
      menu: normalizeMenuCategories(response.menu),
    }));
}

export function createPublicWaiterCall(publicCode: string) {
  return apiRequest<WaiterCall>(`/api/public/tables/${publicCode}/waiter-calls`, {
    method: "POST",
  });
}

export function createPublicOrder(
  publicCode: string,
  payload: {
    customerName?: string;
    notes?: string;
    paymentMethod?: string;
    items?: OrderItemInput[];
    menuSelections?: MenuOrderSelectionInput[];
  },
) {
  return apiRequest<CustomerOrder>(`/api/public/tables/${publicCode}/orders`, {
    method: "POST",
    body: payload,
  }).then(normalizeCustomerOrder);
}

export function getWaiterCalls(token: string) {
  return apiRequest<WaiterCall[]>("/api/workspace/waiter-calls", { token }).then((response) => response.map(normalizeWaiterCall));
}

export function getWorkspaceAlertsSignal(token: string) {
  return apiRequest<WorkspaceAlertsSignal>("/api/workspace/alerts/signal", { token }).then(normalizeWorkspaceAlertsSignal);
}

export function resolveWaiterCall(token: string, waiterCallId: string) {
  return apiRequest<WaiterCall>(`/api/workspace/waiter-calls/${waiterCallId}/resolve`, {
    method: "PATCH",
    token,
  }).then(normalizeWaiterCall);
}
