import Link from "next/link";
import { redirect } from "next/navigation";
import { BrandMark } from "@/components/brand-mark";
import { RestaurantSignupForm } from "@/components/restaurant-signup-form";
import { ElectricBg } from "@/components/electric-bg";
import { getPublicCommercialPlans } from "@/lib/api";

export const dynamic = "force-dynamic";

export default async function SignupPage({ searchParams }: { searchParams: Promise<{ plano?: string; segmento?: string }> }) {
  const params = await searchParams;
  if (params.segmento && params.segmento !== "restaurante" && params.segmento !== "petshop") {
    redirect("/segmentos");
  }

  const segment: 1 | 2 = params.segmento === "petshop" ? 2 : 1;
  const segmentKey = segment === 2 ? "petshop" : "restaurante";
  const segmentLabel = segment === 2 ? "Pet shop" : "Restaurante";

  let plans;
  try {
    plans = await getPublicCommercialPlans(segment);
  } catch {
    return (
      <main className="page-shell zp-signup-sales-page">
        <ElectricBg />
        <section className="surface-card zp-signup-unavailable">
          <span className="eyebrow">Cadastro seguro</span>
          <h1>Planos temporariamente indisponiveis</h1>
          <p>Nao foi possivel confirmar os valores no servidor. Tente novamente em instantes.</p>
          <Link className="ghost-link" href={`/segmentos/${segment === 2 ? "pet-shop" : "restaurantes"}`}>Voltar aos planos</Link>
        </section>
      </main>
    );
  }

  const selectedPlan = plans.find((plan) => plan.key === params.plano) ?? plans.find((plan) => plan.recommended) ?? plans[0];
  if (!selectedPlan) return null;
  const shortName = (name: string) => name.replace("ZeroPaper Pet ", "").replace("ZeroPaper ", "");

  return (
    <main className="page-shell zp-signup-sales-page">
      <ElectricBg />
      <section className="top-link-row">
        <Link className="ghost-link" href={`/segmentos/${segment === 2 ? "pet-shop" : "restaurantes"}`}>Voltar para {segmentLabel.toLowerCase()}</Link>
      </section>

      <section className="zp-signup-segment-context" aria-label="Tipo de negocio selecionado">
        <span>Tipo de negocio</span><strong>{segmentLabel}</strong>
        <Link href="/segmentos">Trocar segmento</Link>
      </section>

      <section className="zp-signup-sales-layout">
        <section className="surface-card zp-signup-sales-intro">
          <div className="brand-lockup compact"><BrandMark small variant="full" /><div className="brand-copy"><span className="eyebrow">ZeroPaper</span><strong>Cadastro {segmentLabel}</strong></div></div>
          <nav className="zp-signup-plan-switch" aria-label="Trocar plano">
            {plans.map((plan) => (
              <Link key={plan.key} className={plan.key === selectedPlan.key ? "is-active" : ""} href={`/cadastro?segmento=${segmentKey}&plano=${plan.key}`}>
                {plan.recommended ? <span className="zp-plan-badge">Mais indicado</span> : null}
                <strong>{shortName(plan.name)}</strong>
                <span>R$ {plan.monthlyPrice.toFixed(0)}<small>/mes</small></span>
              </Link>
            ))}
          </nav>
          <article className="zp-signup-selected-plan">
            <span>O que esta incluido</span><strong>{selectedPlan.name}</strong>
            <b>R$ {selectedPlan.monthlyPrice.toFixed(0)}<small>/mes</small></b>
            <ul>{selectedPlan.features.map((feature) => <li key={feature}>{feature}</li>)}</ul>
          </article>
        </section>

        <section className="surface-card login-form-card zp-signup-sales-form">
          <div className="zp-signup-form-plan-row"><div><span className="eyebrow">Plano selecionado</span><strong>{shortName(selectedPlan.name)}</strong></div><b className="zp-signup-form-price">R$ {selectedPlan.monthlyPrice.toFixed(0)}<small>/mes</small></b></div>
          <h1 className="form-title">Criar sua conta</h1>
          <RestaurantSignupForm selectedPlan={selectedPlan} segment={segment} />
        </section>
      </section>
    </main>
  );
}
