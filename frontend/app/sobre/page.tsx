import type { Metadata } from "next";
import Link from "next/link";
import { PublicSiteHeader } from "@/components/public-site-header";
import { LandingMotion } from "@/components/landing-motion";
import { ElectricBg } from "@/components/electric-bg";

export const metadata: Metadata = {
  title: "Sobre | ZeroPaper",
  description: "O ZeroPaper e uma plataforma modular para pequenos negocios venderem, organizarem pedidos e atenderem clientes sem papel e sem bagunca.",
  alternates: { canonical: "/sobre" },
};

const pillars = [
  {
    icon: (
      <svg width="20" height="20" viewBox="0 0 20 20" fill="none" stroke="currentColor" strokeWidth="1.6" strokeLinecap="round" strokeLinejoin="round">
        <rect x="3" y="12" width="3" height="5" rx="0.6" />
        <rect x="8.5" y="8" width="3" height="9" rx="0.6" />
        <rect x="14" y="4" width="3" height="13" rx="0.6" />
        <path d="M3 6l4-3 4 3 4-4" strokeWidth="1.5" />
      </svg>
    ),
    title: "Modular por natureza",
    text: "Cada negocio ativa o que precisa. Sem pagar por funcionalidades que nao usa.",
  },
  {
    icon: (
      <svg width="20" height="20" viewBox="0 0 20 20" fill="none" stroke="currentColor" strokeWidth="1.6" strokeLinecap="round" strokeLinejoin="round">
        <circle cx="10" cy="10" r="7" />
        <path d="M10 6v4l3 3" />
      </svg>
    ),
    title: "Rapido de comecar",
    text: "Configuracao em minutos. Sem hardware especial, sem contrato de longo prazo.",
  },
  {
    icon: (
      <svg width="20" height="20" viewBox="0 0 20 20" fill="none" stroke="currentColor" strokeWidth="1.6" strokeLinecap="round" strokeLinejoin="round">
        <path d="M10 2L3 6v5c0 4.4 2.9 8 7 9 4.1-1 7-4.6 7-9V6l-7-4z" />
        <path d="M7 10l2 2 4-4" />
      </svg>
    ),
    title: "Confiavel e seguro",
    text: "Dados protegidos, operacao estavel. O negocio depende da plataforma e ela precisa funcionar.",
  },
];

export default function SobrePage() {
  return (
    <main className="zpld" id="sobre-page">
      <LandingMotion />
      <ElectricBg />

      <div className="zpld-bg" aria-hidden="true">
        <span className="zpld-orb zpld-orb-a" />
        <span className="zpld-orb zpld-orb-b" />
        <div className="zpld-grid" />
      </div>

      <PublicSiteHeader />

      <section className="zpld-section zpld-page-hero" aria-labelledby="sobre-title">
        <div className="zpld-section-head" style={{ marginBottom: "3rem" }}>
          <Link href="/" className="zpld-breadcrumb">← Voltar para home</Link>
          <span>Sobre o ZeroPaper</span>
          <h1 id="sobre-title" className="zpld-h1" style={{ fontSize: "clamp(2rem,3vw,3.2rem)", textAlign: "center" }}>
            Tecnologia para quem vende no dia a dia.
          </h1>
          <p>
            O ZeroPaper nasceu para dar a pequenos negocios as ferramentas que antes
            eram exclusivas de grandes redes — de forma simples, modular e acessivel.
          </p>
        </div>

        <div className="zpld-mod-grid zp-lp-reveal" style={{ marginBottom: "3rem" }}>
          {pillars.map((p) => (
            <div key={p.title} className="zpld-mod-card">
              <span className="zpld-mod-icon">{p.icon}</span>
              <div>
                <strong>{p.title}</strong>
                <p>{p.text}</p>
              </div>
            </div>
          ))}
        </div>

        {/* Criador */}
        <div className="zpld-sobre-criador-teaser zp-lp-reveal">
          <div>
            <span className="zpld-section-eyebrow">Quem construiu isso</span>
            <p style={{ margin: "6px 0 0", color: "var(--zpld-muted)", fontSize: "0.9rem" }}>
              Desenvolvido por Alexssander Ferreira — backend dev, hacker e pianista.
            </p>
          </div>
          <Link href="/criador" className="zpld-btn-ghost" style={{ flexShrink: 0 }}>
            Sobre o criador →
          </Link>
        </div>

        {/* Privacidade */}
        <div className="zpld-final-inner zp-lp-reveal" style={{ maxWidth: "600px", margin: "3rem auto 0" }}>
          <span>Privacidade</span>
          <h2 style={{ fontSize: "clamp(1.4rem,2vw,1.9rem)", margin: 0 }}>
            Seus dados sao seus.
          </h2>
          <p style={{ textAlign: "left", maxWidth: "none" }}>
            O ZeroPaper coleta apenas os dados necessarios para operar a plataforma.
            Nao vendemos, compartilhamos ou usamos dados dos negocios e dos clientes
            para outros fins. Os dados ficam armazenados em servidores seguros e podem
            ser exportados ou removidos a qualquer momento pelo titular da conta.
          </p>
          <p style={{ textAlign: "left", maxWidth: "none", color: "var(--zpld-muted)", fontSize: "0.83rem" }}>
            Esta pagina e um esboço. Uma politica de privacidade completa sera publicada
            em breve em <Link href="/privacidade" className="zpld-link">/privacidade</Link>.
          </p>
          <div className="zpld-ctas" style={{ justifyContent: "center" }}>
            <Link className="zpld-btn-primary" href="/contato">Falar com o time</Link>
            <Link className="zpld-btn-ghost" href="/privacidade">Politica de privacidade</Link>
          </div>
        </div>
      </section>
    </main>
  );
}
