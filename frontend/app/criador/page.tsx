import type { Metadata } from "next";
import Link from "next/link";
import { PublicSiteHeader } from "@/components/public-site-header";
import { LandingMotion } from "@/components/landing-motion";
import { CreatorEffects } from "@/components/creator-effects";
import { CreatorMethod } from "@/components/creator-method";
import { CreatorSkills } from "@/components/creator-skills";
import { CriadorReveal } from "@/components/criador-reveal";
import { CreatorProjects } from "@/components/creator-projects";

export const metadata: Metadata = {
  title: "Alexssander Ferreira | Developer · Hacker · Pianist",
  description: "Backend developer, criador do ZeroPaper. C#, ASP.NET Core, Python, segurança web e IA aplicada. Aberto a colaboracoes e oportunidades.",
  alternates: { canonical: "/criador" },
};

const skills = [
  { tag: "</>", label: "C# / ASP.NET Core", sub: "APIs, EF Core, arquitetura, logica de negocio" },
  { tag: "PY",  label: "Python & Automacao", sub: "Scripts, ETL, workflows e IA aplicada" },
  { tag: "SEC", label: "Seguranca Web",       sub: "Burp Suite, enumeracao, pentest e defesa" },
  { tag: "AI",  label: "IA Workflows",        sub: "Modelos integrados em produto real" },
  { tag: "VPS", label: "Linux / Deploy",      sub: "VPS, systemd, Nginx, infraestrutura" },
  { tag: "♪",   label: "Piano",               sub: "Disciplina criativa fora do codigo" },
];


const projects = [
  { tag: "Produto",    name: "ZeroPaper",  live: true,  desc: "SaaS modular: QR Code, caixa, WhatsApp IA e operacao em producao." },
  { tag: "Automacao",  name: "StoreFlow",  live: false, desc: "Controle interno de loja: rotinas, faltas e operacao limpa." },
  { tag: "Seguranca",  name: "CTF Labs",   live: false, desc: "TryHackMe: injecao, RCE, enumeracao e escalonamento de privilegios." },
];

