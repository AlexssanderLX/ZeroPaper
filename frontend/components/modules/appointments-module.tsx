"use client";

import { FormEvent, useEffect, useState } from "react";
import {
  createAppointment,
  getAppointmentAvailability,
  getAppointmentHistory,
  getAppointmentSettings,
  getAppointments,
  getCatalogServices,
  getPetShopCustomers,
  getPets,
  getProfessionals,
  rescheduleAppointment,
  updateAppointmentAssignee,
  updateAppointmentNotes,
  updateAppointmentStatus,
  type AppointmentDto,
  type AppointmentHistoryEntryDto,
  type AppointmentSettingsDto,
  type AppointmentStatus,
  type AvailabilityDto,
  type CatalogServiceDto,
  type CustomerProfileDto,
  type PetDto,
  type ProfessionalDto,
  ApiError,
} from "@/lib/api";
import { formatCurrency, formatDateTime, handleApiError, type AsyncVoid } from "@/components/modules/module-utils";

type View = "calendar" | "detail" | "create" | "reschedule";

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

function isTerminal(s: AppointmentStatus) {
  return s === 4 || s === 5 || s === 6;
}

function escapeText(v: string) {
  return v.replace(/[<>&"']/g, (c) => ({ "<": "&lt;", ">": "&gt;", "&": "&amp;", '"': "&quot;", "'": "&#39;" })[c] ?? c);
}

function localDateString(date: Date) {
  return date.toISOString().slice(0, 10);
}

function formatTimeUtc(utcStr: string, tz?: string) {
  try {
    return new Intl.DateTimeFormat("pt-BR", {
      hour: "2-digit",
      minute: "2-digit",
      timeZone: tz ?? "America/Sao_Paulo",
    }).format(new Date(utcStr));
  } catch {
    return utcStr.slice(11, 16);
  }
}

function getDayRange(dateStr: string): { fromUtc: string; toUtc: string } {
  return {
    fromUtc: `${dateStr}T00:00:00Z`,
    toUtc: `${dateStr}T23:59:59Z`,
  };
}

function getWeekRange(dateStr: string): { fromUtc: string; toUtc: string } {
  const d = new Date(dateStr);
  const day = d.getUTCDay();
  const monday = new Date(d);
  monday.setUTCDate(d.getUTCDate() - day + (day === 0 ? -6 : 1));
  const sunday = new Date(monday);
  sunday.setUTCDate(monday.getUTCDate() + 6);
  return {
    fromUtc: `${localDateString(monday)}T00:00:00Z`,
    toUtc: `${localDateString(sunday)}T23:59:59Z`,
  };
}

export function AppointmentsModule({ token, onUnauthorized }: { token: string; onUnauthorized: AsyncVoid }) {
  const [view, setView] = useState<View>("calendar");
  const [calendarMode, setCalendarMode] = useState<"day" | "week">("day");
  const [selectedDate, setSelectedDate] = useState(localDateString(new Date()));
  const [appointments, setAppointments] = useState<AppointmentDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [selectedAppointment, setSelectedAppointment] = useState<AppointmentDto | null>(null);
  const [history, setHistory] = useState<AppointmentHistoryEntryDto[]>([]);
  const [historyLoading, setHistoryLoading] = useState(false);
  const [professionals, setProfessionals] = useState<ProfessionalDto[]>([]);
  const [filterProfessional, setFilterProfessional] = useState("");
  const [errorMessage, setErrorMessage] = useState("");
  const [successMessage, setSuccessMessage] = useState("");
  const [isActioning, setIsActioning] = useState(false);
  const [cancellationReason, setCancellationReason] = useState("");
  const [showCancelForm, setShowCancelForm] = useState(false);
  const [internalNotes, setInternalNotes] = useState("");
  const [showNotesForm, setShowNotesForm] = useState(false);
  const [assigneeId, setAssigneeId] = useState("");
  const [showAssigneeForm, setShowAssigneeForm] = useState(false);

  // Create form
  const [customers, setCustomers] = useState<CustomerProfileDto[]>([]);
  const [selectedCustomerId, setSelectedCustomerId] = useState("");
  const [customerPets, setCustomerPets] = useState<PetDto[]>([]);
  const [services, setServices] = useState<CatalogServiceDto[]>([]);
  const [createPetId, setCreatePetId] = useState("");
  const [createServiceId, setCreateServiceId] = useState("");
  const [createAssigneeId, setCreateAssigneeId] = useState("");
  const [createDate, setCreateDate] = useState(selectedDate);
  const [availability, setAvailability] = useState<AvailabilityDto | null>(null);
  const [createSlot, setCreateSlot] = useState("");
  const [createNotes, setCreateNotes] = useState("");
  const [isSaving, setIsSaving] = useState(false);
  const [availabilityLoading, setAvailabilityLoading] = useState(false);

  // Reschedule form
  const [rescheduleDate, setRescheduleDate] = useState("");
  const [rescheduleAvailability, setRescheduleAvailability] = useState<AvailabilityDto | null>(null);
  const [rescheduleSlot, setRescheduleSlot] = useState("");
  const [rescheduleLoading, setRescheduleLoading] = useState(false);

  // Settings — loaded only for timezone display
  const [settings, setSettings] = useState<AppointmentSettingsDto | null>(null);

  const tz = settings?.timeZone;

  async function loadAppointments(date = selectedDate, mode = calendarMode, profId = filterProfessional) {
    setLoading(true);
    try {
      const range = mode === "day" ? getDayRange(date) : getWeekRange(date);
      const data = await getAppointments(token, {
        ...range,
        assignedUserId: profId || undefined,
      });
      setAppointments(data);
      setErrorMessage("");
    } catch (error) {
      await handleApiError(error, onUnauthorized, setErrorMessage, "Nao foi possivel carregar a agenda.");
    } finally {
      setLoading(false);
    }
  }

  async function loadSupport() {
    try {
      const [profs, srvs] = await Promise.all([getProfessionals(token), getCatalogServices(token)]);
      setProfessionals(profs);
      setServices(srvs.filter((s) => s.isPublished));
    } catch {
      // Non-critical
    }
  }

  useEffect(() => {
    void loadAppointments();
    void loadSupport();
    void loadSettings();
  }, [token]);

  function navigateDay(delta: number) {
    const d = new Date(selectedDate);
    d.setUTCDate(d.getUTCDate() + delta);
    const next = localDateString(d);
    setSelectedDate(next);
    void loadAppointments(next, calendarMode, filterProfessional);
  }

  function navigateWeek(delta: number) {
    const d = new Date(selectedDate);
    d.setUTCDate(d.getUTCDate() + delta * 7);
    const next = localDateString(d);
    setSelectedDate(next);
    void loadAppointments(next, calendarMode, filterProfessional);
  }

  function switchMode(mode: "day" | "week") {
    setCalendarMode(mode);
    void loadAppointments(selectedDate, mode, filterProfessional);
  }

  async function handleSelectAppointment(appt: AppointmentDto) {
    setSelectedAppointment(appt);
    setShowCancelForm(false);
    setShowNotesForm(false);
    setShowAssigneeForm(false);
    setInternalNotes(appt.internalNotes ?? "");
    setAssigneeId(appt.assignedUserId ?? "");
    setCancellationReason("");
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
    if (!selectedAppointment) return;
    setIsActioning(true);
    setErrorMessage("");
    try {
      const updated = await updateAppointmentStatus(token, selectedAppointment.id, {
        status: newStatus,
        cancellationReason: reason ?? null,
      });
      setSelectedAppointment(updated);
      setAppointments((prev) => prev.map((a) => (a.id === updated.id ? updated : a)));
      setShowCancelForm(false);
      setSuccessMessage(`Status atualizado para: ${statusLabel(newStatus)}`);
    } catch (error) {
      if (error instanceof ApiError && error.status === 409) {
        setErrorMessage("Transicao de status invalida ou agendamento ja nesse estado.");
      } else {
        await handleApiError(error, onUnauthorized, setErrorMessage, "Nao foi possivel alterar o status.");
      }
    } finally {
      setIsActioning(false);
    }
  }

  async function handleSaveNotes(e: FormEvent) {
    e.preventDefault();
    if (!selectedAppointment) return;
    setIsActioning(true);
    setErrorMessage("");
    try {
      const updated = await updateAppointmentNotes(token, selectedAppointment.id, {
        internalNotes: internalNotes.trim() || null,
        customerNotes: selectedAppointment.customerNotes,
      });
      setSelectedAppointment(updated);
      setShowNotesForm(false);
      setSuccessMessage("Observacoes salvas.");
    } catch (error) {
      await handleApiError(error, onUnauthorized, setErrorMessage, "Nao foi possivel salvar as observacoes.");
    } finally {
      setIsActioning(false);
    }
  }

  async function handleSaveAssignee(e: FormEvent) {
    e.preventDefault();
    if (!selectedAppointment) return;
    setIsActioning(true);
    setErrorMessage("");
    try {
      const updated = await updateAppointmentAssignee(token, selectedAppointment.id, {
        assignedUserId: assigneeId || null,
      });
      setSelectedAppointment(updated);
      setShowAssigneeForm(false);
      setSuccessMessage("Profissional atualizado.");
    } catch (error) {
      await handleApiError(error, onUnauthorized, setErrorMessage, "Nao foi possivel atualizar o profissional.");
    } finally {
      setIsActioning(false);
    }
  }

  // Create flow
  async function loadCustomersForCreate() {
    try {
      const res = await getPetShopCustomers(token, { pageSize: 200 });
      setCustomers(res.items);
    } catch {
      // Non-critical
    }
  }

  async function handleCustomerSelect(customerId: string) {
    setSelectedCustomerId(customerId);
    setCreatePetId("");
    setCustomerPets([]);
    if (!customerId) return;
    try {
      const res = await getPets(token, { customerProfileId: customerId, isActive: true, pageSize: 50 });
      setCustomerPets(res.items);
    } catch {
      // Non-critical
    }
  }

  async function loadAvailabilityForCreate() {
    if (!createServiceId || !createDate) return;
    setAvailabilityLoading(true);
    setCreateSlot("");
    setAvailability(null);
    try {
      const avail = await getAppointmentAvailability(token, {
        date: createDate,
        serviceId: createServiceId,
        assignedUserId: createAssigneeId || undefined,
      });
      setAvailability(avail);
    } catch (error) {
      if (error instanceof ApiError && error.status === 409) {
        setErrorMessage("Sem horarios disponiveis para esse dia.");
      } else {
        await handleApiError(error, onUnauthorized, setErrorMessage, "Nao foi possivel carregar horarios.");
      }
    } finally {
      setAvailabilityLoading(false);
    }
  }

  useEffect(() => {
    if (view === "create" && createServiceId && createDate) {
      void loadAvailabilityForCreate();
    }
  }, [createServiceId, createDate, createAssigneeId, view]);

  function openCreate() {
    void loadCustomersForCreate();
    setSelectedCustomerId("");
    setCustomerPets([]);
    setCreatePetId("");
    setCreateServiceId("");
    setCreateAssigneeId("");
    setCreateDate(selectedDate);
    setCreateSlot("");
    setCreateNotes("");
    setAvailability(null);
    setErrorMessage("");
    setSuccessMessage("");
    setView("create");
  }

  async function handleCreate(e: FormEvent) {
    e.preventDefault();
    if (!createSlot) {
      setErrorMessage("Selecione um horario.");
      return;
    }
    setIsSaving(true);
    setErrorMessage("");
    try {
      await createAppointment(token, {
        petId: createPetId,
        menuItemId: createServiceId,
        assignedUserId: createAssigneeId || null,
        startsAtUtc: createSlot,
        customerNotes: createNotes.trim() || null,
      });
      setSuccessMessage("Agendamento criado.");
      setView("calendar");
      await loadAppointments(selectedDate, calendarMode, filterProfessional);
    } catch (error) {
      if (error instanceof ApiError && error.status === 409) {
        setErrorMessage("Horario indisponivel. Por favor escolha outro horario.");
        setCreateSlot("");
        void loadAvailabilityForCreate();
      } else {
        await handleApiError(error, onUnauthorized, setErrorMessage, "Nao foi possivel criar o agendamento.");
      }
    } finally {
      setIsSaving(false);
    }
  }

  // Reschedule flow
  async function loadRescheduleAvailability() {
    if (!selectedAppointment || !rescheduleDate) return;
    setRescheduleLoading(true);
    setRescheduleSlot("");
    setRescheduleAvailability(null);
    try {
      const avail = await getAppointmentAvailability(token, {
        date: rescheduleDate,
        serviceId: selectedAppointment.menuItemId,
        assignedUserId: selectedAppointment.assignedUserId ?? undefined,
      });
      setRescheduleAvailability(avail);
    } catch {
      setErrorMessage("Nao foi possivel carregar horarios para reagendamento.");
    } finally {
      setRescheduleLoading(false);
    }
  }

  useEffect(() => {
    if (view === "reschedule" && rescheduleDate) {
      void loadRescheduleAvailability();
    }
  }, [rescheduleDate, view]);

  function openReschedule() {
    if (!selectedAppointment) return;
    setRescheduleDate(selectedAppointment.startsAtUtc.slice(0, 10));
    setRescheduleSlot("");
    setRescheduleAvailability(null);
    setErrorMessage("");
    setView("reschedule");
  }

  async function handleReschedule(e: FormEvent) {
    e.preventDefault();
    if (!selectedAppointment || !rescheduleSlot) {
      setErrorMessage("Selecione um horario.");
      return;
    }
    setIsSaving(true);
    setErrorMessage("");
    try {
      const updated = await rescheduleAppointment(token, selectedAppointment.id, {
        startsAtUtc: rescheduleSlot,
      });
      setSelectedAppointment(updated);
      setAppointments((prev) => prev.map((a) => (a.id === updated.id ? updated : a)));
      setSuccessMessage("Agendamento reagendado.");
      setView("detail");
    } catch (error) {
      if (error instanceof ApiError && error.status === 409) {
        setErrorMessage("Horario indisponivel. Escolha outro horario.");
        setRescheduleSlot("");
        void loadRescheduleAvailability();
      } else {
        await handleApiError(error, onUnauthorized, setErrorMessage, "Nao foi possivel reagendar.");
      }
    } finally {
      setIsSaving(false);
    }
  }

  // Settings — load only for timezone
  async function loadSettings() {
    try {
      const s = await getAppointmentSettings(token);
      setSettings(s);
    } catch {
      // Non-critical
    }
  }

  // Group by day for week view
  function groupByDay(appts: AppointmentDto[]) {
    const map = new Map<string, AppointmentDto[]>();
    for (const a of appts) {
      const day = a.startsAtUtc.slice(0, 10);
      if (!map.has(day)) map.set(day, []);
      map.get(day)!.push(a);
    }
    return map;
  }

  // ─── Reschedule view ────────────────────────────────────────────────────────
  if (view === "reschedule" && selectedAppointment) {
    return (
      <section className="surface-card workspace-summary-card module-summary-card simple-module-summary">
        <div className="workspace-summary-head">
          <div className="hero-stack">
            <h1>Reagendar — {escapeText(selectedAppointment.petName)}</h1>
          </div>
          <div className="toolbar-actions compact">
            <button type="button" className="secondary-button" onClick={() => setView("detail")}>
              Cancelar
            </button>
          </div>
        </div>

        {errorMessage && <p className="error-message">{errorMessage}</p>}

        <form onSubmit={handleReschedule} className="settings-form">
          <div className="form-field">
            <label htmlFor="rs-date">Data</label>
            <input
              id="rs-date"
              type="date"
              required
              value={rescheduleDate}
              onChange={(e) => setRescheduleDate(e.target.value)}
            />
          </div>

          {rescheduleLoading && <p className="workspace-inline-loading">Carregando horarios...</p>}

          {!rescheduleLoading && rescheduleAvailability && (
            <div className="form-field">
              <label>Horario disponivel</label>
              {rescheduleAvailability.slots.length === 0 ? (
                <p className="body-copy">Sem horarios para esta data.</p>
              ) : (
                <div style={{ display: "flex", flexWrap: "wrap", gap: "0.5rem" }}>
                  {rescheduleAvailability.slots.map((slot) => (
                    <button
                      key={slot.startsAtUtc}
                      type="button"
                      className={rescheduleSlot === slot.startsAtUtc ? "primary-button" : "secondary-button"}
                      onClick={() => setRescheduleSlot(slot.startsAtUtc)}
                    >
                      {formatTimeUtc(slot.startsAtUtc, tz)}
                    </button>
                  ))}
                </div>
              )}
            </div>
          )}

          <div className="toolbar-actions">
            <button type="submit" className="primary-button" disabled={isSaving || !rescheduleSlot}>
              {isSaving ? "Salvando..." : "Confirmar reagendamento"}
            </button>
          </div>
        </form>
      </section>
    );
  }

  // ─── Settings view ───────────────────────────────────────────────────────────
  // ─── Create view ─────────────────────────────────────────────────────────────
  if (view === "create") {
    return (
      <section className="surface-card workspace-summary-card module-summary-card simple-module-summary">
        <div className="workspace-summary-head">
          <div className="hero-stack">
            <h1>Novo agendamento</h1>
          </div>
          <div className="toolbar-actions compact">
            <button
              type="button"
              className="secondary-button"
              onClick={() => {
                setErrorMessage("");
                setView("calendar");
              }}
            >
              Cancelar
            </button>
          </div>
        </div>

        {errorMessage && <p className="error-message">{errorMessage}</p>}

        <form onSubmit={handleCreate} className="settings-form">
          <div className="form-field">
            <label htmlFor="cr-customer">Tutor</label>
            <select
              id="cr-customer"
              required
              value={selectedCustomerId}
              onChange={(e) => void handleCustomerSelect(e.target.value)}
            >
              <option value="">Selecione o tutor...</option>
              {customers.map((c) => (
                <option key={c.id} value={c.id}>
                  {c.name} ({c.phoneNumber})
                </option>
              ))}
            </select>
          </div>

          {selectedCustomerId && (
            <div className="form-field">
              <label htmlFor="cr-pet">Animal</label>
              <select
                id="cr-pet"
                required
                value={createPetId}
                onChange={(e) => setCreatePetId(e.target.value)}
              >
                <option value="">Selecione o animal...</option>
                {customerPets.map((p) => (
                  <option key={p.id} value={p.id}>
                    {p.name}
                  </option>
                ))}
              </select>
            </div>
          )}

          <div className="form-field">
            <label htmlFor="cr-service">Servico</label>
            <select
              id="cr-service"
              required
              value={createServiceId}
              onChange={(e) => setCreateServiceId(e.target.value)}
            >
              <option value="">Selecione o servico...</option>
              {services.map((s) => (
                <option key={s.id} value={s.id}>
                  {s.name} ({s.estimatedDurationMinutes} min) — {formatCurrency(s.price)}
                </option>
              ))}
            </select>
          </div>

          {professionals.length > 0 && (
            <div className="form-field">
              <label htmlFor="cr-prof">Profissional</label>
              <select
                id="cr-prof"
                value={createAssigneeId}
                onChange={(e) => setCreateAssigneeId(e.target.value)}
              >
                <option value="">Qualquer profissional</option>
                {professionals.map((p) => (
                  <option key={p.id} value={p.id}>
                    {p.name}
                  </option>
                ))}
              </select>
            </div>
          )}

          <div className="form-field">
            <label htmlFor="cr-date">Data</label>
            <input
              id="cr-date"
              type="date"
              required
              value={createDate}
              onChange={(e) => setCreateDate(e.target.value)}
            />
          </div>

          {availabilityLoading && <p className="workspace-inline-loading">Carregando horarios...</p>}

          {!availabilityLoading && availability && createServiceId && (
            <div className="form-field">
              <label>Horario</label>
              {availability.slots.length === 0 ? (
                <p className="body-copy">Sem horarios disponiveis para esta data.</p>
              ) : (
                <div style={{ display: "flex", flexWrap: "wrap", gap: "0.5rem" }}>
                  {availability.slots.map((slot) => (
                    <button
                      key={slot.startsAtUtc}
                      type="button"
                      className={createSlot === slot.startsAtUtc ? "primary-button" : "secondary-button"}
                      onClick={() => setCreateSlot(slot.startsAtUtc)}
                    >
                      {formatTimeUtc(slot.startsAtUtc, tz)}
                    </button>
                  ))}
                </div>
              )}
            </div>
          )}

          <div className="form-field">
            <label htmlFor="cr-notes">Observacoes do cliente</label>
            <textarea
              id="cr-notes"
              rows={2}
              value={createNotes}
              onChange={(e) => setCreateNotes(e.target.value)}
            />
          </div>

          <div className="toolbar-actions">
            <button
              type="submit"
              className="primary-button"
              disabled={isSaving || !createPetId || !createServiceId || !createSlot}
            >
              {isSaving ? "Criando..." : "Criar agendamento"}
            </button>
          </div>
        </form>
      </section>
    );
  }

  // ─── Detail view ─────────────────────────────────────────────────────────────
  if (view === "detail" && selectedAppointment) {
    const appt = selectedAppointment;
    const terminal = isTerminal(appt.status);
    const canConfirm = appt.status === 1;
    const canStart = appt.status === 2;
    const canComplete = appt.status === 3;
    const canCancel = appt.status === 1 || appt.status === 2;
    const canNoShow = appt.status === 2;
    const canReschedule = appt.status === 1 || appt.status === 2;

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
            <button
              type="button"
              className="secondary-button"
              onClick={() => {
                setErrorMessage("");
                setSuccessMessage("");
                setView("calendar");
              }}
            >
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
            <strong>Fim:</strong> {formatDateTime(appt.endsAtUtc)}
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
          {appt.cancellationReason && (
            <p>
              <strong>Motivo cancelamento:</strong> {escapeText(appt.cancellationReason)}
            </p>
          )}
          {appt.customerOrderId && (
            <p>
              <strong>Pedido vinculado:</strong> {appt.customerOrderId.slice(0, 8)}...
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
            {canReschedule && (
              <button
                type="button"
                className="secondary-button"
                onClick={openReschedule}
              >
                Reagendar
              </button>
            )}
            {canNoShow && (
              <button
                type="button"
                className="secondary-button"
                disabled={isActioning}
                onClick={() => void handleStatusChange(6)}
              >
                Ausencia
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
              <label htmlFor="cancel-reason">Motivo do cancelamento</label>
              <input
                id="cancel-reason"
                type="text"
                required
                value={cancellationReason}
                onChange={(e) => setCancellationReason(e.target.value)}
              />
            </div>
            <div className="toolbar-actions compact">
              <button type="submit" className="primary-button" disabled={isActioning}>
                Confirmar cancelamento
              </button>
              <button type="button" className="secondary-button" onClick={() => setShowCancelForm(false)}>
                Desistir
              </button>
            </div>
          </form>
        )}

        <div style={{ marginTop: "1.5rem" }}>
          <div className="toolbar-actions compact">
            <button
              type="button"
              className="secondary-button"
              onClick={() => setShowNotesForm((v) => !v)}
            >
              {showNotesForm ? "Fechar observacoes" : "Editar observacao interna"}
            </button>
            {professionals.length > 0 && (
              <button
                type="button"
                className="secondary-button"
                onClick={() => setShowAssigneeForm((v) => !v)}
              >
                {showAssigneeForm ? "Fechar profissional" : "Alterar profissional"}
              </button>
            )}
          </div>

          {showNotesForm && (
            <form onSubmit={handleSaveNotes} className="settings-form" style={{ marginTop: "1rem" }}>
              <div className="form-field">
                <label htmlFor="int-notes">Observacao interna</label>
                <textarea
                  id="int-notes"
                  rows={3}
                  value={internalNotes}
                  onChange={(e) => setInternalNotes(e.target.value)}
                />
              </div>
              <button type="submit" className="primary-button" disabled={isActioning}>
                Salvar
              </button>
            </form>
          )}

          {showAssigneeForm && (
            <form onSubmit={handleSaveAssignee} className="settings-form" style={{ marginTop: "1rem" }}>
              <div className="form-field">
                <label htmlFor="assignee">Profissional</label>
                <select
                  id="assignee"
                  value={assigneeId}
                  onChange={(e) => setAssigneeId(e.target.value)}
                >
                  <option value="">Nenhum</option>
                  {professionals.map((p) => (
                    <option key={p.id} value={p.id}>
                      {p.name}
                    </option>
                  ))}
                </select>
              </div>
              <button type="submit" className="primary-button" disabled={isActioning}>
                Salvar
              </button>
            </form>
          )}
        </div>

        <div style={{ marginTop: "1.5rem" }}>
          <h2>Historico</h2>
          {historyLoading && <p className="workspace-inline-loading">Carregando...</p>}
          {!historyLoading && history.length === 0 && <p className="body-copy">Sem historico.</p>}
          {!historyLoading && history.length > 0 && (
            <ul className="simple-list">
              {history.map((h) => (
                <li key={h.id} className="simple-list-item">
                  <span>
                    {h.previousStatus ? `${statusLabel(h.previousStatus)} → ` : ""}
                    <strong>{statusLabel(h.newStatus)}</strong>
                  </span>
                  {h.changedByUserName && <span> por {escapeText(h.changedByUserName)}</span>}
                  <span> em {formatDateTime(h.changedAtUtc)}</span>
                  {h.reason && <span> — {escapeText(h.reason)}</span>}
                </li>
              ))}
            </ul>
          )}
        </div>
      </section>
    );
  }

  // ─── Calendar (main) view ────────────────────────────────────────────────────
  const grouped = calendarMode === "week" ? groupByDay(appointments) : null;

  return (
    <section className="surface-card workspace-summary-card module-summary-card simple-module-summary">
      <div className="workspace-summary-head">
        <div className="hero-stack">
          <h1>Agenda</h1>
        </div>
        <div className="toolbar-actions compact">
          <button type="button" className="primary-button" onClick={openCreate}>
            Novo agendamento
          </button>
        </div>
      </div>

      {errorMessage && <p className="error-message">{errorMessage}</p>}
      {successMessage && <p className="success-message">{successMessage}</p>}

      <div className="ps-cal-toolbar">
        <button
          type="button"
          className={calendarMode === "day" ? "primary-button" : "secondary-button"}
          onClick={() => switchMode("day")}
        >
          Dia
        </button>
        <button
          type="button"
          className={calendarMode === "week" ? "primary-button" : "secondary-button"}
          onClick={() => switchMode("week")}
        >
          Semana
        </button>

        <div className="ps-date-nav">
          <button
            type="button"
            className="ps-nav-btn"
            aria-label="Anterior"
            onClick={() => (calendarMode === "day" ? navigateDay(-1) : navigateWeek(-1))}
          >
            ‹
          </button>
          <input
            type="date"
            className="ps-date-input"
            value={selectedDate}
            onChange={(e) => {
              setSelectedDate(e.target.value);
              void loadAppointments(e.target.value, calendarMode, filterProfessional);
            }}
          />
          <button
            type="button"
            className="ps-nav-btn"
            aria-label="Próximo"
            onClick={() => (calendarMode === "day" ? navigateDay(1) : navigateWeek(1))}
          >
            ›
          </button>
        </div>

        {professionals.length > 0 && (
          <select
            value={filterProfessional}
            onChange={(e) => {
              setFilterProfessional(e.target.value);
              void loadAppointments(selectedDate, calendarMode, e.target.value);
            }}
            className="ps-select"
          >
            <option value="">Todos os profissionais</option>
            {professionals.map((p) => (
              <option key={p.id} value={p.id}>
                {p.name}
              </option>
            ))}
          </select>
        )}
      </div>

      {loading && <p className="workspace-inline-loading">Carregando...</p>}

      {!loading && calendarMode === "day" && (
        <>
          {appointments.length === 0 && (
            <p className="body-copy">Nenhum agendamento para este dia.</p>
          )}
          <ul className="simple-list">
            {appointments
              .sort((a, b) => a.startsAtUtc.localeCompare(b.startsAtUtc))
              .map((appt) => (
                <li
                  key={appt.id}
                  className="simple-list-item simple-list-item--clickable"
                  onClick={() => void handleSelectAppointment(appt)}
                >
                  <div className="ps-entity-row">
                    <div className="ps-entity-main">
                      <strong>{escapeText(appt.petName)}</strong>
                      <span className="ps-entity-meta">
                        {escapeText(appt.serviceName)}
                        {appt.assignedUserName && ` · ${escapeText(appt.assignedUserName)}`}
                      </span>
                    </div>
                    <div className="ps-entity-aside">
                      <span className="ps-time-badge">{formatTimeUtc(appt.startsAtUtc, tz)}</span>
                      <span className="ps-entity-meta">{statusLabel(appt.status)}</span>
                    </div>
                  </div>
                </li>
              ))}
          </ul>
        </>
      )}

      {!loading && calendarMode === "week" && grouped && (
        <div>
          {grouped.size === 0 && <p className="body-copy">Nenhum agendamento esta semana.</p>}
          {Array.from(grouped.entries())
            .sort(([a], [b]) => a.localeCompare(b))
            .map(([day, appts]) => (
              <div key={day} style={{ marginBottom: "1.5rem" }}>
                <h2 style={{ marginBottom: "0.5rem" }}>
                  {new Intl.DateTimeFormat("pt-BR", { weekday: "long", day: "2-digit", month: "2-digit" }).format(
                    new Date(`${day}T12:00:00Z`),
                  )}
                </h2>
                <ul className="simple-list">
                  {appts
                    .sort((a, b) => a.startsAtUtc.localeCompare(b.startsAtUtc))
                    .map((appt) => (
                      <li
                        key={appt.id}
                        className="simple-list-item simple-list-item--clickable"
                        onClick={() => void handleSelectAppointment(appt)}
                      >
                        <div className="ps-entity-row">
                          <div className="ps-entity-main">
                            <strong>{escapeText(appt.petName)}</strong>
                            <span className="ps-entity-meta">{escapeText(appt.serviceName)} · {statusLabel(appt.status)}</span>
                          </div>
                          <span className="ps-time-badge">{formatTimeUtc(appt.startsAtUtc, tz)}</span>
                        </div>
                      </li>
                    ))}
                </ul>
              </div>
            ))}
        </div>
      )}
    </section>
  );
}
