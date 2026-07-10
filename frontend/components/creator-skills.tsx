"use client";

import { useEffect, useRef, useState } from "react";

/* ── Visuais únicos por card ─────────────────────────── */

function VisualCsharp({ active }: { active: boolean }) {
  const lines = [
    { t: "keyword", v: "public async " }, { t: "type", v: "Task" }, { t: "plain", v: "<IActionResult> " }, { t: "fn", v: "GetOrders" }, { t: "plain", v: "()" },
    { t: "bracket", v: "{" },
    { t: "keyword", v: "  var " }, { t: "plain", v: "result = " }, { t: "keyword", v: "await " }, { t: "fn", v: "_service" }, { t: "plain", v: ".QueryAsync();" },
    { t: "keyword", v: "  return " }, { t: "fn", v: "Ok" }, { t: "plain", v: "(result);" },
    { t: "bracket", v: "}" },
  ];
  const [shown, setShown] = useState(0);
  useEffect(() => {
    if (!active) return;
    const t = setInterval(() => setShown(n => Math.min(n + 1, lines.length)), 120);
    return () => clearInterval(t);
  }, [active]);

  const grouped = [
    lines.slice(0, 5), lines.slice(5, 6), lines.slice(6, 11), lines.slice(11, 14), lines.slice(14),
  ];
  let total = 0;

  return (
    <div className="cs2-visual-code">
      {grouped.map((row, ri) => {
        const rowStart = total;
        total += row.length;
        const rowVisible = shown > rowStart;
        return (
          <div key={ri} className="cs2-code-row" style={{ opacity: rowVisible ? 1 : 0, transform: rowVisible ? "none" : "translateX(-8px)", transition: "opacity 0.3s, transform 0.3s" }}>
            <span className="cs2-ln">{ri + 1}</span>
            {row.map((t, ti) => (
              <span key={ti} className={`cs2-tok cs2-tok-${t.t}`}>{t.v}</span>
            ))}
          </div>
        );
      })}
      <span className="cs2-cursor">█</span>
    </div>
  );
}

function VisualPython({ active }: { active: boolean }) {
  const [angle, setAngle] = useState(0);
  useEffect(() => {
    if (!active) return;
    if ((window as any).__zpLite) return;
    let raf = 0;
    const animate = () => { setAngle(a => a + 0.8); raf = requestAnimationFrame(animate); };
    raf = requestAnimationFrame(animate);
    return () => cancelAnimationFrame(raf);
  }, [active]);
  const teeth = 8;
  const r = 22, ri = 15, tooth = 5;
  const bigGear = Array.from({ length: teeth }, (_, i) => {
    const a = (i / teeth) * Math.PI * 2 + (angle * Math.PI) / 180;
    const ao = ((i + 0.4) / teeth) * Math.PI * 2 + (angle * Math.PI) / 180;
    const x1 = Math.cos(a) * r, y1 = Math.sin(a) * r;
    const x2 = Math.cos(a) * (r + tooth), y2 = Math.sin(a) * (r + tooth);
    const x3 = Math.cos(ao) * (r + tooth), y3 = Math.sin(ao) * (r + tooth);
    const x4 = Math.cos(ao) * r, y4 = Math.sin(ao) * r;
    return `M ${x1} ${y1} L ${x2} ${y2} L ${x3} ${y3} L ${x4} ${y4}`;
  }).join(" ") + ` M 0 0 m -${r} 0 a ${r} ${r} 0 1 0 ${r * 2} 0 a ${r} ${r} 0 1 0 -${r * 2} 0`;

  const sr = 13, sri = 9, steeth = 6;
  const smallGear = Array.from({ length: steeth }, (_, i) => {
    const a = (i / steeth) * Math.PI * 2 - (angle * Math.PI) / 180;
    const ao = ((i + 0.4) / steeth) * Math.PI * 2 - (angle * Math.PI) / 180;
    const x1 = Math.cos(a) * sr, y1 = Math.sin(a) * sr;
    const x2 = Math.cos(a) * (sr + 4), y2 = Math.sin(a) * (sr + 4);
    const x3 = Math.cos(ao) * (sr + 4), y3 = Math.sin(ao) * (sr + 4);
    const x4 = Math.cos(ao) * sr, y4 = Math.sin(ao) * sr;
    return `M ${x1} ${y1} L ${x2} ${y2} L ${x3} ${y3} L ${x4} ${y4}`;
  }).join(" ") + ` M 0 0 m -${sr} 0 a ${sr} ${sr} 0 1 0 ${sr * 2} 0 a ${sr} ${sr} 0 1 0 -${sr * 2} 0`;

  return (
    <div className="cs2-visual-python">
      <svg viewBox="-60 -40 120 80" fill="none">
        <g opacity="0.85">
          <path d={bigGear} stroke="#7ec8ff" strokeWidth="1.2" fill="rgba(126,200,255,0.06)" />
          <circle r={ri} stroke="#7ec8ff" strokeWidth="0.8" fill="rgba(126,200,255,0.04)" />
          <circle r="4" fill="#7ec8ff" opacity="0.4" />
        </g>
        <g transform="translate(35,0)" opacity="0.7">
          <path d={smallGear} stroke="#7ec8ff" strokeWidth="1" fill="rgba(126,200,255,0.04)" />
          <circle r={sri} stroke="#7ec8ff" strokeWidth="0.6" fill="rgba(126,200,255,0.03)" />
          <circle r="3" fill="#7ec8ff" opacity="0.3" />
        </g>
        <text x="-28" y="5" fill="#7ec8ff" fontSize="9" fontFamily="monospace" opacity="0.5">PY</text>
      </svg>
    </div>
  );
}

