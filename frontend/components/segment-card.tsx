import Link from "next/link";
import type React from "react";

/* ── Ícones SVG por chave de segmento ── */
export const segmentIconMap: Record<string, React.ReactNode> = {
  restaurant: (
    <svg width="22" height="22" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.7" strokeLinecap="round" strokeLinejoin="round">
      <path d="M3 11l1-9h16l1 9" />
      <path d="M3 11a9 9 0 0 0 18 0" />
      <path d="M12 11v10" />
      <path d="M8 21h8" />
    </svg>
  ),
  retail: (
    <svg width="22" height="22" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.7" strokeLinecap="round" strokeLinejoin="round">
      <path d="M6 2L3 6v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2V6l-3-4z" />
      <line x1="3" y1="6" x2="21" y2="6" />
      <path d="M16 10a4 4 0 0 1-8 0" />
    </svg>
  ),
  petshop: (
    <svg width="22" height="22" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.7" strokeLinecap="round" strokeLinejoin="round">
      <path d="M10 5.172C10 3.782 8.423 2.679 6.5 3c-2.823.47-4.113 6.006-4 7 .08.703 1.725 1.722 3.656 1 1.261-.472 1.96-1.1 2.344-1.66" />
      <path d="M14 5.172C14 3.782 15.577 2.679 17.5 3c2.823.47 4.113 6.006 4 7-.08.703-1.725 1.722-3.656 1-1.261-.472-1.96-1.1-2.344-1.66" />
      <path d="M8 14v.5" />
      <path d="M16 14v.5" />
      <path d="M11.25 16.25h1.5L12 17l-.75-.75z" />
      <path d="M4.42 11.247A13.15 13.15 0 0 0 4 14.556C4 18.728 7.582 21 12 21s8-2.272 8-6.444c0-1.061-.162-2.2-.493-3.309" />
    </svg>
  ),
  technical: (
    <svg width="22" height="22" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.7" strokeLinecap="round" strokeLinejoin="round">
      <path d="M14.7 6.3a1 1 0 0 0 0 1.4l1.6 1.6a1 1 0 0 0 1.4 0l3.77-3.77a6 6 0 0 1-7.94 7.94l-6.91 6.91a2.12 2.12 0 0 1-3-3l6.91-6.91a6 6 0 0 1 7.94-7.94l-3.76 3.76z" />
    </svg>
  ),
  auto: (
    <svg width="22" height="22" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.7" strokeLinecap="round" strokeLinejoin="round">
      <path d="M5 17H3a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h11l5 5v9a2 2 0 0 1-2 2h-2" />
      <circle cx="7.5" cy="17.5" r="2.5" />
      <circle cx="17.5" cy="17.5" r="2.5" />
      <path d="M14 3v5h5" />
    </svg>
  ),
  custom: (
    <svg width="22" height="22" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.7" strokeLinecap="round" strokeLinejoin="round">
      <rect x="3" y="3" width="7" height="7" rx="1" />
      <rect x="14" y="3" width="7" height="7" rx="1" />
      <rect x="3" y="14" width="7" height="7" rx="1" />
      <path d="M14 17.5h7M17.5 14v7" />
    </svg>
  ),
};

type Props = {
  segKey: string;
  name: string;
  description: string;
  modules: string[];
  available: boolean;
  href: string;
};

export function SegmentCard({ segKey, name, description, modules, available, href }: Props) {
  return (
    <article className={`zp-lp-seg-card zp-lp-reveal${available ? " is-available" : " is-unavailable"}`}>
      <div className="zp-lp-seg-head">
        <span className="zpld-seg-svg-icon" aria-hidden="true">
          {segmentIconMap[segKey] ?? segmentIconMap.custom}
        </span>
        <span className={`zp-lp-seg-status ${available ? "zp-lp-status-available" : "zp-lp-status-unavailable"}`}>
          {available ? "Disponível" : "Indisponível"}
        </span>
      </div>

      <strong>{name}</strong>
      <p>{description}</p>

      {modules.length > 0 && (
        <div className="zp-lp-seg-modules">
          {modules.map((m) => (
            <span key={m} className="zp-lp-seg-mod">{m}</span>
          ))}
        </div>
      )}

      {available ? (
        <Link href={href} className="zp-lp-seg-cta">
          Ver planos →
        </Link>
      ) : (
        <a
          href={`/contato?assunto=${encodeURIComponent(name)}`}
          className="zp-lp-seg-cta zp-lp-seg-cta-muted"
        >
          Entrar em lista de espera →
        </a>
      )}
    </article>
  );
}
