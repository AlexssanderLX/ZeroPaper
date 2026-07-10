"use client";

import { useEffect, useRef, useState } from "react";

const steps = [
  {
    n: "01",
    title: "Entender",
    text: "Analiso o problema real antes de escrever qualquer linha.",
    icon: (
      // Magnifying glass with scan line
      <svg width="28" height="28" viewBox="0 0 28 28" fill="none" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round">
        <circle cx="12" cy="12" r="8" />
        <path d="M18 18l6 6" />
        <path d="M8 12h8" className="cm-scan" />
      </svg>
    ),
    color: "#6ddf9d",
    detail: "Constraints · Objetivos · Contexto",
  },
  {
    n: "02",
    title: "Estruturar",
    text: "Desenho o fluxo, responsabilidades e manutencao futura.",
    icon: (
      // Blueprint/layers
      <svg width="28" height="28" viewBox="0 0 28 28" fill="none" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round">
        <rect x="4" y="18" width="20" height="4" rx="1" />
        <rect x="6" y="12" width="16" height="4" rx="1" />
        <rect x="9" y="6"  width="10" height="4" rx="1" />
      </svg>
    ),
    color: "#7ec8ff",
    detail: "Arquitetura · Fluxo · Módulos",
  },
  {
    n: "03",
    title: "Construir",
    text: "Implemento com clareza, testes e melhoria continua.",
    icon: (
      // Code brackets
      <svg width="28" height="28" viewBox="0 0 28 28" fill="none" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round">
        <path d="M9 8L3 14l6 6" />
        <path d="M19 8l6 6-6 6" />
        <path d="M16 5l-4 18" />
      </svg>
    ),
    color: "#d4a843",
    detail: "C# · Python · APIs · Testes",
  },
  {
    n: "04",
    title: "Hardening",
    text: "Valido, refatoro e reduzo riscos para entregar algo confiavel.",
    icon: (
      // Shield with check
      <svg width="28" height="28" viewBox="0 0 28 28" fill="none" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round">
        <path d="M14 3L4 7v7c0 7 6 11 10 12 4-1 10-5 10-12V7L14 3z" />
        <path d="M9 14l3 3 6-6" />
      </svg>
    ),
    color: "#ff7c7c",
    detail: "Segurança · Refatoracao · Deploy",
  },
];

function MethodCard({ step, index, active }: { step: typeof steps[0]; index: number; active: boolean }) {
  const [progress, setProgress] = useState(0);

  useEffect(() => {
    if (!active) return;
    const delay = index * 180;
    const timer = setTimeout(() => {
      let start: number | null = null;
      const duration = 900;
      const animate = (ts: number) => {
        if (!start) start = ts;
        const p = Math.min((ts - start) / duration, 1);
        // ease out cubic
        setProgress(1 - Math.pow(1 - p, 3));
        if (p < 1) requestAnimationFrame(animate);
      };
      requestAnimationFrame(animate);
    }, delay);
    return () => clearTimeout(timer);
  }, [active, index]);

  const isLast = index === steps.length - 1;

  return (
    <div
      className="cm-card"
      style={{
        "--cm-color": step.color,
        opacity: progress,
        transform: `translateY(${(1 - progress) * 28}px)`,
      } as React.CSSProperties}
    >
      {/* Top bar progress */}
      <div className="cm-card-bar">
        <div className="cm-card-bar-fill" style={{ width: `${progress * 100}%`, background: step.color }} />
      </div>

      {/* Number */}
      <span className="cm-card-n" style={{ color: step.color, opacity: 0.22 + progress * 0.18 }}>
        {step.n}
      </span>

      {/* Icon with glow */}
      <div className="cm-card-icon" style={{ color: step.color, boxShadow: `0 0 ${progress * 24}px ${step.color}30` }}>
        {step.icon}
      </div>

      <strong className="cm-card-title">{step.title}</strong>
      <p className="cm-card-text">{step.text}</p>
      <span className="cm-card-detail" style={{ color: step.color }}>{step.detail}</span>

      {/* Connector arrow (not on last) */}
      {!isLast && (
        <div
          className="cm-arrow"
          style={{ opacity: progress, color: step.color }}
          aria-hidden="true"
        >
          <svg width="20" height="20" viewBox="0 0 20 20" fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round">
            <path d="M4 10h12M12 6l4 4-4 4" />
          </svg>
        </div>
      )}
    </div>
  );
}

