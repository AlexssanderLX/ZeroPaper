"use client";

import { FormEvent, useEffect, useState } from "react";
import Link from "next/link";
import QRCode from "qrcode";
import { APP_BASE_URL, createTable, deleteTableAlertSound, getTables, updateTable, uploadTableAlertSound, type DiningTable } from "@/lib/api";
import { handleApiError, type AsyncVoid } from "@/components/modules/module-utils";

export function TablesModule({ token, onUnauthorized }: { token: string; onUnauthorized: AsyncVoid }) {
  const [tables, setTables] = useState<DiningTable[]>([]);
  const [qrModalTableId, setQrModalTableId] = useState("");
  const [editingTableId, setEditingTableId] = useState("");
  const [name, setName] = useState("");
  const [seats, setSeats] = useState("4");
  const [comandaLabel, setComandaLabel] = useState("");
  const [editName, setEditName] = useState("");
  const [editSeats, setEditSeats] = useState("4");
  const [editComandaLabel, setEditComandaLabel] = useState("");
  const [copiedTableId, setCopiedTableId] = useState("");
  const [downloadingTableId, setDownloadingTableId] = useState("");
  const [uploadingSoundTableId, setUploadingSoundTableId] = useState("");
  const [removingSoundTableId, setRemovingSoundTableId] = useState("");
  const [loading, setLoading] = useState(true);
  const [isSaving, setIsSaving] = useState(false);
  const [isUpdating, setIsUpdating] = useState(false);
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
    if (typeof window !== "undefined") {
      return new URL(accessUrl, window.location.origin).toString();
    }

    if (APP_BASE_URL) {
      return new URL(accessUrl, APP_BASE_URL).toString();
    }

    return accessUrl;
  }

  function buildQrImageUrl(accessUrl: string, size = 320, margin = 12) {
    return `https://api.qrserver.com/v1/create-qr-code/?size=${size}x${size}&margin=${margin}&data=${encodeURIComponent(buildAbsoluteAccessUrl(accessUrl))}`;
  }

  function buildQrDownloadUrl(accessUrl: string) {
    return `https://api.qrserver.com/v1/create-qr-code/?size=1200x1200&margin=24&format=png&data=${encodeURIComponent(buildAbsoluteAccessUrl(accessUrl))}`;
  }

  function escapeHtml(value: string) {
    return value
      .replaceAll("&", "&amp;")
      .replaceAll("<", "&lt;")
      .replaceAll(">", "&gt;")
      .replaceAll('"', "&quot;")
      .replaceAll("'", "&#39;");
  }

  function replaceTableInState(nextTable: DiningTable) {
    setTables((currentValue) => currentValue.map((table) => (table.id === nextTable.id ? nextTable : table)));
  }

  function resetCreateForm() {
    setName("");
    setSeats("4");
    setComandaLabel("");
  }

  function resetTableEditor() {
    setEditingTableId("");
    setEditName("");
    setEditSeats("4");
    setEditComandaLabel("");
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

  async function handlePrintQr(table: DiningTable) {
    setErrorMessage("");
    setSuccessMessage("");

    const printWindow = window.open("", `zeropaper-qr-${table.id}`, "width=420,height=640");

    if (!printWindow) {
      setErrorMessage("O navegador bloqueou a janela de impressao. Libere pop-ups para continuar.");
      return;
    }

    printWindow.document.open();
    printWindow.document.write(`
      <!DOCTYPE html>
      <html lang="pt-BR">
        <head>
          <meta charset="utf-8" />
          <title>Preparando QR</title>
          <style>
            body {
              margin: 0;
              min-height: 100vh;
              display: grid;
              place-items: center;
              background: #f6efe7;
              color: #231915;
              font-family: Georgia, "Times New Roman", serif;
            }

            p {
              margin: 0;
              font-size: 18px;
            }
          </style>
        </head>
        <body>
          <p>Preparando arquivo de impressao...</p>
        </body>
      </html>
    `);
    printWindow.document.close();

    try {
      const qrDataUrl = await QRCode.toDataURL(buildAbsoluteAccessUrl(table.accessUrl), {
        width: 1400,
        margin: 2,
      });

      const safeTableName = escapeHtml(table.name);

      printWindow.document.open();
      printWindow.document.write(`
        <!DOCTYPE html>
        <html lang="pt-BR">
          <head>
            <meta charset="utf-8" />
            <title>QR ${safeTableName}</title>
            <style>
              @page {
                size: 80mm 120mm;
                margin: 8mm;
              }

              body {
                margin: 0;
                min-height: 100vh;
                display: grid;
                place-items: center;
                background: #f6efe7;
                color: #231915;
                font-family: Georgia, "Times New Roman", serif;
              }

              .sheet {
                width: 100%;
                max-width: 78mm;
                padding: 10mm 8mm;
                border-radius: 10mm;
                background: #fffaf5;
                box-sizing: border-box;
                text-align: center;
                box-shadow: 0 12px 30px rgba(35, 25, 21, 0.12);
              }

              .eyebrow {
                margin: 0 0 4mm;
                font: 600 11pt Arial, sans-serif;
                text-transform: uppercase;
                letter-spacing: 0.12em;
              }

              .table-name {
                margin: 0 0 5mm;
                font-size: 26pt;
                line-height: 0.95;
              }

              .message {
                margin: 0 0 6mm;
                font: 15pt Arial, sans-serif;
                line-height: 1.35;
              }

              img {
                display: block;
                width: 100%;
                height: auto;
                margin: 0 auto;
              }

              @media print {
                body {
                  background: #fff;
                }

                .sheet {
                  box-shadow: none;
                }
              }
            </style>
          </head>
          <body>
            <main class="sheet">
              <p class="eyebrow">ZeroPaper</p>
              <h1 class="table-name">${safeTableName}</h1>
              <p class="message">Escaneie para acessar o pedido da mesa.</p>
              <img id="qr-image" src="${qrDataUrl}" alt="QR da mesa ${safeTableName}" />
            </main>
            <script>
              const image = document.getElementById("qr-image");
              const triggerPrint = () => window.setTimeout(() => window.print(), 120);

              if (image && image.complete) {
                triggerPrint();
              } else if (image) {
                image.addEventListener("load", triggerPrint, { once: true });
              } else {
                triggerPrint();
              }

              window.addEventListener("afterprint", () => {
                window.close();
              });
            </script>
          </body>
        </html>
      `);
      printWindow.document.close();
      setSuccessMessage("Impressao preparada.");
    } catch {
      printWindow.close();
      setErrorMessage("Nao foi possivel preparar o QR para impressao.");
    }
  }

  async function handleTableSoundUpload(tableId: string, file?: File | null) {
    if (!file) {
      return;
    }

    try {
      setUploadingSoundTableId(tableId);
      const response = await uploadTableAlertSound(token, tableId, file);
      replaceTableInState(response.table);
      setSuccessMessage("Som da mesa atualizado.");
      setErrorMessage("");
    } catch (error) {
      await handleApiError(error, onUnauthorized, setErrorMessage, "Nao foi possivel enviar o som da mesa.");
    } finally {
      setUploadingSoundTableId("");
    }
  }

  async function handleResetTableSound(tableId: string) {
    try {
      setRemovingSoundTableId(tableId);
      const response = await deleteTableAlertSound(token, tableId);
      replaceTableInState(response);
      setSuccessMessage("Som padrao da mesa restaurado.");
      setErrorMessage("");
    } catch (error) {
      await handleApiError(error, onUnauthorized, setErrorMessage, "Nao foi possivel restaurar o som padrao da mesa.");
    } finally {
      setRemovingSoundTableId("");
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
        comandaLabel,
      });

      setTables((currentValue) => [createdTable, ...currentValue]);
      setQrModalTableId(createdTable.id);
      setSuccessMessage("Mesa criada. QR pronto para imprimir.");
      resetCreateForm();
      setErrorMessage("");
    } catch (error) {
      await handleApiError(
        error,
        onUnauthorized,
        setErrorMessage,
        "Nao foi possivel criar a mesa.",
      );
    } finally {
      setIsSaving(false);
    }
  }

  async function handleEditSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    if (!editingTableId) {
      return;
    }

    setIsUpdating(true);
    setSuccessMessage("");

    try {
      const updatedTable = await updateTable(token, editingTableId, {
        name: editName,
        seats: Number(editSeats),
        comandaLabel: editComandaLabel,
      });

      replaceTableInState(updatedTable);
      setSuccessMessage("Mesa atualizada.");
      setErrorMessage("");
      resetTableEditor();
    } catch (error) {
      await handleApiError(error, onUnauthorized, setErrorMessage, "Nao foi possivel atualizar a mesa.");
    } finally {
      setIsUpdating(false);
    }
  }

  const qrModalTable = tables.find((table) => table.id === qrModalTableId) ?? null;

  function handleEditTable(table: DiningTable) {
    setEditingTableId(table.id);
    setEditName(table.name);
    setEditSeats(String(table.seats));
    setEditComandaLabel(table.comandaLabel ?? "");
    setSuccessMessage("");
    setErrorMessage("");
  }

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

            <div className="field-group">
              <label htmlFor="tableComanda">Comanda opcional</label>
              <input
                id="tableComanda"
                value={comandaLabel}
                maxLength={40}
                onChange={(event) => setComandaLabel(event.target.value)}
                placeholder="Ex.: C-12 ou Comanda bar"
              />
            </div>

            <div className="toolbar-actions menu-category-form-actions">
              <button className="primary-link button-link" type="submit" disabled={isSaving}>
                {isSaving ? "Criando..." : "Criar mesa"}
              </button>
            </div>
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
                      <p>{table.isDeliveryChannel ? "Canal fixo para delivery e IA." : "QR pronto para compartilhar"}</p>
                    </div>
                    {table.isDeliveryChannel ? <span className="status-chip ready">Delivery</span> : null}
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
                        onClick={() => handlePrintQr(table)}
                      >
                        Imprimir QR
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
                        <article className="table-preview-stat">
                          <span>{table.isDeliveryChannel ? "Canal" : "Comanda"}</span>
                          <strong>{table.isDeliveryChannel ? "Delivery" : table.comandaLabel?.trim() ? table.comandaLabel : "Livre"}</strong>
                        </article>
                      </div>

                      <div className="table-ready-note">
                        <p>
                          {table.isDeliveryChannel
                            ? "Use este link fixo no atendimento digital e no delivery."
                            : table.comandaLabel?.trim()
                              ? `Mesa preparada com referencia de comanda ${table.comandaLabel}.`
                              : "QR pronto para imprimir e usar no salao."}
                        </p>
                      </div>

                      <div className="table-alert-sound-card">
                        <div className="table-alert-sound-copy">
                          <strong>Som da mesa</strong>
                          <p>{table.hasCustomAlertSound ? "Som proprio para identificar essa mesa." : "Usa o som padrao dos alertas."}</p>
                        </div>

                        {table.alertSoundUrl ? (
                          <audio className="table-alert-sound-player" controls preload="metadata" src={table.alertSoundUrl} />
                        ) : null}

                        <div className="toolbar-actions compact table-card-actions table-sound-actions">
                          <label className="ghost-link button-link">
                            {uploadingSoundTableId === table.id ? "Enviando..." : "Enviar som"}
                            <input
                              type="file"
                              accept=".wav,.mp3,.ogg,audio/wav,audio/mpeg,audio/ogg"
                              hidden
                              onChange={(event) => {
                                const file = event.target.files?.[0];
                                void handleTableSoundUpload(table.id, file);
                                event.currentTarget.value = "";
                              }}
                            />
                          </label>

                          {table.hasCustomAlertSound ? (
                            <button
                              className="ghost-link button-link"
                              type="button"
                              disabled={removingSoundTableId === table.id}
                              onClick={() => void handleResetTableSound(table.id)}
                            >
                              {removingSoundTableId === table.id ? "Restaurando..." : "Usar padrao"}
                            </button>
                          ) : null}
                        </div>
                      </div>

                      <div className="toolbar-actions compact table-card-actions">
                        <button className="ghost-link button-link" type="button" onClick={() => handleEditTable(table)}>
                          {table.isDeliveryChannel ? "Editar canal" : "Editar"}
                        </button>
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
              <span className="eyebrow">ZeroPaper</span>
              <p className="qr-modal-message">Bom apetite! Escaneie para pedir.</p>
              <h2>{qrModalTable.name}</h2>
              <img
                className="qr-modal-image"
                src={buildQrDownloadUrl(qrModalTable.accessUrl)}
                alt={`QR ampliado da ${qrModalTable.name}`}
                loading="eager"
                referrerPolicy="no-referrer"
              />
              <p className="qr-modal-caption">Abra o cardapio da mesa e envie o pedido direto do celular.</p>
            </div>

            <div className="toolbar-actions compact qr-modal-actions no-print">
              <button className="ghost-link button-link" type="button" onClick={() => void handleDownloadQr(qrModalTable.accessUrl, qrModalTable.id, qrModalTable.name)}>
                Baixar QR
              </button>
              <button className="ghost-link button-link" type="button" onClick={() => handlePrintQr(qrModalTable)}>
                Imprimir QR
              </button>
              <button className="ghost-link button-link" type="button" onClick={() => setQrModalTableId("")}>
                Fechar
              </button>
            </div>
          </section>
        </div>
      ) : null}

      {editingTableId ? (
        <div className="admin-modal-backdrop" onClick={resetTableEditor}>
          <section className="surface-card admin-sensitive-modal table-editor-modal" onClick={(event) => event.stopPropagation()}>
            <span className="eyebrow">Editar mesa</span>
            <h2>Atualize a mesa sem sair da lista</h2>
            <p>Revise nome, lugares e a referencia opcional de comanda. Ao salvar, a mesa continua no mesmo ponto da tela.</p>

            <form className="module-form" onSubmit={handleEditSubmit}>
              <div className="field-group">
                <label htmlFor="editTableName">Nome da mesa</label>
                <input
                  id="editTableName"
                  value={editName}
                  onChange={(event) => setEditName(event.target.value)}
                  placeholder="Mesa 01"
                />
              </div>

              <div className="field-group">
                <label htmlFor="editTableSeats">Lugares</label>
                <input
                  id="editTableSeats"
                  type="number"
                  min="1"
                  value={editSeats}
                  onChange={(event) => setEditSeats(event.target.value)}
                />
              </div>

              <div className="field-group">
                <label htmlFor="editTableComanda">Comanda opcional</label>
                <input
                  id="editTableComanda"
                  value={editComandaLabel}
                  maxLength={40}
                  onChange={(event) => setEditComandaLabel(event.target.value)}
                  placeholder="Ex.: C-12 ou Comanda bar"
                />
              </div>

              <div className="toolbar-actions menu-category-form-actions">
                <button className="ghost-link button-link" type="button" onClick={resetTableEditor}>
                  Fechar
                </button>
                <button className="primary-link button-link" type="submit" disabled={isUpdating}>
                  {isUpdating ? "Salvando..." : "Salvar mesa"}
                </button>
              </div>
            </form>

            {errorMessage ? <p className="module-feedback error">{errorMessage}</p> : null}
          </section>
        </div>
      ) : null}
    </>
  );
}
