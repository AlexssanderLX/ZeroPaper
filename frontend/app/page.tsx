import Link from "next/link";

const highlights = [
  {
    title: "Pedidos por QR",
    text: "Receba pedidos com mais fluidez direto na operacao da unidade.",
  },
  {
    title: "Cozinha alinhada",
    text: "Organize preparo, prioridade e saida com mais clareza para a equipe.",
  },
  {
    title: "Estoque sob controle",
    text: "Acompanhe itens criticos e reduza falhas no ritmo do dia a dia.",
  },
];

const valuePoints = [
  "Atendimento mais agil no salao",
  "Menos tarefa manual para a equipe",
  "Mais visibilidade para a cozinha",
  "Mais controle sobre a operacao",
];

const modules = ["Salao", "Cozinha", "Pedidos", "QR Code", "Estoque"];

export default function Home() {
  return (
    <main className="page-shell">
      <section className="hero-panel">
        <div className="hero-stack">
          <div className="brand-lockup">
            <div className="brand-mark" aria-hidden="true">
              <span>Z</span>
              <span>P</span>
            </div>
            <div className="brand-copy">
              <span className="eyebrow">ZeroPaper</span>
              <strong>Restaurant Operating System</strong>
            </div>
          </div>

          <h1>Operacao, pedidos e estoque no mesmo fluxo.</h1>
          <p className="hero-description">
            Uma plataforma pensada para restaurantes que querem atender melhor,
            organizar a cozinha e manter a operacao mais leve ao longo do dia.
          </p>

          <div className="hero-actions">
            <Link className="primary-link" href="/login">
              Acessar plataforma
            </Link>
          </div>
        </div>

        <div className="hero-showcase ambient-panel">
          <div className="showcase-header">
            <span className="eyebrow">Destaques</span>
            <strong>Recursos centrais da experiencia</strong>
          </div>

          <div className="highlight-grid">
            {highlights.map((item) => (
              <article key={item.title} className="info-card interactive-card">
                <h2>{item.title}</h2>
                <p>{item.text}</p>
              </article>
            ))}
          </div>

          <div className="showcase-footer">
            <span />
            <p>Uma plataforma unica para atendimento, cozinha e operacao.</p>
          </div>
        </div>
      </section>

      <section className="content-grid">
        <section className="surface-card emphasis-card ambient-panel subtle interactive-card">
          <span className="eyebrow">Para a rotina da casa</span>
          <h2>Mais ritmo no atendimento. Mais previsibilidade na operacao.</h2>
          <p className="body-copy">
            ZeroPaper conecta entrada de pedidos, fluxo de cozinha e acompanhamento
            operacional em uma experiencia mais simples para a equipe.
          </p>
        </section>

        <section className="surface-card">
          <span className="eyebrow">Na pratica</span>
          <div className="point-list">
            {valuePoints.map((item, index) => (
              <div key={item} className="point-item interactive-card">
                <span>{String(index + 1).padStart(2, "0")}</span>
                <p>{item}</p>
              </div>
            ))}
          </div>
        </section>
      </section>

      <section className="surface-card modules-card ambient-panel subtle">
        <div className="modules-header">
          <span className="eyebrow">ZeroPaper</span>
          <h2>Uma experiencia unica para cada unidade.</h2>
        </div>

        <div className="tag-grid">
          {modules.map((module) => (
            <span key={module} className="tag-item interactive-card">
              {module}
            </span>
          ))}
        </div>
      </section>
    </main>
  );
}
