import Link from "next/link";
import { BrandMark } from "@/components/brand-mark";
import { RestaurantSignupForm } from "@/components/restaurant-signup-form";
import { commercialPlans, getCommercialPlan } from "@/lib/commercial-plans";
import { ElectricBg } from "@/components/electric-bg";

export default async function SignupPage({
  searchParams,
}: {
  searchParams: Promise<{ plano?: string }>;
}) {
  const { plano } = await searchParams;
  const selectedPlan = getCommercialPlan(plano);

  return (
    <main className="page-shell zp-signup-sales-page">
      <ElectricBg />
      <section className="top-link-row">
        <Link className="ghost-link" href="/">
          Voltar para a home
        </Link>
      </section>

      <section className="zp-signup-sales-layout">
        {/* ── Painel esquerdo: plano ─────────────────────────────── */}
        <section className="surface-card zp-signup-sales-intro">
          <div className="brand-lockup compact">
            <BrandMark small variant="full" />
            <div className="brand-copy">
              <span className="eyebrow">ZeroPaper</span>
              <strong>Cadastro</strong>
            </div>
          </div>

          <nav className="zp-signup-plan-switch" aria-label="Trocar plano">
            {commercialPlans.map((plan) => (
              <Link
                key={plan.slug}
                className={plan.slug === selectedPlan.slug ? "is-active" : ""}
                href={`/cadastro?plano=${plan.slug}`}
              >
                {plan.badge ? <span className="zp-plan-badge">{plan.badge}</span> : null}
                <strong>{plan.name.replace("ZeroPaper ", "")}</strong>
                <span>{plan.priceLabel}<small>/mes</small></span>
              </Link>
            ))}
          </nav>

          <article className="zp-signup-selected-plan">
            <span>O que esta incluido</span>
            <strong>{selectedPlan.name}</strong>
            <b>
              {selectedPlan.priceLabel}
              <small>/mes</small>
            </b>
            <ul>
              {selectedPlan.features.map((feature) => (
                <li key={feature}>{feature}</li>
              ))}
            </ul>
          </article>
        </section>

        {/* ── Painel direito: form ───────────────────────────────── */}
        <section className="surface-card login-form-card zp-signup-sales-form">
          <div className="zp-signup-form-plan-row">
            <div>
              <span className="eyebrow">Plano selecionado</span>
              <strong>{selectedPlan.name.replace("ZeroPaper ", "")}</strong>
            </div>
            <b className="zp-signup-form-price">
              {selectedPlan.priceLabel}
              <small>/mes</small>
            </b>
          </div>

          <h2 className="form-title">Criar sua conta</h2>
          <RestaurantSignupForm selectedPlan={selectedPlan} />
        </section>
      </section>
    </main>
  );
}
