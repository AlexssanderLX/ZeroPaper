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
};

export const PORTAL_SESSION_KEY = "zp.portal.session";

export const ownerModules: PortalModule[] = [
  {
    slug: "implantacao",
    title: "Implantacao",
    eyebrow: "Guia",
  },
  {
    slug: "cardapio",
    title: "Cardapio",
    eyebrow: "Cardapio",
  },
  {
    slug: "estoque",
    title: "Estoque",
    eyebrow: "Estoque",
  },
  {
    slug: "mesas",
    title: "Mesas",
    eyebrow: "Mesas",
  },
  {
    slug: "pedidos",
    title: "Pedidos para a cozinha",
    eyebrow: "Cozinha",
  },
  {
    slug: "caixa",
    title: "Caixa",
    eyebrow: "Caixa",
  },
  {
    slug: "impressao",
    title: "Impressao",
    eyebrow: "Impressao",
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
