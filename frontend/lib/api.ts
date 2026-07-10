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

export type ShortcutLoginPayload = {
  token: string;
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
  includesMenuModule: boolean;
  includesTablesModule: boolean;
  includesKitchenModule: boolean;
  includesCashModule: boolean;
  includesStockModule: boolean;
  includesDeliveryModule: boolean;
  includesPrintingModule: boolean;
  includesWaiterCallModule: boolean;
  includesAiAssistantModule: boolean;
  hasCoupons?: boolean;
  hasAutoPrint?: boolean;
  hasBasicReports?: boolean;
  hasAdvancedReports?: boolean;
  hasManagementDashboard?: boolean;
  hasSalesAgents?: boolean;
};

export type Coupon = {
  id: string;
  code: string;
  description?: string | null;
  discountType: string;
  discountValue: number;
  minimumOrderAmount: number;
  startsAtUtc?: string | null;
  endsAtUtc?: string | null;
  isActive: boolean;
  usageLimit?: number | null;
  usageCount: number;
  createdAtUtc: string;
  updatedAtUtc: string;
};

export type SaveCouponPayload = {
  code: string;
  description?: string | null;
  discountType: string;
  discountValue: number;
  minimumOrderAmount: number;
  startsAtUtc?: string | null;
  endsAtUtc?: string | null;
  usageLimit?: number | null;
};

export type CashClosingPaymentMethod = {
  method: string;
  amount: number;
  ordersCount: number;
};

export type CashClosingReport = {
  referenceDate: string;
  totalSold: number;
  ordersCount: number;
  averageTicket: number;
  discountsTotal: number;
  cancelledOrdersCount: number;
  paymentMethods: CashClosingPaymentMethod[];
};

export type MercadoPagoStatus = {
  configured: boolean;
  connected: boolean;
  accountUserId?: string | null;
  liveMode: boolean;
  connectedAtUtc?: string | null;
};

export type MercadoPagoConnectResponse = {
  authorizationUrl: string;
};

export type MercadoPagoCheckoutResponse = {
  available: boolean;
  initPoint?: string | null;
  preferenceId?: string | null;
  message: string;
};

export type DiningTable = {
  id: string;
  name: string;
  internalCode: string;
  comandaLabel?: string | null;
  isDeliveryChannel: boolean;
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
  startingPrice?: number | null;
  displayOrder: number;
  isActive: boolean;
  maxAdditionalSelections?: number | null;
  hasAdditionalOptions?: boolean;
  additionalGroups: MenuItemAdditionalGroup[];
};

export type MenuItemAdditionalGroup = {
  id: string;
  sourceMenuAdditionalCatalogGroupId?: string | null;
  name: string;
  allowMultiple: boolean;
  displayOrder: number;
  maxAdditionalSelections?: number | null;
  options: MenuItemAdditionalOption[];
};

export type MenuItemAdditionalOption = {
  id: string;
  groupId: string;
  sourceMenuAdditionalCatalogOptionId?: string | null;
  name: string;
  price: number;
  displayOrder: number;
};

export type MenuAdditionalCatalogGroup = {
  id: string;
  name: string;
  allowMultiple: boolean;
  displayOrder: number;
  maxAdditionalSelections?: number | null;
  linkedItemCount?: number;
  linkedItemNames?: string[];
  options: MenuAdditionalCatalogOption[];
};

export type MenuAdditionalCatalogOption = {
  id: string;
  groupId: string;
  name: string;
  price: number;
  displayOrder: number;
};

export type MenuCategory = {
  id: string;
  name: string;
  imageUrl?: string | null;
  displayOrder: number;
  items: MenuItem[];
};

export type MenuCategorySummary = {
  id: string;
  name: string;
  imageUrl?: string | null;
  displayOrder: number;
  totalItems: number;
  activeItems: number;
  hiddenItems: number;
  itemsWithoutImage: number;
  itemsWithAdditionals: number;
  startingPrice?: number | null;
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
  additionalOptionIds?: string[];
};

export type OrderItemAdditionalSelection = {
  sourceMenuItemAdditionalOptionId?: string | null;
  groupName: string;
  optionName: string;
  unitPrice: number;
};

export type OrderItem = {
  id: string;
  menuItemId?: string | null;
  name: string;
  categoryName?: string | null;
  imageUrl?: string | null;
  quantity: number;
  baseUnitPrice: number;
  unitPrice: number;
  totalPrice: number;
  notes?: string | null;
  additionalSelections: OrderItemAdditionalSelection[];
};

export type UpdateOrderItemPayload = {
  id?: string | null;
  name?: string;
  quantity: number;
  unitPrice?: number;
  notes?: string | null;
};

export type UpdateOrderPayload = {
  customerName?: string | null;
  notes?: string | null;
  deliveryPhone?: string | null;
  deliveryAddress?: string | null;
  deliveryNumber?: string | null;
  deliveryComplement?: string | null;
  deliveryPostalCode?: string | null;
  fulfillmentType?: string | null;
  paymentMethod?: string | null;
  items: UpdateOrderItemPayload[];
  menuSelections?: MenuOrderSelectionInput[];
};

export type AdjustOrderValuePayload = {
  finalAmount?: number;
  discountAmount: number;
  surchargeAmount: number;
  note?: string | null;
};

export type OrderPaymentInput = {
  method: string;
  amount: number;
};

export type MarkOrderPaidInput = {
  orderId: string;
  paymentMethod?: string;
  payments?: OrderPaymentInput[];
};

export type MarkAllOrdersPaidResult = {
  markedCount: number;
  ignoredCount: number;
  ignoredReasons: string[];
};

export type OrderPayment = {
  id: string;
  method: string;
  amount: number;
  createdAtUtc: string;
};

