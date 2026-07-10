import type { Metadata } from "next";
import type React from "react";
import Link from "next/link";
import { PublicSiteHeader } from "@/components/public-site-header";
import { LandingMotion } from "@/components/landing-motion";
import { platformModules, howItWorksSteps } from "@/lib/landing-data";
import { ElectricBg } from "@/components/electric-bg";
import { segmentIconMap } from "@/components/segment-card";

export const metadata: Metadata = {
  title: "ZeroPaper | Plataforma modular para pequenos negocios",
  description:
    "Pedidos, atendimento, caixa e operacao em um so fluxo. Configuravel por modulos para restaurantes, varejo, pet shops e mais.",
  alternates: { canonical: "/" },
  openGraph: {
    title: "ZeroPaper | Plataforma modular para pequenos negocios",
    description: "Venda, atenda e organize sem papel e sem bagunca.",
    url: "/",
    siteName: "ZeroPaper",
    locale: "pt_BR",
    type: "website",
  },
};

const heroStats = [
  { value: "23", label: "Pedidos hoje", accent: "green" },
  { value: "R$ 1.840", label: "Caixa do dia", accent: "gold" },
  { value: "18", label: "Clientes ativos", accent: "muted" },
];

const highlightModules = platformModules.slice(0, 4);

const moduleIcons: Record<string, React.ReactNode> = {
  catalog: (
    <svg width="20" height="20" viewBox="0 0 20 20" fill="none" stroke="currentColor" strokeWidth="1.6" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
      <rect x="2" y="3" width="6" height="6" rx="1.2" />
      <rect x="2" y="11" width="6" height="6" rx="1.2" />
      <line x1="12" y1="5" x2="18" y2="5" />
      <line x1="12" y1="8" x2="16" y2="8" />
      <line x1="12" y1="13" x2="18" y2="13" />
      <line x1="12" y1="16" x2="15" y2="16" />
    </svg>
  ),
  orders: (
    <svg width="20" height="20" viewBox="0 0 20 20" fill="none" stroke="currentColor" strokeWidth="1.6" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
      <rect x="3" y="3" width="6" height="6" rx="0.8" />
      <rect x="3" y="11" width="6" height="6" rx="0.8" />
      <rect x="11" y="3" width="6" height="6" rx="0.8" />
      <rect x="11" y="11" width="6" height="6" rx="0.8" />
    </svg>
  ),
  whatsapp: (
    <svg width="20" height="20" viewBox="0 0 20 20" fill="none" stroke="currentColor" strokeWidth="1.6" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
      <path d="M17 10.5A7 7 0 1 1 9.5 3h.5a7 7 0 0 1 7 7v.5z" />
      <path d="M7 8h6M7 11h4" />
      <path d="M3 17l1.5-3.5" />
    </svg>
  ),
  cash: (
    <svg width="20" height="20" viewBox="0 0 20 20" fill="none" stroke="currentColor" strokeWidth="1.6" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
      <rect x="2" y="5" width="16" height="10" rx="1.5" />
      <circle cx="10" cy="10" r="2.5" />
      <line x1="5" y1="5" x2="5" y2="15" strokeWidth="2.4" strokeOpacity="0.2" />
      <line x1="15" y1="5" x2="15" y2="15" strokeWidth="2.4" strokeOpacity="0.2" />
    </svg>
  ),
  print: (
    <svg width="20" height="20" viewBox="0 0 20 20" fill="none" stroke="currentColor" strokeWidth="1.6" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
      <path d="M5 7V3h10v4" />
      <rect x="3" y="7" width="14" height="7" rx="1.2" />
      <path d="M5 14v3h10v-3" />
      <circle cx="15" cy="10.5" r="1" fill="currentColor" stroke="none" />
    </svg>
  ),
  customers: (
    <svg width="20" height="20" viewBox="0 0 20 20" fill="none" stroke="currentColor" strokeWidth="1.6" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
      <circle cx="10" cy="7" r="3.2" />
      <path d="M3.5 17c0-3.6 2.9-6.5 6.5-6.5s6.5 2.9 6.5 6.5" />
    </svg>
  ),
  coupons: (
    <svg width="20" height="20" viewBox="0 0 20 20" fill="none" stroke="currentColor" strokeWidth="1.6" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
      <path d="M3 10.5V5.5A1.5 1.5 0 0 1 4.5 4h11A1.5 1.5 0 0 1 17 5.5v5a2 2 0 0 0 0 4v.5A1.5 1.5 0 0 1 15.5 16H4.5A1.5 1.5 0 0 1 3 14.5V14a2 2 0 0 0 0-3.5z" />
      <line x1="9" y1="4" x2="9" y2="16" strokeDasharray="2 2" />
    </svg>
  ),
  reports: (
    <svg width="20" height="20" viewBox="0 0 20 20" fill="none" stroke="currentColor" strokeWidth="1.6" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
      <rect x="3" y="12" width="3" height="5" rx="0.6" />
      <rect x="8.5" y="8" width="3" height="9" rx="0.6" />
      <rect x="14" y="4" width="3" height="13" rx="0.6" />
      <path d="M3 6l4-3 4 3 4-4" strokeWidth="1.5" />
    </svg>
  ),
};

