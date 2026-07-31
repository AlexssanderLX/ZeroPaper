"use client";

import { FormEvent, useEffect, useState } from "react";
import {
  createAppointmentOrder,
  getAppointments,
  getAppointmentHistory,
  updateAppointmentStatus,
  type AppointmentDto,
  type AppointmentHistoryEntryDto,
  type AppointmentStatus,
  ApiError,
} from "@/lib/api";
import { formatCurrency, formatDateTime, handleApiError, type AsyncVoid } from "@/components/modules/module-utils";

function statusLabel(s: AppointmentStatus) {
  const map: Record<number, string> = {
    1: "Solicitado",
    2: "Confirmado",
    3: "Em atendimento",
    4: "Concluido",
    5: "Cancelado",
    6: "Ausencia",
  };
  return map[s] ?? String(s);
}

function escapeText(v: string) {
  return v.replace(/[<>&"']/g, (c) => ({ "<": "&lt;", ">": "&gt;", "&": "&amp;", '"': "&quot;", "'": "&#39;" })[c] ?? c);
}

function localDateString(d: Date) {
  return d.toISOString().slice(0, 10);
}

function getDayRange(dateStr: string) {
  return {
    fromUtc: `${dateStr}T00:00:00Z`,
    toUtc: `${dateStr}T23:59:59Z`,
  };
}

type View = "list" | "detail";

export function PetAttendanceModule({ token, onUnauthorized }: { token: string; onUnauthorized: AsyncVoid }) {
  const today = localDateString(new Date());
  const [view, setView] = useState<View>("list");
  const [selectedDate, setSelectedDate] = useState(today);
  const [appointments, setAppointments] = useState<AppointmentDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [selected, setSelected] = useState<AppointmentDto | null>(null);
  const [history, setHistory] = useState<AppointmentHistoryEntryDto[]>([]);
  const [historyLoading, setHistoryLoading] = useState(false);
  const [errorMessage, setErrorMessage] = useState("");
  const [successMessage, setSuccessMessage] = useState("");
  const [isActioning, setIsActioning] = useState(false);
  const [showOrderForm, setShowOrderForm] = useState(false);
  const [orderPaymentMethod, setOrderPaymentMethod] = useState("Pix");
  const [orderNotes, setOrderNotes] = useState("");
  const [orderSaving, setOrderSaving] = useState(false);
  const [showCancelForm, setShowCancelForm] = useState(false);
  const [cancellationReason, setCancellationReason] = useState("");

  async function loadAppointments(date = selectedDate) {
    setLoading(true);
    try {
      const range = getDayRange(date);
      const data = await getAppointments(token, range);
      setAppointments(data.sort((a, b) => a.startsAtUtc.localeCompare(b.startsAtUtc)));
      setErrorMessage("");
    } catch (error) {
      await handleApiError(error, onUnauthorized, setErrorMessage, "Nao foi possivel carregar os atendimentos.");
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    void loadAppointments();
  }, [token]);

  async function handleSelect(appt: AppointmentDto) {
    setSelected(appt);
    setShowOrderForm(false);
    setShowCancelForm(false);
    setErrorMessage("");
    setSuccessMessage("");
    setView("detail");
    setHistoryLoading(true);
    setHistory([]);
    try {
      const h = await getAppointmentHistory(token, appt.id);
      setHistory(h);
    } catch {
      // Non-critical
    } finally {
      setHistoryLoading(false);
    }
  }

  async function handleStatusChange(newStatus: AppointmentStatus, reason?: string) {
    if (!selected) return;
    setIsActioning(true);
    setErrorMessage("");
    try {
      const updated = await updateAppointmentStatus(token, selected.id, {
        status: newStatus,
        cancellationReason: reason ?? null,
      });
      setSelected(updated);
      setAppointments((prev) => prev.map((a) => (a.id === updated.id ? updated : a)));
      setShowCancelForm(false);
      setSuccessMessage(`Status: ${statusLabel(newStatus)}`);
    } catch (error) {
      if (error instanceof ApiError && error.status === 409) {
        setErrorMessage("Transicao invalida ou agendamento ja nesse estado.");
      } else {
        await handleApiError(error, onUnauthorized, setErrorMessage, "Nao foi possivel alterar o status.");
      }
    } finally {
      setIsActioning(false);
    }
  }

  async function handleCreateOrder(e: FormEvent) {
    e.preventDefault();
    if (!selected) return;
    setOrderSaving(true);
    setErrorMessage("");
    try {
      const res = await createAppointmentOrder(token, selected.id, {
        paymentMethod: orderPaymentMethod,
        notes: orderNotes.trim() || null,
      });
      setSelected(res.appointment);
      setAppointments((prev) => prev.map((a) => (a.id === res.appointment.id ? res.appointment : a)));
      setShowOrderForm(false);
      setSuccessMessage("Pedido criado e vinculado ao atendimento.");
    } catch (error) {
      if (error instanceof ApiError && error.status === 409) {
        setErrorMessage("Ja existe um pedido vinculado ou atendimento em estado invalido.");
      } else {
        await handleApiError(error, onUnauthorized, setErrorMessage, "Nao foi possivel criar o pedido.");
      }
    } finally {
      setOrderSaving(false);
    }
  }

  // Grouped counts for the dashboard
  const counts = {
    requested: appointments.filter((a) => a.status === 1).length,
    confirmed: appointments.filter((a) => a.status === 2).length,
    inProgress: appointments.filter((a) => a.status === 3).length,
    completed: appointments.filter((a) => a.status === 4).length,
    cancelled: appointments.filter((a) => a.status === 5 || a.status === 6).length,
  };

  if (view === "detail" && selected) {
    const appt = selected;
    const terminal = appt.status === 4 || appt.status === 5 || appt.status === 6;
    const canConfirm = appt.status === 1;
    const canStart = appt.status === 2;
    const canComplete = appt.status === 3;
    const canCancel = appt.status === 1 || appt.status === 2;
    const canCreateOrder = !appt.customerOrderId && (appt.status === 2 || appt.status === 3 || appt.status === 4);

    return (
      <section className="surface-card workspace-summary-card module-summary-card simple-module-summary">
        <div className="workspace-summary-head">
          <div className="hero-stack">
            <h1>{escapeText(appt.petName)}</h1>
            <p className="body-copy">
              {escapeText(appt.serviceName)} &middot; {statusLabel(appt.status)}
            </p>
          </div>
          <div className="toolbar-actions compact">
            <button type="button" className="secondary-button" onClick={() => setView("list")}>
              Voltar
            </button>
          </div>
        </div>

        {errorMessage && <p className="error-message">{errorMessage}</p>}
        {successMessage && <p className="success-message">{successMessage}</p>}

        <div className="settings-form">
          <p>
            <strong>Inicio:</strong> {formatDateTime(appt.startsAtUtc)}
          </p>
          <p>
            <strong>Duracao:</strong> {appt.durationMinutes} min
          </p>
          <p>
            <strong>Valor:</strong> {formatCurrency(appt.unitPrice)}
          </p>
          {appt.assignedUserName && (
            <p>
              <strong>Profissional:</strong> {escapeText(appt.assignedUserName)}
            </p>
          )}
          {appt.customerNotes && (
            <p>
              <strong>Obs. cliente:</strong> {escapeText(appt.customerNotes)}
            </p>
          )}
          {appt.internalNotes && (
            <p>
              <strong>Obs. interna:</strong> {escapeText(appt.internalNotes)}
            </p>
          )}
          {appt.customerOrderId ? (
            <p>
              <strong>Pedido:</strong> vinculado ({appt.customerOrderId.slice(0, 8)}...)
            </p>
          ) : (
            <p>
              <strong>Pedido:</strong> nenhum vinculado
            </p>
          )}
          {appt.cancellationReason && (
            <p>
              <strong>Cancelamento:</strong> {escapeText(appt.cancellationReason)}
            </p>
          )}
        </div>

        {!terminal && (
          <div className="toolbar-actions compact" style={{ marginTop: "1rem", flexWrap: "wrap" }}>
            {canConfirm && (
              <button
                type="button"
                className="primary-button"
                disabled={isActioning}
                onClick={() => void handleStatusChange(2)}
              >
                Confirmar
              </button>
            )}
            {canStart && (
              <button
                type="button"
                className="primary-button"
                disabled={isActioning}
                onClick={() => void handleStatusChange(3)}
              >
                Iniciar
              </button>
            )}
            {canComplete && (
              <button
                type="button"
                className="primary-button"
                disabled={isActioning}
                onClick={() => void handleStatusChange(4)}
              >
                Concluir
              </button>
            )}
            {canCancel && !showCancelForm && (
              <button
                type="button"
                className="secondary-button"
                onClick={() => setShowCancelForm(true)}
              >
                Cancelar
              </button>
            )}
          </div>
        )}

        {showCancelForm && (
          <form
            onSubmit={(e) => {
              e.preventDefault();
              void handleStatusChange(5, cancellationReason);
            }}
            className="settings-form"
            style={{ marginTop: "1rem" }}
          >
            <div className="form-field">
              <label htmlFor="at-cancel-reason">Motivo do cancelamento</label>
              <input
                id="at-cancel-reason"
                type="text"
                required
                value={cancellationReason}
                onChange={(e) => setCancellationReason(e.target.value)}
              />
            </div>
            <div className="toolbar-actions compact">
              <button type="submit" className="primary-button" disabled={isActioning}>
                Confirmar
              </button>
              <button type="button" className="secondary-button" onClick={() => setShowCancelForm(false)}>
                Desistir
              </button>
            </div>
          </form>
        )}

        {canCreateOrder && (
          <div style={{ marginTop: "1rem" }}>
            <button
              type="button"
              className="secondary-button"
              onClick={() => setShowOrderForm((v) => !v)}
            >
              {showOrderForm ? "Fechar" : "Gerar pedido de pagamento"}
            </button>

            {showOrderForm && (
              <form onSubmit={handleCreateOrder} className="settings-form" style={{ marginTop: "1rem" }}>
                <div className="form-field">
                  <label htmlFor="at-payment">Forma de pagamento</label>
                  <select
                    id="at-payment"
                    value={orderPaymentMethod}
                    onChange={(e) => setOrderPaymentMethod(e.target.value)}
                  >
                    <option value="Pix">Pix</option>
                    <option value="Credit">Credito</option>
                    <option value="Debit">Debito</option>
                    <option value="Cash">Dinheiro</option>
                    <option value="Undefined">Escolher no caixa</option>
                  </select>
                </div>
                <div className="form-field">
                  <label htmlFor="at-order-notes">Obs. do pedido</label>
                  <input
                    id="at-order-notes"
                    type="text"
                    value={orderNotes}
                    onChange={(e) => setOrderNotes(e.target.value)}
                  />
                </div>
                <button type="submit" className="primary-button" disabled={orderSaving}>
                  {orderSaving ? "Criando..." : "Criar pedido"}
                </button>
              </form>
            )}
          </div>
        )}

        <div style={{ marginTop: "1.5rem" }}>
          <h2>Historico</h2>
          {historyLoading && <p className="workspace-inline-loading">Carregando...</p>}
          {!historyLoading && history.length === 0 && <p className="body-copy">Sem historico.</p>}
          {!historyLoading && history.length > 0 && (
            <ul className="simple-list">
              {history.map((h) => (
                <li key={h.id} className="simple-list-item">
                  {h.previousStatus ? `${statusLabel(h.previousStatus)} → ` : ""}
                  <strong>{statusLabel(h.newStatus)}</strong>
                  {h.changedByUserName && ` por ${escapeText(h.changedByUserName)}`}
                  {` em ${formatDateTime(h.changedAtUtc)}`}
                  {h.reason && ` — ${escapeText(h.reason)}`}
                </li>
              ))}
            </ul>
          )}
        </div>
      </section>
    );
  }

  return (
    <section className="surface-card workspace-summary-card module-summary-card simple-module-summary">
      <div className="workspace-summary-head">
        <div className="hero-stack">
          <h1>Atendimentos do dia</h1>
        </div>
        <div className="toolbar-actions compact">
          <input
            type="date"
            value={selectedDate}
            onChange={(e) => {
              setSelectedDate(e.target.value);
              void loadAppointments(e.target.value);
            }}
          />
        </div>
      </div>

      {errorMessage && <p className="error-message">{errorMessage}</p>}

      {/* Summary badges */}
      <div style={{ display: "flex", gap: "0.75rem", flexWrap: "wrap", marginBottom: "1rem" }}>
        {[
          { label: "Solicitados", count: counts.requested },
          { label: "Confirmados", count: counts.confirmed },
          { label: "Em atendimento", count: counts.inProgress },
          { label: "Concluidos", count: counts.completed },
          { label: "Cancelados/Ausencia", count: counts.cancelled },
        ].map((s) => (
          <div
            key={s.label}
            style={{
              padding: "0.5rem 0.75rem",
              borderRadius: 8,
              background: "var(--color-surface-secondary, #f5f5f5)",
              fontSize: "0.875em",
            }}
          >
            <strong>{s.count}</strong> {s.label}
          </div>
        ))}
      </div>

      {loading && <p className="workspace-inline-loading">Carregando...</p>}

      {!loading && appointments.length === 0 && (
        <p className="body-copy">Nenhum atendimento para este dia.</p>
      )}

      {!loading && appointments.length > 0 && (
        <ul className="simple-list">
          {appointments.map((appt) => (
            <li
              key={appt.id}
              className="simple-list-item simple-list-item--clickable"
              onClick={() => void handleSelect(appt)}
            >
              <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center" }}>
                <div>
                  <strong>{escapeText(appt.petName)}</strong>
                  <br />
                  <span className="body-copy">
                    {escapeText(appt.serviceName)}
                    {appt.assignedUserName && ` — ${escapeText(appt.assignedUserName)}`}
                  </span>
                </div>
                <span className="body-copy">{statusLabel(appt.status)}</span>
              </div>
            </li>
          ))}
        </ul>
      )}
    </section>
  );
}
