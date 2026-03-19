import Link from "next/link";
import { RestaurantSignupForm } from "@/components/restaurant-signup-form";

export default function SignupPage() {
  return (
    <main className="page-shell">
      <section className="top-link-row">
        <Link className="ghost-link" href="/">
          Voltar para a home
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
              <strong>Novo cadastro</strong>
            </div>
          </div>

          <h1 className="login-title">Abra sua unidade na plataforma</h1>
          <p className="body-copy">
            Cadastre os dados principais do restaurante e receba o acesso inicial para entrar, criar mesas e organizar a operacao.
          </p>

          <div className="access-list">
            <article className="access-card interactive-card">
              <h2>Cadastro liberado</h2>
              <p>Use o codigo de acesso enviado pela ZeroPaper para concluir o cadastro inicial.</p>
            </article>
            <article className="access-card interactive-card">
              <h2>Solicitacao rapida</h2>
              <p>Se ainda nao tiver codigo, a propria tela permite pedir liberacao comercial por email.</p>
            </article>
          </div>
        </section>

        <section className="surface-card login-form-card">
          <span className="eyebrow">Cadastro</span>
          <h2 className="form-title">Criar acesso inicial</h2>
          <RestaurantSignupForm />
        </section>
      </section>
    </main>
  );
}
