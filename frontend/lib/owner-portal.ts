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
  summary: string;
  actionLabel: string;
  stats: string[];
  highlights: string[];
};

export const PORTAL_SESSION_KEY = "zp.portal.session";

export const ownerModules: PortalModule[] = [
  {
    slug: "cardapio",
    title: "Cardapio",
    eyebrow: "Venda",
    summary: "Monte o cardapio da unidade com categorias e itens prontos para o cliente pedir pelo QR.",
    actionLabel: "Abrir cardapio",
    stats: ["Categorias", "Itens", "Pedido rapido"],
    highlights: ["Montar vitrine", "Organizar secoes", "Liberar pedido por toque"],
  },
  {
    slug: "mesas",
    title: "Mesas e QR",
    eyebrow: "Salao",
    summary: "Crie mesas, gere o link do QR e deixe o acesso publico pronto para o cliente.",
    actionLabel: "Abrir mesas",
    stats: ["Mesas", "QR ativo", "Link publico"],
    highlights: ["Cadastrar mesa", "Copiar link", "Abrir pagina publica"],
  },
  {
    slug: "pedidos",
    title: "Pedidos",
    eyebrow: "Fluxo",
    summary: "Acompanhe tudo o que entrou na unidade e mova cada pedido pelo andamento certo.",
    actionLabel: "Abrir pedidos",
    stats: ["Entrada", "Andamento", "Historico"],
    highlights: ["Pedido manual", "Pedido por QR", "Status do pedido"],
  },
  {
    slug: "estoque",
    title: "Estoque",
    eyebrow: "Controle",
    summary: "Registre os insumos principais e acompanhe o que precisa de reposicao.",
    actionLabel: "Abrir estoque",
    stats: ["Itens", "Quantidade", "Alerta"],
    highlights: ["Cadastrar item", "Ajustar quantidade", "Ver reposicao"],
  },
  {
    slug: "ajustes",
    title: "Unidade",
    eyebrow: "Conta",
    summary: "Edite os dados principais da empresa e mantenha a base da unidade organizada.",
    actionLabel: "Abrir unidade",
    stats: ["Empresa", "Contato", "Endereco web"],
    highlights: ["Nome da unidade", "Contato", "Slug de acesso"],
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
