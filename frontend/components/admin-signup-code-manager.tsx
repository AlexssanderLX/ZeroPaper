"use client";

import { FormEvent, useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { createSignupCode, getSignupCodes, type CreateSignupCodeResult, type SignupCode } from "@/lib/api";
import { useAppSession } from "@/components/app-session-provider";

function formatDate(value: string) {
  return new Intl.DateTimeFormat("pt-BR", {
    dateStyle: "short",
    timeStyle: "short",
  }).format(new Date(value));
}

export function AdminSignupCodeManager() {
  const router = useRouter();
  const { session, clearSession } = useAppSession();
  const [codes, setCodes] = useState<SignupCode[]>([]);
  const [createdCode, setCreatedCode] = useState<CreateSignupCodeResult | null>(null);
  const [loading, setLoading] = useState(true);
  const [errorMessage, setErrorMessage] = useState("");

  async function loadCodes() {
    setLoading(true);

    try {
      const response = await getSignupCodes(session.token);
      setCodes(response);
      setErrorMessage("");
    } catch {
      await clearSession();
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    if (session.profile !== "admin") {
      router.replace("/login");
      return;
    }

    void loadCodes();
  }, [router, session.profile, session.token]);

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    const formData = new FormData(event.currentTarget);
    const label = String(formData.get("label") ?? "").trim();
    const boundEmail = String(formData.get("boundEmail") ?? "").trim().toLowerCase();
    const expiresInDays = Number(formData.get("expiresInDays") ?? "7");
    const maxUses = Number(formData.get("maxUses") ?? "1");

    if (!label) {
      setErrorMessage("Informe um nome para o codigo.");
      return;
    }

    try {
      const response = await createSignupCode(session.token, {
        label,
        boundEmail: boundEmail || undefined,
        expiresInDays,
        maxUses,
      });

      setCreatedCode(response);
      setErrorMessage("");
      (event.currentTarget as HTMLFormElement).reset();
      await loadCodes();
    } catch {
      setErrorMessage("Nao foi possivel gerar o codigo agora.");
    }
  }

  if (session.profile !== "admin") {
    return null;
  }

  return (
    <main className="page-shell app-shell">
      <section className="hero-panel module-main-panel">
        <div className="hero-stack">
          <span className="eyebrow">ZeroPaper Root</span>
          <h1>Libere novos cadastros com codigo.</h1>
          <p className="hero-description">
            Gere codigos de acesso para unidades aprovadas e mantenha o cadastro da plataforma sob seu controle.
          </p>
        </div>

        <section className="hero-showcase ambient-panel">
          <div className="showcase-header">
            <span className="eyebrow">Conta root</span>
            <strong>{session.ownerName}</strong>
          </div>

          <div className="highlight-grid">
            <article className="info-card interactive-card">
              <h2>{codes.length}</h2>
              <p>Codigos registrados</p>
            </article>
            <article className="info-card interactive-card">
              <h2>{codes.filter((item) => item.isActive).length}</h2>
              <p>Codigos ativos</p>
            </article>
          </div>
        </section>
      </section>

      <section className="module-body-grid">
        <section className="surface-card module-form-card">
          <span className="eyebrow">Novo codigo</span>
          <h2>Gerar liberacao</h2>
          <form className="module-form" onSubmit={handleSubmit}>
            <div className="field-group">
              <label htmlFor="label">Identificacao</label>
              <input id="label" name="label" placeholder="Ex.: Restaurante do bairro" />
            </div>

            <div className="field-group">
              <label htmlFor="boundEmail">Email vinculado</label>
              <input id="boundEmail" name="boundEmail" type="email" placeholder="opcional" />
            </div>

            <div className="module-inline-grid">
              <div className="field-group">
                <label htmlFor="expiresInDays">Validade em dias</label>
                <input id="expiresInDays" name="expiresInDays" type="number" min="1" max="90" defaultValue="7" />
              </div>
              <div className="field-group">
                <label htmlFor="maxUses">Usos</label>
                <input id="maxUses" name="maxUses" type="number" min="1" max="100" defaultValue="1" />
              </div>
            </div>

            <button className="primary-link button-link" type="submit">
              Gerar codigo
            </button>
          </form>

          {createdCode ? (
            <div className="module-empty-state">
              <strong>Codigo gerado</strong>
              <p>{createdCode.rawCode}</p>
            </div>
          ) : null}

          {errorMessage ? <p className="module-feedback error">{errorMessage}</p> : null}
        </section>

        <section className="surface-card module-list-card">
          <div className="module-section-head">
            <span className="eyebrow">Liberacoes</span>
            <strong>{codes.length} codigos</strong>
          </div>

          {loading ? (
            <p className="loading-state">Carregando codigos...</p>
          ) : codes.length === 0 ? (
            <div className="module-empty-state">
              <strong>Nenhum codigo gerado.</strong>
              <p>Crie o primeiro codigo para liberar o cadastro de uma nova unidade.</p>
            </div>
          ) : (
            <div className="module-card-list">
              {codes.map((code) => (
                <article key={code.id} className="module-entity-card interactive-card">
                  <div className="entity-head">
                    <div>
                      <h3>{code.label}</h3>
                      <p>{code.boundEmail || "Sem email vinculado"}</p>
                    </div>
                    <span className={`status-chip ${code.isActive ? "available" : "inactive"}`}>
                      {code.isActive ? "Ativo" : "Inativo"}
                    </span>
                  </div>

                  <div className="entity-meta-grid">
                    <span>Valido ate {formatDate(code.expiresAtUtc)}</span>
                    <span>{code.usedCount}/{code.maxUses} usos</span>
                  </div>
                </article>
              ))}
            </div>
          )}
        </section>
      </section>
    </main>
  );
}
