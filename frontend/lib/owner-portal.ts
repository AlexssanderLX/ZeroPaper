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
};

export type PortalFeatureAccess = {
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
};

export const PORTAL_SESSION_KEY = "zp.portal.session";

export const ownerModules: PortalModule[] = [
  {
    slug: "implantacao",
    title: "Configurar app",
    eyebrow: "Guia inicial",
  },
  {
    slug: "cardapio",
    title: "Cardapio",
    eyebrow: "Cardapio",
    featureKey: "includesMenuModule",
  },
  {
    slug: "estoque",
    title: "Estoque",
    eyebrow: "Estoque",
    featureKey: "includesStockModule",
  },
  {
    slug: "mesas",
    title: "Mesas",
    eyebrow: "Mesas",
    featureKey: "includesTablesModule",
  },
  {
    slug: "pedidos",
    title: "Cozinha",
    eyebrow: "Cozinha",
    featureKey: "includesKitchenModule",
  },
  {
    slug: "caixa",
    title: "Caixa",
    eyebrow: "Caixa",
    featureKey: "includesCashModule",
  },
  {
    slug: "cupons",
    title: "Cupons",
    eyebrow: "Descontos",
    featureKey: "hasCoupons",
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
];

export function getModuleBySlug(slug: string) {
  return ownerModules.find((module) => module.slug === slug);
}

export function isPortalModuleAvailable(module: PortalModule, access?: Partial<PortalFeatureAccess> | null) {
  if (!module.featureKey) {
    return true;
  }

  return Boolean(access?.[module.featureKey]);
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
