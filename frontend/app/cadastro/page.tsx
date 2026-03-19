import Link from "next/link";
import { BrandMark } from "@/components/brand-mark";
import { RestaurantSignupForm } from "@/components/restaurant-signup-form";

const signupMarks = ["Codigo validado", "Entrada imediata", "Painel liberado"];
const signupSteps = ["Dados da unidade", "Codigo de liberacao", "Entrada no painel"];

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
              <strong>Nova unidade</strong>
            </div>
          </div>

          <h1 className="login-title">Sua unidade entra pronta para operar.</h1>
          <p className="body-copy">
            Preencha os dados principais, valide o codigo de liberacao e entre
            direto no painel da sua unidade.
          </p>

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
              <span className="eyebrow">Entrada inicial</span>
              <strong>Cadastro liberado</strong>

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
              <span className="eyebrow">Acesso</span>
              <strong>Codigo ativo</strong>
              <p>Validacao imediata</p>
            </article>

            <article className="signup-stage-window signup-stage-window-chip">
              <span className="eyebrow">Painel</span>
              <strong>Unidade pronta</strong>
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
