import Link from "next/link";
import { LoginAccessForm } from "@/components/login-access-form";

const accessCards = [
  {
    title: "Acesso da unidade",
    text: "Para dono, gerencia e equipe do restaurante.",
  },
  {
    title: "Acesso da operacao",
    text: "Para gestao interna da plataforma ZeroPaper.",
  },
];

export default function LoginPage() {
  return (
    <main className="page-shell">
      <section className="top-link-row">
        <Link className="ghost-link" href="/">
          ZeroPaper
        </Link>
      </section>

      <section className="login-layout">
        <section className="surface-card login-copy-card ambient-panel subtle">
          <div className="brand-lockup compact">
            <div className="brand-mark small" aria-hidden="true">
              <span>Z</span>
              <span>P</span>
            </div>
            <div className="brand-copy">
              <span className="eyebrow">ZeroPaper</span>
              <strong>Secure Access</strong>
            </div>
          </div>

          <h1 className="login-title">Entre na sua operacao</h1>
          <p className="body-copy">
            Acesse seu ambiente e siga direto para o controle da unidade.
          </p>

          <div className="access-list">
            {accessCards.map((item) => (
              <article key={item.title} className="access-card interactive-card">
                <h2>{item.title}</h2>
                <p>{item.text}</p>
              </article>
            ))}
          </div>
        </section>

        <section className="surface-card login-form-card">
          <span className="eyebrow">Login</span>
          <h2 className="form-title">Identificacao</h2>
          <LoginAccessForm />
        </section>
      </section>
    </main>
  );
}
