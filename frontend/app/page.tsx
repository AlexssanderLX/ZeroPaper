import Link from "next/link";
import { BrandMark } from "@/components/brand-mark";

const heroSignals = [
  "Pedido por QR nas mesas",
  "Cozinha organizada por etapa",
  "Caixa com visao clara do dia",
  "Impressao automatica opcional",
];

const moduleCards = [
  {
    eyebrow: "Implantacao",
    title: "Entrada guiada da unidade",
    text: "Voce organiza os pontos principais sem depender de um processo tecnico longo logo no primeiro acesso.",
  },
  {
    eyebrow: "Cardapio",
    title: "Cardapio vivo e facil de atualizar",
    text: "Preco, foto, categoria e disponibilidade ficam na sua mao para o salao responder mais rapido.",
  },
  {
    eyebrow: "Estoque",
    title: "Controle interno de reposicao",
    text: "A unidade acompanha insumos e organiza melhor o ritmo da casa sem depender de planilha solta.",
  },
  {
    eyebrow: "Mesas",
    title: "Mesas com QR pronto para uso",
    text: "Cada mesa ganha um QR proprio para pedido, observacao e chamado de atendente sem baixar aplicativo.",
  },
  {
    eyebrow: "Pedidos",
    title: "Cozinha com leitura rapida",
    text: "Os pedidos chegam em ordem clara para fazer e para prontos, com menos cliques e mais visibilidade.",
  },
  {
    eyebrow: "Caixa",
    title: "Caixa simples para fechar conta",
    text: "Pedidos a cobrar e pagos ficam separados por forma de pagamento, com total do dia facil de conferir.",
  },
  {
    eyebrow: "Impressao",
    title: "Impressao termica ou A4",
    text: "Voce escolhe o perfil da impressora e acompanha o status sem perder o controle da operacao.",
  },
  {
    eyebrow: "Atendimento",
    title: "Chamada de atendente no painel",
    text: "A equipe recebe alerta sonoro e visual direto no sistema, com som ajustavel por unidade ou por mesa.",
  },
];

const flowSteps = [
  {
    step: "01",
    title: "Cliente pede pela mesa",
    text: "O QR abre uma experiencia simples no celular para pedir, observar detalhes e chamar atendimento.",
  },
  {
    step: "02",
    title: "Sua equipe responde no painel",
    text: "Pedido, som, cozinha, caixa e impressao conversam entre si sem baguncar o fluxo da operacao.",
  },
  {
    step: "03",
    title: "Tudo fica centralizado",
    text: "Cardapio, estoque, mesas, alertas e configuracoes da unidade ficam no mesmo lugar.",
  },
];

const highlights = [
  {
    value: "8",
    label: "frentes conectadas",
  },
  {
    value: "1",
    label: "painel da operacao",
  },
  {
    value: "QR",
    label: "entrada de pedido",
  },
];

