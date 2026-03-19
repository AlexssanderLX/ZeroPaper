import Link from "next/link";
import { BrandMark } from "@/components/brand-mark";

const pillars = [
  {
    eyebrow: "QR nas mesas",
    title: "Agora voce recebe pedidos direto da mesa.",
    text: "O cliente acessa o QR, envia o pedido e sua unidade acompanha tudo no mesmo fluxo.",
  },
  {
    eyebrow: "Cozinha em ritmo",
    title: "Agora voce acompanha o que entrou e o que precisa sair.",
    text: "A fila da cozinha fica mais clara para sua operacao responder com mais ritmo.",
  },
  {
    eyebrow: "Estoque essencial",
    title: "Agora voce enxerga os itens criticos antes da falta.",
    text: "O basico da reposicao fica visivel no mesmo sistema, sem controle solto por fora.",
  },
];

const productMarks = ["QR por mesa", "Pedidos da unidade", "Estoque basico", "Painel do dono"];

export default function Home() {
  return (
    <main className="page-shell">
      <section className="hero-panel sales-hero">
        <div className="hero-stack sales-copy">
          <div className="brand-lockup">
            <BrandMark />
            <div className="brand-copy">
              <span className="eyebrow">ZeroPaper</span>
              <strong>Restaurant Operating System</strong>
            </div>
          </div>

          <h1>Agora voce acompanha pedidos, cozinha e estoque no mesmo fluxo.</h1>
          <p className="hero-description">
            Com a ZeroPaper, voce centraliza o que entra pelas mesas, acompanha
            o andamento da cozinha e mantem a operacao da unidade mais organizada
            ao longo do dia.
          </p>

          <div className="hero-actions">
            <Link className="primary-link" href="/cadastro">
              Cadastrar minha unidade
            </Link>
            <Link className="ghost-link" href="/login">
              Acessar unidade
            </Link>
          </div>

          <div className="sales-chip-row">
            {productMarks.map((mark) => (
              <span key={mark} className="sales-chip">
                {mark}
              </span>
            ))}
          </div>
        </div>

        <div className="sales-stage" aria-hidden="true">
          <div className="sales-glow" />

          <article className="sales-window sales-window-main">
            <div className="sales-window-bar">
              <span />
              <span />
              <span />
            </div>

            <div className="sales-window-head">
              <strong>Fluxo da unidade</strong>
              <small>ao vivo</small>
            </div>

            <div className="sales-order-stack">
              <div className="sales-order-card">
                <div>
                  <span className="sales-order-label">Mesa 08</span>
                  <strong>2 pedidos novos</strong>
                </div>
                <em>QR ativo</em>
              </div>

              <div className="sales-order-card soft">
                <div>
                  <span className="sales-order-label">Cozinha</span>
                  <strong>Fila organizada</strong>
                </div>
                <em>3 em preparo</em>
              </div>

              <div className="sales-order-card muted">
                <div>
                  <span className="sales-order-label">Estoque</span>
                  <strong>Reposicao sinalizada</strong>
                </div>
                <em>2 alertas</em>
              </div>
            </div>
          </article>

          <article className="sales-window sales-window-float">
            <span className="eyebrow">QR pronto</span>
            <strong>Mesa 12</strong>
            <p>/q/mesa-12</p>
          </article>

          <article className="sales-window sales-window-side">
            <span className="eyebrow">Entrada</span>
            <strong>Pedido recebido</strong>
            <div className="sales-line-group">
              <span />
              <span />
              <span />
            </div>
          </article>
        </div>
      </section>

      <section className="sales-grid">
        {pillars.map((item) => (
          <article key={item.title} className="surface-card sales-card interactive-card">
            <span className="eyebrow">{item.eyebrow}</span>
            <h2>{item.title}</h2>
            <p>{item.text}</p>
          </article>
        ))}
      </section>

      <section className="surface-card sales-banner ambient-panel subtle">
        <span className="eyebrow">ZeroPaper</span>
        <h2>Agora voce conduz a unidade com mais controle no dia a dia.</h2>
      </section>
    </main>
  );
}
