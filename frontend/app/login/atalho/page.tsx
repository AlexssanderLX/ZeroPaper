"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { useEffect, useState } from "react";
import { loginWithShortcut } from "@/lib/api";
import { savePortalSession } from "@/lib/owner-portal";

export default function ShortcutLoginPage() {
  const router = useRouter();
  const [status, setStatus] = useState("Validando atalho seguro...");
  const [errorMessage, setErrorMessage] = useState("");

  useEffect(() => {
    async function authenticateShortcut() {
      const hash = window.location.hash.startsWith("#") ? window.location.hash.slice(1) : window.location.hash;
      const params = new URLSearchParams(hash);
      const shortcutToken = params.get("token")?.trim();

      window.history.replaceState(null, "", "/login/atalho");

      if (!shortcutToken) {
        setErrorMessage("Atalho sem token. Gere um novo link dentro da unidade.");
        setStatus("Nao foi possivel entrar pelo atalho.");
        return;
      }

      try {
        const response = await loginWithShortcut({ token: shortcutToken });
        savePortalSession({
          token: response.token,
          email: response.email,
          profile: "restaurant",
          restaurantName: response.restaurantName,
          ownerName: response.ownerName,
          role: response.role,
          expiresAtUtc: response.expiresAtUtc,
        });

        setStatus("Acesso confirmado. Abrindo painel...");
        router.replace("/app");
      } catch {
        setErrorMessage("Atalho expirado, revogado ou invalido. Entre com email e senha e gere um novo.");
        setStatus("Nao foi possivel entrar pelo atalho.");
      }
    }

    void authenticateShortcut();
  }, [router]);

  return (
    <main className="page-shell">
      <section className="surface-card app-loading-card ambient-panel subtle">
        <span className="eyebrow">ZeroPaper</span>
        <h1>Atalho da unidade</h1>
        <p>{status}</p>
        {errorMessage ? <p className="module-feedback error">{errorMessage}</p> : null}
        {errorMessage ? (
          <Link className="primary-link button-link" href="/login">
            Entrar com email e senha
          </Link>
        ) : null}
      </section>
    </main>
  );
}