export type CustomerOrder = {
  id: string;
  number: number;
  tableId: string;
  tableName: string;
  publicCode?: string | null;
  status: string;
  paymentMethod: string;
  requestedPaymentMethod: string;
  paymentStatus: string;
  printStatus: string;
  customerName?: string | null;
  notes?: string | null;
  isDeliveryOrder: boolean;
  fulfillmentType: string;
  deliveryPhone?: string | null;
  deliveryAddress?: string | null;
  deliveryNumber?: string | null;
  deliveryComplement?: string | null;
  deliveryPostalCode?: string | null;
  deliveryFreightAmount: number;
  deliveryDistanceKm?: number | null;
  deliveryFreightProvider?: string | null;
  deliveryFreightCalculatedAtUtc?: string | null;
  canEditPublicly: boolean;
  publicEditAllowedUntilUtc?: string | null;
  publicEditUrl?: string | null;
  publicDeliveryCustomerUrl?: string | null;
  deliveryAssistantMessage?: string | null;
  originalTotalAmount: number;
  totalAmount: number;
  totalItemQuantity: number;
  isEdited: boolean;
  editedAtUtc?: string | null;
  discountAmount: number;
  surchargeAmount: number;
  couponId?: string | null;
  couponCode?: string | null;
  couponDiscountAmount?: number;
  couponAppliedAtUtc?: string | null;
  priceAdjustmentNote?: string | null;
  priceAdjustedAtUtc?: string | null;
  hasPriceAdjustment: boolean;
  paymentTotalAmount: number;
  remainingPaymentAmount: number;
  submittedAtUtc: string;
  paidAtUtc?: string | null;
  printedAtUtc?: string | null;
  printAttempts: number;
  printLastError?: string | null;
  printAgentName?: string | null;
  printPrinterName?: string | null;
  salesAgentId?: string | null;
  salesAgentName?: string | null;
  salesOrigin?: string | null;
  payments: OrderPayment[];
  items: OrderItem[];
};

export type CouponValidation = {
  isValid: boolean;
  code: string;
  message?: string | null;
  discountAmount: number;
  finalSubtotal: number;
  coupon?: {
    id: string;
    code: string;
    description?: string | null;
    discountType: string;
    discountValue: number;
    minimumOrderAmount: number;
    startsAtUtc?: string | null;
    endsAtUtc?: string | null;
    isActive: boolean;
    usageLimit?: number | null;
    usageCount: number;
  } | null;
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
  customerName?: string | null;
  isDeliveryOrder: boolean;
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
  autoPrintEnabled?: boolean;
  paperProfile: string;
  ordersPerPage: number;
  hasAgentKey: boolean;
  hasAgentToken?: boolean;
  agentId?: string | null;
  agentOnline: boolean;
  agentName?: string | null;
  printerName?: string | null;
  appVersion?: string | null;
  registeredAtUtc?: string | null;
  lastSeenAtUtc?: string | null;
  lastError?: string | null;
  lastErrorAtUtc?: string | null;
  pendingJobs: number;
  failedJobs: number;
  printedJobs: number;
  downloadUrl: string;
  downloadUrlX86: string;
  downloadUrlX64: string;
  legacyDownloadUrl: string;
  recentOrders: PrintOrderSummary[];
};

export type RotatePrintingAgentKeyResult = {
  agentKey: string;
  agentToken?: string;
  printing: PrintingSettings;
};

export type AiAssistantSettings = {
  unitDisplayName: string;
  apiConfigured: boolean;
  whatsAppServerConfigured: boolean;
  isEnabled: boolean;
  model: string;
  systemPrompt: string;
  greetingMessage: string;
  redirectMessage: string;
  fallbackMessage: string;
  orderingLink?: string | null;
  pixReceiverName?: string | null;
  pixKey?: string | null;
  pixMessage?: string | null;
  serviceDays?: number[] | null;
  serviceStartTime?: string | null;
  serviceEndTime?: string | null;
  maxOutputTokens: number;
  whatsAppEnabled: boolean;
  whatsAppConfigured: boolean;
  whatsAppInstanceId?: string | null;
  whatsAppInstanceTokenMasked?: string | null;
  hasWhatsAppAccountSecurityToken: boolean;
  isWhatsAppConnected: boolean;
  whatsAppConnectedPhone?: string | null;
  whatsAppConnectedAtUtc?: string | null;
  whatsAppDisconnectedAtUtc?: string | null;
  whatsAppLastIncomingAtUtc?: string | null;
  whatsAppLastOutgoingAtUtc?: string | null;
  whatsAppWebhookReceiveUrl?: string | null;
  whatsAppWebhookMessageStatusUrl?: string | null;
  whatsAppWebhookConnectedUrl?: string | null;
  whatsAppWebhookDisconnectedUrl?: string | null;
  recentWhatsAppConversations: {
    id: string;
    externalPhone: string;
    customerName?: string | null;
    lastMessagePreview: string;
    lastDirection: string;
    lastIncomingAtUtc?: string | null;
    lastOutgoingAtUtc?: string | null;
    lastInteractionAtUtc?: string | null;
    messageCount: number;
  }[];
};

export type AiAssistantTestResult = {
  reply: string;
  model: string;
  generatedAtUtc: string;
};

export type AiAssistantQuickStatus = {
  isEnabled: boolean;
  isConfigured: boolean;
};

export type WhatsAppConnectionSnapshot = {
  serverConfigured: boolean;
  instanceConfigured: boolean;
  instanceName: string;
  state?: string | null;
  isConnected: boolean;
  connectedPhone?: string | null;
  qrCodeBase64?: string | null;
  qrCodeText?: string | null;
  pairingCode?: string | null;
  message?: string | null;
};

export type PrepareWhatsAppConnectionPayload = {
  phoneNumber?: string;
  forceNewSession?: boolean;
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
  logoUrl?: string | null;
  accessSlug: string;
  contactEmail?: string | null;
  contactPhone?: string | null;
  alerts: AlertSettings;
  shortcutAccess: OwnerShortcutAccess;
};

export type OwnerShortcutAccess = {
  isEnabled: boolean;
  createdAtUtc?: string | null;
  expiresAtUtc?: string | null;
  lastUsedAtUtc?: string | null;
};

export type GenerateOwnerShortcutAccessResult = {
  shortcutAccess: OwnerShortcutAccess;
  rawToken: string;
  shortcutUrl: string;
};

export type OwnerProfile = {
  id: string;
  fullName: string;
  email: string;
  role: string;
};

