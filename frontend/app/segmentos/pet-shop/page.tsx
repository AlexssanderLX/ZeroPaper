import type { Metadata } from "next";
import Link from "next/link";
import { CalendarDays, CreditCard, PawPrint, Scissors, ShieldCheck, Users } from "lucide-react";
import { PublicSiteHeader } from "@/components/public-site-header";
import { LandingMotion } from "@/components/landing-motion";
import { ElectricBg } from "@/components/electric-bg";
import { getPublicCommercialPlans, type PublicCommercialPlan } from "@/lib/api";

export const dynamic = "force-dynamic";
export const metadata: Metadata = {
  title: "Sistema para Pet Shops | ZeroPaper",
  description: "Tutores, animais, catalogo de servicos, agenda, atendimento e cobrancas em um fluxo seguro para pet shops.",
  alternates: { canonical: "/segmentos/pet-shop" },
};

const features = [
  { icon: Users, title: "Tutores organizados", text: "Dados de contato e historico vinculados somente a unidade correta." },
  { icon: PawPrint, title: "Ficha de cada animal", text: "Porte, especie, alergias, restricoes, comportamento e foto em um so lugar." },
  { icon: Scissors, title: "Catalogo de servicos", text: "Banho, tosa e outros atendimentos com preco e duracao configurados." },
  { icon: CalendarDays, title: "Agenda sem conflito", text: "Disponibilidade, profissionais, bloqueios e historico de status em tempo real." },
  { icon: CreditCard, title: "Pedido e cobranca", text: "Transforme o atendimento em pedido e mantenha o financeiro conectado." },
  { icon: ShieldCheck, title: "Seguro por empresa", text: "Tenant e empresa vem da sessao; dados de outras unidades nunca sao confiados ao navegador." },
];

export default async function PetShopPage() {
  let plans: PublicCommercialPlan[] = [];
  try { plans = await getPublicCommercialPlans(2); } catch { /* CTA de tentativa permanece disponivel */ }
  const shortName = (name: string) => name.replace("ZeroPaper Pet ", "");

  return (
    <main className="zpld" id="petshop-page">
      <LandingMotion /><ElectricBg />
      <div className="zpld-bg" aria-hidden="true"><span className="zpld-orb zpld-orb-a" /><span className="zpld-orb zpld-orb-b" /><span className="zpld-orb zpld-orb-c" /><div className="zpld-grid" /></div>
      <PublicSiteHeader />

      <section className="zpld-section zpld-page-hero" aria-labelledby="pet-title">
        <div className="zpld-section-head">
          <Link href="/segmentos" className="zpld-breadcrumb">← Todos os segmentos</Link>
          <span>ZeroPaper para Pet Shops</span>
          <h1 id="pet-title" className="zpld-h1">Mais cuidado com os animais. Menos confusao na agenda.</h1>
          <p>Tutores, pets, servicos, profissionais, agendamentos e cobrancas no mesmo fluxo — com isolamento seguro por empresa.</p>
          <div className="zpld-ctas"><Link className="zpld-btn-primary" href="/cadastro?segmento=petshop&plano=operacao">Comecar como Pet Shop →</Link><a className="zpld-btn-ghost" href="#planos-pet">Ver planos</a></div>
        </div>
      </section>

      <section className="zpld-section" aria-labelledby="pet-features-title">
        <div className="zpld-section-head zp-lp-reveal"><span>Operacao completa</span><h2 id="pet-features-title">Feito para a rotina do seu pet shop.</h2></div>
        <div className="zpld-mod-grid">
          {features.map(({ icon: Icon, title, text }) => <article key={title} className="zpld-mod-card zp-lp-reveal"><span className="zpld-mod-icon"><Icon size={20} aria-hidden="true" /></span><div><strong>{title}</strong><p>{text}</p></div></article>)}
        </div>
      </section>

      <section className="zpld-section zp-lp-reveal" id="planos-pet" aria-labelledby="pet-plans-title">
        <div className="zpld-section-head"><span>Planos confirmados pelo servidor</span><h2 id="pet-plans-title">Escolha o nivel ideal para sua operacao.</h2><p>Mensalidade fixa por unidade. O valor exibido e validado novamente pelo backend no cadastro.</p></div>
        {plans.length ? <div className="zp-lp-plans-grid">
          {plans.map((plan) => <article key={plan.key} className={`zp-lp-plan-card${plan.recommended ? " is-spotlight" : ""}`}>
            {plan.recommended ? <em className="zp-lp-plan-badge">Mais indicado</em> : null}
            <span className="zp-lp-plan-audience">Pet shop • ate {plan.maxUsers} usuarios</span><h3>{shortName(plan.name)}</h3>
            <div className="zp-lp-plan-price"><strong>R$ {plan.monthlyPrice.toFixed(0)}</strong><small>/mes</small></div>
            <ul className="zp-lp-plan-features">{plan.features.map((feature) => <li key={feature}>{feature}</li>)}</ul>
            <Link className="zp-lp-plan-cta" href={`/cadastro?segmento=petshop&plano=${plan.key}`}>Escolher {shortName(plan.name)} →</Link>
          </article>)}
        </div> : <div className="zp-public-plans-error"><strong>Planos indisponiveis agora.</strong><p>Tente novamente em instantes para receber valores confirmados pelo servidor.</p></div>}
      </section>
    </main>
  );
}
