"use client";

import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { BrandMark } from "@/components/brand-mark";
import { confirmPublicSubscriptionPayment } from "@/lib/api";

export default function SignupConfirmacaoPage() {
  const router = useRouter();
  const [message, setMessage] = useState("Recebi sua solicitacao e vou liberar seu acesso em breve.");
  const [paid, setPaid] = useState(false);

  useEffect(() => {
    const confirmationToken = new URLSearchParams(window.location.search).get("pagamento");
    if (!confirmationToken) {
      const timer = setTimeout(() => router.push("/"), 4000);
      return () => clearTimeout(timer);
    }
    let cancelled = false;
    let attempts = 0;
    const confirm = async () => {
      attempts++;
      try {
        const result = await confirmPublicSubscriptionPayment(confirmationToken);
        if (cancelled) return;
        setMessage(result.message);
        if (result.accessActive) { setPaid(true); return; }
      } catch { if (!cancelled) setMessage("Estamos confirmando o pagamento com o Mercado Pago."); }
      if (!cancelled && attempts < 12) window.setTimeout(() => void confirm(), 5000);
    };
    void confirm();
    return () => { cancelled = true; };
  }, [router]);

  return (
    <main className="page-shell zp-signup-sales-page">
      <section className="zp-confirm-shell">
        <div className="zp-confirm-brand">
          <BrandMark small variant="full" />
        </div>

        <div className="zp-confirm-card surface-card">
          <span className="zp-confirm-check" aria-hidden="true">✓</span>
          <h1 className="zp-confirm-title">{paid ? "Pagamento confirmado" : "Cadastro recebido"}</h1>
          <p className="zp-confirm-text">
            {message}
          </p>
          {paid ? <button className="primary-link button-link" type="button" onClick={() => router.push("/login")}>Entrar no ZeroPaper</button> : <p className="zp-confirm-redirect">A confirmacao pode levar alguns instantes.</p>}
        </div>
      </section>
    </main>
  );
}
