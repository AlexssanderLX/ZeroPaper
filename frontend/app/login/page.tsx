import Link from "next/link";
import { BrandMark } from "@/components/brand-mark";
import { LoginAccessForm } from "@/components/login-access-form";

const loginMarks = ["Unidade", "Root", "Recuperacao"];
const loginEntries = ["Identificacao", "Sessao", "Painel"];

export default function LoginPage() {
  return (
    <main className="page-shell">
      <section className="top-link-row">
        <Link className="ghost-link" href="/">
          ZeroPaper
        </Link>
      </section>

      <section className="login-layout login-premium-layout">
        <section className="surface-card login-copy-card ambient-panel subtle login-intro-card">
          <div className="brand-lockup compact">
            <BrandMark small />
            <div className="brand-copy">
              <span className="eyebrow">ZeroPaper</span>
              <strong>Acesso</strong>
            </div>
          </div>

          <h1 className="login-title">Entrar na ZeroPaper.</h1>
          <p className="body-copy">Escolha o perfil e siga para o painel.</p>

          <div className="login-mark-row">
            {loginMarks.map((item) => (
              <span key={item} className="login-mark">
                {item}
              </span>
            ))}
          </div>

          <div className="login-stage" aria-hidden="true">
            <div className="login-stage-orb" />

            <article className="login-stage-window login-stage-window-main">
              <span className="eyebrow">Sessao</span>
              <strong>Entrada segura</strong>

              <div className="login-stage-rail">
                {loginEntries.map((entry) => (
                  <span key={entry}>{entry}</span>
                ))}
              </div>
            </article>

            <article className="login-stage-window login-stage-window-accent">
              <span className="eyebrow">Perfil</span>
              <strong>Unidade</strong>
              <p>Painel</p>
            </article>

            <article className="login-stage-window login-stage-window-chip">
              <span className="eyebrow">Perfil</span>
              <strong>Operacao</strong>
            </article>
          </div>
        </section>

        <section className="surface-card login-form-card login-form-shell">
          <span className="eyebrow">Login</span>
          <h2 className="form-title">Identificacao</h2>
          <LoginAccessForm />
        </section>
      </section>
    </main>
  );
}
