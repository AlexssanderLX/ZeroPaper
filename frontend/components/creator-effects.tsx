"use client";

import { useEffect, useRef } from "react";

export function CreatorEffects() {
  const canvasRef = useRef<HTMLCanvasElement>(null);

  useEffect(() => {
    if ((window as any).__zpLite) return;
    if (window.matchMedia("(prefers-reduced-motion: reduce)").matches) return;

    const canvas = canvasRef.current!;
    const ctx = canvas.getContext("2d")!;
    let raf = 0;
    let t = 0;

    const resize = () => {
      canvas.width = window.innerWidth;
      canvas.height = window.innerHeight;
    };
    resize();
    window.addEventListener("resize", resize);

    // Floating code fragments
    const fragments = Array.from({ length: 28 }, (_, i) => ({
      x: Math.random() * window.innerWidth,
      y: Math.random() * window.innerHeight,
      vx: (Math.random() - 0.5) * 0.3,
      vy: -0.15 - Math.random() * 0.25,
      alpha: Math.random() * 0.18 + 0.04,
      size: Math.random() * 5 + 8,
      text: ["</>", "{ }", "=>", "//", "01", "10", "&&", "||", "SEC", "CTF", "API", "::"][i % 12],
      color: i % 4 === 0 ? "#6ddf9d" : i % 4 === 1 ? "#d4a843" : "#ffffff",
    }));

    // Horizontal scan line
    let scanY = -40;

    const draw = () => {
      ctx.clearRect(0, 0, canvas.width, canvas.height);
      t += 0.008;

      // Animated radial pulses from bottom-left
      for (let i = 0; i < 3; i++) {
        const r = ((t * 60 + i * 140) % 500);
        const alpha = (1 - r / 500) * 0.06;
        ctx.beginPath();
        ctx.arc(canvas.width * 0.12, canvas.height * 0.88, r, 0, Math.PI * 2);
        ctx.strokeStyle = `rgba(109,223,157,${alpha})`;
        ctx.lineWidth = 1.5;
        ctx.stroke();
      }

      // Floating code fragments
      for (const f of fragments) {
        f.x += f.vx;
        f.y += f.vy;
        if (f.y < -20) { f.y = canvas.height + 20; f.x = Math.random() * canvas.width; }
        if (f.x < -30) f.x = canvas.width + 30;
        if (f.x > canvas.width + 30) f.x = -30;

        ctx.save();
        ctx.globalAlpha = f.alpha * (0.7 + 0.3 * Math.sin(t * 1.2 + f.x));
        ctx.font = `${f.size}px 'Courier New', monospace`;
        ctx.fillStyle = f.color;
        ctx.fillText(f.text, f.x, f.y);
        ctx.restore();
      }

      // Moving scan line
      scanY = (scanY + 0.6) % (canvas.height + 40);
      const scanGrad = ctx.createLinearGradient(0, scanY - 20, 0, scanY + 20);
      scanGrad.addColorStop(0, "rgba(109,223,157,0)");
      scanGrad.addColorStop(0.5, "rgba(109,223,157,0.04)");
      scanGrad.addColorStop(1, "rgba(109,223,157,0)");
      ctx.fillStyle = scanGrad;
      ctx.fillRect(0, scanY - 20, canvas.width, 40);

      // Corner grid glow
      const gridGlow = ctx.createRadialGradient(
        canvas.width, 0, 0,
        canvas.width, 0, canvas.width * 0.5
      );
      gridGlow.addColorStop(0, "rgba(36,79,125,0.1)");
      gridGlow.addColorStop(1, "rgba(36,79,125,0)");
      ctx.fillStyle = gridGlow;
      ctx.fillRect(0, 0, canvas.width, canvas.height);

      raf = requestAnimationFrame(draw);
    };

    draw();
    return () => {
      cancelAnimationFrame(raf);
      window.removeEventListener("resize", resize);
    };
  }, []);

  return (
    <canvas
      ref={canvasRef}
      aria-hidden="true"
      style={{ position: "fixed", inset: 0, zIndex: -1, pointerEvents: "none", opacity: 0.85 }}
    />
  );
}
