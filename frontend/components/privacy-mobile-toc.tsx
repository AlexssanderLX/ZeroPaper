"use client";

import { useState, useEffect } from "react";

const toc = [
  { id: "s1",  label: "Quem somos" },
  { id: "s2",  label: "Dados coletados" },
  { id: "s3",  label: "Finalidade e base legal" },
  { id: "s4",  label: "Compartilhamento" },
  { id: "s5",  label: "Armazenamento e seguranca" },
  { id: "s6",  label: "Retencao de dados" },
  { id: "s7",  label: "Seus direitos (LGPD)" },
  { id: "s8",  label: "Cookies" },
  { id: "s9",  label: "Menores de idade" },
  { id: "s10", label: "Alteracoes" },
  { id: "s11", label: "Contato" },
];

export function PrivacyMobileToc() {
  const [open, setOpen] = useState(false);
  const [active, setActive] = useState("s1");

  useEffect(() => {
    const obs = new IntersectionObserver(
      (entries) => {
        for (const e of entries) {
          if (e.isIntersecting) setActive(e.target.id);
        }
      },
      { rootMargin: "-30% 0px -60% 0px", threshold: 0 }
    );
    toc.forEach(({ id }) => {
      const el = document.getElementById(id);
      if (el) obs.observe(el);
    });
    return () => obs.disconnect();
  }, []);

  const handleClick = (id: string) => {
    setOpen(false);
    document.getElementById(id)?.scrollIntoView({ behavior: "smooth", block: "start" });
  };

  const current = toc.find((t) => t.id === active);

  return (
    <div className="priv-mtoc" data-open={open}>
      <button
        className="priv-mtoc-trigger"
        onClick={() => setOpen((o) => !o)}
        aria-expanded={open}
        aria-label="Indice da pagina"
      >
        <span className="priv-mtoc-icon" aria-hidden="true">
          <svg width="16" height="16" viewBox="0 0 16 16" fill="none">
            <rect x="2" y="3" width="12" height="1.5" rx="0.75" fill="currentColor" />
            <rect x="2" y="7.25" width="8" height="1.5" rx="0.75" fill="currentColor" />
            <rect x="2" y="11.5" width="10" height="1.5" rx="0.75" fill="currentColor" />
          </svg>
        </span>
        <span className="priv-mtoc-current">{current?.label ?? "Nesta pagina"}</span>
        <span className="priv-mtoc-chevron" aria-hidden="true">
          <svg width="12" height="12" viewBox="0 0 12 12" fill="none">
            <path d="M2 4l4 4 4-4" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round" />
          </svg>
        </span>
      </button>

      {open && (
        <nav className="priv-mtoc-menu" aria-label="Secoes">
          {toc.map((t, i) => (
            <button
              key={t.id}
              className={`priv-mtoc-item ${active === t.id ? "priv-mtoc-item-active" : ""}`}
              onClick={() => handleClick(t.id)}
            >
              <span className="priv-mtoc-n">{String(i + 1).padStart(2, "0")}</span>
              {t.label}
            </button>
          ))}
        </nav>
      )}
    </div>
  );
}
