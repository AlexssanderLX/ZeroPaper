"use client";

import { useEffect, useRef, useState } from "react";

/* ── Visual: ZeroPaper — painel de pedidos ao vivo ─── */
function VisualZeroPaper({ active }: { active: boolean }) {
  const orders = [
    { id: 47, val: "R$ 78,90", status: "novo",     label: "Link / QR" },
    { id: 46, val: "R$ 124,00", status: "pago",    label: "Pix" },
    { id: 45, val: "R$ 55,00",  status: "pronto",  label: "Entrega" },
  ];
  const [tick, setTick] = useState(0);
  useEffect(() => {
    if (!active) return;
    const t = setInterval(() => setTick(n => n + 1), 2200);
    return () => clearInterval(t);
  }, [active]);
  const highlighted = tick % 3;

  return (
    <div className="cp-visual cp-visual-zp">
      <div className="cp-zp-header">
        <span className="cp-zp-title">ZeroPaper — Painel</span>
        <span className="cp-zp-live"><span className="cp-zp-dot" />Ao vivo</span>
      </div>
      <div className="cp-zp-stats">
        {[{ n: "23", l: "Pedidos" }, { n: "R$ 1.840", l: "Caixa" }, { n: "18", l: "Clientes" }].map((s, i) => (
          <div key={i} className="cp-zp-stat"><strong>{s.n}</strong><span>{s.l}</span></div>
        ))}
      </div>
      <div className="cp-zp-orders">
        {orders.map((o, i) => (
          <div key={o.id} className={`cp-zp-order ${i === highlighted ? "cp-zp-order-hi" : ""}`}>
            <div>
              <strong>Pedido #{o.id}</strong>
              <span>{o.val} · {o.label}</span>
            </div>
            <span className={`cp-zp-badge cp-zp-badge-${o.status}`}>
              {o.status === "novo" ? "Novo" : o.status === "pago" ? "✓" : "✓"}
            </span>
          </div>
        ))}
      </div>
    </div>
  );
}

/* ── Visual: YourRhythm Studio — progresso do aluno ─ */
function VisualYourRhythm({ active }: { active: boolean }) {
  const [xp, setXp] = useState(0);
  const [note, setNote] = useState(0);
  useEffect(() => {
    if (!active) return;
    const t1 = setInterval(() => setXp(n => Math.min(n + 3, 78)), 40);
    const t2 = setInterval(() => setNote(n => (n + 1) % 8), 600);
    return () => { clearInterval(t1); clearInterval(t2); };
  }, [active]);

  const missions = ["Escala de Do", "Arpejo Am", "Ritmo 3/4"];
  const notes = ["♩", "♪", "♫", "♬", "𝅗𝅥", "♩", "♪", "♫"];

  return (
    <div className="cp-visual cp-visual-yr">
      {/* Header */}
      <div className="cp-yr-header">
        <span className="cp-yr-icon">♪</span>
        <div>
          <strong>YourRhythm Studio</strong>
          <span>Plataforma de ensino musical</span>
        </div>
        <span className="cp-yr-note">{notes[note]}</span>
      </div>

      {/* XP bar */}
      <div className="cp-yr-xp-row">
        <span>Nível 4 · XP</span>
        <span>{xp}/100</span>
      </div>
      <div className="cp-yr-xp-bar">
        <div className="cp-yr-xp-fill" style={{ width: `${xp}%` }} />
      </div>

      {/* Missions */}
      <div className="cp-yr-missions">
        {missions.map((m, i) => (
          <div key={i} className={`cp-yr-mission ${i === 0 ? "cp-yr-mission-done" : i === 1 ? "cp-yr-mission-active" : ""}`}>
            <span>{i === 0 ? "✓" : i === 1 ? "▶" : "○"}</span>
            <span>{m}</span>
          </div>
        ))}
      </div>
    </div>
  );
}

/* ── Visual: CTF Labs — terminal de ataque ─────────── */
function VisualCTF({ active }: { active: boolean }) {
  const steps = [
    { t: "cmd",  v: "$ nmap -sV 10.10.10.5" },
    { t: "info", v: "→ 80/tcp open http" },
    { t: "info", v: "→ 22/tcp open ssh" },
    { t: "cmd",  v: "$ gobuster dir -u http://..." },
    { t: "hit",  v: "→ /admin [200]" },
    { t: "cmd",  v: "$ sqlmap -u '...?id=1'" },
    { t: "vuln", v: "! SQL Injection found" },
    { t: "cmd",  v: "$ python3 exploit.py" },
    { t: "root", v: "# whoami → root 🏁" },
  ];
  const [shown, setShown] = useState(0);
  useEffect(() => {
    if (!active) return;
    const t = setInterval(() => setShown(n => n < steps.length ? n + 1 : 1), 900);
    return () => clearInterval(t);
  }, [active]);

  return (
    <div className="cp-visual cp-visual-ctf">
      <div className="cp-ctf-bar">
        <span /><span /><span />
        <code>kali@thm ~ $ <span className="cp-ctf-blink">_</span></code>
      </div>
      <div className="cp-ctf-body">
        {steps.slice(0, shown).map((s, i) => (
          <div key={i} className={`cp-ctf-line cp-ctf-${s.t}`}>{s.v}</div>
        ))}
      </div>
    </div>
  );
}

