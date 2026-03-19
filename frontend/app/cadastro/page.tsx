import Link from "next/link";
import { BrandMark } from "@/components/brand-mark";
import { RestaurantSignupForm } from "@/components/restaurant-signup-form";

const signupMarks = ["Codigo", "Cadastro", "Entrada"];
const signupSteps = ["Unidade", "Liberacao", "Painel"];

export default function SignupPage() {
  return (
    <main className="page-shell">
      <section className="top-link-row">
        <Link className="ghost-link" href="/">
          Voltar para a home
        </Link>
      </section>

      <section className="login-layout signup-layout">
        <section className="surface-card login-copy-card ambient-panel subtle signup-intro-card">
          <div className="brand-lockup compact">
            <BrandMark small />
            <div className="brand-copy">
              <span className="eyebrow">ZeroPaper</span>
              <strong>Cadastro</strong>
            </div>
          </div>

          <h1 className="login-title">Criar acesso inicial.</h1>
          <p className="body-copy">Cadastro da unidade com entrada imediata.</p>

          <div className="signup-mark-row">
            {signupMarks.map((item) => (
              <span key={item} className="signup-mark">
                {item}
              </span>
            ))}
          </div>

          <div className="signup-stage" aria-hidden="true">
            <div className="signup-stage-orb" />

            <article className="signup-stage-window signup-stage-window-main">
              <span className="eyebrow">Cadastro</span>
              <strong>Entrada inicial</strong>

              <div className="signup-stage-rail">
                {signupSteps.map((step) => (
                  <span key={step}>{step}</span>
                ))}
              </div>

              <div className="signup-stage-meter">
                <span />
                <span />
                <span />
              </div>
            </article>

            <article className="signup-stage-window signup-stage-window-accent">
              <span className="eyebrow">Codigo</span>
              <strong>Liberado</strong>
              <p>5 min</p>
            </article>

            <article className="signup-stage-window signup-stage-window-chip">
              <span className="eyebrow">Painel</span>
              <strong>Unidade</strong>
            </article>
          </div>
        </section>

        <section className="surface-card login-form-card signup-form-shell">
          <span className="eyebrow">Cadastro</span>
          <h2 className="form-title">Criar acesso inicial</h2>
          <RestaurantSignupForm />
        </section>
      </section>
    </main>
  );
}
