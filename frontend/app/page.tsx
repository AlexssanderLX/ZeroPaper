import Link from "next/link";

const currentClientCapabilities = [
  {
    title: "Entrar no sistema da empresa",
    text: "Cada restaurante acessa o proprio ambiente dentro da estrutura da ZeroPaper, sem precisar manter um dominio separado.",
  },
  {
    title: "Receber pedidos por QR Code",
    text: "O cliente final pode iniciar a jornada por QR Code e o restaurante centraliza esse fluxo dentro da operacao.",
  },
  {
    title: "Organizar o fluxo para cozinha",
    text: "Os pedidos podem seguir uma linha mais clara para preparo, acompanhamento e saida dentro de cada local.",
  },
  {
    title: "Acompanhar operacao e estoque",
    text: "A base do sistema ja foi pensada para ajudar o restaurante a reduzir tarefa manual e ganhar mais controle do dia a dia.",
  },
];

const workflow = [
  "Cliente acessa o restaurante pelo QR Code",
  "O pedido entra no fluxo operacional da empresa",
  "A equipe acompanha preparo e saida para a cozinha",
  "A operacao ganha apoio no controle e reposicao de estoque",
];

const modules = ["Pedidos", "Cozinha", "QR Code", "Estoque", "Acesso"];

export default function Home() {
  return (
    <main className="page-shell">
      <section className="hero-block">
        <div className="hero-content">
          <span className="eyebrow">Plataforma ZeroPaper</span>
          <h1>ZeroPaper</h1>
          <p className="lead lead-strong">
            Sistema para restaurantes com pedidos por QR Code, organizacao de cozinha
            e apoio ao controle de estoque.
          </p>
          <p className="lead">
            O foco da plataforma e reduzir tarefas manuais, centralizar a operacao
            de cada empresa e manter tudo dentro do seu proprio ecossistema.
          </p>

          <div className="hero-actions">
            <Link className="primary-link" href="/login">
              Entrar no sistema
            </Link>
          </div>
        </div>
      </section>

      <section className="section-grid">
        <section className="surface-card">
          <div className="section-title">
            <span className="eyebrow">Cliente hoje</span>
            <h2>O que o restaurante ja pode fazer</h2>
          </div>

          <div className="feature-grid">
            {currentClientCapabilities.map((feature) => (
              <article key={feature.title} className="feature-card">
                <h3>{feature.title}</h3>
                <p>{feature.text}</p>
              </article>
            ))}
          </div>
        </section>

        <section className="surface-card">
          <div className="section-title">
            <span className="eyebrow">Como funciona</span>
            <h2>Fluxo inicial pensado para uso real</h2>
          </div>

          <div className="workflow-list">
            {workflow.map((item, index) => (
              <div key={item} className="workflow-item">
                <span>{String(index + 1).padStart(2, "0")}</span>
                <p>{item}</p>
              </div>
            ))}
          </div>
        </section>
      </section>

      <section className="section-grid bottom">
        <section className="surface-card">
          <div className="section-title">
            <span className="eyebrow">ZeroPaper</span>
            <h2>Um unico ambiente para operar cada restaurante</h2>
          </div>

          <p className="body-copy">
            Cada cliente entra no proprio ambiente dentro da plataforma e usa a
            operacao web que voce entrega.
          </p>
        </section>

        <section className="surface-card">
          <div className="section-title">
            <span className="eyebrow">Operacao</span>
            <h2>Frentes centrais da plataforma</h2>
          </div>

          <div className="tag-grid">
            {modules.map((module) => (
              <span key={module} className="tag-item">
                {module}
              </span>
            ))}
          </div>
        </section>
      </section>
    </main>
  );
}
