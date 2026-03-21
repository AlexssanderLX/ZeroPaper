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
    slug: "cardapio",
    title: "Cardapio",
    eyebrow: "Cardapio",
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
