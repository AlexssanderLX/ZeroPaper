"use client";

import { FormEvent, useEffect, useState } from "react";
import {
  getAiAssistantSettings,
  updateAiAssistantSettings,
  type AiAssistantSettings,
} from "@/lib/api";
import { handleApiError, type AsyncVoid } from "@/components/modules/module-utils";

const SERVICE_DAYS = [
  { value: 1, shortLabel: "Seg", label: "Segunda" },
  { value: 2, shortLabel: "Ter", label: "Terca" },
  { value: 3, shortLabel: "Qua", label: "Quarta" },
  { value: 4, shortLabel: "Qui", label: "Quinta" },
  { value: 5, shortLabel: "Sex", label: "Sexta" },
  { value: 6, shortLabel: "Sab", label: "Sabado" },
  { value: 0, shortLabel: "Dom", label: "Domingo" },
];

const ALL_DAY_VALUES = SERVICE_DAYS.map((d) => d.value);

function getSelectedDays(settings: Pick<AiAssistantSettings, "serviceDays"> | null) {
  if (!settings || settings.serviceDays == null) return ALL_DAY_VALUES;
  return settings.serviceDays.filter((d) => ALL_DAY_VALUES.includes(d));
}

function getDaysLabel(days: number[]) {
  if (days.length === 0) return "Nenhum dia";
  if (days.length === SERVICE_DAYS.length) return "Todos os dias";
  if (JSON.stringify(days.sort()) === JSON.stringify([1, 2, 3, 4, 5])) return "Segunda a sexta";
  if (JSON.stringify(days.sort()) === JSON.stringify([1, 2, 3, 4, 5, 6])) return "Segunda a sabado";
  return SERVICE_DAYS.filter((d) => days.includes(d.value)).map((d) => d.shortLabel).join(", ");
}

export function HorariosModule({ token, onUnauthorized }: { token: string; onUnauthorized: AsyncVoid }) {
  const [full, setFull] = useState<AiAssistantSettings | null>(null);
  const [selectedDays, setSelectedDays] = useState<number[]>(ALL_DAY_VALUES);
  const [startTime, setStartTime] = useState<string>("");
  const [endTime, setEndTime] = useState<string>("");
  const [loading, setLoading] = useState(true);
  const [isSaving, setIsSaving] = useState(false);
  const [successMessage, setSuccessMessage] = useState("");
  const [errorMessage, setErrorMessage] = useState("");

  useEffect(() => {
    let isMounted = true;
    setLoading(true);
    void getAiAssistantSettings(token).then((res) => {
      if (!isMounted) return;
      setFull(res);
      setSelectedDays(getSelectedDays(res));
      setStartTime(res.serviceStartTime || "");
      setEndTime(res.serviceEndTime || "");
      setLoading(false);
    }).catch(async (error) => {
      if (!isMounted) return;
      await handleApiError(error, onUnauthorized, setErrorMessage, "Nao foi possivel carregar os horarios.");
      setLoading(false);
    });
    return () => { isMounted = false; };
  }, [token]);

  function toggleDay(value: number) {
    setSelectedDays((prev) =>
      prev.includes(value) ? prev.filter((d) => d !== value) : [...prev, value],
    );
  }

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!full) return;
    if (selectedDays.length === 0) {
      setErrorMessage("Selecione pelo menos um dia.");
      return;
    }

    try {
      setIsSaving(true);
      const res = await updateAiAssistantSettings(token, {
        isEnabled: full.isEnabled,
        model: full.model,
        systemPrompt: full.systemPrompt,
        greetingMessage: full.greetingMessage,
        redirectMessage: full.redirectMessage,
        fallbackMessage: full.fallbackMessage,
        orderingLink: full.orderingLink || undefined,
        pixReceiverName: full.pixReceiverName || undefined,
        pixKey: full.pixKey || undefined,
        pixMessage: full.pixMessage || undefined,
        serviceDays: selectedDays,
        serviceStartTime: startTime || undefined,
        serviceEndTime: endTime || undefined,
        maxOutputTokens: Number(full.maxOutputTokens),
        whatsAppEnabled: full.whatsAppEnabled,
        whatsAppInstanceId: full.whatsAppInstanceId || undefined,
      });
      setFull(res);
      setSuccessMessage("Horarios salvos.");
      setErrorMessage("");
    } catch (error) {
      await handleApiError(error, onUnauthorized, setErrorMessage, "Nao foi possivel salvar os horarios.");
    } finally {
      setIsSaving(false);
    }
  }

  const hasWindow = Boolean(startTime && endTime);
  const daysLabel = getDaysLabel(selectedDays);
  const scheduleLabel = hasWindow ? `${startTime} às ${endTime}` : "Sem bloqueio de horario";

  return (
    <section className="module-body-grid single">
      <section className="surface-card hrs-shell">

        <div className="hrs-head">
          <div className="hrs-head-copy">
            <span className="eyebrow">Unidade</span>
            <h2>Horarios de funcionamento</h2>
          </div>
          {!loading && (
            <span className={`zpprint-chip ${selectedDays.length > 0 ? "is-ready" : "is-pending"}`}>
              {selectedDays.length > 0 ? "Configurado" : "Sem horario"}
            </span>
          )}
        </div>

        {loading ? (
          <p className="loading-state">Carregando horarios...</p>
        ) : (
          <form className="hrs-form" onSubmit={handleSubmit}>

            {/* Resumo atual */}
            <div className="hrs-summary">
              <article className="hrs-summary-card">
                <small>Dias</small>
                <strong>{daysLabel}</strong>
              </article>
              <article className="hrs-summary-card">
                <small>Horario</small>
                <strong>{scheduleLabel}</strong>
              </article>
            </div>

            {/* Dias */}
            <div className="hrs-block">
              <p className="hrs-block-label">Dias em que a unidade funciona</p>
              <div className="hrs-day-grid" role="group" aria-label="Dias de funcionamento">
                {SERVICE_DAYS.map((day) => {
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

            {/* Horario */}
            <div className="hrs-block">
              <p className="hrs-block-label">Faixa de horario <span className="hrs-optional">opcional</span></p>
              <div className="hrs-time-grid">
                <div className="field-group">
                  <label>Abertura</label>
                  <input
                    type="time"
                    step={60}
                    value={startTime}
                    onChange={(e) => setStartTime(e.target.value)}
                  />
                </div>
                <div className="field-group">
                  <label>Encerramento</label>
                  <input
                    type="time"
                    step={60}
                    value={endTime}
                    onChange={(e) => setEndTime(e.target.value)}
                  />
                </div>
              </div>
              {hasWindow ? (
                <button
                  type="button"
                  className="zpprint-btn is-ghost"
                  onClick={() => { setStartTime(""); setEndTime(""); }}
                >
                  Remover bloqueio de horario
                </button>
              ) : (
                <p className="hrs-hint">
                  Sem faixa definida, a unidade fica disponivel o dia todo nos dias marcados.
                </p>
              )}
            </div>

            <div className="zpprint-step-actions">
              <button className="zpprint-btn is-primary" type="submit" disabled={isSaving || selectedDays.length === 0}>
                {isSaving ? "Salvando..." : "Salvar horarios"}
              </button>
            </div>

          </form>
        )}

        {successMessage ? <p className="module-feedback success">{successMessage}</p> : null}
        {errorMessage ? <p className="module-feedback error">{errorMessage}</p> : null}
      </section>
    </section>
  );
}