export default function CriadorPage() {
  return (
    <main className="zpld zpld-criador" id="criador-page">
      <LandingMotion />
      <CreatorEffects />
      <CriadorReveal />

      <div className="zpld-bg" aria-hidden="true">
        <span className="zpld-orb zpld-orb-a" />
        <span className="zpld-orb zpld-orb-b" />
        <div className="zpld-grid" />
      </div>

      <PublicSiteHeader />

      {/* ── HERO ─────────────────────────────────────────────────── */}
      <section className="zpld-section zpld-criador-hero">
        <div className="zpld-criador-scanlines" aria-hidden="true" />

        <div className="zpld-criador-hero-inner">
          <div className="zpld-criador-hero-text zp-lp-reveal">
            <Link href="/sobre" className="zpld-breadcrumb">← Sobre o ZeroPaper</Link>
            <p className="zpld-criador-eyebrow">
              <span className="zpld-criador-dot" />
              Criador do ZeroPaper
            </p>

            <h1 className="zpld-criador-h1">
              <span className="zpld-criador-name" data-text="Alexssander">Alexssander</span>
              <br />
              <span className="zpld-criador-roles">
                <em>Developer</em>
                <span aria-hidden="true"> · </span>
                <em className="zpld-criador-role-accent">Hacker</em>
                <span aria-hidden="true"> · </span>
                <em>Pianist</em>
              </span>
            </h1>

            <p className="zpld-criador-bio">
              Backend developer com 2 anos resolvendo problemas reais. Construo sistemas
              com estrutura, automacao e raciocinio de segurança — o ZeroPaper e a prova
              disso rodando em producao.
            </p>

            <div className="zpld-criador-tags">
              {["C# · ASP.NET", "Python", "Pentest", "Linux VPS", "IA aplicada"].map((t) => (
                <span key={t} className="zpld-criador-tag">{t}</span>
              ))}
            </div>

            <div className="zpld-ctas" style={{ justifyContent: "flex-start", marginTop: "2.2rem", flexWrap: "wrap" }}>
              <a className="zpld-btn-primary" href="https://wa.me/5511977936534" target="_blank" rel="noopener noreferrer">
                Iniciar conversa →
              </a>
              <a className="zpld-btn-ghost" href="https://alexssanderlx.com.br" target="_blank" rel="noopener noreferrer">
                Portfolio completo
              </a>
              <Link className="zpld-btn-ghost" href="/contato">
                Enviar mensagem
              </Link>
            </div>
          </div>

          {/* Terminal animado */}
          <div className="zpld-criador-terminal zp-lp-reveal" aria-hidden="true">
            <div className="zpld-criador-term-bar">
              <span /><span /><span />
              <code>root@alex-lx:~# <span className="zpld-criador-blink">_</span></code>
            </div>
            <div className="zpld-criador-term-body">
              <p><span className="ct-dim">$</span> <span className="ct-cmd">whoami</span></p>
              <p className="ct-green">alexssander_ferreira</p>
              <br />
              <p><span className="ct-dim">$</span> <span className="ct-cmd">cat mission.txt</span></p>
              <p className="ct-out">build clear systems</p>
              <p className="ct-out">automate workflows</p>
              <p className="ct-out">protect applications</p>
              <br />
              <p><span className="ct-dim">$</span> <span className="ct-cmd">ls projects/</span></p>
              <p><span className="ct-gold">ZeroPaper</span>{"  "}<span className="ct-out">StoreFlow</span>{"  "}<span className="ct-out">CTF</span></p>
              <br />
              <p><span className="ct-dim">$</span> <span className="ct-cmd">uptime</span></p>
              <p className="ct-out">2 anos · aprendendo sempre<span className="zpld-criador-cursor">█</span></p>
            </div>
            {/* Glitch bar decorativa */}
            <div className="zpld-criador-term-footer">
              <span className="zpld-criador-term-status">
                <span className="ct-green">●</span> online
              </span>
              <span className="ct-dim">alexssanderlx.com.br</span>
            </div>
          </div>
        </div>

        {/* Stats */}
        <div className="zpld-criador-stats zp-lp-reveal">
          {[
            { n: "2+",  label: "Anos em sistemas reais" },
            { n: "10+", label: "Projetos e automacoes" },
            { n: "CTF", label: "Labs de seguranca ativos" },
            { n: "24/7",label: "Codigo, seguranca e musica" },
          ].map((s) => (
            <div key={s.label} className="zpld-criador-stat">
              <strong>{s.n}</strong>
              <span>{s.label}</span>
            </div>
          ))}
        </div>
      </section>

      {/* ── SKILLS (animado) ─────────────────────────────────────── */}
      <CreatorSkills />

      {/* ── PROJECTS (animado) ───────────────────────────────────── */}
      <CreatorProjects />

      {/* ── METHOD (animado) ─────────────────────────────────────── */}
      <CreatorMethod />

      {/* ── CTA ──────────────────────────────────────────────────── */}
      <section className="zpld-section zpld-final zp-lp-reveal" aria-labelledby="criador-cta-title">
        <div className="zpld-final-inner" style={{ maxWidth: "620px" }}>
          <span>Proximo passo</span>
          <h2 id="criador-cta-title">
            Vamos construir algo util com clareza e impacto.
          </h2>
          <p>Aberto a desafios, colaboracoes e oportunidades de impacto real.</p>
          <div className="zpld-ctas" style={{ justifyContent: "center", flexWrap: "wrap" }}>
            <a className="zpld-btn-primary" href="https://wa.me/5511977936534" target="_blank" rel="noopener noreferrer">
              Falar no WhatsApp →
            </a>
            <a className="zpld-btn-ghost" href="mailto:alexssander.f.almeida2006@gmail.com">
              Enviar email
            </a>
            <a className="zpld-btn-ghost" href="https://alexssanderlx.com.br" target="_blank" rel="noopener noreferrer">
              Portfolio completo
            </a>
          </div>
        </div>
      </section>
    </main>
  );
}