const projects = [
  {
    tag: "SaaS · Produto",
    name: "ZeroPaper",
    stack: "C# · ASP.NET · Next.js · MySQL",
    desc: "Plataforma modular para restaurantes e varejo. QR Code, caixa, WhatsApp com IA, impressao e relatorios — em producao.",
    live: true,
    href: "https://zeropaperflow.com.br",
    color: "#6ddf9d",
    Visual: VisualZeroPaper,
  },
  {
    tag: "SaaS · Educacao",
    name: "YourRhythm Studio",
    stack: "C# · ASP.NET Core MVC · Razor",
    desc: "Plataforma de gestao para professores e escolas de musica. Alunos, missoes, XP, progresso e repertorio em um so lugar.",
    live: false,
    href: null,
    color: "#d4a843",
    Visual: VisualYourRhythm,
  },
  {
    tag: "Seguranca · Labs",
    name: "CTF / Pentest",
    stack: "Kali · Burp Suite · TryHackMe",
    desc: "Labs mapeados como missoes: enumeracao, SQL injection, RCE e escalonamento de privilegios. Seguranca na pratica.",
    live: false,
    href: "https://alexssanderlx.com.br/Home/Projects#pentest",
    color: "#ff7c7c",
    Visual: VisualCTF,
  },
];

function ProjectCard({ proj, index, active }: { proj: typeof projects[0]; index: number; active: boolean }) {
  const [progress, setProgress] = useState(0);
  const { Visual } = proj;

  useEffect(() => {
    if (!active) return;
    const delay = index * 150;
    const timer = setTimeout(() => {
      let start: number | null = null;
      const animate = (ts: number) => {
        if (!start) start = ts;
        const p = Math.min((ts - start) / 800, 1);
        setProgress(1 - Math.pow(1 - p, 3));
        if (p < 1) requestAnimationFrame(animate);
      };
      requestAnimationFrame(animate);
    }, delay);
    return () => clearTimeout(timer);
  }, [active, index]);

  const style = { "--cp-color": proj.color, opacity: progress, transform: `translateY(${(1 - progress) * 32}px)` } as React.CSSProperties;

  const inner = (
    <>
      <div className="cp-card-line" style={{ background: `linear-gradient(90deg, ${proj.color}, ${proj.color}40, transparent)`, opacity: progress }} />
      <div className="cp-card-visual"><Visual active={active} /></div>
      <div className="cp-card-info">
        <div className="cp-card-meta">
          <span className="cp-card-tag" style={{ color: proj.color, background: `${proj.color}15` }}>{proj.tag}</span>
          {proj.live && <span className="cp-card-live"><span className="cp-live-dot" style={{ background: proj.color }} />Ao vivo</span>}
        </div>
        <strong className="cp-card-name">{proj.name}</strong>
        <code className="cp-card-stack">{proj.stack}</code>
        <p className="cp-card-desc">{proj.desc}</p>
        {proj.href && <span className="cp-card-link" style={{ color: proj.color }}>Ver projeto →</span>}
      </div>
    </>
  );

  if (proj.href) {
    return (
      <a className="cp-card" style={style} href={proj.href} target="_blank" rel="noopener noreferrer">
        {inner}
      </a>
    );
  }

  return <div className="cp-card" style={style}>{inner}</div>;
}

export function CreatorProjects() {
  const ref = useRef<HTMLDivElement>(null);
  const [active, setActive] = useState(false);

  useEffect(() => {
    const obs = new IntersectionObserver(([e]) => { if (e.isIntersecting) { setActive(true); obs.disconnect(); } }, { threshold: 0.15 });
    if (ref.current) obs.observe(ref.current);
    return () => obs.disconnect();
  }, []);

  return (
    <section className="zpld-section cp-section" ref={ref} aria-labelledby="projects-title">
      <div className="zpld-section-head" style={{ position: "relative", zIndex: 1 }}>
        <span>Trabalho real</span>
        <h2 id="projects-title">Projetos, nao prototipos.</h2>
        <p>Sistemas construidos e entregues. Cada um com problema real, stack definida e resultado concreto.</p>
      </div>
      <div className="cp-grid">
        {projects.map((p, i) => <ProjectCard key={p.name} proj={p} index={i} active={active} />)}
      </div>
    </section>
  );
}