function VisualSec({ active }: { active: boolean }) {
  const [ring, setRing] = useState(0);
  useEffect(() => {
    if (!active) return;
    if ((window as any).__zpLite) return;
    const t = setInterval(() => setRing(r => (r + 1) % 60), 50);
    return () => clearInterval(t);
  }, [active]);
  const blips = [{ cx: 28, cy: -18 }, { cx: -22, cy: 15 }, { cx: 10, cy: 25 }];
  const sweepAngle = (ring / 60) * 360;
  const rad = (sweepAngle * Math.PI) / 180;
  const lineX = Math.cos(rad - Math.PI / 2) * 32;
  const lineY = Math.sin(rad - Math.PI / 2) * 32;

  return (
    <div className="cs2-visual-sec">
      <svg viewBox="-50 -40 100 80" fill="none">
        {[10, 20, 30].map((r, i) => (
          <circle key={i} cx="0" cy="0" r={r} stroke="#ff7c7c" strokeWidth="0.7" opacity={0.15 + i * 0.08} />
        ))}
        <defs>
          <radialGradient id="sweep-grad" cx="50%" cy="50%" r="50%">
            <stop offset="0%" stopColor="#ff7c7c" stopOpacity="0.25" />
            <stop offset="100%" stopColor="#ff7c7c" stopOpacity="0" />
          </radialGradient>
        </defs>
        <path d={`M 0 0 L ${lineX} ${lineY} A 32 32 0 0 1 0 -32 Z`} fill="url(#sweep-grad)" />
        <line x1="0" y1="0" x2={lineX} y2={lineY} stroke="#ff7c7c" strokeWidth="1" opacity="0.7" />
        {blips.map((b, i) => {
          const bAngle = Math.atan2(b.cy, b.cx) * 180 / Math.PI + 90;
          const diff = ((sweepAngle - bAngle) % 360 + 360) % 360;
          const fade = diff < 60 ? 1 - diff / 60 : 0;
          return <circle key={i} cx={b.cx} cy={b.cy} r="3" fill="#ff7c7c" opacity={fade} />;
        })}
        <circle cx="0" cy="0" r="3" fill="#ff7c7c" opacity="0.9" />
        <circle cx="0" cy="0" r="7" stroke="#ff7c7c" strokeWidth="0.5" opacity="0.3" />
        <text x="-9" y="38" fill="#ff7c7c" fontSize="6" fontFamily="monospace" opacity="0.4">SEC · SCAN</text>
      </svg>
    </div>
  );
}

