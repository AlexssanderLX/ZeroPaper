export type AccessProfile = "restaurant" | "admin";

export type PortalSession = {
  email: string;
  profile: AccessProfile;
  restaurantName: string;
  ownerName: string;
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
    slug: "mesas",
    title: "Mesas e QR",
    eyebrow: "Salao",
    summary: "Abra mesas, gere acessos por QR e acompanhe cada atendimento.",
    actionLabel: "Abrir modulo",
    stats: ["Mesas ativas", "QR por unidade", "Abertura rapida"],
    highlights: ["Cadastro de mesas", "Vinculo com QR", "Entrada por unidade"],
  },
  {
    slug: "pedidos",
    title: "Pedidos",
    eyebrow: "Fluxo",
    summary: "Centralize entrada, andamento e historico dos pedidos da casa.",
    actionLabel: "Ver pedidos",
    stats: ["Fila ativa", "Prioridades", "Historico do dia"],
    highlights: ["Entrada por QR", "Status por pedido", "Acompanhamento continuo"],
  },
  {
    slug: "cozinha",
    title: "Cozinha",
    eyebrow: "Producao",
    summary: "Organize preparo, prioridade e saida com mais clareza para a equipe.",
    actionLabel: "Entrar na cozinha",
    stats: ["Fila por preparo", "Tempo de saida", "Visao da equipe"],
    highlights: ["Pedidos em tela", "Separacao por etapa", "Ritmo operacional"],
  },
  {
    slug: "estoque",
    title: "Estoque",
    eyebrow: "Controle",
    summary: "Tenha visibilidade dos itens criticos e reduza falhas no dia a dia.",
    actionLabel: "Acompanhar estoque",
    stats: ["Itens criticos", "Entradas e saidas", "Base operacional"],
    highlights: ["Visao de insumos", "Controle de reposicao", "Operacao mais leve"],
  },
  {
    slug: "equipe",
    title: "Equipe",
    eyebrow: "Acessos",
    summary: "Gerencie perfis, acessos e rotinas da equipe da unidade.",
    actionLabel: "Gerenciar equipe",
    stats: ["Perfis ativos", "Niveis de acesso", "Rotina da unidade"],
    highlights: ["Dono e gerencia", "Acessos por papel", "Organizacao da operacao"],
  },
  {
    slug: "ajustes",
    title: "Ajustes",
    eyebrow: "Conta",
    summary: "Concentre informacoes da unidade, operacao e configuracoes da conta.",
    actionLabel: "Abrir ajustes",
    stats: ["Dados da unidade", "Acesso da conta", "Preferencias"],
    highlights: ["Dados da empresa", "Configuracao geral", "Base da operacao"],
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