export function CreatorMethod() {
  const sectionRef = useRef<HTMLDivElement>(null);
  const canvasRef = useRef<HTMLCanvasElement>(null);
  const [active, setActive] = useState(false);

  useEffect(() => {
    const obs = new IntersectionObserver(
      ([e]) => { if (e.isIntersecting) { setActive(true); obs.disconnect(); } },
      { threshold: 0.25 }
    );
    if (sectionRef.current) obs.observe(sectionRef.current);
    return () => obs.disconnect();
  }, []);

  // Animated background canvas
  useEffect(() => {
    if (!active) return;
    if ((window as any).__zpLite) return;
    if (window.matchMedia("(prefers-reduced-motion: reduce)").matches) return;
    const canvas = canvasRef.current!;
    const ctx = canvas.getContext("2d")!;
    let raf = 0;
    let t = 0;

    const resize = () => {
      const rect = canvas.parentElement!.getBoundingClientRect();
      canvas.width = rect.width;
      canvas.height = rect.height;
    };
    resize();
    window.addEventListener("resize", resize);

    // Particles flowing left→right (progress metaphor)
    const particles = Array.from({ length: 40 }, () => ({
      x: Math.random() * canvas.width,
      y: Math.random() * canvas.height,
      vx: 0.3 + Math.random() * 0.5,
      vy: (Math.random() - 0.5) * 0.2,
      r: Math.random() * 1.8 + 0.5,
      alpha: Math.random() * 0.4 + 0.1,
      color: steps[Math.floor(Math.random() * steps.length)].color,
    }));

    const draw = () => {
      ctx.clearRect(0, 0, canvas.width, canvas.height);
      t += 0.01;

      // Horizontal progress gradient sweep
      const sweep = ctx.createLinearGradient(0, 0, canvas.width, 0);
      sweep.addColorStop(0,   "rgba(109,223,157,0.03)");
      sweep.addColorStop(0.33,"rgba(126,200,255,0.03)");
      sweep.addColorStop(0.66,"rgba(212,168,67,0.03)");
      sweep.addColorStop(1,   "rgba(255,124,124,0.03)");
      ctx.fillStyle = sweep;
      ctx.fillRect(0, 0, canvas.width, canvas.height);

      // Flowing particles
      for (const p of particles) {
        p.x += p.vx;
        p.y += p.vy;
        if (p.x > canvas.width + 4) { p.x = -4; p.y = Math.random() * canvas.height; }

        ctx.beginPath();
        ctx.arc(p.x, p.y, p.r, 0, Math.PI * 2);
        ctx.fillStyle = p.color;
        ctx.globalAlpha = p.alpha * (0.6 + 0.4 * Math.sin(t + p.x * 0.02));
        ctx.fill();
        ctx.globalAlpha = 1;
      }

      // Horizontal flow line
      const lineY = canvas.height * 0.5 + Math.sin(t * 0.4) * 12;
      const lineGrad = ctx.createLinearGradient(0, 0, canvas.width, 0);
      lineGrad.addColorStop(0,    "rgba(109,223,157,0)");
      lineGrad.addColorStop(0.1,  "rgba(109,223,157,0.15)");
      lineGrad.addColorStop(0.35, "rgba(126,200,255,0.15)");
      lineGrad.addColorStop(0.65, "rgba(212,168,67,0.15)");
      lineGrad.addColorStop(0.9,  "rgba(255,124,124,0.15)");
      lineGrad.addColorStop(1,    "rgba(255,124,124,0)");
      ctx.beginPath();
      ctx.moveTo(0, lineY);
      ctx.lineTo(canvas.width, lineY);
      ctx.strokeStyle = lineGrad;
      ctx.lineWidth = 1;
      ctx.stroke();

      raf = requestAnimationFrame(draw);
    };

    draw();
    return () => { cancelAnimationFrame(raf); window.removeEventListener("resize", resize); };
  }, [active]);

  return (
    <section className="zpld-section cm-section" ref={sectionRef} aria-labelledby="method-section-title">
      <canvas ref={canvasRef} className="cm-canvas" aria-hidden="true" />

      <div className="zpld-section-head" style={{ position: "relative", zIndex: 1 }}>
        <span>Processo</span>
        <h2 id="method-section-title">Entender → Estruturar → Construir → Hardening.</h2>
        <p>Cada etapa tem proposito. Sem atalhos, sem gambiarras.</p>
      </div>

      <div className="cm-grid" style={{ position: "relative", zIndex: 1 }}>
        {steps.map((step, i) => (
          <MethodCard key={step.n} step={step} index={i} active={active} />
        ))}
      </div>
    </section>
  );
}
