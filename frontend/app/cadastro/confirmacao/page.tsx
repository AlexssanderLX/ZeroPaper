"use client";

import { useEffect } from "react";
import { useRouter } from "next/navigation";
import { BrandMark } from "@/components/brand-mark";

export default function SignupConfirmacaoPage() {
  const router = useRouter();

  useEffect(() => {
    const timer = setTimeout(() => {
      router.push("/");
    }, 4000);
    return () => clearTimeout(timer);
  }, [router]);

  return (
    <main className="page-shell zp-signup-sales-page">
      <section className="zp-confirm-shell">
        <div className="zp-confirm-brand">
          <BrandMark small variant="full" />
        </div>

        <div className="zp-confirm-card surface-card">
          <span className="zp-confirm-check" aria-hidden="true">✓</span>
          <h1 className="zp-confirm-title">Pre-cadastro enviado</h1>
          <p className="zp-confirm-text">
            Recebi sua solicitacao e vou liberar seu acesso em breve.
          </p>
          <p className="zp-confirm-redirect">Voltando para a pagina inicial...</p>
        </div>
      </section>
    </main>
  );
}
