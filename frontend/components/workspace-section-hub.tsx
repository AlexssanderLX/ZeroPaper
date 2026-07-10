"use client";

import Link from "next/link";

type WorkspaceSectionCard = {
  href: string;
  eyebrow: string;
  title: string;
  description: string;
  value: string;
  label: string;
  tone?: "default" | "ready" | "pending";
};

export function WorkspaceSectionHub({
  cards,
}: {
  cards: WorkspaceSectionCard[];
}) {
  return (
    <section className="module-grid module-subentry-grid">
      {cards.map((card) => (
        <Link
          key={card.href}
          className={`surface-card module-card interactive-card module-entry-link module-subentry-link module-subentry-link-${card.tone ?? "default"}`}
          href={card.href}
        >
          <div className="module-card-head module-subentry-head">
            <div className="module-subentry-headline">
              <span className="eyebrow">{card.eyebrow}</span>
              <h2>{card.title}</h2>
            </div>
            <span className="module-card-arrow" aria-hidden="true">
              {"\u2197"}
            </span>
          </div>
          <p className="module-subentry-copy">{card.description}</p>
          <div className="module-card-metric">
            <strong>{card.value}</strong>
            <span>{card.label}</span>
          </div>
        </Link>
      ))}
    </section>
  );
}
