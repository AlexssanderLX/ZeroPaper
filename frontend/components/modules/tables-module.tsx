"use client";

import { FormEvent, useEffect, useState } from "react";
import Link from "next/link";
import { createTable, getTables, type DiningTable } from "@/lib/api";
import { handleApiError, type AsyncVoid } from "@/components/modules/module-utils";

export function TablesModule({ token, onUnauthorized }: { token: string; onUnauthorized: AsyncVoid }) {
  const [tables, setTables] = useState<DiningTable[]>([]);
  const [name, setName] = useState("");
  const [seats, setSeats] = useState("4");
  const [copiedTableId, setCopiedTableId] = useState("");
  const [loading, setLoading] = useState(true);
  const [isSaving, setIsSaving] = useState(false);
  const [errorMessage, setErrorMessage] = useState("");
  const [successMessage, setSuccessMessage] = useState("");

  async function loadTables() {
    setLoading(true);

    try {
      setTables(await getTables(token));
      setErrorMessage("");
    } catch (error) {
      await handleApiError(error, onUnauthorized, setErrorMessage, "Nao foi possivel carregar as mesas.");
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    void loadTables();
  }, [token]);

  async function handleCopyLink(accessUrl: string, tableId: string) {
    try {
      const absoluteUrl = typeof window === "undefined" ? accessUrl : new URL(accessUrl, window.location.origin).toString();
      await navigator.clipboard.writeText(absoluteUrl);
      setCopiedTableId(tableId);
      window.setTimeout(() => setCopiedTableId(""), 2400);
    } catch {
      setErrorMessage("Nao foi possivel copiar o link da mesa.");
    }
  }

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setIsSaving(true);
    setSuccessMessage("");

    try {
      await createTable(token, {
        name,
        seats: Number(seats),
      });

      setName("");
      setSeats("4");
      setSuccessMessage("Mesa criada com acesso publico pronto.");
      await loadTables();
    } catch (error) {
      await handleApiError(error, onUnauthorized, setErrorMessage, "Nao foi possivel criar a mesa.");
    } finally {
      setIsSaving(false);
    }
  }

  return (
    <section className="module-body-grid">
      <section className="surface-card module-form-card">
        <span className="eyebrow">Nova mesa</span>
        <h2>Criar mesa com acesso por QR</h2>
        <form className="module-form" onSubmit={handleSubmit}>
          <div className="field-group">
            <label htmlFor="tableName">Nome da mesa</label>
            <input id="tableName" value={name} onChange={(event) => setName(event.target.value)} placeholder="Mesa 01" />
          </div>

          <div className="field-group">
            <label htmlFor="tableSeats">Lugares</label>
            <input
              id="tableSeats"
              type="number"
              min="1"
              value={seats}
              onChange={(event) => setSeats(event.target.value)}
            />
          </div>

          <button className="primary-link button-link" type="submit" disabled={isSaving}>
            {isSaving ? "Criando..." : "Criar mesa"}
          </button>
        </form>

        {successMessage ? <p className="module-feedback success">{successMessage}</p> : null}
        {errorMessage ? <p className="module-feedback error">{errorMessage}</p> : null}
      </section>

      <section className="surface-card module-list-card">
        <div className="module-section-head">
          <span className="eyebrow">Mesas ativas</span>
          <strong>{tables.length} registradas</strong>
        </div>

        {loading ? (
          <p className="loading-state">Carregando mesas...</p>
        ) : tables.length === 0 ? (
          <div className="module-empty-state">
            <strong>Nenhuma mesa criada.</strong>
            <p>Cadastre a primeira mesa para liberar o acesso publico por QR e receber pedidos.</p>
          </div>
        ) : (
          <div className="module-card-list">
            {tables.map((table) => (
              <article key={table.id} className="module-entity-card interactive-card">
                <div className="entity-head">
                  <div>
                    <h3>{table.name}</h3>
                    <p>{table.internalCode}</p>
                  </div>
                  <span className={`status-chip ${table.status.toLowerCase()}`}>{table.status}</span>
                </div>

                <div className="entity-meta-grid">
                  <span>{table.seats} lugares</span>
                  <span>{table.openOrderCount} pedidos abertos</span>
                </div>

                <div className="entity-link-stack">
                  <p>Codigo publico: {table.publicCode}</p>
                  <p>{table.accessUrl}</p>
                  <div className="toolbar-actions compact">
                    <Link className="ghost-link" href={table.accessUrl}>
                      Abrir pagina publica
                    </Link>
                    <button className="ghost-link button-link" type="button" onClick={() => void handleCopyLink(table.accessUrl, table.id)}>
                      {copiedTableId === table.id ? "Link copiado" : "Copiar link"}
                    </button>
                  </div>
                </div>
              </article>
            ))}
          </div>
        )}
      </section>
    </section>
  );
}
