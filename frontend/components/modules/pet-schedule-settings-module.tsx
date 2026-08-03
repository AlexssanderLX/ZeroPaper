"use client";

import { FormEvent, useEffect, useState } from "react";
import {
  getAppointmentSettings,
  updateAppointmentSettings,
  getAppointmentBlocks,
  createAppointmentBlock,
  deleteAppointmentBlock,
  getProfessionals,
  type AppointmentBlockDto,
  type ProfessionalDto,
} from "@/lib/api";
import { formatDateTime, handleApiError, type AsyncVoid } from "@/components/modules/module-utils";

const WEEK_DAYS = [
  { value: 1, shortLabel: "Seg", label: "Segunda" },
  { value: 2, shortLabel: "Ter", label: "Terca" },
  { value: 3, shortLabel: "Qua", label: "Quarta" },
  { value: 4, shortLabel: "Qui", label: "Quinta" },
  { value: 5, shortLabel: "Sex", label: "Sexta" },
  { value: 6, shortLabel: "Sab", label: "Sabado" },
  { value: 0, shortLabel: "Dom", label: "Domingo" },
];

const ALL_DAY_VALUES = WEEK_DAYS.map((d) => d.value);

const SLOT_OPTIONS = [
  { value: 15, label: "15 min" },
  { value: 30, label: "30 min" },
  { value: 45, label: "45 min" },
  { value: 60, label: "1 hora" },
  { value: 90, label: "1h30" },
  { value: 120, label: "2 horas" },
];

function parseDays(serviceDays: string): number[] {
  return serviceDays
    .split(",")
    .map((s) => parseInt(s.trim(), 10))
    .filter((n) => !isNaN(n) && n >= 0 && n <= 6);
}

function getDaysLabel(days: number[]) {
  if (days.length === 0) return "Nenhum dia";
  if (days.length === WEEK_DAYS.length) return "Todos os dias";
  const sorted = [...days].sort((a, b) => a - b);
  if (JSON.stringify(sorted) === JSON.stringify([1, 2, 3, 4, 5])) return "Segunda a sexta";
  if (JSON.stringify(sorted) === JSON.stringify([1, 2, 3, 4, 5, 6])) return "Segunda a sabado";
  return WEEK_DAYS.filter((d) => days.includes(d.value))
    .map((d) => d.shortLabel)
    .join(", ");
}