export type DeliveryFreightSettings = {
  isEnabled: boolean;
  originPostalCode?: string | null;
  pricePerKm: number;
  baseFee: number;
  baseDistanceKm: number;
  provider: string;
  providerConfigured: boolean;
  isTestMode: boolean;
  cacheDays: number;
  pickupEstimatedMinutes?: number | null;
  deliveryEstimatedMinutes?: number | null;
};

export type DeliveryFreightQuote = {
  isEnabled: boolean;
  isConfigured: boolean;
  isAvailable: boolean;
  isTestMode: boolean;
  provider: string;
  originPostalCode?: string | null;
  destinationPostalCode?: string | null;
  distanceKm?: number | null;
  baseFee: number;
  baseDistanceKm: number;
  chargedDistanceKm: number;
  pricePerKm: number;
  freightAmount: number;
  totalWithFreight: number;
  fromCache: boolean;
  message?: string | null;
};

export type PublicDeliveryCustomerProfile = {
  found: boolean;
  customerName?: string | null;
  deliveryPhone?: string | null;
  deliveryAddress?: string | null;
  deliveryNumber?: string | null;
  deliveryComplement?: string | null;
  deliveryPostalCode?: string | null;
  lastOrderAtUtc?: string | null;
  message?: string | null;
};

export type PublicCustomerProfile = {
  found: boolean;
  message?: string | null;
  customerName?: string | null;
  maskedPhone?: string | null;
  primaryAddress?: PublicCustomerPrimaryAddress | null;
  businessName?: string | null;
  businessSlug?: string | null;
  canEditProfile: boolean;
  canReorder: boolean;
  hasActiveOrder: boolean;
  recentOrders: PublicCustomerRecentOrder[];
};

export type PublicCustomerPrimaryAddress = {
  street?: string | null;
  number?: string | null;
  neighborhood?: string | null;
  complement?: string | null;
  zipCode?: string | null;
};

export type PublicCustomerRecentOrder = {
  orderNumber?: number | null;
  displayCode?: string | null;
  createdAt: string;
  status: string;
  total: number;
  fulfillmentType: string;
  items: PublicCustomerRecentOrderItem[];
};

export type PublicCustomerRecentOrderItem = {
  name: string;
  quantity: number;
  unitPrice?: number | null;
  total?: number | null;
};

export type PublicTableView = {
  restaurantName: string;
  restaurantLogoUrl?: string | null;
  tableName: string;
  accessCode: string;
  isDeliveryChannel: boolean;
  isOnlinePaymentAvailable: boolean;
  deliveryEditWindowMinutes: number;
  isOrderingAvailable: boolean;
  orderingUnavailableMessage?: string | null;
  serviceDays?: number[] | null;
  serviceStartTime?: string | null;
  serviceEndTime?: string | null;
  pickupEstimatedMinutes?: number | null;
  deliveryEstimatedMinutes?: number | null;
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
  ownerPassword: string;
  contactPhone: string;
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
  requiresApproval: boolean;
  message: string;
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
  lastUsedAtUtc?: string | null;
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

export type AdminOwner = {
  id: string;
  companyId: string;
  companyName: string;
  accessSlug: string;
  fullName: string;
  email: string;
  contactPhone?: string | null;
  isActive: boolean;
  isCompanyActive: boolean;
  hasActiveSession: boolean;
  activeSessionCount: number;
  createdAtUtc: string;
  updatedAtUtc: string;
  lastLoginAtUtc?: string | null;
  lastSeenAtUtc?: string | null;
};

export type AdminDashboardSummary = {
  totalCompanies: number;
  activeCompanies: number;
  totalUsers: number;
  onlineUsers: number;
  availableSignupCodes: number;
  usedSignupCodes: number;
  expiredSignupCodes: number;
  ordersToday: number;
  openOrders: number;
  pendingPayments: number;
  failedPrints: number;
  aiInteractionsToday: number;
};

export type AdminCompanyFlow = {
  companyId: string;
  restaurantName: string;
  accessSlug: string;
  ownerName: string;
  ownerEmail: string;
  contactPhone?: string | null;
  planName: string;
  planTier?: string;
  monthlyPrice: number;
  maxUsers: number;
  includesMenuModule: boolean;
  includesTablesModule: boolean;
  includesKitchenModule: boolean;
  includesCashModule: boolean;
  includesStockModule: boolean;
  includesDeliveryModule: boolean;
  includesPrintingModule: boolean;
  includesWaiterCallModule: boolean;
  includesAiAssistantModule: boolean;
  hasWhatsAppAI?: boolean;
  hasDelivery?: boolean;
  hasAutoPrint?: boolean;
  hasBasicReports?: boolean;
  hasManagementDashboard?: boolean;
  hasAdvancedReports?: boolean;
  hasCoupons?: boolean;
  hasRecurringCustomers?: boolean;
  isCompanyActive: boolean;
  ordersToday: number;
  deliveryOrdersToday: number;
  paidOrdersToday: number;
  deletedOrdersToday: number;
  openOrders: number;
  pendingPayments: number;
  failedPrints: number;
  printedToday: number;
  tablesCount: number;
  menuItemsCount: number;
  stockItemsCount: number;
  teamMembersCount: number;
  deliveryEnabled: boolean;
  aiEnabled: boolean;
  aiConfigured: boolean;
  aiModel: string;
  aiInteractionsToday: number;
  successfulAiInteractionsToday: number;
  lastOrderAtUtc?: string | null;
  hasMasterPassword: boolean;
  masterPasswordRotatedAtUtc?: string | null;
};

export type AdminDashboard = {
  summary: AdminDashboardSummary;
  codes: SignupCode[];
  users: AdminUser[];
  companies: AdminCompanyFlow[];
};

export type AdminSensitiveActionPayload = {
  password: string;
};

export type CreateAdminOwnerPayload = {
  companyId: string;
  fullName: string;
  email: string;
  ownerPassword: string;
  rootPassword: string;
};

export type UpdateAdminOwnerPayload = {
  fullName: string;
  email: string;
  rootPassword: string;
};

export type ResetAdminOwnerPasswordPayload = {
  newPassword: string;
  rootPassword: string;
};

export type AdminOwnerSensitivePayload = {
  rootPassword: string;
};

export type HardDeleteAdminOwnerPayload = AdminOwnerSensitivePayload & {
  confirmationText: string;
};

export type UpdateAdminCompanyPlanPayload = AdminSensitiveActionPayload & {
  planName?: string | null;
  includesMenuModule: boolean;
  includesTablesModule: boolean;
  includesKitchenModule: boolean;
  includesCashModule: boolean;
  includesStockModule: boolean;
  includesDeliveryModule: boolean;
  includesPrintingModule: boolean;
  includesWaiterCallModule: boolean;
  includesAiAssistantModule: boolean;
  maxUsers: number;
};

export type DeleteAdminCompanyPayload = AdminSensitiveActionPayload & {
  confirmationText: string;
};

export type AdminCompanyPlanUpdate = {
  companyId: string;
  restaurantName: string;
  planName: string;
  planTier?: string;
  monthlyPrice: number;
  maxUsers: number;
  includesMenuModule: boolean;
  includesTablesModule: boolean;
  includesKitchenModule: boolean;
  includesCashModule: boolean;
  includesStockModule: boolean;
  includesDeliveryModule: boolean;
  includesPrintingModule: boolean;
  includesWaiterCallModule: boolean;
  includesAiAssistantModule: boolean;
  hasWhatsAppAI?: boolean;
  hasDelivery?: boolean;
  hasAutoPrint?: boolean;
  hasBasicReports?: boolean;
  hasManagementDashboard?: boolean;
  hasAdvancedReports?: boolean;
  hasCoupons?: boolean;
  hasRecurringCustomers?: boolean;
};

export type CleanupSignupCodesResult = {
  deletedCount: number;
  remainingCount: number;
};

export type AdminCompanyMasterPasswordReveal = {
  companyId: string;
  restaurantName: string;
  hasMasterPassword: boolean;
  maskedPassword: string;
  rotatedAtUtc?: string | null;
  rawPassword: string;
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
      const errorBody = (await response.json()) as { detail?: string; title?: string; message?: string };
      message = errorBody.detail || errorBody.message || errorBody.title || message;
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
      const errorBody = (await response.json()) as { detail?: string; title?: string; message?: string };
      message = errorBody.detail || errorBody.message || errorBody.title || message;
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
    const hostname = window.location.hostname;

    if (hostname === "localhost" || hostname === "127.0.0.1") {
      try {
        const configuredUrl = new URL(CONFIGURED_API_BASE_URL);

        return configuredUrl.origin;
      } catch {
        return CONFIGURED_API_BASE_URL;
      }
    }

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
    startingPrice: item.startingPrice ?? item.price,
    maxAdditionalSelections: item.maxAdditionalSelections ?? null,
    hasAdditionalOptions: item.hasAdditionalOptions ?? (item.additionalGroups?.length ?? 0) > 0,
    additionalGroups: (item.additionalGroups ?? []).map((group) => ({
      ...group,
      maxAdditionalSelections: group.maxAdditionalSelections ?? null,
      options: group.options.map((option) => ({
        ...option,
      })),
    })),
  };
}

