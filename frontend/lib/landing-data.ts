export type BusinessSegment = {
  key: string;
  name: string;
  description: string;
  status: "available" | "configurable" | "custom" | "consult";
  statusLabel: string;
  icon: string;
  href: string;
  ctaLabel: string;
  modules: string[];
};

export type PlatformModule = {
  key: string;
  eyebrow: string;
  title: string;
  text: string;
  icon: string;
};

export type HowItWorksStep = {
  step: number;
  title: string;
  text: string;
};

export type Benefit = {
  key: string;
  title: string;
  text: string;
};

export const businessSegments: BusinessSegment[] = [
  {
    key: "restaurant",
    name: "Restaurantes e delivery",
    description: "Cardapio, QR por mesa, pedidos, cozinha, delivery, caixa e impressao.",
    status: "available",
    statusLabel: "Disponivel agora",
    icon: "🍽️",
    href: "/segmentos/restaurantes",
    ctaLabel: "Ver planos",
    modules: ["Cardapio", "QR Code", "Cozinha", "Delivery", "Caixa", "Impressao"],
  },
  {
    key: "retail",
    name: "Acai, conveniencia e varejo local",
    description: "Catalogo, pedidos, retirada, delivery, caixa, cupons e clientes recorrentes.",
    status: "configurable",
    statusLabel: "Configuravel",
    icon: "🛒",
    href: "/segmentos/varejo",
    ctaLabel: "Ver possibilidade",
    modules: ["Catalogo", "Pedidos", "Caixa", "Cupons", "Clientes"],
  },
  {
    key: "petshop",
    name: "Pet shops",
    description: "Servicos, clientes, atendimento, WhatsApp e agenda por modulo.",
    status: "custom",
    statusLabel: "Implantacao personalizada",
    icon: "🐾",
    href: "/segmentos/pet-shop",
    ctaLabel: "Conversar sobre meu caso",
    modules: ["Servicos", "Clientes", "WhatsApp", "Agenda"],
  },
  {
    key: "technical",
    name: "Assistencia tecnica",
    description: "Clientes, servicos, status de atendimento, cobranças e ordem de servico.",
    status: "custom",
    statusLabel: "Implantacao personalizada",
    icon: "🔧",
    href: "/segmentos/assistencia-tecnica",
    ctaLabel: "Conversar sobre meu caso",
    modules: ["Clientes", "Servicos", "OS", "Cobrança"],
  },
  {
    key: "auto",
    name: "Oficinas e servicos",
    description: "Atendimentos, servicos, clientes, cobranças e acompanhamento operacional.",
    status: "configurable",
    statusLabel: "Sob configuracao",
    icon: "🚗",
    href: "/segmentos/oficinas",
    ctaLabel: "Ver possibilidade",
    modules: ["Atendimentos", "Clientes", "Cobrança", "Operacao"],
  },
  {
    key: "custom",
    name: "Plano personalizado",
    description: "Monte uma configuracao por modulos para o fluxo real do seu negocio.",
    status: "consult",
    statusLabel: "Sob consulta",
    icon: "⚙️",
    href: "/segmentos/personalizado",
    ctaLabel: "Montar meu plano",
    modules: [],
  },
];

export const platformModules: PlatformModule[] = [
  {
    key: "catalog",
    eyebrow: "Catalogo",
    title: "Catalogo digital",
    text: "Produtos, servicos ou cardapio com fotos, precos e adicionais. O cliente acessa pelo celular, sem precisar de app.",
    icon: "📋",
  },
  {
    key: "orders",
    eyebrow: "Pedidos",
    title: "Pedidos por link ou QR Code",
    text: "O cliente pede pelo proprio celular. O pedido cai direto no painel sem intermediario.",
    icon: "📲",
  },
  {
    key: "whatsapp",
    eyebrow: "Atendimento",
    title: "Atendimento pelo WhatsApp",
    text: "A IA conversa, orienta e direciona o cliente. Voce define quando e como ativar.",
    icon: "💬",
  },
  {
    key: "cash",
    eyebrow: "Financeiro",
    title: "Caixa e cobranças",
    text: "Cobranca, pagamento e fechamento do dia com os totais certos. Sem planilha.",
    icon: "💰",
  },
  {
    key: "print",
    eyebrow: "Operacao",
    title: "Impressao operacional",
    text: "Pedido confirmado e imprime automaticamente na area de producao. Menos clique, menos erro.",
    icon: "🖨️",
  },
  {
    key: "customers",
    eyebrow: "Clientes",
    title: "Clientes e historico",
    text: "Perfil, historico de pedidos, recorrencia e dados de atendimento por cliente.",
    icon: "👤",
  },
  {
    key: "coupons",
    eyebrow: "Marketing",
    title: "Cupons e campanhas",
    text: "Desconto por percentual ou valor fixo. Vinculado a produto, categoria ou pedido minimo.",
    icon: "🎟️",
  },
  {
    key: "reports",
    eyebrow: "Gestao",
    title: "Relatorios e gestao",
    text: "Dashboard com vendas, horarios de pico, ticket medio, comparativos e produtos mais pedidos.",
    icon: "📊",
  },
];

export const howItWorksSteps: HowItWorksStep[] = [
  {
    step: 1,
    title: "Escolha o tipo de negocio",
    text: "Restaurante, varejo, pet shop, assistencia ou outro segmento. O fluxo e configurado para a sua realidade.",
  },
  {
    step: 2,
    title: "Ative os modulos necessarios",
    text: "Voce nao precisa usar tudo de uma vez. Comece com o essencial e adicione modulos conforme crescer.",
  },
  {
    step: 3,
    title: "Receba pedidos e atendimentos",
    text: "Link, QR Code ou WhatsApp. O cliente pede e o painel do negocio recebe em tempo real.",
  },
  {
    step: 4,
    title: "Acompanhe caixa e operacao",
    text: "Fechamento de caixa, relatorios, clientes e gestao em um so painel. Sem papel, sem bagunca.",
  },
];

export const benefits: Benefit[] = [
  {
    key: "no-paper",
    title: "Menos papel e menos perda",
    text: "Pedidos digitais chegam direto no painel, sem comanda manual e sem risco de perder pedido.",
  },
  {
    key: "control",
    title: "Mais controle da operacao",
    text: "Voce ve em tempo real o que esta acontecendo: pedidos, producao e caixa no mesmo lugar.",
  },
  {
    key: "speed",
    title: "Atendimento mais rapido",
    text: "Menos espera para o cliente e menos retrabalho para a equipe. O fluxo roda mais fluido.",
  },
  {
    key: "history",
    title: "Historico de clientes",
    text: "Saiba quem compra, o que pede e com que frequencia. Dados para tomar decisoes melhores.",
  },
  {
    key: "billing",
    title: "Cobranca mais organizada",
    text: "Caixa fechado com contexto do pedido. Sem calcular na mao nem perder lancamento.",
  },
  {
    key: "growth",
    title: "Crescimento por modulos",
    text: "Começa simples. Quando o negocio cresce, adiciona WhatsApp IA, relatórios, cupons e mais.",
  },
];