const segmentTeaser = [
  { key: "restaurant", label: "Restaurante" },
  { key: "retail",     label: "Varejo" },
  { key: "petshop",    label: "Pet shop" },
  { key: "technical",  label: "Assistencia" },
  { key: "auto",       label: "Oficinas" },
];

export default function Home() {
  return (
    <main className="zpld" id="home">
      <LandingMotion />
      <ElectricBg />

      {/* ── Background orbs ──────────────────────────────────── */}
      <div className="zpld-bg" aria-hidden="true">
        <span className="zpld-orb zpld-orb-a" />
        <span className="zpld-orb zpld-orb-b" />
        <span className="zpld-orb zpld-orb-c" />
        <div className="zpld-grid" />
      </div>

      <PublicSiteHeader />

      {/* ══════════════════════════════════════════════════════
          HERO — headline animado
      ══════════════════════════════════════════════════════ */}
      <section className="zpld-hero" aria-labelledby="zpld-title">
        <div className="zpld-hero-inner">

          <span className="zpld-badge zpld-anim-1" role="text">
            <i className="zpld-badge-dot" aria-hidden="true" />
            Plataforma modular para pequenos negocios
          </span>

          <h1 id="zpld-title" className="zpld-h1">
            <span className="zpld-h1-line zpld-anim-2">Venda, atenda</span>
            <span className="zpld-h1-line zpld-h1-accent zpld-anim-3">e organize</span>
            <span className="zpld-h1-line zpld-h1-dim zpld-anim-4">sem bagunca.</span>
          </h1>

          <p className="zpld-sub zpld-anim-5">
            Uma plataforma modular para pequenos negocios venderem por link, QR Code e
            WhatsApp, organizarem pedidos, clientes, cobranças e operacao em um so painel.
          </p>

          <div className="zpld-ctas zpld-anim-6">
            <Link className="zpld-btn-primary" href="/cadastro?plano=operacao">
              Comecar agora
            </Link>
            <a className="zpld-btn-ghost" href="#como-funciona">
              Como funciona
            </a>
          </div>

          {/* Stat cards */}
          <div className="zpld-stats zpld-anim-7" aria-label="Numeros ao vivo">
            {heroStats.map((s) => (
              <div key={s.label} className={`zpld-stat zpld-stat-${s.accent}`}>
                <strong>{s.value}</strong>
                <span>{s.label}</span>
              </div>
            ))}
          </div>
        </div>

        {/* Visual decorativo — painel flutuante */}
        <div className="zpld-hero-panel zpld-anim-8" aria-hidden="true">
          <div className="zpld-panel">
            <div className="zpld-panel-bar">
              <span className="zpld-panel-dot d1" /><span className="zpld-panel-dot d2" /><span className="zpld-panel-dot d3" />
              <span className="zpld-panel-title">ZeroPaper — Painel ao vivo</span>
              <span className="zpld-panel-live">● Ao vivo</span>
            </div>
            <div className="zpld-panel-stats">
              <div><span>Pedidos</span><strong>23</strong></div>
              <div><span>Caixa</span><strong>R$ 1.840</strong></div>
              <div><span>Clientes</span><strong>18</strong></div>
            </div>
            <div className="zpld-panel-feed">
              <div className="zpld-feed-row zpld-feed-new">
                <span className="zpld-feed-dot green" />
                <div>
                  <b>Pedido #47 recebido</b>
                  <p>R$ 78,90 · Link / QR · agora</p>
                </div>
                <em>Novo</em>
              </div>
              <div className="zpld-feed-row">
                <span className="zpld-feed-dot gold" />
                <div>
                  <b>Cobranca #46 enviada</b>
                  <p>Pix · R$ 124,00 · 4 min</p>
                </div>
                <em className="zpld-feed-ok">✓</em>
              </div>
              <div className="zpld-feed-row">
                <span className="zpld-feed-dot muted" />
                <div>
                  <b>Pedido #45 concluido</b>
                  <p>Entrega · R$ 55,00 · 12 min</p>
                </div>
                <em className="zpld-feed-ok">✓</em>
              </div>
            </div>
            <div className="zpld-panel-mods">
              <span>Ativos:</span>
              {["Catalogo","Pedidos","WhatsApp IA","Caixa"].map(m=>(
                <span key={m} className="zpld-mod-chip">{m}</span>
              ))}
            </div>
          </div>
          <div className="zpld-float-chip zpld-chip-a">
            <span className="zpld-chip-dot green" />WhatsApp IA ativo
          </div>
          <div className="zpld-float-chip zpld-chip-b">
            <span className="zpld-chip-dot gold" />Caixa aberto
          </div>
        </div>
      </section>

      {/* ══════════════════════════════════════════════════════
          COMO FUNCIONA
      ══════════════════════════════════════════════════════ */}
      <section className="zpld-section" id="como-funciona" aria-labelledby="zpld-how-title">
        <div className="zpld-section-head zp-lp-reveal">
          <span>Como funciona</span>
          <h2 id="zpld-how-title">Simples de comecar. Poderoso no dia a dia.</h2>
        </div>
        <div className="zpld-steps">
          {howItWorksSteps.map((step) => (
            <div key={step.step} className="zpld-step zp-lp-reveal">
              <div className="zpld-step-num">{step.step}</div>
              <strong>{step.title}</strong>
              <p>{step.text}</p>
            </div>
          ))}
        </div>
      </section>

      {/* ══════════════════════════════════════════════════════
          MODULOS
      ══════════════════════════════════════════════════════ */}
      <section className="zpld-section" id="recursos" aria-labelledby="zpld-mod-title">
        <div className="zpld-section-head zp-lp-reveal">
          <span>Modulos</span>
          <h2 id="zpld-mod-title">Ative o que faz sentido para o seu negocio.</h2>
          <p>Comece com o essencial. Adicione modulos conforme a operacao crescer.</p>
        </div>
        <div className="zpld-mod-grid">
          {highlightModules.map((mod) => (
            <article key={mod.key} className="zpld-mod-card zp-lp-reveal">
              <span className="zpld-mod-icon">{moduleIcons[mod.key]}</span>
              <div>
                <span className="zpld-mod-eyebrow">{mod.eyebrow}</span>
                <strong>{mod.title}</strong>
                <p>{mod.text}</p>
              </div>
            </article>
          ))}
        </div>
      </section>

      {/* ══════════════════════════════════════════════════════
          PARA SEU NEGÓCIO — teaser, sem cards de segmento
      ══════════════════════════════════════════════════════ */}
      <section className="zpld-section zpld-segment-teaser zp-lp-reveal" aria-labelledby="zpld-seg-title">
        <div className="zpld-segment-teaser-inner">
          <div className="zpld-seg-icons" aria-hidden="true">
            {segmentTeaser.map((s) => (
              <span key={s.label} className="zpld-seg-icon-pill" title={s.label}>
                {segmentIconMap[s.key]}
              </span>
            ))}
          </div>
          <h2 id="zpld-seg-title">Para qual tipo de negocio?</h2>
          <p>
            Restaurante, varejo local, pet shop, assistencia tecnica e mais. O ZeroPaper
            e configurado por modulos para o fluxo do seu negocio.
          </p>
          <Link className="zpld-btn-primary" href="/segmentos">
            Explorar segmentos →
          </Link>
        </div>
      </section>

      {/* ══════════════════════════════════════════════════════
          BENEFICIOS
      ══════════════════════════════════════════════════════ */}
      <section className="zpld-section" aria-labelledby="zpld-ben-title">
        <div className="zpld-section-head zp-lp-reveal">
          <span>Por que o ZeroPaper</span>
          <h2 id="zpld-ben-title">O que muda na pratica.</h2>
        </div>
        <div className="zpld-ben-grid">
          {[
            {
              icon: <svg width="20" height="20" viewBox="0 0 20 20" fill="none" stroke="currentColor" strokeWidth="1.6" strokeLinecap="round" strokeLinejoin="round"><path d="M3 15l4-8 4 5 2-3 4 6"/><line x1="3" y1="15" x2="17" y2="15" strokeOpacity="0.3"/></svg>,
              title: "Menos papel e menos perda",
              text: "Pedidos digitais chegam direto no painel, sem comanda manual.",
            },
            {
              icon: <svg width="20" height="20" viewBox="0 0 20 20" fill="none" stroke="currentColor" strokeWidth="1.6" strokeLinecap="round" strokeLinejoin="round"><rect x="3" y="12" width="3" height="5" rx="0.6"/><rect x="8.5" y="8" width="3" height="9" rx="0.6"/><rect x="14" y="4" width="3" height="13" rx="0.6"/><path d="M3 6l4-3 4 3 4-4" strokeWidth="1.5"/></svg>,
              title: "Controle da operacao",
              text: "Pedidos, producao e caixa em tempo real, no mesmo lugar.",
            },
            {
              icon: <svg width="20" height="20" viewBox="0 0 20 20" fill="none" stroke="currentColor" strokeWidth="1.6" strokeLinecap="round" strokeLinejoin="round"><path d="M11 2L7 11h5l-3 7 8-10h-5l3-6z"/></svg>,
              title: "Atendimento mais rapido",
              text: "Menos espera para o cliente. O fluxo roda mais fluido.",
            },
            {
              icon: <svg width="20" height="20" viewBox="0 0 20 20" fill="none" stroke="currentColor" strokeWidth="1.6" strokeLinecap="round" strokeLinejoin="round"><rect x="3" y="3" width="6" height="6" rx="1.2"/><rect x="11" y="3" width="6" height="6" rx="1.2"/><rect x="3" y="11" width="6" height="6" rx="1.2"/><path d="M11 14h6M14 11v6"/></svg>,
              title: "Crescimento por modulos",
              text: "Comeca simples e adiciona modulos conforme o negocio cresce.",
            },
          ].map((b) => (
            <div key={b.title} className="zpld-benefit zp-lp-reveal">
              <span className="zpld-benefit-icon" aria-hidden="true">{b.icon}</span>
              <strong>{b.title}</strong>
              <p>{b.text}</p>
            </div>
          ))}
        </div>
      </section>

      {/* ══════════════════════════════════════════════════════
          FINAL CTA
      ══════════════════════════════════════════════════════ */}
      <section className="zpld-final-cta zp-lp-reveal" aria-labelledby="zpld-cta-title">
        <div className="zpld-final-inner">
          <span>Comece agora</span>
          <h2 id="zpld-cta-title">Seu negocio organizado a partir de hoje.</h2>
          <p>Configure em minutos. Sem contrato, sem taxa de adesao.</p>
          <div className="zpld-ctas">
            <Link className="zpld-btn-primary" href="/cadastro?plano=operacao">
              Criar conta agora
            </Link>
            <Link className="zpld-btn-ghost" href="/segmentos">
              Ver segmentos
            </Link>
          </div>
        </div>
      </section>
    </main>
  );
}
