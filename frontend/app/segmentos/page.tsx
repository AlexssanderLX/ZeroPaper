import type { Metadata } from "next";
import Link from "next/link";
import { PublicSiteHeader } from "@/components/public-site-header";
import { LandingMotion } from "@/components/landing-motion";
import { ElectricBg } from "@/components/electric-bg";
import { SegmentCard } from "@/components/segment-card";
import { businessSegments } from "@/lib/landing-data";
import { fetchSegmentAvailability } from "@/lib/segment-availability";

export const metadata: Metadata = {
  title: "Segmentos | ZeroPaper",
  description:
    "O ZeroPaper e configuravel para restaurantes, varejo, pet shop, assistencia tecnica, oficinas e outros negocios. Escolha o segmento para ver os planos.",
  alternates: { canonical: "/segmentos" },
};

export default async function SegmentosPage() {
  const availability = await fetchSegmentAvailability();

  return (
    <main className="zpld" id="segmentos-page">
      <LandingMotion />
      <ElectricBg />

      <div className="zpld-bg" aria-hidden="true">
        <span className="zpld-orb zpld-orb-a" />
        <span className="zpld-orb zpld-orb-b" />
        <span className="zpld-orb zpld-orb-c" />
        <div className="zpld-grid" />
      </div>

      <PublicSiteHeader />

      <section className="zpld-section zpld-page-hero" aria-labelledby="seg-page-title">
        <div className="zpld-section-head" style={{ marginBottom: "3rem" }}>
          <Link href="/" className="zpld-breadcrumb">← Voltar para home</Link>
          <span>Segmentos</span>
          <h1
            id="seg-page-title"
            className="zpld-h1"
            style={{ fontSize: "clamp(2rem,3.2vw,3.4rem)", textAlign: "center" }}
          >
            Para qual tipo de negocio?
          </h1>
          <p>Escolha o segmento para ver os planos, modulos e configuracao recomendada.</p>
        </div>

        <div className="zp-lp-seg-grid">
          {businessSegments.map((seg) => (
            <SegmentCard
              key={seg.key}
              segKey={seg.key}
              name={seg.name}
              description={seg.description}
              modules={seg.modules}
              available={availability.get(seg.key) ?? false}
              href={seg.href}
            />
          ))}
        </div>
      </section>
    </main>
  );
}
