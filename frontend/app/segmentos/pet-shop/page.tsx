import type { Metadata } from "next";
import Link from "next/link";
import { PublicSiteHeader } from "@/components/public-site-header";
import { LandingMotion } from "@/components/landing-motion";
import { ElectricBg } from "@/components/electric-bg";
import { getPublicCommercialPlans, type PublicCommercialPlan } from "@/lib/api";

export const dynamic = "force-dynamic";

export const metadata: Metadata = {
  title: "Planos para Pet Shops | ZeroPaper",
  description: "Agenda, tutores, animais, servicos e atendimento para pet shops.",
  alternates: { canonical: "/segmentos/pet-shop" },
};

export default async function PetShopPage() {
  let plans: PublicCommercialPlan[] = [];
  try { plans = await getPublicCommercialPlans(2); } catch { /* cadastro exibe falha segura */ }

  return (
    <main className="zpld" id="pet-shop-page">
      <LandingMotion />
      <ElectricBg />
      <div className="zpld-bg" aria-hidden="true">
        <span className="zpld-orb zpld-orb-a" />
        <span className="zpld-orb zpld-orb-b" />
        <div className="zpld-grid" />
      </div>
      <PublicSiteHeader />

      <section className="zpld-section zpld-page-hero" aria-labelledby="pet-title">
        <div className="zpld-section-head" style={{ marginBottom: "2.5rem" }}>
          <Link href="/segmentos" className="zpld-breadcrumb">Voltar aos segmentos</Link>
          <span>Pet shops</span>
          <h1 id="pet-title" className="zpld-h1" style={{ fontSize: "clamp(1.9rem,3vw,3.2rem)", textAlign: "center" }}>
            Agenda, tutores, animais e servicos em um unico fluxo.
          </h1>
          <p>O MVP do ZeroPaper Pet para organizar o atendimento da sua unidade.</p>
          <div className="zpld-ctas" style={{ justifyContent: "center", marginTop: "0.5rem" }}>
            <Link className="zpld-btn-primary" href="/cadastro?segmento=petshop&plano=operacao">Criar conta agora</Link>
            <a className="zpld-btn-ghost" href="#planos-pet">Ver planos</a>
          </div>
        </div>
      </section>

      <section className="zpld-section zp-lp-reveal" id="planos-pet" aria-labelledby="pet-plans-title">
        <div className="zpld-section-head">
          <span>Planos</span>
          <h2 id="pet-plans-title">Escolha o nivel ideal para o seu pet shop.</h2>
          <p>Mensalidade fixa por unidade, com os recursos confirmados pelo servidor.</p>
        </div>
        <div className="zp-lp-plans-grid">
          {plans.map((plan) => (
            <article key={plan.key} className={`zp-lp-plan-card zp-lp-reveal${plan.recommended ? " is-spotlight" : ""}`}>
              {plan.recommended ? <em className="zp-lp-plan-badge">Mais indicado</em> : null}
              <h3>{plan.name.replace("ZeroPaper Pet ", "")}</h3>
              <div className="zp-lp-plan-price"><strong>R$ {plan.monthlyPrice.toFixed(0)}</strong><small>/mes</small></div>
              <ul className="zp-lp-plan-features">{plan.features.map((feature) => <li key={feature}>{feature}</li>)}</ul>
              <Link className="zp-lp-plan-cta" href={`/cadastro?segmento=petshop&plano=${plan.key}`}>Escolher {plan.name.replace("ZeroPaper Pet ", "")}</Link>
            </article>
          ))}
        </div>
      </section>
    </main>
  );
}
