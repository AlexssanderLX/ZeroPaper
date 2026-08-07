export type AccessProfile = "restaurant" | "admin";

export type PortalSession = {
  token: string;
  email: string;
  profile: AccessProfile;
  restaurantName: string;
  ownerName: string;
  role: string;
  expiresAtUtc: string;
};

export type PortalModule = {
  slug: string;
  title: string;
  eyebrow: string;
  featureKey?: keyof PortalFeatureAccess;
  segments?: Array<"restaurant" | "petshop">;
};

export type PortalFeatureAccess = {
  capabilities?: Partial<{
    hasCustomerProfiles: boolean;
    hasCatalog: boolean;
    hasPets: boolean;
    hasAppointments: boolean;
    hasOnlinePayments: boolean;
    hasAiAssistant: boolean;
    hasCoupons: boolean;
    hasReports: boolean;
    hasPrinting: boolean;
    hasDelivery: boolean;
    hasTables: boolean;
    hasKitchen: boolean;
    hasWaiterCalls: boolean;
  }>;
  includesMenuModule: boolean;
  includesTablesModule: boolean;
  includesKitchenModule: boolean;
  includesCashModule: boolean;
  includesStockModule: boolean;
  includesDeliveryModule: boolean;
  includesPrintingModule: boolean;
  includesWaiterCallModule: boolean;
  includesAiAssistantModule: boolean;
  hasCoupons: boolean;
  hasBasicReports: boolean;
  hasAdvancedReports: boolean;
  hasSalesAgents: boolean;
  hasCustomerProfiles: boolean;
  hasPets: boolean;
  hasAppointments: boolean;
};

export const PORTAL_SESSION_KEY = "zp.portal.session";

export const ownerModules: PortalModule[] = [
  {
    slug: "implantacao",
    title: "Configurar app",
    eyebrow: "Guia inicial",
    segments: ["restaurant"],
  },
  {
    slug: "cardapio",
    title: "Cardapio",
    eyebrow: "Cardapio",
    featureKey: "includesMenuModule",
    segments: ["restaurant", "petshop"],
  },
  {
    slug: "estoque",
    title: "Estoque",
    eyebrow: "Estoque",
    featureKey: "includesStockModule",
    segments: ["restaurant"],
  },
  {
    slug: "mesas",
    title: "Mesas",
    eyebrow: "Mesas",
    featureKey: "includesTablesModule",
    segments: ["restaurant"],
  },
  {
    slug: "pedidos",
    title: "Cozinha",
    eyebrow: "Cozinha",
    featureKey: "includesKitchenModule",
    segments: ["restaurant"],
  },
  {
    slug: "caixa",
    title: "Caixa",
    eyebrow: "Caixa",
    featureKey: "includesCashModule",
    segments: ["restaurant"],
  },
  {
    slug: "cupons",
    title: "Cupons",
    eyebrow: "Descontos",
    featureKey: "hasCoupons",
    segments: ["restaurant"],
  },
  {
    slug: "relatorios",
    title: "Relatorios",
    eyebrow: "Fluxo do dia",
    featureKey: "hasBasicReports",
  },
  {
    slug: "analise-vendas",
    title: "Analise de vendas",
    eyebrow: "Gestao",
    featureKey: "hasAdvancedReports",
  },
  {
    slug: "impressao",
    title: "Impressao",
    eyebrow: "Impressao",
    featureKey: "includesPrintingModule",
  },
  {
    slug: "entrega",
    title: "Entrega e frete",
    eyebrow: "Delivery",
    featureKey: "includesDeliveryModule",
    segments: ["restaurant"],
  },
  {
    slug: "atendimento",
    title: "Atendimento, IA e WhatsApp",
    eyebrow: "IA",
    featureKey: "includesAiAssistantModule",
  },
  {
    slug: "vendedores",
    title: "Vendedores",
    eyebrow: "Vendas",
    featureKey: "hasSalesAgents",
    segments: ["restaurant"],
  },
  {
    slug: "pagamentos",
    title: "Pagamento online",
    eyebrow: "Mercado Pago",
  },
  {
    slug: "ajustes",
    title: "Unidade",
    eyebrow: "Unidade",
  },
  // Pet Shop modules
  {
    slug: "horarios-pet",
    title: "Horarios de atendimento",
    eyebrow: "Pet Shop",
    featureKey: "hasAppointments",
    segments: ["petshop"],
  },
  {
    slug: "tutores",
    title: "Tutores",
    eyebrow: "Pet Shop",
    featureKey: "hasCustomerProfiles",
    segments: ["petshop"],
  },
  {
    slug: "animais",
    title: "Animais",
    eyebrow: "Pet Shop",
    featureKey: "hasPets",
    segments: ["petshop"],
  },
  {
    slug: "agenda",
    title: "Meus agendamentos",
    eyebrow: "Pet Shop",
    featureKey: "hasAppointments",
    segments: ["petshop"],
  },
  {
    slug: "atendimentos-pet",
    title: "Atendimentos",
    eyebrow: "Pet Shop",
    featureKey: "hasAppointments",
    segments: ["petshop"],
  },
];

export function getModuleBySlug(slug: string) {
  return ownerModules.find((module) => module.slug === slug);
}

export function isPortalModuleAvailable(module: PortalModule, access?: Partial<PortalFeatureAccess> | null) {
  if (!module.featureKey) {
    return true;
  }

  const capabilityByFeature: Partial<Record<keyof PortalFeatureAccess, keyof NonNullable<PortalFeatureAccess["capabilities"]>>> = {
    includesMenuModule: "hasCatalog",
    includesTablesModule: "hasTables",
    includesKitchenModule: "hasKitchen",
    includesCashModule: "hasOnlinePayments",
    includesDeliveryModule: "hasDelivery",
    includesPrintingModule: "hasPrinting",
    includesWaiterCallModule: "hasWaiterCalls",
    includesAiAssistantModule: "hasAiAssistant",
    hasCoupons: "hasCoupons",
    hasBasicReports: "hasReports",
    hasCustomerProfiles: "hasCustomerProfiles",
    hasPets: "hasPets",
    hasAppointments: "hasAppointments",
  };
  const capabilityKey = capabilityByFeature[module.featureKey];
  const capabilityValue = capabilityKey ? access?.capabilities?.[capabilityKey] : undefined;

  return capabilityValue ?? Boolean(access?.[module.featureKey]);
}

export function isPortalModuleForSegment(module: PortalModule, businessSegment?: number) {
  if (!module.segments?.length) {
    return true;
  }

  const segment = businessSegment === 2 ? "petshop" : "restaurant";
  return module.segments.includes(segment);
}

export function buildOwnerName(email: string) {
  const [rawName] = email.split("@");
  return rawName
    .split(/[.\-_]/)
    .filter(Boolean)
    .map((part) => part.charAt(0).toUpperCase() + part.slice(1))
    .join(" ");
}

export function savePortalSession(session: PortalSession) {
  const serialized = JSON.stringify(session);

  try {
    window.sessionStorage.setItem(PORTAL_SESSION_KEY, serialized);
  } catch {
    // Ignore storage failures and let the caller continue with the in-memory session only.
  }
}

export function loadPortalSession() {
  try {
    return window.sessionStorage.getItem(PORTAL_SESSION_KEY);
  } catch {
    return null;
  }
}

export function clearPortalSession() {
  try {
    window.sessionStorage.removeItem(PORTAL_SESSION_KEY);
  } catch {
    // Ignore storage cleanup failures.
  }
}
