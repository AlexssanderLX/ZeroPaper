import type { Metadata } from "next";
import Link from "next/link";
import { PublicSiteHeader } from "@/components/public-site-header";
import { LandingMotion } from "@/components/landing-motion";
import { ElectricBg } from "@/components/electric-bg";
import { ContactForm } from "@/components/contact-form";

export const metadata: Metadata = {
  title: "Contato | ZeroPaper",
  description: "Fale com o time do ZeroPaper. Tire duvidas, solicite uma demonstracao ou converse sobre a implantacao no seu negocio.",
  alternates: { canonical: "/contato" },
};

const channels = [
  {
    icon: (
      <svg width="22" height="22" viewBox="0 0 22 22" fill="none" stroke="currentColor" strokeWidth="1.6" strokeLinecap="round" strokeLinejoin="round">
        <path d="M19 11.5A8 8 0 1 1 10.5 3h.5a8 8 0 0 1 8 8v.5z" />
        <path d="M8 9h6M8 12.5h4" />
        <path d="M3 19l1.7-4" />
      </svg>
    ),
    label: "WhatsApp",
    description: "Resposta rapida para duvidas e demonstracoes.",
    action: "Iniciar conversa",
    href: "https://wa.me/5511977936534",
  },
  {
    icon: (
      <svg width="22" height="22" viewBox="0 0 22 22" fill="none" stroke="currentColor" strokeWidth="1.6" strokeLinecap="round" strokeLinejoin="round">
        <rect x="2" y="5" width="18" height="13" rx="2" />
        <path d="M2 8l9 6 9-6" />
      </svg>
    ),
    label: "E-mail",
    description: "Para assuntos mais detalhados ou parceiros.",
    action: "Enviar e-mail",
    href: "mailto:alexssander.f.almeida2006@gmail.com",
  },
];

export default function ContatoPage() {
  return (
    <main className="zpld" id="contato-page">
      <LandingMotion />
      <ElectricBg />

      <div className="zpld-bg" aria-hidden="true">
        <span className="zpld-orb zpld-orb-a" />
        <span className="zpld-orb zpld-orb-b" />
        <div className="zpld-grid" />
      </div>

      <PublicSiteHeader />

      <section className="zpld-section zpld-page-hero" aria-labelledby="contato-title">
        <div className="zpld-section-head" style={{ marginBottom: "2.5rem" }}>
          <Link href="/" className="zpld-breadcrumb">← Voltar para home</Link>
          <span>Contato</span>
          <h1 id="contato-title" className="zpld-h1" style={{ fontSize: "clamp(2rem,3vw,3rem)", textAlign: "center" }}>
            Fale com a gente.
          </h1>
          <p>
            Tire duvidas, solicite uma demonstracao ou conte sobre o seu negocio.
            O time do ZeroPaper responde rapido.
          </p>
        </div>

        {/* Canais diretos */}
        <div className="zpld-mod-grid" style={{ maxWidth: "720px", margin: "0 auto 3rem" }}>
          {channels.map((ch) => (
            <a
              key={ch.label}
              href={ch.href}
              target="_blank"
              rel="noopener noreferrer"
              className="zpld-mod-card zp-lp-reveal"
              style={{ textDecoration: "none", flexDirection: "column", gap: "14px" }}
            >
              <div style={{ display: "flex", alignItems: "center", gap: "12px" }}>
                <span className="zpld-mod-icon">{ch.icon}</span>
                <strong style={{ color: "var(--zpld-text)", fontSize: "1rem" }}>{ch.label}</strong>
              </div>
              <p style={{ margin: 0, color: "var(--zpld-muted)", fontSize: "0.88rem", lineHeight: 1.6 }}>
                {ch.description}
              </p>
              <span className="zpld-link">{ch.action} →</span>
            </a>
          ))}
        </div>

        {/* Formulário de mensagem */}
        <div className="zpld-mod-card zp-lp-reveal" style={{ maxWidth: "720px", margin: "0 auto", flexDirection: "column", gap: "20px", padding: "2rem" }}>
          <div>
            <span className="zpld-section-eyebrow">Mensagem direta</span>
            <h2 style={{ margin: "6px 0 4px", fontSize: "1.25rem", color: "var(--zpld-text)" }}>
              Deixe sua mensagem
            </h2>
            <p style={{ margin: 0, color: "var(--zpld-muted)", fontSize: "0.88rem" }}>
              Preencha abaixo e responderei pelo seu email.
            </p>
          </div>
          <ContactForm />
        </div>
      </section>
    </main>
  );
}
