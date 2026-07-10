"use client";

import { useState } from "react";
import {
  ApiError,
  getCustomerHistory,
  getCustomerProfile,
  updateCustomerProfile,
  type CustomerHistoryEntry,
  type CustomerProfile,
  type SaveCustomerProfilePayload,
} from "@/lib/api";
import { formatCurrency, formatDateTime, handleApiError, type AsyncVoid } from "@/components/modules/module-utils";

const HISTORY_LIMIT = 8;

type ProfileDraft = {
  name: string;
  zipCode: string;
  street: string;
  number: string;
  neighborhood: string;
  complement: string;
};

function toDraft(profile: CustomerProfile | null): ProfileDraft {
  return {
    name: profile?.name ?? "",
    zipCode: profile?.zipCode ?? "",
    street: profile?.street ?? "",
    number: profile?.number ?? "",
    neighborhood: profile?.neighborhood ?? "",
    complement: profile?.complement ?? "",
  };
}

function buildAddressLines(profile: CustomerProfile): string[] {
  const lines: string[] = [];
  const street = [profile.street, profile.number].filter(Boolean).join(", ");
  if (street) {
    lines.push(street);
  }
  if (profile.neighborhood) {
    lines.push(profile.neighborhood);
  }
  if (profile.complement) {
    lines.push(profile.complement);
  }
  if (profile.zipCode) {
    lines.push(`CEP ${profile.zipCode}`);
  }
  return lines;
}

