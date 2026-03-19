const API_BASE_URL = process.env.NEXT_PUBLIC_API_BASE_URL ?? "http://localhost:5097";

type RequestOptions = {
  method?: "GET" | "POST" | "PUT" | "PATCH" | "DELETE";
  body?: unknown;
  token?: string;
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
  profile: "restaurant" | "admin";
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
  lowStockItems: number;
  teamMembers: number;
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
};

export type MenuItem = {
  id: string;
  categoryId: string;
  name: string;
  description?: string | null;
  accentLabel?: string | null;
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
  customerName?: string | null;
  notes?: string | null;
  totalAmount: number;
  submittedAtUtc: string;
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

export type TeamMember = {
  id: string;
  fullName: string;
  email: string;
  role: string;
  isActive: boolean;
  lastLoginAtUtc?: string | null;
};

export type CompanySettings = {
  legalName: string;
  tradeName: string;
  accessSlug: string;
  contactEmail?: string | null;
  contactPhone?: string | null;
};

export type PublicTableView = {
  restaurantName: string;
  tableName: string;
  accessCode: string;
  menu: MenuCategory[];
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
  const headers = new Headers({
    Accept: "application/json",
  });

  if (options.body !== undefined) {
    headers.set("Content-Type", "application/json");
  }

  if (options.token) {
    headers.set("Authorization", `Bearer ${options.token}`);
  }

  const response = await fetch(`${API_BASE_URL}${path}`, {
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
  return apiRequest<DiningTable[]>("/api/workspace/tables", { token });
}

export function getMenu(token: string) {
  return apiRequest<MenuCategory[]>("/api/workspace/menu", { token });
}

export function createMenuCategory(token: string, payload: { name: string }) {
  return apiRequest<MenuCategory>("/api/workspace/menu/categories", {
    method: "POST",
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
    price: number;
  },
) {
  return apiRequest<MenuItem>("/api/workspace/menu/items", {
    method: "POST",
    token,
    body: payload,
  });
}

export function createTable(token: string, payload: { name: string; seats: number }) {
  return apiRequest<DiningTable>("/api/workspace/tables", {
    method: "POST",
    token,
    body: payload,
  });
}

export function getOrders(token: string, kitchenOnly = false) {
  return apiRequest<CustomerOrder[]>(`/api/workspace/orders?kitchenOnly=${kitchenOnly}`, { token });
}

export function createOrder(
  token: string,
  payload: {
    tableId?: string;
    customerName?: string;
    notes?: string;
    items?: OrderItemInput[];
    menuSelections?: MenuOrderSelectionInput[];
  },
) {
  return apiRequest<CustomerOrder>("/api/workspace/orders", {
    method: "POST",
    token,
    body: payload,
  });
}

export function updateOrderStatus(token: string, orderId: string, status: string) {
  return apiRequest<CustomerOrder>(`/api/workspace/orders/${orderId}/status`, {
    method: "PATCH",
    token,
    body: { status },
  });
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

export function getTeamMembers(token: string) {
  return apiRequest<TeamMember[]>("/api/workspace/team", { token });
}

export function createTeamMember(
  token: string,
  payload: {
    fullName: string;
    email: string;
    password: string;
    role: string;
  },
) {
  return apiRequest<TeamMember>("/api/workspace/team", {
    method: "POST",
    token,
    body: payload,
  });
}

export function getCompanySettings(token: string) {
  return apiRequest<CompanySettings>("/api/workspace/settings", { token });
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
  });
}

export function getPublicTable(publicCode: string) {
  return apiRequest<PublicTableView>(`/api/public/tables/${publicCode}`);
}

export function createPublicOrder(
  publicCode: string,
  payload: {
    customerName?: string;
    notes?: string;
    items?: OrderItemInput[];
    menuSelections?: MenuOrderSelectionInput[];
  },
) {
  return apiRequest<CustomerOrder>(`/api/public/tables/${publicCode}/orders`, {
    method: "POST",
    body: payload,
  });
}