function normalizeMenuAdditionalCatalogGroups(groups: MenuAdditionalCatalogGroup[]) {
  return groups.map((group) => ({
    ...group,
    maxAdditionalSelections: group.maxAdditionalSelections ?? null,
    linkedItemCount: group.linkedItemCount ?? 0,
    linkedItemNames: group.linkedItemNames ?? [],
    options: group.options.map((option) => ({
      ...option,
    })),
  }));
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
    fulfillmentType: order.fulfillmentType || (order.isDeliveryOrder ? "Delivery" : "Local"),
    originalTotalAmount: Number(order.originalTotalAmount ?? order.totalAmount ?? 0),
    totalAmount: Number(order.totalAmount ?? 0),
    totalItemQuantity: Number(order.totalItemQuantity ?? order.items?.reduce((total, item) => total + Number(item.quantity ?? 0), 0) ?? 0),
    isEdited: Boolean(order.isEdited),
    discountAmount: Number(order.discountAmount ?? 0),
    surchargeAmount: Number(order.surchargeAmount ?? 0),
    couponDiscountAmount: Number(order.couponDiscountAmount ?? 0),
    couponId: order.couponId ?? null,
    couponCode: order.couponCode ?? null,
    couponAppliedAtUtc: order.couponAppliedAtUtc ?? null,
    hasPriceAdjustment: Boolean(
      order.hasPriceAdjustment ||
        Number(order.discountAmount ?? 0) > 0 ||
        Number(order.surchargeAmount ?? 0) > 0 ||
        order.priceAdjustedAtUtc,
    ),
    paymentTotalAmount: Number(order.paymentTotalAmount ?? 0),
    remainingPaymentAmount: Number(order.remainingPaymentAmount ?? 0),
    payments: order.payments ?? [],
    items: (order.items ?? []).map(normalizeOrderItem),
  };
}

