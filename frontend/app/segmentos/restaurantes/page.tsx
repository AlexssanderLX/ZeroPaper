import type { Metadata } from "next";
import type React from "react";
import Link from "next/link";
import { PublicSiteHeader } from "@/components/public-site-header";
import { LandingMotion } from "@/components/landing-motion";
import { ElectricBg } from "@/components/electric-bg";
import { commercialPlans } from "@/lib/commercial-plans";

export const metadata: Metadata = {
  title: "Planos para Restaurantes | ZeroPaper",
  description:
    "Cardapio digital, QR por mesa, cozinha, delivery, caixa, WhatsApp IA e relatorios. Planos Essencial, Operacao e Gestao para restaurantes e delivery.",
  alternates: { canonical: "/segmentos/restaurantes" },
};

const restaurantFeatures: { icon: React.ReactNode; title: string; text: string }[] = [
  {
    icon: (
      <svg width="20" height="20" viewBox="0 0 20 20" fill="none" stroke="currentColor" strokeWidth="1.6" strokeLinecap="round" strokeLinejoin="round">
        <rect x="2" y="3" width="6" height="6" rx="1.2" />
        <rect x="2" y="11" width="6" height="6" rx="1.2" />
        <line x1="12" y1="5" x2="18" y2="5" />
        <line x1="12" y1="8" x2="16" y2="8" />
        <line x1="12" y1="13" x2="18" y2="13" />
        <line x1="12" y1="16" x2="15" y2="16" />
      </svg>
    ),
    title: "Cardapio digital",
    text: "Fotos, categorias, precos, adicionais e disponibilidade. O cliente acessa pelo QR da mesa ou pelo link.",
  },
  {
    icon: (
      <svg width="20" height="20" viewBox="0 0 20 20" fill="none" stroke="currentColor" strokeWidth="1.6" strokeLinecap="round" strokeLinejoin="round">
        <rect x="3" y="3" width="6" height="6" rx="0.8" />
        <rect x="3" y="11" width="6" height="6" rx="0.8" />
        <rect x="11" y="3" width="6" height="6" rx="0.8" />
        <rect x="11" y="11" width="6" height="6" rx="0.8" />
      </svg>
    ),
    title: "QR Code por mesa",
    text: "Cada mesa tem seu QR. O cliente pede pelo celular sem baixar app.",
  },
  {
    icon: (
      <svg width="20" height="20" viewBox="0 0 20 20" fill="none" stroke="currentColor" strokeWidth="1.6" strokeLinecap="round" strokeLinejoin="round">
        <rect x="2" y="4" width="16" height="12" rx="1.5" />
        <path d="M6 9l2 2 4-4" />
        <line x1="2" y1="8" x2="18" y2="8" />
      </svg>
    ),
    title: "Cozinha em tempo real",
    text: "Fila clara de A fazer, Preparando e Prontos. A equipe sabe o que produzir.",
  },
  {
    icon: (
      <svg width="20" height="20" viewBox="0 0 20 20" fill="none" stroke="currentColor" strokeWidth="1.6" strokeLinecap="round" strokeLinejoin="round">
        <path d="M3 10h14M3 10l4-4M3 10l4 4" />
        <circle cx="15" cy="10" r="3" />
      </svg>
    ),
    title: "Delivery e retirada",
    text: "Pedidos de entrega e retirada no mesmo fluxo, com endereco, taxa e status.",
  },
  {
    icon: (
      <svg width="20" height="20" viewBox="0 0 20 20" fill="none" stroke="currentColor" strokeWidth="1.6" strokeLinecap="round" strokeLinejoin="round">
        <rect x="2" y="5" width="16" height="10" rx="1.5" />
        <circle cx="10" cy="10" r="2.5" />
        <line x1="5" y1="5" x2="5" y2="15" strokeWidth="2.4" strokeOpacity="0.2" />
        <line x1="15" y1="5" x2="15" y2="15" strokeWidth="2.4" strokeOpacity="0.2" />
      </svg>
    ),
    title: "Caixa e cobranças",
    text: "Cobranca com contexto do pedido. Feche o dia com os totais certos.",
  },
  {
    icon: (
      <svg width="20" height="20" viewBox="0 0 20 20" fill="none" stroke="currentColor" strokeWidth="1.6" strokeLinecap="round" strokeLinejoin="round">
        <path d="M5 7V3h10v4" />
        <rect x="3" y="7" width="14" height="7" rx="1.2" />
        <path d="M5 14v3h10v-3" />
        <circle cx="15" cy="10.5" r="1" fill="currentColor" stroke="none" />
      </svg>
    ),
    title: "Impressao automatica",
    text: "Pedido confirmado, imprime na cozinha. Sem clique extra, sem erro de preparo.",
  },
  {
    icon: (
      <svg width="20" height="20" viewBox="0 0 20 20" fill="none" stroke="currentColor" strokeWidth="1.6" strokeLinecap="round" strokeLinejoin="round">
        <path d="M17 10.5A7 7 0 1 1 9.5 3h.5a7 7 0 0 1 7 7v.5z" />
        <path d="M7 8h6M7 11h4" />
        <path d="M3 17l1.5-3.5" />
      </svg>
    ),
    title: "WhatsApp com IA",
    text: "A IA conversa, orienta e leva o cliente ao pedido. Voce ativa quando quiser.",
  },
  {
    icon: (
      <svg width="20" height="20" viewBox="0 0 20 20" fill="none" stroke="currentColor" strokeWidth="1.6" strokeLinecap="round" strokeLinejoin="round">
        <rect x="3" y="12" width="3" height="5" rx="0.6" />
        <rect x="8.5" y="8" width="3" height="9" rx="0.6" />
        <rect x="14" y="4" width="3" height="13" rx="0.6" />
        <path d="M3 6l4-3 4 3 4-4" strokeWidth="1.5" />
      </svg>
    ),
    title: "Relatorios e gestao",
    text: "Dashboard com vendas, ticket medio, pico de horario e produtos mais pedidos.",
  },
];

