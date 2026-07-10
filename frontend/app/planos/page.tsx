import type { Metadata } from "next";
import Link from "next/link";
import { PublicSiteHeader } from "@/components/public-site-header";
import { LandingMotion } from "@/components/landing-motion";
import { ElectricBg } from "@/components/electric-bg";
import { SegmentCard } from "@/components/segment-card";
import { businessSegments } from "@/lib/landing-data";
import { fetchSegmentAvailability } from "@/lib/segment-availability";

export const metadata: Metadata = {
  title: "Planos | ZeroPaper",
  description:
    "Os planos do ZeroPaper variam conforme o tipo de negocio e os modulos escolhidos. Escolha seu segmento para ver os planos.",
  alternates: { canonical: "/planos" },
};

export default async function PlanosPage() {
  const availability = await fetchSegmentAvailability();

  return (
    <main className="zpld" id="planos-page">
      <LandingMotion />
      <ElectricBg />

      <div className="zpld-bg" aria-hidden="true">
        <span className="zpld-orb zpld-orb-a" />
        <span className="zpld-orb zpld-orb-b" />
        <div className="zpld-grid" />
      </div>

      <PublicSiteHeader />

      <section className="zpld-section zpld-page-hero" aria-labelledby="plans-page-title">
        <div className="zpld-section-head" style={{ marginBottom: "3rem" }}>
          <Link href="/" className="zpld-breadcrumb">← Voltar para home</Link>
          <span>Planos</span>
          <h1
            id="plans-page-title"
            className="zpld-h1"
            style={{ fontSize: "clamp(2rem,3.2vw,3.4rem)", textAlign: "center" }}
          >
            Os planos variam conforme o tipo de negocio.
          </h1>
          <p>Escolha o segmento abaixo para ver os planos, modulos e valores recomendados.</p>
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