function decoratePublicDeliveryOrder(order: CustomerOrder, _publicCode: string): CustomerOrder {
  const normalized = normalizeCustomerOrder(order);

  if (!normalized.isDeliveryOrder) {
    return normalized;
  }

  const fallbackAssistantMessage =
    normalized.deliveryAssistantMessage ??
    `Recebemos seu delivery com os dados informados.\n\nA unidade acompanha a entrega e confirma qualquer detalhe de pagamento pelo atendimento. Se precisar corrigir algo, fale com a unidade por esse atendimento.`;

  return {
    ...normalized,
    canEditPublicly: false,
    publicEditAllowedUntilUtc: null,
    publicEditUrl: null,
    deliveryAssistantMessage: fallbackAssistantMessage,
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

function normalizeMenuCategory(category: MenuCategory) {
  return {
    ...category,
    imageUrl: resolveApiAssetUrl(category.imageUrl),
    items: category.items.map(normalizeMenuItem),
  };
}

function normalizeMenuCategorySummary(category: MenuCategorySummary): MenuCategorySummary {
  return {
    ...category,
    imageUrl: resolveApiAssetUrl(category.imageUrl),
    startingPrice: category.startingPrice ?? null,
  };
}

function normalizeMenuCategories(categories: MenuCategory[]) {
  return categories.map(normalizeMenuCategory);
}

function normalizeAlertSettings(alerts: AlertSettings): AlertSettings {
  return {
    ...alerts,
    soundUrl: resolveApiAssetUrl(alerts.soundUrl),
    volumePercent: Number.isFinite(alerts.volumePercent) ? alerts.volumePercent : 100,
    playbackSeconds: Number.isFinite(alerts.playbackSeconds) ? alerts.playbackSeconds : 6,
  };
}

function normalizeCompanySettings(settings: CompanySettings): CompanySettings {
  return {
    ...settings,
    logoUrl: resolveApiAssetUrl(settings.logoUrl),
    alerts: normalizeAlertSettings(settings.alerts),
    shortcutAccess: normalizeOwnerShortcutAccess(settings.shortcutAccess),
  };
}

function normalizeOwnerShortcutAccess(shortcutAccess?: OwnerShortcutAccess | null): OwnerShortcutAccess {
  return {
    isEnabled: Boolean(shortcutAccess?.isEnabled),
    createdAtUtc: shortcutAccess?.createdAtUtc ?? null,
    expiresAtUtc: shortcutAccess?.expiresAtUtc ?? null,
    lastUsedAtUtc: shortcutAccess?.lastUsedAtUtc ?? null,
  };
}

export function loginPortal(payload: LoginPayload) {
  return apiRequest<LoginResult>("/api/auth/login", {
    method: "POST",
    body: payload,
  });
}

export function loginWithShortcut(payload: ShortcutLoginPayload) {
  return apiRequest<LoginResult>("/api/auth/shortcut-login", {
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

export function getAdminDashboard(token: string) {
  return apiRequest<AdminDashboard>("/api/admin/dashboard", { token });
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

export function deleteSignupCode(token: string, codeId: string) {
  return apiRequest<void>(`/api/admin/signup-codes/${codeId}`, {
    method: "DELETE",
    token,
  });
}

export function cleanupSignupCodes(token: string) {
  return apiRequest<CleanupSignupCodesResult>("/api/admin/signup-codes/cleanup", {
    method: "POST",
    token,
  });
}

export function getAdminUsers(token: string) {
  return apiRequest<AdminUser[]>("/api/admin/users", { token });
}

export function getAdminOwners(token: string) {
  return apiRequest<AdminOwner[]>("/api/admin/owners", { token });
}

export function createAdminOwner(token: string, payload: CreateAdminOwnerPayload) {
  return apiRequest<AdminOwner>("/api/admin/owners", {
    method: "POST",
    token,
    body: payload,
  });
}

export function updateAdminOwner(token: string, ownerId: string, payload: UpdateAdminOwnerPayload) {
  return apiRequest<AdminOwner>(`/api/admin/owners/${ownerId}`, {
    method: "PUT",
    token,
    body: payload,
  });
}

export function resetAdminOwnerPassword(token: string, ownerId: string, payload: ResetAdminOwnerPasswordPayload) {
  return apiRequest<void>(`/api/admin/owners/${ownerId}/reset-password`, {
    method: "POST",
    token,
    body: payload,
  });
}

export function deactivateAdminOwner(token: string, ownerId: string, payload: AdminOwnerSensitivePayload) {
  return apiRequest<AdminOwner>(`/api/admin/owners/${ownerId}/deactivate`, {
    method: "PATCH",
    token,
    body: payload,
  });
}

export function reactivateAdminOwner(token: string, ownerId: string, payload: AdminOwnerSensitivePayload) {
  return apiRequest<AdminOwner>(`/api/admin/owners/${ownerId}/reactivate`, {
    method: "PATCH",
    token,
    body: payload,
  });
}

export function hardDeleteAdminOwner(token: string, ownerId: string, payload: HardDeleteAdminOwnerPayload) {
  return apiRequest<void>(`/api/admin/owners/${ownerId}`, {
    method: "DELETE",
    token,
    body: payload,
  });
}

export function reactivateAdminUser(token: string, userId: string, payload: AdminSensitiveActionPayload) {
  return apiRequest<AdminUser>(`/api/admin/users/${userId}/reactivate`, {
    method: "PATCH",
    token,
    body: payload,
  });
}

export function deactivateAdminUser(token: string, userId: string, payload: AdminSensitiveActionPayload) {
  return apiRequest<AdminUser>(`/api/admin/users/${userId}/deactivate`, {
    method: "PATCH",
    token,
    body: payload,
  });
}

export function deleteAdminUser(token: string, userId: string, payload: AdminSensitiveActionPayload) {
  return apiRequest<void>(`/api/admin/users/${userId}`, {
    method: "DELETE",
    token,
    body: payload,
  });
}

export function revealAdminMasterPassword(token: string, companyId: string, payload: AdminSensitiveActionPayload) {
  return apiRequest<AdminCompanyMasterPasswordReveal>(`/api/admin/companies/${companyId}/master-password/reveal`, {
    method: "POST",
    token,
    body: payload,
  });
}

export function rotateAdminMasterPassword(token: string, companyId: string, payload: AdminSensitiveActionPayload) {
  return apiRequest<AdminCompanyMasterPasswordReveal>(`/api/admin/companies/${companyId}/master-password/rotate`, {
    method: "POST",
    token,
    body: payload,
  });
}

export function updateAdminCompanyPlan(token: string, companyId: string, payload: UpdateAdminCompanyPlanPayload) {
  return apiRequest<AdminCompanyPlanUpdate>(`/api/admin/companies/${companyId}/plan`, {
    method: "PATCH",
    token,
    body: payload,
  });
}

export function deleteAdminCompany(token: string, companyId: string, payload: DeleteAdminCompanyPayload) {
  return apiRequest<void>(`/api/admin/companies/${companyId}`, {
    method: "DELETE",
    token,
    body: payload,
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

export function ensureCashOrderTable(token: string) {
  return apiRequest<DiningTable>("/api/workspace/tables/cash-order", {
    method: "POST",
    token,
  }).then(normalizeDiningTable);
}

export function getMenu(token: string) {
  return apiRequest<MenuCategory[]>("/api/workspace/menu", { token }).then(normalizeMenuCategories);
}

export function getMenuCategorySummaries(token: string) {
  return apiRequest<MenuCategorySummary[]>("/api/workspace/menu/categories", { token })
    .then((response) => response.map(normalizeMenuCategorySummary));
}

export function getMenuCategoryItems(token: string, categoryId: string) {
  return apiRequest<MenuCategory>(`/api/workspace/menu/categories/${categoryId}/items`, { token })
    .then(normalizeMenuCategory);
}

export function getMenuItem(token: string, menuItemId: string) {
  return apiRequest<MenuItem>(`/api/workspace/menu/items/${menuItemId}`, { token }).then(normalizeMenuItem);
}

export function getMenuAdditionals(token: string) {
  return apiRequest<MenuAdditionalCatalogGroup[]>("/api/workspace/menu/additionals", { token })
    .then(normalizeMenuAdditionalCatalogGroups);
}

export function createMenuCategory(token: string, payload: { name: string; imageUrl?: string | null }) {
  return apiRequest<MenuCategory>("/api/workspace/menu/categories", {
    method: "POST",
    token,
    body: payload,
  }).then(normalizeMenuCategory);
}

export function updateMenuCategory(token: string, categoryId: string, payload: { name: string; imageUrl?: string | null }) {
  return apiRequest<MenuCategory>(`/api/workspace/menu/categories/${categoryId}`, {
    method: "PUT",
    token,
    body: payload,
  }).then(normalizeMenuCategory);
}

type MenuItemAdditionalGroupPayload = Array<{
  catalogGroupId?: string | null;
  name: string;
  allowMultiple: boolean;
  maxAdditionalSelections?: number | null;
  options: Array<{
    catalogOptionId?: string | null;
    name: string;
    price: number;
  }>;
}>;

type MenuAdditionalCatalogGroupPayload = {
  name: string;
  allowMultiple: boolean;
  maxAdditionalSelections?: number | null;
  options: Array<{
    name: string;
    price: number;
  }>;
};

export function createMenuAdditionalGroup(token: string, payload: MenuAdditionalCatalogGroupPayload) {
  return apiRequest<MenuAdditionalCatalogGroup>("/api/workspace/menu/additionals", {
    method: "POST",
    token,
    body: payload,
  });
}

export function updateMenuAdditionalGroup(token: string, groupId: string, payload: MenuAdditionalCatalogGroupPayload) {
  return apiRequest<MenuAdditionalCatalogGroup>(`/api/workspace/menu/additionals/${groupId}`, {
    method: "PUT",
    token,
    body: payload,
  });
}

export function deleteMenuAdditionalGroup(token: string, groupId: string) {
  return apiRequest<void>(`/api/workspace/menu/additionals/${groupId}`, {
    method: "DELETE",
    token,
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
    maxAdditionalSelections?: number | null;
    additionalGroups?: MenuItemAdditionalGroupPayload;
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
    maxAdditionalSelections?: number | null;
    additionalGroups?: MenuItemAdditionalGroupPayload;
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

export function uploadMenuCategoryImage(token: string, file: File) {
  const formData = new FormData();
  formData.append("file", file);

  return apiFormRequest<UploadMenuItemImageResult>("/api/workspace/menu/categories/images", formData, token)
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

export function createTable(token: string, payload: { name: string; seats: number; comandaLabel?: string | null }) {
  return apiRequest<DiningTable>("/api/workspace/tables", {
    method: "POST",
    token,
    body: payload,
  }).then(normalizeDiningTable);
}

export function ensureDeliveryTable(token: string) {
  return apiRequest<DiningTable>("/api/workspace/tables/delivery", {
    method: "POST",
    token,
  }).then(normalizeDiningTable);
}

export function updateTable(token: string, tableId: string, payload: { name: string; seats: number; comandaLabel?: string | null }) {
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

export function getOrders(token: string, kitchenOnly = false, summaryOnly = false) {
  return apiRequest<CustomerOrder[]>(`/api/workspace/orders?kitchenOnly=${kitchenOnly}&summaryOnly=${summaryOnly}`, { token }).then((response) =>
    response.map(normalizeCustomerOrder),
  );
}

export function getOrder(token: string, orderId: string) {
  return apiRequest<CustomerOrder>(`/api/workspace/orders/${orderId}`, { token }).then(normalizeCustomerOrder);
}

export function updateOrderStatus(token: string, orderId: string, status: string, password?: string) {
  return apiRequest<CustomerOrder>(`/api/workspace/orders/${orderId}/status`, {
    method: "PATCH",
    token,
    body: { status, password },
  }).then(normalizeCustomerOrder);
}

export function updateOrder(token: string, orderId: string, payload: UpdateOrderPayload) {
  return apiRequest<CustomerOrder>(`/api/workspace/orders/${orderId}`, {
    method: "PUT",
    token,
    body: payload,
  }).then(normalizeCustomerOrder);
}

export function adjustOrderValue(token: string, orderId: string, payload: AdjustOrderValuePayload) {
  return apiRequest<CustomerOrder>(`/api/workspace/orders/${orderId}/adjustment`, {
    method: "PATCH",
    token,
    body: payload,
  }).then(normalizeCustomerOrder);
}

export function updateOrdersStatusBatch(token: string, orderIds: string[], status: string, password?: string) {
  return apiRequest<void>("/api/workspace/orders/batch-status", {
    method: "POST",
    token,
    body: { orderIds, status, password },
  });
}

export function updateOrderPayment(
  token: string,
  orderId: string,
  paymentStatus: string,
  paymentMethod?: string,
  payments?: OrderPaymentInput[],
) {
  return apiRequest<CustomerOrder>(`/api/workspace/orders/${orderId}/payment`, {
    method: "PATCH",
    token,
    body: { paymentStatus, paymentMethod, payments },
  }).then(normalizeCustomerOrder);
}

export function markAllOrdersPaid(token: string, orders: MarkOrderPaidInput[]) {
  return apiRequest<MarkAllOrdersPaidResult>("/api/workspace/orders/mark-all-paid", {
    method: "PATCH",
    token,
    body: { orders },
  });
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
  return apiRequest<CompanySettings>("/api/workspace/settings", { token }).then(normalizeCompanySettings);
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
  }).then(normalizeCompanySettings);
}

export function uploadCompanyLogo(token: string, file: File) {
  const formData = new FormData();
  formData.append("file", file);

  return apiFormRequest<CompanySettings>("/api/workspace/settings/logo", formData, token)
    .then(normalizeCompanySettings);
}

export function deleteCompanyLogo(token: string) {
  return apiRequest<CompanySettings>("/api/workspace/settings/logo", {
    method: "DELETE",
    token,
  }).then(normalizeCompanySettings);
}

export function generateOwnerShortcutAccess(token: string, password: string) {
  return apiRequest<GenerateOwnerShortcutAccessResult>("/api/workspace/settings/shortcut-access", {
    method: "POST",
    token,
    body: { password },
  }).then((response) => ({
    ...response,
    shortcutAccess: normalizeOwnerShortcutAccess(response.shortcutAccess),
  }));
}

export function revokeOwnerShortcutAccess(token: string, password: string) {
  return apiRequest<OwnerShortcutAccess>("/api/workspace/settings/shortcut-access", {
    method: "DELETE",
    token,
    body: { password },
  }).then(normalizeOwnerShortcutAccess);
}

export function getOwnerProfile(token: string) {
  return apiRequest<OwnerProfile>("/api/workspace/profile", { token });
}

export function updateOwnerProfile(token: string, payload: { fullName: string; email: string }) {
  return apiRequest<OwnerProfile>("/api/workspace/profile", {
    method: "PUT",
    token,
    body: payload,
  });
}

export function changeOwnerPassword(
  token: string,
  payload: {
    currentPassword: string;
    newPassword: string;
    confirmPassword: string;
  },
) {
  return apiRequest<void>("/api/workspace/profile/password", {
    method: "PUT",
    token,
    body: payload,
  });
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

export function getDeliveryFreightSettings(token: string) {
  return apiRequest<DeliveryFreightSettings>("/api/workspace/delivery/freight", { token });
}

export function updateDeliveryFreightSettings(
  token: string,
  payload: {
    isEnabled: boolean;
    originPostalCode?: string;
    pricePerKm: number;
    baseFee: number;
    baseDistanceKm: number;
    pickupEstimatedMinutes?: number | null;
    deliveryEstimatedMinutes?: number | null;
    password: string;
  },
) {
  return apiRequest<DeliveryFreightSettings>("/api/workspace/delivery/freight", {
    method: "PATCH",
    token,
    body: payload,
  });
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

export type PrintingTestJobResult = {
  jobId: string;
  status: string;
  queuedAtUtc: string;
  printing: PrintingSettings;
};

export function createPrintingTestJob(token: string, notes?: string) {
  return apiRequest<PrintingTestJobResult>("/api/workspace/printing/test-job", {
    method: "POST",
    token,
    body: { notes: notes?.trim() ? notes.trim() : undefined },
  });
}

export function getCoupons(token: string) {
  return apiRequest<Coupon[]>("/api/workspace/coupons", { token });
}

export function createCoupon(token: string, payload: SaveCouponPayload) {
  return apiRequest<Coupon>("/api/workspace/coupons", {
    method: "POST",
    token,
    body: payload,
  });
}

export function updateCoupon(token: string, couponId: string, payload: SaveCouponPayload) {
  return apiRequest<Coupon>(`/api/workspace/coupons/${couponId}`, {
    method: "PUT",
    token,
    body: payload,
  });
}

export function updateCouponStatus(token: string, couponId: string, isActive: boolean) {
  return apiRequest<Coupon>(`/api/workspace/coupons/${couponId}/status`, {
    method: "PATCH",
    token,
    body: { isActive },
  });
}

export function getCashClosing(token: string, date: string) {
  return apiRequest<CashClosingReport>(`/api/workspace/cash-closing/${date}`, { token });
}

export function getMercadoPagoStatus(token: string) {
  return apiRequest<MercadoPagoStatus>("/api/workspace/payments/mercadopago/status", { token });
}

export function startMercadoPagoConnection(token: string) {
  return apiRequest<MercadoPagoConnectResponse>("/api/workspace/payments/mercadopago/connect", {
    method: "POST",
    token,
  });
}

export function disconnectMercadoPago(token: string) {
  return apiRequest<void>("/api/workspace/payments/mercadopago/disconnect", {
    method: "DELETE",
    token,
  });
}

export function createPublicMercadoPagoCheckout(publicCode: string, orderId: string) {
  return apiRequest<MercadoPagoCheckoutResponse>(
    `/api/public/tables/${publicCode}/orders/${orderId}/mercadopago/checkout`,
    {
      method: "POST",
    },
  );
}

export function getAiAssistantSettings(token: string) {
  return apiRequest<AiAssistantSettings>("/api/workspace/ai", { token });
}

export function getAiAssistantQuickStatus(token: string) {
  return apiRequest<AiAssistantQuickStatus>("/api/workspace/ai/status", { token });
}

export function updateAiAssistantQuickStatus(token: string, isEnabled: boolean) {
  return apiRequest<AiAssistantQuickStatus>("/api/workspace/ai/status", {
    method: "PATCH",
    token,
    body: { isEnabled },
  });
}

export function updateAiAssistantSettings(
  token: string,
  payload: {
    isEnabled: boolean;
    model: string;
    systemPrompt: string;
    greetingMessage: string;
    redirectMessage: string;
    fallbackMessage: string;
    orderingLink?: string;
    pixReceiverName?: string;
    pixKey?: string;
    pixMessage?: string;
    serviceDays?: number[];
    serviceStartTime?: string;
    serviceEndTime?: string;
    maxOutputTokens: number;
    whatsAppEnabled: boolean;
    whatsAppInstanceId?: string;
    newWhatsAppInstanceToken?: string;
    newWhatsAppAccountSecurityToken?: string;
  },
) {
  return apiRequest<AiAssistantSettings>("/api/workspace/ai", {
    method: "PATCH",
    token,
    body: payload,
  });
}

export function generateAiAssistantTemplate(token: string) {
  return apiRequest<AiAssistantSettings>("/api/workspace/ai/generate-template", {
    method: "POST",
    token,
  });
}

export function testAiAssistant(token: string, message: string) {
  return apiRequest<AiAssistantTestResult>("/api/workspace/ai/test", {
    method: "POST",
    token,
    body: { message },
  });
}

export function prepareWhatsAppConnection(token: string, payload?: PrepareWhatsAppConnectionPayload) {
  return apiRequest<WhatsAppConnectionSnapshot>("/api/workspace/ai/whatsapp/prepare", {
    method: "POST",
    token,
    body: payload,
  });
}

export function getPublicTable(publicCode: string) {
  return apiRequest<PublicTableView>(`/api/public/tables/${publicCode}`)
    .then((response) => ({
      ...response,
      restaurantLogoUrl: resolveApiAssetUrl(response.restaurantLogoUrl),
      menu: normalizeMenuCategories(response.menu),
    }));
}

export function getPublicMenuItem(publicCode: string, menuItemId: string) {
  return apiRequest<MenuItem>(`/api/public/tables/${publicCode}/menu/items/${menuItemId}`)
    .then(normalizeMenuItem);
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
    deliveryPhone?: string;
    deliveryAddress?: string;
    deliveryNumber?: string;
    deliveryComplement?: string;
    deliveryPostalCode?: string;
    fulfillmentType?: string;
    paymentMethod?: string;
    couponCode?: string;
    items?: OrderItemInput[];
    menuSelections?: MenuOrderSelectionInput[];
  },
) {
  return apiRequest<CustomerOrder>(`/api/public/tables/${publicCode}/orders`, {
    method: "POST",
    body: payload,
  }).then((response) => decoratePublicDeliveryOrder(response, publicCode));
}

export function createPublicSellerLinkOrder(
  sellerCode: string,
  publicCode: string,
  payload: Parameters<typeof createPublicOrder>[1],
) {
  return apiRequest<CustomerOrder>(`/api/public/seller-link/${sellerCode}/orders`, {
    method: "POST",
    body: payload,
  }).then((response) => decoratePublicDeliveryOrder(response, publicCode));
}

export function validatePublicCoupon(
  publicCode: string,
  payload: {
    code: string;
    subtotal: number;
  },
) {
  return apiRequest<CouponValidation>(`/api/public/tables/${publicCode}/coupons/validate`, {
    method: "POST",
    body: payload,
  });
}

export function quotePublicDeliveryFreight(
  publicCode: string,
  payload: {
    destinationPostalCode?: string;
    subtotal: number;
  },
) {
  return apiRequest<DeliveryFreightQuote>(`/api/public/tables/${publicCode}/freight/quote`, {
    method: "POST",
    body: payload,
  });
}

export function getPublicDeliveryCustomerProfile(publicCode: string, token: string) {
  return apiRequest<PublicDeliveryCustomerProfile>(
    `/api/public/tables/${publicCode}/delivery/customer?token=${encodeURIComponent(token)}`,
  );
}

export function getPublicCustomerProfile(code: string) {
  return apiRequest<PublicCustomerProfile>(`/api/public/customer-profile/${encodeURIComponent(code)}`);
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

export type CustomerProfile = {
  id: string;
  phoneNumber: string;
  name?: string | null;
  zipCode?: string | null;
  street?: string | null;
  number?: string | null;
  neighborhood?: string | null;
  complement?: string | null;
  createdAtUtc: string;
  updatedAtUtc: string;
  lastOrderAtUtc?: string | null;
};

export type CustomerHistoryItem = {
  itemName: string;
  quantity: number;
};

export type CustomerHistoryEntry = {
  orderId: string;
  createdAtUtc: string;
  totalAmount: number;
  items: CustomerHistoryItem[];
};

export type SaveCustomerProfilePayload = {
  name?: string | null;
  zipCode?: string | null;
  street?: string | null;
  number?: string | null;
  neighborhood?: string | null;
  complement?: string | null;
};

export function getCustomerProfile(token: string, phoneNumber: string) {
  return apiRequest<CustomerProfile>(`/api/workspace/customers/${encodeURIComponent(phoneNumber)}/profile`, { token });
}

export function getCustomerHistory(token: string, phoneNumber: string) {
  return apiRequest<CustomerHistoryEntry[]>(`/api/workspace/customers/${encodeURIComponent(phoneNumber)}/history`, { token });
}

export function updateCustomerProfile(token: string, phoneNumber: string, payload: SaveCustomerProfilePayload) {
  return apiRequest<CustomerProfile>(`/api/workspace/customers/${encodeURIComponent(phoneNumber)}/profile`, {
    method: "PUT",
    token,
    body: payload,
  });
}

export type SalesAgent = {
  id: string;
  name: string;
  phone?: string | null;
  code: string;
  commissionPercent?: number | null;
  isActive: boolean;
  createdAtUtc: string;
};

export type CreateSalesAgentPayload = {
  name: string;
  phone?: string | null;
  commissionPercent?: number | null;
};

export type UpdateSalesAgentPayload = {
  name: string;
  phone?: string | null;
  commissionPercent?: number | null;
};

export type PublicSellerLink = {
  sellerName: string;
  companyName: string;
  companyLogoUrl?: string | null;
  cashTablePublicCode: string;
};

export function getSalesAgents(token: string) {
  return apiRequest<SalesAgent[]>("/api/workspace/sellers", { token });
}

export function createSalesAgent(token: string, payload: CreateSalesAgentPayload) {
  return apiRequest<SalesAgent>("/api/workspace/sellers", {
    method: "POST",
    token,
    body: payload,
  });
}

export function updateSalesAgent(token: string, agentId: string, payload: UpdateSalesAgentPayload) {
  return apiRequest<SalesAgent>(`/api/workspace/sellers/${agentId}`, {
    method: "PUT",
    token,
    body: payload,
  });
}

export function updateSalesAgentStatus(token: string, agentId: string, isActive: boolean) {
  return apiRequest<SalesAgent>(`/api/workspace/sellers/${agentId}/status`, {
    method: "PATCH",
    token,
    body: { isActive },
  });
}

export function getPublicSellerLink(code: string) {
  return apiRequest<PublicSellerLink>(`/api/public/seller-link/${code}`, {});
}

export function getSellerOrders(token: string, agentId: string) {
  return apiRequest<CustomerOrder[]>(`/api/workspace/sellers/${agentId}/orders`, { token }).then(
    (res) => res.map(normalizeCustomerOrder),
  );
}
