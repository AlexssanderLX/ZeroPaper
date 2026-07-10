"use client";

import { useEffect } from "react";

/**
 * Revela elementos .zp-lp-reveal ao entrarem na viewport.
 * Universal (IntersectionObserver) — funciona em todos os navegadores,
 * diferente de animation-timeline: view() (somente Chromium).
 */
export function LandingMotion() {
  useEffect(() => {
    const elements = Array.from(document.querySelectorAll<HTMLElement>(".zp-lp-reveal"));

    if (elements.length === 0) {
      return;
    }

    const prefersReduced = window.matchMedia("(prefers-reduced-motion: reduce)").matches;

    if (prefersReduced || typeof IntersectionObserver === "undefined") {
      elements.forEach((element) => element.classList.add("is-revealed"));
      return;
    }

    const observer = new IntersectionObserver(
      (entries) => {
        entries.forEach((entry) => {
          if (entry.isIntersecting) {
            entry.target.classList.add("is-revealed");
            observer.unobserve(entry.target);
          }
        });
      },
      { threshold: 0.16, rootMargin: "0px 0px -8% 0px" },
    );

    elements.forEach((element) => observer.observe(element));

    return () => observer.disconnect();
  }, []);

  return null;
}