function VisualAI({ active }: { active: boolean }) {
  const [pulse, setPulse] = useState(0);
  useEffect(() => {
    if (!active) return;
    if ((window as any).__zpLite) return;
    let raf = 0;
    const animate = () => { setPulse(p => (p + 0.04) % (Math.PI * 2)); raf = requestAnimationFrame(animate); };
    raf = requestAnimationFrame(animate);
    return () => cancelAnimationFrame(raf);
  }, [active]);

  const layers = [[{ x: -44, y: 0 }], [{ x: -16, y: -24 }, { x: -16, y: 0 }, { x: -16, y: 24 }], [{ x: 14, y: -16 }, { x: 14, y: 8 }, { x: 14, y: 32 }], [{ x: 44, y: 0 }]];
  const edges: [number, number, number, number][] = [];
  for (let i = 0; i < layers.length - 1; i++)
    for (const a of layers[i])
      for (const b of layers[i + 1])
        edges.push([a.x, a.y, b.x, b.y]);

  return (
    <div className="cs2-visual-ai">
      <svg viewBox="-52 -38 104 76" fill="none">
        {edges.map(([x1, y1, x2, y2], i) => (
          <line key={i} x1={x1} y1={y1} x2={x2} y2={y2} stroke="#6ddf9d" strokeWidth="0.8"
            opacity={0.15 + 0.2 * Math.abs(Math.sin(pulse + i * 0.4))} />
        ))}
        {layers.flatMap((layer, li) =>
          layer.map((n, ni) => {
            const glow = 0.4 + 0.6 * Math.abs(Math.sin(pulse + li * 0.8 + ni * 0.5));
            return (
              <circle key={`${li}-${ni}`} cx={n.x} cy={n.y} r={4 + glow * 1.5}
                fill="rgba(109,223,157,0.12)" stroke="#6ddf9d" strokeWidth="1"
                opacity={glow} />
            );
          })
        )}
      </svg>
    </div>
  );
}

function VisualLinux({ active }: { active: boolean }) {
  const cmds = ["$ ssh deploy@vps -i ~/.ssh/key", "→ Connected to 216.238.105.103", "$ systemctl restart zeropaper-backend", "✓ Active: running (2s ago)", "$ nginx -t && nginx -s reload", "✓ nginx: configuration OK"];
  const [shown, setShown] = useState(0);
  useEffect(() => {
    if (!active) return;
    const t = setInterval(() => setShown(n => n < cmds.length ? n + 1 : 1), 1100);
    return () => clearInterval(t);
  }, [active]);
  return (
    <div className="cs2-visual-linux">
      <div className="cs2-linux-bar"><span /><span /><span /><code>zsh</code></div>
      <div className="cs2-linux-body">
        {cmds.slice(0, shown).map((c, i) => (
          <div key={i} className={c.startsWith("✓") ? "cs2-linux-ok" : c.startsWith("→") ? "cs2-linux-info" : "cs2-linux-cmd"}>
            {c}
          </div>
        ))}
        <span className="cs2-linux-cursor">█</span>
      </div>
    </div>
  );
}

function VisualPiano({ active }: { active: boolean }) {
  const [t, setT] = useState(0);
  useEffect(() => {
    if (!active) return;
    if ((window as any).__zpLite) return;
    let raf = 0;
    const animate = () => { setT(n => n + 0.06); raf = requestAnimationFrame(animate); };
    raf = requestAnimationFrame(animate);
    return () => cancelAnimationFrame(raf);
  }, [active]);
  const bars = 16;
  const freqs = [1, 1.7, 2.3, 0.8, 3.1, 1.4, 2.7, 0.6, 1.9, 2.5, 1.1, 3.3, 0.9, 2.0, 1.6, 2.8];
  const whites = [0, 1, 3, 5, 7, 8, 10, 12];
  const blacks = [2, 4, 6, 9, 11];

  return (
    <div className="cs2-visual-piano">
      <svg viewBox="0 0 100 60" fill="none">
        {/* Wave */}
        <polyline
          points={Array.from({ length: bars }, (_, i) => {
            const h = 8 + 12 * Math.abs(Math.sin(t * freqs[i] + i * 0.5));
            const x = 3 + i * 5.9;
            const y = 28 - h / 2;
            return `${x},${y}`;
          }).join(" ")}
          stroke="#d4a843" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round" opacity="0.7"
        />
        <polyline
          points={Array.from({ length: bars }, (_, i) => {
            const h = 8 + 12 * Math.abs(Math.sin(t * freqs[i] + i * 0.5));
            const x = 3 + i * 5.9;
            const y = 28 + h / 2;
            return `${x},${y}`;
          }).join(" ")}
          stroke="#d4a843" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round" opacity="0.3"
        />
        {/* Piano keys */}
        {whites.map((_, i) => (
          <rect key={i} x={2 + i * 12} y="40" width="10" height="18" rx="2"
            fill="rgba(255,255,255,0.08)" stroke="rgba(255,255,255,0.15)" strokeWidth="0.8" />
        ))}
        {blacks.map((_, i) => {
          const positions = [9, 21, 45, 57, 69];
          return (
            <rect key={i} x={positions[i]} y="40" width="7" height="11" rx="1.5"
              fill="rgba(212,168,67,0.3)" stroke="#d4a843" strokeWidth="0.6" />
          );
        })}
      </svg>
    </div>
  );
}

