"use client";

import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { loginWithDemo } from "@/lib/api";
import { savePortalSession } from "@/lib/owner-portal";

export default function DemoLoginPage() {
  const router = useRouter();
  const [status, setStatus] = useState("Validando acesso de demonstracao...");
  const [failed, setFailed] = useState(false);

  useEffect(() => {
    async function authenticate() {
      const hash = window.location.hash.startsWith("#") ? window.location.hash.slice(1) : window.location.hash;
      const token = new URLSearchParams(hash).get("token")?.trim();
      window.history.replaceState(null, "", "/demo");
      if (!token) {
        setStatus("Este link de demonstracao e invalido ou foi revogado.");
        setFailed(true);
        return;
      }

      try {
        const response = await loginWithDemo({ token });
        savePortalSession({
          token: response.token,
          email: response.email,
          profile: "restaurant",
          restaurantName: response.restaurantName,
          ownerName: response.ownerName,
          role: response.role,
          expiresAtUtc: response.expiresAtUtc,
          isDemoAccount: true,
        });
        setStatus("Acesso confirmado. Abrindo ambiente de demonstracao...");
        router.replace("/app");
      } catch {
        setStatus("Este link de demonstracao expirou, foi revogado ou nao existe mais.");
        setFailed(true);
      }
    }

    void authenticate();
  }, [router]);

  return (
    <main className="page-shell">
      <section className="surface-card app-loading-card ambient-panel subtle">
        <span className="eyebrow">ZeroPaper Demo</span>
        <h1>Restaurante demonstracao</h1>
        <p>{status}</p>
        {failed ? <p className="module-feedback error">Solicite um novo link ao responsavel pela demonstracao.</p> : null}
      </section>
    </main>
  );
}
