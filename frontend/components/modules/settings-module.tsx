"use client";

import { FormEvent, useEffect, useState } from "react";
import { getCompanySettings, updateCompanySettings, type CompanySettings } from "@/lib/api";
import { handleApiError, type AsyncVoid } from "@/components/modules/module-utils";

export function SettingsModule({ token, onUnauthorized }: { token: string; onUnauthorized: AsyncVoid }) {
  const [settings, setSettings] = useState<CompanySettings | null>(null);
  const [draft, setDraft] = useState<CompanySettings | null>(null);
  const [loading, setLoading] = useState(true);
  const [errorMessage, setErrorMessage] = useState("");
  const [successMessage, setSuccessMessage] = useState("");

  async function loadSettings() {
    setLoading(true);

    try {
      const response = await getCompanySettings(token);
      setSettings(response);
      setDraft(response);
      setErrorMessage("");
    } catch (error) {
      await handleApiError(error, onUnauthorized, setErrorMessage, "Nao foi possivel carregar os ajustes.");
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    void loadSettings();
  }, [token]);

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    if (!draft) {
      return;
    }

    try {
      const response = await updateCompanySettings(token, {
        legalName: draft.legalName,
        tradeName: draft.tradeName,
        contactEmail: draft.contactEmail || undefined,
        contactPhone: draft.contactPhone || undefined,
      });

      setSettings(response);
      setDraft(response);
      setSuccessMessage("Dados da unidade atualizados.");
    } catch (error) {
      await handleApiError(error, onUnauthorized, setErrorMessage, "Nao foi possivel salvar os ajustes.");
    }
  }

  return (
    <section className="module-body-grid single">
      <section className="surface-card module-form-card">
        <span className="eyebrow">Dados da unidade</span>
        <h2>Ajustes gerais</h2>

        {loading || !draft ? (
          <p className="loading-state">Carregando ajustes...</p>
        ) : (
          <form className="module-form" onSubmit={handleSubmit}>
            <div className="module-inline-grid">
              <div className="field-group">
                <label>Razao social</label>
                <input
                  value={draft.legalName}
                  onChange={(event) => setDraft((current) => (current ? { ...current, legalName: event.target.value } : current))}
                />
              </div>
              <div className="field-group">
                <label>Nome da unidade</label>
                <input
                  value={draft.tradeName}
                  onChange={(event) => setDraft((current) => (current ? { ...current, tradeName: event.target.value } : current))}
                />
              </div>
            </div>

            <div className="module-inline-grid">
              <div className="field-group">
                <label>Email</label>
                <input
                  value={draft.contactEmail || ""}
                  onChange={(event) => setDraft((current) => (current ? { ...current, contactEmail: event.target.value } : current))}
                />
              </div>
              <div className="field-group">
                <label>Telefone</label>
                <input
                  value={draft.contactPhone || ""}
                  onChange={(event) => setDraft((current) => (current ? { ...current, contactPhone: event.target.value } : current))}
                />
              </div>
            </div>

            <div className="field-group">
              <label>Slug atual</label>
              <input value={settings?.accessSlug || ""} disabled />
            </div>

            <button className="primary-link button-link" type="submit">
              Salvar ajustes
            </button>
          </form>
        )}

        {successMessage ? <p className="module-feedback success">{successMessage}</p> : null}
        {errorMessage ? <p className="module-feedback error">{errorMessage}</p> : null}
      </section>
    </section>
  );
}