export default function Home() {
  return (
    <main className="page-shell landing-page">
      <section className="surface-card landing-hero-card">
        <div className="landing-hero-grid">
          <div className="landing-copy">
            <div className="landing-brand-lockup">
              <span className="eyebrow">ZeroPaper</span>
              <BrandMark variant="full" />
            </div>

            <span className="eyebrow landing-top-eyebrow">Menos papel, menos ruido, mais controle da operacao</span>
            <h1>Organize mesas, cozinha, caixa e atendimento em um unico sistema.</h1>
            <p>
              O ZeroPaper ajuda o restaurante a atender melhor no salao com QR nas mesas, cozinha organizada, caixa
              claro, alerta de atendimento e impressao pronta para acompanhar o ritmo da casa.
            </p>

            <div className="landing-actions">
              <Link className="primary-link" href="/cadastro">
                Quero usar na minha unidade
              </Link>
              <Link className="ghost-link" href="/login">
                Ver area do cliente
              </Link>
            </div>

            <div className="landing-signal-row">
              {heroSignals.map((signal) => (
                <span key={signal} className="landing-signal-pill">
                  {signal}
                </span>
              ))}
            </div>

            <div className="landing-highlight-row">
              {highlights.map((item) => (
                <article key={item.label} className="landing-highlight-card">
                  <strong>{item.value}</strong>
                  <span>{item.label}</span>
                </article>
              ))}
            </div>
          </div>

          <aside className="landing-preview-shell" aria-label="Resumo da operacao">
            <article className="landing-brand-panel">
              <BrandMark variant="full" />
              <div className="landing-brand-panel-copy">
                <span className="eyebrow">ZeroPaper</span>
                <strong>Feito para restaurantes que querem operar com mais clareza no salao.</strong>
              </div>
            </article>

            <article className="landing-preview-main">
              <span className="eyebrow">Fluxo principal</span>
              <h2>Do pedido da mesa ate a resposta da equipe, tudo segue no mesmo fluxo.</h2>
              <p>
                O cliente pede, a cozinha acompanha, o caixa enxerga melhor o fechamento e a equipe continua no
                controle da unidade.
              </p>
            </article>

            <div className="landing-preview-list">
              <article className="landing-preview-item">
                <span className="landing-preview-index">1</span>
                <div>
                  <span className="eyebrow">Mesa</span>
                  <strong>QR pronto para vender melhor no salao</strong>
                  <p>Pedido, observacao e chamado de atendente em uma experiencia simples para o cliente.</p>
                </div>
              </article>

              <article className="landing-preview-item">
                <span className="landing-preview-index">2</span>
                <div>
                  <span className="eyebrow">Operacao</span>
                  <strong>Cozinha e caixa falam a mesma lingua</strong>
                  <p>Menos confusao no dia a dia, com leitura compacta e etapas mais claras para a equipe.</p>
                </div>
              </article>

              <article className="landing-preview-item">
                <span className="landing-preview-index">3</span>
                <div>
                  <span className="eyebrow">Impressao</span>
                  <strong>Impressao pronta para acompanhar o ritmo</strong>
                  <p>O sistema pode acompanhar a impressora e avisar falha sem baguncar o resto do painel.</p>
                </div>
              </article>
            </div>
          </aside>
        </div>
      </section>

      <section className="surface-card landing-section-card">
        <div className="landing-section-head">
          <div>
            <span className="eyebrow">O que sua unidade recebe</span>
            <h2>Uma operacao mais limpa para atender, produzir, cobrar e acompanhar o dia.</h2>
          </div>
          <p>
            O ZeroPaper nao fica preso so ao cardapio. Ele conecta o que acontece na mesa, no preparo, no caixa e no
            acompanhamento da unidade.
          </p>
        </div>

        <div className="landing-module-grid">
          {moduleCards.map((module, index) => (
            <article key={module.title} className="surface-card landing-module-card">
              <div className="landing-module-head">
                <span className="eyebrow">{module.eyebrow}</span>
                <span className="landing-module-index">0{index + 1}</span>
              </div>
              <h3>{module.title}</h3>
              <p>{module.text}</p>
            </article>
          ))}
        </div>
      </section>

      <section className="surface-card landing-section-card landing-flow-card">
        <div className="landing-section-head">
          <div>
            <span className="eyebrow">Como funciona na pratica</span>
            <h2>Uma sequencia facil de entender para o cliente e simples de operar para a equipe.</h2>
          </div>
          <p>
            A ideia aqui e reduzir ruido operacional e deixar o restaurante com mais clareza no atendimento presencial.
          </p>
        </div>

        <div className="landing-flow-grid">
          {flowSteps.map((step) => (
            <article key={step.step} className="surface-card landing-flow-card-item">
              <span className="landing-flow-number">{step.step}</span>
              <h3>{step.title}</h3>
              <p>{step.text}</p>
            </article>
          ))}
        </div>
      </section>

      <section className="surface-card landing-cta-card">
        <div className="landing-section-head landing-section-head-compact">
          <div>
            <span className="eyebrow">ZeroPaper</span>
            <h2>Mais organizacao para a unidade. Mais clareza para a equipe. Mais controle para o dono.</h2>
          </div>
          <p>Uma plataforma pensada para restaurante presencial que quer operar melhor sem se perder em tela demais.</p>
        </div>

        <div className="landing-actions">
          <Link className="primary-link" href="/cadastro">
            Comecar minha implantacao
          </Link>
          <Link className="ghost-link" href="/login">
            Acessar sistema
          </Link>
        </div>
      </section>
    </main>
  );
}