const skills = [
  { tag: "</>", label: "C# / ASP.NET Core",  sub: "APIs, EF Core, arquitetura, logica de negocio", color: "#6ddf9d", Visual: VisualCsharp },
  { tag: "PY",  label: "Python & Automacao",  sub: "Scripts, ETL, workflows e IA aplicada",         color: "#7ec8ff", Visual: VisualPython },
  { tag: "SEC", label: "Seguranca Web",        sub: "Burp Suite, enumeracao, pentest e defesa",       color: "#ff7c7c", Visual: VisualSec },
  { tag: "AI",  label: "IA Workflows",         sub: "Modelos integrados em produto real",             color: "#6ddf9d", Visual: VisualAI },
  { tag: "VPS", label: "Linux / Deploy",       sub: "VPS, systemd, Nginx, infraestrutura",            color: "#b0ffa0", Visual: VisualLinux },
  { tag: "♪",   label: "Piano",                sub: "Disciplina criativa fora do codigo",             color: "#d4a843", Visual: VisualPiano },
];

function SkillCard({ skill, index, active }: { skill: typeof skills[0]; index: number; active: boolean }) {
  const [progress, setProgress] = useState(0);
  const { Visual } = skill;

  useEffect(() => {
    if (!active) return;
    const delay = index * 110;
    const timer = setTimeout(() => {
      let start: number | null = null;
      const animate = (ts: number) => {
        if (!start) start = ts;
        const p = Math.min((ts - start) / 750, 1);
        setProgress(1 - Math.pow(1 - p, 3));
        if (p < 1) requestAnimationFrame(animate);
      };
      requestAnimationFrame(animate);
    }, delay);
    return () => clearTimeout(timer);
  }, [active, index]);

  return (
    <div className="cs2-card" style={{ "--cs2-color": skill.color, opacity: progress, transform: `translateY(${(1 - progress) * 30}px)` } as React.CSSProperties}>
      <div className="cs2-card-top-line" style={{ background: `linear-gradient(90deg, ${skill.color}, transparent)`, opacity: progress }} />
      <div className="cs2-card-visual-wrap">
        <Visual active={active} />
      </div>
      <div className="cs2-card-footer">
        <span className="cs2-tag" style={{ color: skill.color, background: `${skill.color}15` }}>{skill.tag}</span>
        <strong className="cs2-label">{skill.label}</strong>
        <p className="cs2-sub">{skill.sub}</p>
      </div>
    </div>
  );
}

export function CreatorSkills() {
  const ref = useRef<HTMLDivElement>(null);
  const [active, setActive] = useState(false);

  useEffect(() => {
    const obs = new IntersectionObserver(([e]) => { if (e.isIntersecting) { setActive(true); obs.disconnect(); } }, { threshold: 0.15 });
    if (ref.current) obs.observe(ref.current);
    return () => obs.disconnect();
  }, []);

  return (
    <section className="zpld-section cs2-section" ref={ref} aria-labelledby="skills2-title">
      <div className="zpld-section-head" style={{ position: "relative", zIndex: 1 }}>
        <span>Core areas</span>
        <h2 id="skills2-title">Onde gero valor.</h2>
        <p>Backend, automacao, seguranca e IA — combinados em sistemas que funcionam em producao.</p>
      </div>
      <div className="cs2-grid">
        {skills.map((s, i) => <SkillCard key={s.label} skill={s} index={i} active={active} />)}
      </div>
    </section>
  );
}
