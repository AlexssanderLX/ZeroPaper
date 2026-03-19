import Link from "next/link";
import { BrandMark } from "@/components/brand-mark";

const pillars = [
  {
    eyebrow: "Mesas",
    title: "Pedidos pelo QR",
    text: "Cada mesa entra pronta para abrir no celular.",
  },
  {
    eyebrow: "Pedidos",
    title: "Fila organizada",
    text: "Os pedidos chegam em um fluxo direto para a unidade.",
  },
  {
    eyebrow: "Estoque",
    title: "Controle essencial",
    text: "O basico do dia a dia fica no mesmo painel.",
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

          <h1>Agora voce conduz a unidade em um fluxo mais direto.</h1>
          <p className="hero-description">Pedidos, mesas e operacao no mesmo painel.</p>

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
              <small>ZeroPaper</small>
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
        <h2>Entrada, pedidos e mesas no mesmo ritmo.</h2>
      </section>
    </main>
  );
}
