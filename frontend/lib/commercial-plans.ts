export type CommercialPlan = {
  slug: string;
  name: string;
  monthlyPrice: number;
  priceLabel: string;
  audience: string;
  headline: string;
  features: string[];
  spotlight?: boolean;
  premium?: boolean;
  badge?: string;
  maxUsers: number;
};

export const commercialPlans: CommercialPlan[] = [
  {
    slug: "essencial",
    name: "ZeroPaper Essencial",
    monthlyPrice: 80,
    priceLabel: "R$ 80",
    audience: "Para comecar a vender por QR Code.",
    headline: "",
    features: [
      "Cardapio digital com fotos",
      "Mesas com QR Code",
      "Pedidos no painel em tempo real",
      "Cozinha e caixa integrados",
      "Botao chamar atendente",
      "Impressao manual por pedido",
    ],
    maxUsers: 3,
  },
  {
    slug: "operacao",
    name: "ZeroPaper Operacao",
    monthlyPrice: 120,
    priceLabel: "R$ 120",
    audience: "Para automatizar atendimento e delivery.",
    headline: "",
    features: [
      "Tudo do Essencial",
      "WhatsApp com IA integrada",
      "Delivery e retirada com taxa",
      "Impressao automatica na cozinha",
      "Cupons e descontos",
      "Relatorios de vendas basicos",
      "Suporte prioritario",
    ],
    spotlight: true,
    badge: "Mais indicado",
    maxUsers: 8,
  },
  {
    slug: "gestao",
    name: "ZeroPaper Gestao",
    monthlyPrice: 180,
    priceLabel: "R$ 180",
    audience: "Para controlar vendas e crescimento.",
    headline: "",
    features: [
      "Tudo do Operacao",
      "Dashboard com visao geral do negocio",
      "Relatorios avancados por periodo",
      "Produtos mais vendidos e horarios de pico",
      "Ticket medio e comparativos diarios",
      "Controle de clientes recorrentes",
      "Comissao por vendedor",
    ],
    premium: true,
    badge: "Mais completo",
    maxUsers: 15,
  },
];

export const defaultCommercialPlan = commercialPlans[1];

export function getCommercialPlan(slug?: string | null) {
  return commercialPlans.find((plan) => plan.slug === slug) ?? defaultCommercialPlan;
}
