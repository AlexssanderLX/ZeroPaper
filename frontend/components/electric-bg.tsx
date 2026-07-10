"use client";

import { useEffect, useRef } from "react";

interface Particle {
  x: number;
  y: number;
  vx: number;
  vy: number;
  r: number;
  opacity: number;
}

interface Arc {
  ax: number; ay: number;
  bx: number; by: number;
  life: number;
  maxLife: number;
  color: string;
}

export function ElectricBg() {
  const canvasRef = useRef<HTMLCanvasElement>(null);

  useEffect(() => {
    const canvas = canvasRef.current;
    if (!canvas) return;

    if (window.matchMedia("(prefers-reduced-motion: reduce)").matches) return;

    const ctx = canvas.getContext("2d");
    if (!ctx) return;

    let raf: number;
    let w = 0;
    let h = 0;
    const PARTICLE_COUNT = Math.min(60, Math.floor(window.innerWidth / 22));
    const particles: Particle[] = [];
    const arcs: Arc[] = [];

    const resize = () => {
      w = canvas.width = window.innerWidth;
      h = canvas.height = window.innerHeight;
    };

    const spawnParticle = (): Particle => ({
      x: Math.random() * w,
      y: Math.random() * h,
      vx: (Math.random() - 0.5) * 0.28,
      vy: (Math.random() - 0.5) * 0.28,
      r: Math.random() * 1.2 + 0.4,
      opacity: Math.random() * 0.4 + 0.12,
    });

    resize();
    for (let i = 0; i < PARTICLE_COUNT; i++) particles.push(spawnParticle());

    let tick = 0;

    const draw = () => {
      tick++;
      ctx.clearRect(0, 0, w, h);

      // Move particles
      for (const p of particles) {
        p.x += p.vx;
        p.y += p.vy;
        if (p.x < -20) p.x = w + 20;
        if (p.x > w + 20) p.x = -20;
        if (p.y < -20) p.y = h + 20;
        if (p.y > h + 20) p.y = -20;
      }

      // Spawn arcs occasionally
      if (tick % 18 === 0 && arcs.length < 8) {
        const a = particles[Math.floor(Math.random() * particles.length)];
        let nearest: Particle | null = null;
        let minDist = 200;
        for (const b of particles) {
          if (b === a) continue;
          const dx = b.x - a.x;
          const dy = b.y - a.y;
          const d = Math.sqrt(dx * dx + dy * dy);
          if (d < minDist) { minDist = d; nearest = b; }
        }
        if (nearest) {
          const isGold = Math.random() < 0.28;
          arcs.push({
            ax: a.x, ay: a.y,
            bx: nearest.x, by: nearest.y,
            life: 0,
            maxLife: 22 + Math.floor(Math.random() * 18),
            color: isGold ? "rgba(255,208,143," : "rgba(109,223,157,",
          });
        }
      }

      // Draw arcs
      for (let i = arcs.length - 1; i >= 0; i--) {
        const arc = arcs[i];
        arc.life++;
        const progress = arc.life / arc.maxLife;
        const alpha = progress < 0.5 ? progress * 2 : (1 - progress) * 2;

        ctx.beginPath();
        // Jagged arc via random midpoints
        const segments = 6;
        const pts: [number, number][] = [[arc.ax, arc.ay]];
        for (let s = 1; s < segments; s++) {
          const t = s / segments;
          const mx = arc.ax + (arc.bx - arc.ax) * t;
          const my = arc.ay + (arc.by - arc.ay) * t;
          const jitter = 8 * (1 - Math.abs(t - 0.5) * 2);
          pts.push([mx + (Math.random() - 0.5) * jitter, my + (Math.random() - 0.5) * jitter]);
        }
        pts.push([arc.bx, arc.by]);

        ctx.moveTo(pts[0][0], pts[0][1]);
        for (let j = 1; j < pts.length; j++) ctx.lineTo(pts[j][0], pts[j][1]);

        ctx.strokeStyle = arc.color + (alpha * 0.55) + ")";
        ctx.lineWidth = 0.7;
        ctx.shadowColor = arc.color + "0.6)";
        ctx.shadowBlur = 6;
        ctx.stroke();
        ctx.shadowBlur = 0;

        if (arc.life >= arc.maxLife) arcs.splice(i, 1);
      }

      // Draw particles
      for (const p of particles) {
        ctx.beginPath();
        ctx.arc(p.x, p.y, p.r, 0, Math.PI * 2);
        ctx.fillStyle = `rgba(109,223,157,${p.opacity})`;
        ctx.fill();
      }

      raf = requestAnimationFrame(draw);
    };

    window.addEventListener("resize", resize);
    raf = requestAnimationFrame(draw);

    return () => {
      cancelAnimationFrame(raf);
      window.removeEventListener("resize", resize);
    };
  }, []);

  return (
    <canvas
      ref={canvasRef}
      aria-hidden="true"
      style={{
        position: "fixed",
        inset: 0,
        zIndex: -1,
        pointerEvents: "none",
        opacity: 0.7,
      }}
    />
  );
}
