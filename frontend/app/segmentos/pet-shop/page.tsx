import type { Metadata } from "next";
import Link from "next/link";
import { PublicSiteHeader } from "@/components/public-site-header";
import { LandingMotion } from "@/components/landing-motion";
import { ElectricBg } from "@/components/electric-bg";

export const metadata: Metadata = {
  title: "Planos para Pet Shops | ZeroPaper",
  description: "Agenda, tutores, animais, servicos e atendimento para pet shops.",
  alternates: { canonical: "/segmentos/pet-shop" },
};

export default function PetShopPage() {
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
          <span>Pet shops · Beta</span>
          <div className="zp-pet-beta-banner" role="note">
            <strong>Programa beta com acesso controlado</strong>
            <p>O modulo Pet Shop ainda esta em testes e nao possui cobranca. Valores especiais de teste podem ser consultados diretamente com a ZeroPaper.</p>
          </div>
          <h1 id="pet-title" className="zpld-h1" style={{ fontSize: "clamp(1.9rem,3vw,3.2rem)", textAlign: "center" }}>
            Agenda, tutores, animais e servicos em um unico fluxo.
          </h1>
          <p>Teste agenda, tutores, animais e servicos com acompanhamento direto da ZeroPaper.</p>
          <div className="zpld-ctas" style={{ justifyContent: "center", marginTop: "0.5rem" }}>
            <Link className="zpld-btn-primary" href="/cadastro?segmento=petshop&plano=pet-shop">Solicitar acesso ao beta</Link>
            <Link className="zpld-btn-ghost" href="/contato">Falar sobre valores de teste</Link>
          </div>
        </div>
      </section>

      <section className="zpld-section zp-lp-reveal" id="recursos-pet" aria-labelledby="pet-plans-title">
        <div className="zpld-section-head">
          <span>Beta Pet Shop</span>
          <h2 id="pet-plans-title">O que esta disponivel para teste.</h2>
          <p>Sem checkout ou mensalidade durante esta fase. A liberacao e feita manualmente.</p>
        </div>
        <div className="zp-lp-plans-grid">
          <article className="zp-lp-plan-card zp-lp-reveal is-spotlight">
            <em className="zp-lp-plan-badge">Beta</em>
            <h3>ZeroPaper Pet Shop</h3>
            <ul className="zp-lp-plan-features">
              {["Cadastro de tutores e animais", "Catalogo de servicos", "Agenda interna", "Agendamento publico"].map((feature) => <li key={feature}>{feature}</li>)}
            </ul>
            <Link className="zp-lp-plan-cta" href="/cadastro?segmento=petshop&plano=pet-shop">Solicitar acesso</Link>
          </article>
        </div>
      </section>
    </main>
  );
}