function escapeText(v: string) {
  return v.replace(/[<>&"']/g, (c) => ({ "<": "&lt;", ">": "&gt;", "&": "&amp;", '"': "&quot;", "'": "&#39;" })[c] ?? c);
}

type Tab = "config" | "bloqueios";

export function PetScheduleSettingsModule({
  token,
  onUnauthorized,
}: {
  token: string;
  onUnauthorized: AsyncVoid;
}) {
  const [tab, setTab] = useState<Tab>("config");

  // Settings
  const [selectedDays, setSelectedDays] = useState<number[]>([1, 2, 3, 4, 5, 6]);
  const [startTime, setStartTime] = useState("08:00");
  const [endTime, setEndTime] = useState("18:00");
  const [slotInterval, setSlotInterval] = useState(30);
  const [configLoading, setConfigLoading] = useState(true);
  const [configSaving, setConfigSaving] = useState(false);

  // Blocks
  const [blocks, setBlocks] = useState<AppointmentBlockDto[]>([]);
  const [blocksLoading, setBlocksLoading] = useState(false);
  const [professionals, setProfessionals] = useState<ProfessionalDto[]>([]);
  const [blockForm, setBlockForm] = useState({
    assignedUserId: "",
    startsAtLocal: "",
    endsAtLocal: "",
    reason: "",
  });
  const [blockSaving, setBlockSaving] = useState(false);

  // Feedback
  const [successMessage, setSuccessMessage] = useState("");
  const [errorMessage, setErrorMessage] = useState("");

  function clearFeedback() {
    setSuccessMessage("");
    setErrorMessage("");
  }

  useEffect(() => {
    let isMounted = true;

    void getAppointmentSettings(token)
      .then((res) => {
        if (!isMounted) return;
        setSelectedDays(parseDays(res.serviceDays));
        setStartTime(res.startTime || "08:00");
        setEndTime(res.endTime || "18:00");
        setSlotInterval(res.slotIntervalMinutes || 30);
        setConfigLoading(false);
      })
      .catch(async (err) => {
        if (!isMounted) return;
        await handleApiError(err, onUnauthorized, setErrorMessage, "Nao foi possivel carregar as configuracoes.");
        setConfigLoading(false);
      });

    void getProfessionals(token)
      .then((res) => { if (isMounted) setProfessionals(res); })
      .catch(() => {});

    return () => { isMounted = false; };
  }, [token]);

  useEffect(() => {
    if (tab === "bloqueios") void loadBlocks();
  }, [tab]);

  async function loadBlocks() {
    setBlocksLoading(true);
    const now = new Date();
    const future = new Date(now.getTime() + 90 * 24 * 60 * 60 * 1000);
    try {
      const data = await getAppointmentBlocks(token, {
        fromUtc: now.toISOString(),
        toUtc: future.toISOString(),
      });
      setBlocks(data.sort((a, b) => a.startsAtUtc.localeCompare(b.startsAtUtc)));
    } catch {
      // Non-critical
    } finally {
      setBlocksLoading(false);
    }
  }

  function toggleDay(value: number) {
    setSelectedDays((prev) =>
      prev.includes(value) ? prev.filter((d) => d !== value) : [...prev, value],
    );
  }

  async function handleSaveConfig(e: FormEvent) {
    e.preventDefault();
    if (selectedDays.length === 0) {
      setErrorMessage("Selecione pelo menos um dia.");
      return;
    }
    clearFeedback();
    setConfigSaving(true);
    try {
      await updateAppointmentSettings(token, {
        serviceDays: [...selectedDays].sort((a, b) => a - b).join(","),
        startTime,
        endTime,
        slotIntervalMinutes: slotInterval,
      });
      setSuccessMessage("Horarios salvos com sucesso.");
    } catch (err) {
      await handleApiError(err, onUnauthorized, setErrorMessage, "Nao foi possivel salvar.");
    } finally {
      setConfigSaving(false);
    }
  }

  async function handleCreateBlock(e: FormEvent) {
    e.preventDefault();
    clearFeedback();
    setBlockSaving(true);
    try {
      const newBlock = await createAppointmentBlock(token, {
        assignedUserId: blockForm.assignedUserId || null,
        startsAtUtc: new Date(blockForm.startsAtLocal).toISOString(),
        endsAtUtc: new Date(blockForm.endsAtLocal).toISOString(),
        reason: blockForm.reason.trim() || null,
      });
      setBlocks((prev) =>
        [...prev, newBlock].sort((a, b) => a.startsAtUtc.localeCompare(b.startsAtUtc)),
      );
      setBlockForm({ assignedUserId: "", startsAtLocal: "", endsAtLocal: "", reason: "" });
      setSuccessMessage("Bloqueio criado com sucesso.");
    } catch (err) {
      await handleApiError(err, onUnauthorized, setErrorMessage, "Nao foi possivel criar o bloqueio.");
    } finally {
      setBlockSaving(false);
    }
  }

  async function handleDeleteBlock(blockId: string) {
    if (!window.confirm("Remover este bloqueio?")) return;
    clearFeedback();
    try {
      await deleteAppointmentBlock(token, blockId);
      setBlocks((prev) => prev.filter((b) => b.id !== blockId));
    } catch (err) {
      await handleApiError(err, onUnauthorized, setErrorMessage, "Nao foi possivel remover o bloqueio.");
    }
  }

  const daysLabel = getDaysLabel(selectedDays);
  const slotLabel = SLOT_OPTIONS.find((o) => o.value === slotInterval)?.label ?? `${slotInterval} min`;

  return (
    <section className="module-body-grid single">
      <section className="surface-card hrs-shell">

        {/* Header */}
        <div className="hrs-head">
          <div className="hrs-head-copy">
            <span className="eyebrow">Pet Shop</span>
            <h2>Horarios de atendimento</h2>
          </div>
          {!configLoading && (
            <span className={`zpprint-chip ${selectedDays.length > 0 ? "is-ready" : "is-pending"}`}>
              {selectedDays.length > 0 ? "Configurado" : "Sem horario"}
            </span>
          )}
        </div>

        {/* Summary cards */}
        {!configLoading && (
          <div className="hrs-summary">
            <article className="hrs-summary-card">
              <small>Dias</small>
              <strong>{daysLabel}</strong>
            </article>
            <article className="hrs-summary-card">
              <small>Horario</small>
              <strong>{startTime} às {endTime}</strong>
            </article>
            <article className="hrs-summary-card">
              <small>Slot</small>
              <strong>{slotLabel}</strong>
            </article>
          </div>
        )}

        {/* Tab navigation */}
        <div className="ps-tab-nav" role="tablist">
          <button
            type="button"
            role="tab"
            aria-selected={tab === "config"}
            className={`ps-tab-btn ${tab === "config" ? "is-active" : ""}`}
            onClick={() => { setTab("config"); clearFeedback(); }}
          >
            Configuracoes
          </button>
          <button
            type="button"
            role="tab"
            aria-selected={tab === "bloqueios"}
            className={`ps-tab-btn ${tab === "bloqueios" ? "is-active" : ""}`}
            onClick={() => { setTab("bloqueios"); clearFeedback(); }}
          >
            Bloqueios
          </button>
        </div>

        {/* Feedback */}
        {successMessage && <p className="module-feedback success">{successMessage}</p>}
        {errorMessage && <p className="module-feedback error">{errorMessage}</p>}

        {/* ── Config tab ── */}
        {tab === "config" && (
          configLoading ? (
            <p className="loading-state">Carregando configuracoes...</p>
          ) : (
            <form className="hrs-form" onSubmit={handleSaveConfig}>

              <div className="hrs-block">
                <p className="hrs-block-label">Dias de atendimento</p>
                <div className="hrs-day-grid" role="group" aria-label="Dias de atendimento">
                  {WEEK_DAYS.map((day) => {
                    const isOn = selectedDays.includes(day.value);
                    return (
                      <button
                        key={day.value}
                        type="button"
                        className={`hrs-day-btn ${isOn ? "is-on" : ""}`}
                        aria-pressed={isOn}
                        onClick={() => toggleDay(day.value)}
                      >
                        <span>{day.shortLabel}</span>
                        <strong>{day.label}</strong>
                      </button>
                    );
                  })}
                </div>
                <div className="hrs-presets">
                  <button type="button" className="zpprint-btn is-ghost" onClick={() => setSelectedDays(ALL_DAY_VALUES)}>
                    Todos os dias
                  </button>
                  <button type="button" className="zpprint-btn is-ghost" onClick={() => setSelectedDays([1, 2, 3, 4, 5])}>
                    Seg – Sex
                  </button>
                  <button type="button" className="zpprint-btn is-ghost" onClick={() => setSelectedDays([1, 2, 3, 4, 5, 6])}>
                    Seg – Sab
                  </button>
                </div>
              </div>

              <div className="hrs-block">
                <p className="hrs-block-label">Horario de atendimento</p>
                <div className="hrs-time-grid">
                  <div className="field-group">
                    <label>Abertura</label>
                    <input
                      type="time"
                      step={60}
                      value={startTime}
                      onChange={(e) => setStartTime(e.target.value)}
                      required
                    />
                  </div>
                  <div className="field-group">
                    <label>Encerramento</label>
                    <input
                      type="time"
                      step={60}
                      value={endTime}
                      onChange={(e) => setEndTime(e.target.value)}
                      required
                    />
                  </div>
                </div>
              </div>

              <div className="hrs-block">
                <p className="hrs-block-label">Duracao de cada slot de agendamento</p>
                <div className="ps-slot-grid">
                  {SLOT_OPTIONS.map((opt) => (
                    <button
                      key={opt.value}
                      type="button"
                      className={`ps-slot-btn ${slotInterval === opt.value ? "is-on" : ""}`}
                      onClick={() => setSlotInterval(opt.value)}
                    >
                      {opt.label}
                    </button>
                  ))}
                </div>
              </div>

              <div className="zpprint-step-actions">
                <button
                  className="zpprint-btn is-primary"
                  type="submit"
                  disabled={configSaving || selectedDays.length === 0}
                >
                  {configSaving ? "Salvando..." : "Salvar horarios"}
                </button>
              </div>

            </form>
          )
        )}

        {/* ── Bloqueios tab ── */}
        {tab === "bloqueios" && (
          <div className="hrs-form">

            <div className="hrs-block">
              <p className="hrs-block-label">Novo bloqueio</p>
              <form onSubmit={handleCreateBlock} className="ps-block-form">
                {professionals.length > 0 && (
                  <div className="field-group">
                    <label>Profissional</label>
                    <select
                      value={blockForm.assignedUserId}
                      onChange={(e) => setBlockForm((p) => ({ ...p, assignedUserId: e.target.value }))}
                    >
                      <option value="">Toda a agenda</option>
                      {professionals.map((p) => (
                        <option key={p.id} value={p.id}>{p.name}</option>
                      ))}
                    </select>
                  </div>
                )}
                <div className="hrs-time-grid">
                  <div className="field-group">
                    <label>Inicio</label>
                    <input
                      type="datetime-local"
                      required
                      value={blockForm.startsAtLocal}
                      onChange={(e) => setBlockForm((p) => ({ ...p, startsAtLocal: e.target.value }))}
                    />
                  </div>
                  <div className="field-group">
                    <label>Fim</label>
                    <input
                      type="datetime-local"
                      required
                      value={blockForm.endsAtLocal}
                      onChange={(e) => setBlockForm((p) => ({ ...p, endsAtLocal: e.target.value }))}
                    />
                  </div>
                </div>
                <div className="field-group">
                  <label>Motivo (opcional)</label>
                  <input
                    type="text"
                    placeholder="Ferias, manutencao..."
                    value={blockForm.reason}
                    onChange={(e) => setBlockForm((p) => ({ ...p, reason: e.target.value }))}
                  />
                </div>
                <button className="zpprint-btn is-primary" type="submit" disabled={blockSaving}>
                  {blockSaving ? "Criando..." : "Criar bloqueio"}
                </button>
              </form>
            </div>

            <div className="hrs-block">
              <div className="ps-block-list-head">
                <p className="hrs-block-label">Proximos 90 dias</p>
                <button
                  type="button"
                  className="zpprint-btn is-ghost ps-refresh-btn"
                  onClick={() => void loadBlocks()}
                >
                  Atualizar
                </button>
              </div>
              {blocksLoading && <p className="loading-state">Carregando bloqueios...</p>}
              {!blocksLoading && blocks.length === 0 && (
                <p className="hrs-hint">Nenhum bloqueio ativo nos proximos 90 dias.</p>
              )}
              {!blocksLoading && blocks.length > 0 && (
                <ul className="simple-list">
                  {blocks.map((b) => (
                    <li key={b.id} className="simple-list-item">
                      <div className="ps-entity-row">
                        <div className="ps-entity-main">
                          <strong>{formatDateTime(b.startsAtUtc)}</strong>
                          <span className="ps-entity-meta">
                            ate {formatDateTime(b.endsAtUtc)}
                            {b.reason && ` — ${escapeText(b.reason)}`}
                          </span>
                        </div>
                        <button
                          type="button"
                          className="zpprint-btn is-ghost ps-refresh-btn"
                          onClick={() => void handleDeleteBlock(b.id)}
                        >
                          Remover
                        </button>
                      </div>
                    </li>
                  ))}
                </ul>
              )}
            </div>

          </div>
        )}

      </section>
    </section>
  );
}
