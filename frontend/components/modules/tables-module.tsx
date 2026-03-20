"use client";

import { FormEvent, useEffect, useState } from "react";
import Link from "next/link";
import { createTable, getTables, type DiningTable } from "@/lib/api";
import { handleApiError, type AsyncVoid } from "@/components/modules/module-utils";

export function TablesModule({ token, onUnauthorized }: { token: string; onUnauthorized: AsyncVoid }) {
  const [tables, setTables] = useState<DiningTable[]>([]);
  const [qrModalTableId, setQrModalTableId] = useState("");
  const [name, setName] = useState("");
  const [seats, setSeats] = useState("4");
  const [copiedTableId, setCopiedTableId] = useState("");
  const [downloadingTableId, setDownloadingTableId] = useState("");
  const [loading, setLoading] = useState(true);
  const [isSaving, setIsSaving] = useState(false);
  const [errorMessage, setErrorMessage] = useState("");
  const [successMessage, setSuccessMessage] = useState("");

  async function loadTables() {
    setLoading(true);

    try {
      const response = await getTables(token);
      setTables(response);
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

  function buildQrImageUrl(accessUrl: string, size = 320, margin = 12) {
    return `https://api.qrserver.com/v1/create-qr-code/?size=${size}x${size}&margin=${margin}&data=${encodeURIComponent(buildAbsoluteAccessUrl(accessUrl))}`;
  }

  function buildQrDownloadUrl(accessUrl: string) {
    return `https://api.qrserver.com/v1/create-qr-code/?size=1200x1200&margin=24&format=png&data=${encodeURIComponent(buildAbsoluteAccessUrl(accessUrl))}`;
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

  async function handleDownloadQr(accessUrl: string, tableId: string, tableName: string) {
    try {
      setDownloadingTableId(tableId);
      const response = await fetch(buildQrDownloadUrl(accessUrl), { cache: "no-store" });

      if (!response.ok) {
        throw new Error();
      }

      const blob = await response.blob();
      const objectUrl = URL.createObjectURL(blob);
      const anchor = document.createElement("a");
      anchor.href = objectUrl;
      anchor.download = `qr-${tableName.toLowerCase().replace(/[^a-z0-9]+/gi, "-")}.png`;
      document.body.appendChild(anchor);
      anchor.click();
      anchor.remove();
      URL.revokeObjectURL(objectUrl);
      setSuccessMessage("QR baixado.");
    } catch {
      setErrorMessage("Nao foi possivel baixar o QR da mesa.");
    } finally {
      setDownloadingTableId("");
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
      setSuccessMessage("Mesa criada com QR pronto para compartilhar.");
      await loadTables();
    } catch (error) {
      await handleApiError(error, onUnauthorized, setErrorMessage, "Nao foi possivel criar a mesa.");
    } finally {
      setIsSaving(false);
    }
  }

  const qrModalTable = tables.find((table) => table.id === qrModalTableId) ?? null;

  return (
    <>
      <section className="tables-workspace">
        <section className="surface-card module-form-card table-creation-card">
          <span className="eyebrow">Nova mesa</span>
          <h2>Criar mesa com QR pronto</h2>

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
            <span className="eyebrow">Mesas</span>
            <strong>{tables.length} registradas</strong>
          </div>

          {loading ? (
            <p className="loading-state">Carregando mesas...</p>
          ) : tables.length === 0 ? (
            <div className="module-empty-state">
              <strong>Nenhuma mesa criada.</strong>
            </div>
          ) : (
            <div className="module-card-list table-card-list">
              {tables.map((table) => (
                <article key={table.id} className="module-entity-card interactive-card table-list-card table-grid-card">
                  <div className="entity-head">
                    <div>
                      <h3>{table.name}</h3>
                      <p>{table.internalCode}</p>
                    </div>
                  </div>

                  <div className="table-card-grid">
                    <div className="table-card-qr-stack">
                      <button className="table-card-qr" type="button" onClick={() => setQrModalTableId(table.id)}>
                        <img
                          className="table-card-qr-image large"
                          src={buildQrImageUrl(table.accessUrl, 220, 12)}
                          alt={`QR da ${table.name}`}
                          loading="lazy"
                          referrerPolicy="no-referrer"
                        />
                      </button>

                      <button
                        className="ghost-link button-link table-download-button"
                        type="button"
                        disabled={downloadingTableId === table.id}
                        onClick={() => void handleDownloadQr(table.accessUrl, table.id, table.name)}
                      >
                        {downloadingTableId === table.id ? "Baixando..." : "Baixar QR"}
                      </button>
                    </div>

                    <div className="table-card-content">
                      <div className="table-card-stat-grid">
                        <article className="table-preview-stat">
                          <span>Lugares</span>
                          <strong>{table.seats}</strong>
                        </article>
                        <article className="table-preview-stat">
                          <span>Pedidos</span>
                          <strong>{table.openOrderCount}</strong>
                        </article>
                      </div>

                      <div className="entity-meta-grid table-link-meta">
                        <span>{buildAbsoluteAccessUrl(table.accessUrl)}</span>
                      </div>

                      <div className="toolbar-actions compact table-card-actions">
                        <button className="ghost-link button-link" type="button" onClick={() => setQrModalTableId(table.id)}>
                          Ver QR
                        </button>
                        <Link className="ghost-link" href={table.accessUrl}>
                          Abrir pagina
                        </Link>
                        <button className="ghost-link button-link" type="button" onClick={() => void handleCopyLink(table.accessUrl, table.id)}>
                          {copiedTableId === table.id ? "Copiado" : "Copiar link"}
                        </button>
                      </div>
                    </div>
                  </div>
                </article>
              ))}
            </div>
          )}
        </section>
      </section>

      {qrModalTable ? (
        <div className="admin-modal-backdrop qr-modal-backdrop" onClick={() => setQrModalTableId("")}>
          <section className="surface-card qr-modal-sheet" onClick={(event) => event.stopPropagation()}>
            <div className="qr-modal-printable">
              <span className="eyebrow">Mesa</span>
              <h2>{qrModalTable.name}</h2>
              <img
                className="qr-modal-image"
                src={buildQrDownloadUrl(qrModalTable.accessUrl)}
                alt={`QR ampliado da ${qrModalTable.name}`}
                loading="eager"
                referrerPolicy="no-referrer"
              />
              <p className="qr-modal-caption">Aponte a camera para acessar o pedido.</p>
            </div>

            <div className="toolbar-actions compact qr-modal-actions no-print">
              <button className="ghost-link button-link" type="button" onClick={() => void handleDownloadQr(qrModalTable.accessUrl, qrModalTable.id, qrModalTable.name)}>
                Baixar QR
              </button>
              <button className="ghost-link button-link" type="button" onClick={() => window.print()}>
                Imprimir
              </button>
              <button className="ghost-link button-link" type="button" onClick={() => setQrModalTableId("")}>
                Fechar
              </button>
            </div>
          </section>
        </div>
      ) : null}
    </>
  );
}