export default function RestaurantesPage() {
  return (
    <main className="zpld" id="restaurantes-page">
      <LandingMotion />
      <ElectricBg />

      <div className="zpld-bg" aria-hidden="true">
        <span className="zpld-orb zpld-orb-a" />
        <span className="zpld-orb zpld-orb-b" />
        <span className="zpld-orb zpld-orb-c" />
        <div className="zpld-grid" />
      </div>

      <PublicSiteHeader />

      {/* Page hero */}
      <section className="zpld-section zpld-page-hero" aria-labelledby="rest-title">
        <div className="zpld-section-head" style={{ marginBottom: "2.5rem" }}>
          <Link href="/segmentos" className="zpld-breadcrumb">← Todos os segmentos</Link>
          <span>Restaurantes e delivery</span>
          <h1
            id="rest-title"
            className="zpld-h1"
            style={{ fontSize: "clamp(1.9rem,3vw,3.2rem)", textAlign: "center" }}
          >
            Cardapio, QR, cozinha, delivery, caixa e WhatsApp.
          </h1>
          <p>
            O fluxo completo para restaurantes, lanchonetes e delivery. Do pedido na mesa
            ate o fechamento do caixa.
          </p>
          <div className="zpld-ctas" style={{ justifyContent: "center", marginTop: "0.5rem" }}>
            <Link className="zpld-btn-primary" href="/cadastro?plano=operacao">Comecar agora →</Link>
            <a className="zpld-btn-ghost" href="#planos-rest">Ver planos</a>
          </div>
        </div>
      </section>

      {/* Features */}
      <section className="zpld-section" aria-labelledby="rest-feat-title">
        <div className="zpld-section-head zp-lp-reveal">
          <span>Recursos</span>
          <h2 id="rest-feat-title">Tudo que o restaurante precisa, num lugar so.</h2>
        </div>
        <div className="zpld-mod-grid">
          {restaurantFeatures.map((f) => (
            <article key={f.title} className="zpld-mod-card zp-lp-reveal">
              <span className="zpld-mod-icon">{f.icon}</span>
              <div>
                <strong>{f.title}</strong>
                <p>{f.text}</p>
              </div>
            </article>
          ))}
        </div>
      </section>

      {/* Plans */}
      <section
        className="zpld-section zp-lp-reveal"
        id="planos-rest"
        aria-labelledby="rest-plans-title"
      >
        <div className="zpld-section-head">
          <span>Planos</span>
          <h2 id="rest-plans-title">Comece pequeno. Suba quando crescer.</h2>
          <p>Mensalidade fixa por unidade. Sem taxa por pedido, sem surpresa no boleto.</p>
        </div>

        <div className="zp-lp-plans-grid">
          {commercialPlans.map((plan) => (
            <article
              key={plan.slug}
              className={`zp-lp-plan-card zp-lp-reveal${plan.spotlight ? " is-spotlight" : ""}${plan.premium ? " is-premium" : ""}`}
            >
              {plan.badge ? <em className="zp-lp-plan-badge">{plan.badge}</em> : null}
              <span className="zp-lp-plan-audience">{plan.audience}</span>
              <h3>{plan.name.replace("ZeroPaper ", "")}</h3>
              <div className="zp-lp-plan-price">
                <strong>{plan.priceLabel}</strong>
                <small>/mes</small>
              </div>
              <ul className="zp-lp-plan-features">
                {plan.features.map((f) => (
                  <li key={f}>{f}</li>
                ))}
              </ul>
              <Link className="zp-lp-plan-cta" href={`/cadastro?plano=${plan.slug}`}>
                Escolher {plan.name.replace("ZeroPaper ", "")} →
              </Link>
            </article>
          ))}
        </div>
      </section>

      {/* CTA */}
      <section className="zpld-final-cta zp-lp-reveal" aria-labelledby="rest-cta-title">
        <div className="zpld-final-inner">
          <span>Comece hoje</span>
          <h2 id="rest-cta-title">Seu restaurante organizado a partir de agora.</h2>
          <p>Configure em minutos. Sem contrato, sem taxa de adesao.</p>
          <div className="zpld-ctas">
            <Link className="zpld-btn-primary" href="/cadastro?plano=operacao">Criar conta agora →</Link>
          </div>
        </div>
      </section>
    </main>
  );
}