export function CustomerProfilePanel({
  token,
  phoneNumber,
  onUnauthorized,
}: {
  token: string;
  phoneNumber: string;
  onUnauthorized: AsyncVoid;
}) {
  const [open, setOpen] = useState(false);
  const [loaded, setLoaded] = useState(false);
  const [loading, setLoading] = useState(false);
  const [profile, setProfile] = useState<CustomerProfile | null>(null);
  const [history, setHistory] = useState<CustomerHistoryEntry[]>([]);
  const [error, setError] = useState("");
  const [editing, setEditing] = useState(false);
  const [draft, setDraft] = useState<ProfileDraft>(toDraft(null));
  const [saving, setSaving] = useState(false);

  async function loadData() {
    setLoading(true);
    setError("");
    try {
      const profileResult = await getCustomerProfile(token, phoneNumber).catch((profileError) => {
        if (profileError instanceof ApiError && profileError.status === 404) {
          return null;
        }
        throw profileError;
      });

      const historyResult = await getCustomerHistory(token, phoneNumber).catch((historyError) => {
        if (historyError instanceof ApiError && historyError.status === 404) {
          return [] as CustomerHistoryEntry[];
        }
        throw historyError;
      });

      setProfile(profileResult);
      setHistory(historyResult);
      setDraft(toDraft(profileResult));
      setLoaded(true);
    } catch (loadError) {
      await handleApiError(loadError, onUnauthorized, setError, "Nao foi possivel carregar o cliente.");
    } finally {
      setLoading(false);
    }
  }

  function handleToggle() {
    const next = !open;
    setOpen(next);
    if (next && !loaded && !loading) {
      void loadData();
    }
  }

  async function handleSave() {
    setSaving(true);
    setError("");
    const payload: SaveCustomerProfilePayload = {
      name: draft.name.trim() || null,
      zipCode: draft.zipCode.trim() || null,
      street: draft.street.trim() || null,
      number: draft.number.trim() || null,
      neighborhood: draft.neighborhood.trim() || null,
      complement: draft.complement.trim() || null,
    };

    try {
      const updated = await updateCustomerProfile(token, phoneNumber, payload);
      setProfile(updated);
      setDraft(toDraft(updated));
      setEditing(false);
    } catch (saveError) {
      await handleApiError(saveError, onUnauthorized, setError, "Nao foi possivel salvar o cliente.");
    } finally {
      setSaving(false);
    }
  }

  return (
    <section className="zpcust-panel">
      <button
        className="zpcust-toggle"
        type="button"
        aria-expanded={open}
        onClick={handleToggle}
      >
        <span>Cliente</span>
        <small>{profile?.name || phoneNumber}</small>
        <span aria-hidden="true" className="zpcust-toggle-arrow">{open ? "−" : "+"}</span>
      </button>

      {open ? (
        <div className="zpcust-body">
          {loading ? (
            <p className="zpcust-muted">Carregando cliente...</p>
          ) : error ? (
            <p className="module-feedback error">{error}</p>
          ) : editing ? (
            <div className="zpcust-form">
              <label className="zpcust-field">
                <span>Nome</span>
                <input value={draft.name} onChange={(event) => setDraft({ ...draft, name: event.target.value })} />
              </label>
              <label className="zpcust-field">
                <span>CEP</span>
                <input value={draft.zipCode} onChange={(event) => setDraft({ ...draft, zipCode: event.target.value })} inputMode="numeric" />
              </label>
              <label className="zpcust-field">
                <span>Rua</span>
                <input value={draft.street} onChange={(event) => setDraft({ ...draft, street: event.target.value })} />
              </label>
              <label className="zpcust-field">
                <span>Numero</span>
                <input value={draft.number} onChange={(event) => setDraft({ ...draft, number: event.target.value })} />
              </label>
              <label className="zpcust-field">
                <span>Bairro</span>
                <input value={draft.neighborhood} onChange={(event) => setDraft({ ...draft, neighborhood: event.target.value })} />
              </label>
              <label className="zpcust-field">
                <span>Complemento</span>
                <input value={draft.complement} onChange={(event) => setDraft({ ...draft, complement: event.target.value })} />
              </label>
              <div className="zpcust-actions">
                <button className="zpcust-btn zpcust-btn-ghost" type="button" onClick={() => setEditing(false)} disabled={saving}>
                  Cancelar
                </button>
                <button className="zpcust-btn zpcust-btn-primary" type="button" onClick={() => void handleSave()} disabled={saving}>
                  {saving ? "Salvando..." : "Salvar cliente"}
                </button>
              </div>
            </div>
          ) : (
            <>
              {profile ? (
                <div className="zpcust-info">
                  <div className="zpcust-row">
                    <span className="zpcust-label">Nome</span>
                    <span className="zpcust-value">{profile.name || "-"}</span>
                  </div>
                  <div className="zpcust-row">
                    <span className="zpcust-label">Telefone</span>
                    <span className="zpcust-value">{profile.phoneNumber}</span>
                  </div>
                  <div className="zpcust-row">
                    <span className="zpcust-label">Endereco</span>
                    <span className="zpcust-value zpcust-address">
                      {buildAddressLines(profile).length > 0
                        ? buildAddressLines(profile).map((line) => <span key={line}>{line}</span>)
                        : "Sem endereco salvo"}
                    </span>
                  </div>
                  {profile.lastOrderAtUtc ? (
                    <div className="zpcust-row">
                      <span className="zpcust-label">Ultimo pedido</span>
                      <span className="zpcust-value">{formatDateTime(profile.lastOrderAtUtc)}</span>
                    </div>
                  ) : null}
                  <div className="zpcust-actions">
                    <button className="zpcust-btn zpcust-btn-ghost" type="button" onClick={() => setEditing(true)}>
                      Editar dados
                    </button>
                  </div>
                </div>
              ) : (
                <p className="zpcust-muted">Perfil ainda nao encontrado para este telefone.</p>
              )}

              {history.length > 0 ? (
                <div className="zpcust-history">
                  <span className="zpcust-label">Ultimos pedidos</span>
                  <ul className="zpcust-history-list">
                    {history.slice(0, HISTORY_LIMIT).map((entry) => (
                      <li key={entry.orderId} className="zpcust-history-item">
                        <div className="zpcust-history-head">
                          <span>{formatDateTime(entry.createdAtUtc)}</span>
                          <strong>{formatCurrency(entry.totalAmount)}</strong>
                        </div>
                        <div className="zpcust-history-items">
                          {entry.items.map((item, index) => (
                            <span key={`${entry.orderId}-${index}`}>
                              {item.quantity}x {item.itemName}
                            </span>
                          ))}
                        </div>
                      </li>
                    ))}
                  </ul>
                </div>
              ) : null}
            </>
          )}
        </div>
      ) : null}
    </section>
  );
}
