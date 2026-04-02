"use client";

import { ChangeEvent, FormEvent, useEffect, useState } from "react";
import {
  deleteAlertSound,
  getCompanySettings,
  updateAlertSettings,
  updateCompanySettings,
  uploadAlertSound,
  type AlertSettings,
  type CompanySettings,
} from "@/lib/api";
import { handleApiError, type AsyncVoid } from "@/components/modules/module-utils";

export function SettingsModule({ token, onUnauthorized }: { token: string; onUnauthorized: AsyncVoid }) {
  const [settings, setSettings] = useState<CompanySettings | null>(null);
  const [draft, setDraft] = useState<CompanySettings | null>(null);
  const [alertDraft, setAlertDraft] = useState<AlertSettings | null>(null);
  const [loading, setLoading] = useState(true);
  const [isSavingUnit, setIsSavingUnit] = useState(false);
  const [isSavingAlerts, setIsSavingAlerts] = useState(false);
  const [isUploadingSound, setIsUploadingSound] = useState(false);
  const [errorMessage, setErrorMessage] = useState("");
  const [successMessage, setSuccessMessage] = useState("");

  async function loadSettings() {
    setLoading(true);

    try {
      const response = await getCompanySettings(token);
      setSettings(response);
      setDraft(response);
      setAlertDraft(response.alerts);
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

  async function handleUnitSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    if (!draft) {
      return;
    }

    try {
      setIsSavingUnit(true);
      const response = await updateCompanySettings(token, {
        legalName: draft.legalName,
        tradeName: draft.tradeName,
        contactEmail: draft.contactEmail || undefined,
        contactPhone: draft.contactPhone || undefined,
      });

      setSettings(response);
      setDraft(response);
      setAlertDraft(response.alerts);
      setSuccessMessage("Dados da unidade atualizados.");
      setErrorMessage("");
    } catch (error) {
      await handleApiError(error, onUnauthorized, setErrorMessage, "Nao foi possivel salvar os ajustes.");
    } finally {
      setIsSavingUnit(false);
    }
  }

  async function handleAlertSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    if (!alertDraft) {
      return;
    }

    try {
      setIsSavingAlerts(true);
      const response = await updateAlertSettings(token, {
        enableOrderAlerts: alertDraft.enableOrderAlerts,
        enableWaiterCallAlerts: alertDraft.enableWaiterCallAlerts,
        volumePercent: alertDraft.volumePercent,
        playbackSeconds: alertDraft.playbackSeconds,
      });

      setAlertDraft(response);
      setSettings((currentValue) => (currentValue ? { ...currentValue, alerts: response } : currentValue));
      setSuccessMessage("Alertas atualizados.");
      setErrorMessage("");
    } catch (error) {
      await handleApiError(error, onUnauthorized, setErrorMessage, "Nao foi possivel salvar os alertas.");
    } finally {
      setIsSavingAlerts(false);
    }
  }

  async function handleSoundUpload(event: ChangeEvent<HTMLInputElement>) {
    const file = event.target.files?.[0];

    if (!file) {
      return;
    }

    try {
      setIsUploadingSound(true);
      const response = await uploadAlertSound(token, file);
      setAlertDraft(response.alerts);
      setSettings((currentValue) => (currentValue ? { ...currentValue, alerts: response.alerts } : currentValue));
      setSuccessMessage("Som do alerta atualizado.");
      setErrorMessage("");
    } catch (error) {
      await handleApiError(error, onUnauthorized, setErrorMessage, "Nao foi possivel enviar o novo som.");
    } finally {
      setIsUploadingSound(false);
      event.target.value = "";
    }
  }

  async function handleRemoveCustomSound() {
    try {
      setIsUploadingSound(true);
      const response = await deleteAlertSound(token);
      setAlertDraft(response);
      setSettings((currentValue) => (currentValue ? { ...currentValue, alerts: response } : currentValue));
      setSuccessMessage("Som padrao restaurado.");
      setErrorMessage("");
    } catch (error) {
      await handleApiError(error, onUnauthorized, setErrorMessage, "Nao foi possivel restaurar o som padrao.");
    } finally {
      setIsUploadingSound(false);
    }
  }

  function updateAlertDraftField<K extends keyof AlertSettings>(field: K, value: AlertSettings[K]) {
    setAlertDraft((current) => (current ? { ...current, [field]: value } : current));
  }

  return (
    <section className="module-body-grid single">
      <section className="surface-card module-form-card">
        <span className="eyebrow">Dados da unidade</span>
        <h2>Ajustes gerais</h2>

        {loading || !draft || !alertDraft ? (
          <p className="loading-state">Carregando ajustes...</p>
        ) : (
          <>
            <form className="module-form" onSubmit={handleUnitSubmit}>
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

              <button className="primary-link button-link" type="submit" disabled={isSavingUnit}>
                {isSavingUnit ? "Salvando..." : "Salvar ajustes"}
              </button>
            </form>

            <section className="settings-alerts-block">
              <div className="module-section-head">
                <div>
                  <span className="eyebrow">Alertas sonoros</span>
                  <h2>Operacao</h2>
                </div>
              </div>

              <form className="module-form" onSubmit={handleAlertSubmit}>
                <div className="settings-alert-layout">
                  <section className="surface-card settings-alert-panel">
                    <div className="module-section-head compact-order-column-head">
                      <div className="kitchen-column-copy">
                        <span className="eyebrow">Ativos no painel</span>
                        <strong>Chamadas e pedidos</strong>
                      </div>
                    </div>

                    <div className="settings-alert-grid">
                      <label className="settings-alert-toggle">
                        <input
                          type="checkbox"
                          checked={alertDraft.enableOrderAlerts}
                          onChange={(event) => updateAlertDraftField("enableOrderAlerts", event.target.checked)}
                        />
                        <div>
                          <strong>Pedidos novos</strong>
                          <p>Toca quando um pedido chega pelo QR.</p>
                        </div>
                      </label>

                      <label className="settings-alert-toggle">
                        <input
                          type="checkbox"
                          checked={alertDraft.enableWaiterCallAlerts}
                          onChange={(event) => updateAlertDraftField("enableWaiterCallAlerts", event.target.checked)}
                        />
                        <div>
                          <strong>Chamado de atendente</strong>
                          <p>Toca quando uma mesa pede atendimento.</p>
                        </div>
                      </label>
                    </div>

                    <div className="settings-alert-metrics">
                      <article className="settings-alert-metric">
                        <small>Volume atual</small>
                        <strong>{alertDraft.volumePercent}%</strong>
                      </article>
                      <article className="settings-alert-metric">
                        <small>Duracao atual</small>
                        <strong>{alertDraft.playbackSeconds}s</strong>
                      </article>
                    </div>
                  </section>

                  <section className="surface-card settings-alert-panel">
                    <div className="module-section-head compact-order-column-head">
                      <div className="kitchen-column-copy">
                        <span className="eyebrow">Som do painel</span>
                        <strong>{alertDraft.hasCustomSound ? "Audio da unidade" : "Audio padrao do sistema"}</strong>
                      </div>
                    </div>

                    <div className="settings-alert-audio-card">
                      <div className="settings-alert-audio-copy">
                        <p>
                          {alertDraft.soundUrl
                            ? "Esse arquivo vai tocar no painel interno. Voce pode trocar o som direto do computador da unidade."
                            : "Envie um arquivo do computador para usar um som proprio no painel interno."}
                        </p>
                      </div>

                      {alertDraft.soundUrl ? (
                        <audio className="settings-audio-player" controls preload="metadata" src={alertDraft.soundUrl} />
                      ) : null}

                      <div className="settings-sound-controls">
                        <label className="settings-range-control">
                          <div className="settings-range-head">
                            <strong>Volume</strong>
                            <span>{alertDraft.volumePercent}%</span>
                          </div>
                          <input
                            type="range"
                            min={0}
                            max={100}
                            step={5}
                            value={alertDraft.volumePercent}
                            onChange={(event) => updateAlertDraftField("volumePercent", Number(event.target.value))}
                          />
                        </label>

                        <label className="settings-range-control">
                          <div className="settings-range-head">
                            <strong>Duracao do toque</strong>
                            <span>{alertDraft.playbackSeconds}s</span>
                          </div>
                          <input
                            type="range"
                            min={1}
                            max={20}
                            step={1}
                            value={alertDraft.playbackSeconds}
                            onChange={(event) => updateAlertDraftField("playbackSeconds", Number(event.target.value))}
                          />
                        </label>
                      </div>

                      <div className="toolbar-actions compact settings-alert-actions">
                        <label className="ghost-link button-link settings-upload-button">
                          {isUploadingSound ? "Enviando..." : "Enviar som do computador"}
                          <input type="file" accept=".wav,.mp3,.ogg,audio/wav,audio/mpeg,audio/ogg" hidden onChange={(event) => void handleSoundUpload(event)} />
                        </label>

                        {alertDraft.hasCustomSound ? (
                          <button className="ghost-link button-link" type="button" disabled={isUploadingSound} onClick={() => void handleRemoveCustomSound()}>
                            Usar som padrao
                          </button>
                        ) : null}
                      </div>
                    </div>
                  </section>
                </div>

                <button className="primary-link button-link" type="submit" disabled={isSavingAlerts}>
                  {isSavingAlerts ? "Salvando..." : "Salvar alertas"}
                </button>
              </form>
            </section>
          </>
        )}

        {successMessage ? <p className="module-feedback success">{successMessage}</p> : null}
        {errorMessage ? <p className="module-feedback error">{errorMessage}</p> : null}
      </section>
    </section>
  );
}
