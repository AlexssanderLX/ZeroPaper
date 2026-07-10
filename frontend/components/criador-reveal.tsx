"use client";

import { useEffect } from "react";

export function CriadorReveal() {
  useEffect(() => {
    if (window.matchMedia("(prefers-reduced-motion: reduce)").matches) return;

    const observe = (selector: string, cls: string, delay = 0, threshold = 0.18) => {
      const els = document.querySelectorAll<HTMLElement>(selector);
      const obs = new IntersectionObserver((entries) => {
        entries.forEach((e) => {
          if (!e.isIntersecting) return;
          const el = e.target as HTMLElement;
          const idx = el.dataset.revealIdx ? parseInt(el.dataset.revealIdx) : 0;
          setTimeout(() => el.classList.add(cls), delay + idx * 80);
          obs.unobserve(el);
        });
      }, { threshold });
      els.forEach((el, i) => { el.dataset.revealIdx = String(i); obs.observe(el); });
    };

    // Headings — slide up big
    observe(".zpld-criador .zpld-section-head", "cr-head-visible");
    // Hero text elements
    observe(".zpld-criador-hero-text > *", "cr-item-visible");
    // Terminal
    observe(".zpld-criador-terminal", "cr-slide-right-visible");
    // Stats
    observe(".zpld-criador-stat", "cr-stat-visible");
    // Project cards
    observe(".zpld-criador-project-card", "cr-card-visible");
    // CTA section
    observe(".zpld-final-inner", "cr-head-visible");

  }, []);

  return null;
}
