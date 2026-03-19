"use client";

import { FormEvent, useEffect, useState } from "react";
import Link from "next/link";
import { createTable, getTables, type DiningTable } from "@/lib/api";
import { handleApiError, type AsyncVoid } from "@/components/modules/module-utils";

export function TablesModule({ token, onUnauthorized }: { token: string; onUnauthorized: AsyncVoid }) {
  const [tables, setTables] = useState<DiningTable[]>([]);
  const [selectedTableId, setSelectedTableId] = useState("");
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
      const response = await getTables(token);
      setTables(response);
      setSelectedTableId((currentValue) => {
        if (currentValue && response.some((item) => item.id === currentValue)) {
          return currentValue;
        }

        return response[0]?.id ?? "";
      });
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

  function buildAbsoluteAccessUrl(accessUrl: string) {
    return typeof window === "undefined" ? accessUrl : new URL(accessUrl, window.location.origin).toString();
  }

  function buildQrImageUrl(accessUrl: string) {
    return `https://api.qrserver.com/v1/create-qr-code/?size=320x320&margin=12&data=${encodeURIComponent(buildAbsoluteAccessUrl(accessUrl))}`;
  }

  async function handleCopyLink(accessUrl: string, tableId: string, message = "Link copiado.") {
    try {
      await navigator.clipboard.writeText(buildAbsoluteAccessUrl(accessUrl));
      setCopiedTableId(tableId);
      setSuccessMessage(message);
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
      const createdTable = await createTable(token, {
        name,
        seats: Number(seats),
      });

      setName("");
      setSeats("4");
      setSelectedTableId(createdTable.id);
      setSuccessMessage("Mesa criada com QR pronto para compartilhar.");
      await loadTables();
    } catch (error) {
      await handleApiError(error, onUnauthorized, setErrorMessage, "Nao foi possivel criar a mesa.");
    } finally {
      setIsSaving(false);
    }
  }

  const selectedTable = tables.find((table) => table.id === selectedTableId) ?? tables[0] ?? null;

  return (
    <section className="tables-workspace">
      <section className="tables-hero-grid">
        <section className="surface-card module-form-card table-creation-card">
          <span className="eyebrow">Nova mesa</span>
          <h2>Criar mesa com QR pronto</h2>
          <p className="body-copy">
            Cadastre a mesa uma vez e deixe o acesso publico pronto para o cliente abrir no celular.
          </p>

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

        <section className="surface-card table-preview-card">
          <div className="module-section-head">
            <span className="eyebrow">QR da mesa</span>
            <strong>{selectedTable ? selectedTable.name : "Sem mesa"}</strong>
          </div>

          {selectedTable ? (
            <div className="table-preview-layout">
              <div className="table-qr-frame">
                <img
                  className="table-qr-image"
                  src={buildQrImageUrl(selectedTable.accessUrl)}
                  alt={`QR da ${selectedTable.name}`}
                  loading="lazy"
                  referrerPolicy="no-referrer"
                />
              </div>

              <div className="table-preview-copy">
                <div className="table-preview-stat-row">
                  <article className="table-preview-stat">
                    <span>Lugares</span>
                    <strong>{selectedTable.seats}</strong>
                  </article>
                  <article className="table-preview-stat">
                    <span>Pedidos</span>
                    <strong>{selectedTable.openOrderCount}</strong>
                  </article>
                </div>

                <div className="table-preview-code">
                  <span className="eyebrow">Codigo publico</span>
                  <strong>{selectedTable.publicCode}</strong>
                </div>

                <div className="entity-link-stack table-link-stack">
                  <p>{buildAbsoluteAccessUrl(selectedTable.accessUrl)}</p>
                </div>

                <div className="toolbar-actions compact table-preview-actions">
                  <Link className="ghost-link" href={selectedTable.accessUrl}>
                    Abrir pagina
                  </Link>
                  <button
                    className="ghost-link button-link"
                    type="button"
                    onClick={() => void handleCopyLink(selectedTable.accessUrl, selectedTable.id, "Link da mesa copiado.")}
                  >
                    {copiedTableId === selectedTable.id ? "Copiado" : "Copiar link"}
                  </button>
                  <a
                    className="ghost-link"
                    href={buildQrImageUrl(selectedTable.accessUrl)}
                    target="_blank"
                    rel="noreferrer"
                  >
                    Abrir QR
                  </a>
                </div>
              </div>
            </div>
          ) : (
            <div className="module-empty-state">
              <strong>Crie a primeira mesa.</strong>
              <p>Assim que uma mesa for criada, o QR e o link publico aparecem aqui.</p>
            </div>
          )}
        </section>
      </section>

      <section className="surface-card module-list-card">
        <div className="module-section-head">
          <span className="eyebrow">Mesas</span>
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
              <article
                key={table.id}
                className={`module-entity-card interactive-card table-list-card ${selectedTable?.id === table.id ? "is-selected" : ""}`}
              >
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
                  <span>{table.publicCode}</span>
                </div>

                <div className="toolbar-actions compact table-card-actions">
                  <button className="ghost-link button-link" type="button" onClick={() => setSelectedTableId(table.id)}>
                    Ver QR
                  </button>
                  <Link className="ghost-link" href={table.accessUrl}>
                    Abrir pagina
                  </Link>
                  <button className="ghost-link button-link" type="button" onClick={() => void handleCopyLink(table.accessUrl, table.id)}>
                    {copiedTableId === table.id ? "Copiado" : "Copiar link"}
                  </button>
                </div>
              </article>
            ))}
          </div>
        )}
      </section>
    </section>
  );
}
