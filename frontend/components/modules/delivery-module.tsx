"use client";

import { FormEvent, useEffect, useState } from "react";
import {
  getDeliveryFreightSettings,
  updateDeliveryFreightSettings,
  type DeliveryFreightSettings,
} from "@/lib/api";
import { formatCurrency, handleApiError, type AsyncVoid } from "@/components/modules/module-utils";

type DeliveryDraft = {
  isEnabled: boolean;
  originPostalCode: string;
  pricePerKm: string;
  baseFee: string;
  baseDistanceKm: string;
  pickupEstimatedMinutes: string;
  deliveryEstimatedMinutes: string;
  password: string;
};

function normalizeCep(value: string) {
  const digits = value.replace(/\D/g, "").slice(0, 8);
  return digits.length > 5 ? `${digits.slice(0, 5)}-${digits.slice(5)}` : digits;
}

function buildDraft(settings: DeliveryFreightSettings | null): DeliveryDraft {
  return {
    isEnabled: settings?.isEnabled ?? false,
    originPostalCode: normalizeCep(settings?.originPostalCode ?? ""),
    pricePerKm: settings?.pricePerKm ? String(settings.pricePerKm) : "",
    baseFee: settings?.baseFee ? String(settings.baseFee) : "",
    baseDistanceKm: settings?.baseDistanceKm ? String(settings.baseDistanceKm) : "",
    pickupEstimatedMinutes: settings?.pickupEstimatedMinutes ? String(settings.pickupEstimatedMinutes) : "",
    deliveryEstimatedMinutes: settings?.deliveryEstimatedMinutes ? String(settings.deliveryEstimatedMinutes) : "",
    password: "",
  };
}

function formatProviderLabel(settings: DeliveryFreightSettings | null) {
  if (!settings?.provider) {
    return "indefinido";
  }

  const provider = settings.provider.toLowerCase();
  if (provider === "approximate") {
    return "CEP aproximado";
  }

  if (provider === "google") {
    return "Google Routes";
  }

  if (provider === "mock") {
    return "Mock";
  }

  return settings.provider;
}

function formatModeLabel(settings: DeliveryFreightSettings | null) {
  if (settings?.isTestMode) {
    return "Teste";
  }

  if (settings?.provider?.toLowerCase() === "approximate") {
    return "Baixo custo";
  }

  return "Mapas";
}

function normalizeEstimatedMinutes(value: string) {
  const digits = value.replace(/\D/g, "").slice(0, 3);
  if (!digits) {
    return "";
  }

  const minutes = Math.min(300, Math.max(1, Number(digits)));
  return String(minutes);
}

function parseEstimatedMinutes(value: string) {
  const minutes = Number(value || 0);
  return minutes > 0 ? minutes : null;
}

export function DeliveryModule({ token, onUnauthorized }: { token: string; onUnauthorized: AsyncVoid }) {
  const [settings, setSettings] = useState<DeliveryFreightSettings | null>(null);
  const [draft, setDraft] = useState<DeliveryDraft>(buildDraft(null));
  const [loading, setLoading] = useState(true);
  const [isSaving, setIsSaving] = useState(false);
  const [errorMessage, setErrorMessage] = useState("");
  const [successMessage, setSuccessMessage] = useState("");

  async function loadSettings() {
    setLoading(true);

    try {
      const response = await getDeliveryFreightSettings(token);
      setSettings(response);
      setDraft(buildDraft(response));
      setErrorMessage("");
    } catch (error) {
      await handleApiError(error, onUnauthorized, setErrorMessage, "Nao foi possivel carregar a configuracao de entrega.");
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    void loadSettings();
  }, [token]);

  function updateDraft<K extends keyof DeliveryDraft>(field: K, value: DeliveryDraft[K]) {
    setDraft((currentValue) => ({
      ...currentValue,
      [field]: value,
    }));
  }

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setSuccessMessage("");

    if (!draft.password.trim()) {
      setErrorMessage("Informe a senha owner para salvar o frete.");
      return;
    }

    if (draft.isEnabled && draft.originPostalCode.replace(/\D/g, "").length !== 8) {
      setErrorMessage("Informe o CEP fixo da unidade com 8 digitos.");
      return;
    }

    if (draft.isEnabled && Number(draft.pricePerKm) <= 0) {
      setErrorMessage("Informe um valor por KM maior que zero.");
      return;
    }

    try {
      setIsSaving(true);
      const response = await updateDeliveryFreightSettings(token, {
        isEnabled: draft.isEnabled,
        originPostalCode: draft.originPostalCode,
        pricePerKm: Number(draft.pricePerKm || 0),
        baseFee: Number(draft.baseFee || 0),
        baseDistanceKm: Number(draft.baseDistanceKm || 0),
        pickupEstimatedMinutes: parseEstimatedMinutes(draft.pickupEstimatedMinutes),
        deliveryEstimatedMinutes: parseEstimatedMinutes(draft.deliveryEstimatedMinutes),
        password: draft.password,
      });

      setSettings(response);
      setDraft({
        ...buildDraft(response),
        password: "",
      });
      setSuccessMessage("Configuracao de frete atualizada.");
      setErrorMessage("");
    } catch (error) {
      await handleApiError(error, onUnauthorized, setErrorMessage, "Nao foi possivel salvar o frete.");
    } finally {
      setIsSaving(false);
    }
  }

  const previewPricePerKm = Number(draft.pricePerKm || 0);
  const previewBaseFee = Number(draft.baseFee || 0);
  const previewBaseDistanceKm = Number(draft.baseDistanceKm || 0);
  const previewChargedDistanceKm = Math.max(0, 4 - previewBaseDistanceKm);
  const previewFreight = Math.max(0, previewBaseFee + previewPricePerKm * previewChargedDistanceKm);
  const deliveryStatusLabel = draft.isEnabled ? "Frete automatico ativo" : "Frete automatico desligado";
  const pickupTimeLabel = draft.pickupEstimatedMinutes ? `${draft.pickupEstimatedMinutes} min` : "Oculto";
  const deliveryTimeLabel = draft.deliveryEstimatedMinutes ? `${draft.deliveryEstimatedMinutes} min` : "Oculto";

  return (
    <section className="delivery-settings-workspace">
      <section className="surface-card delivery-command-panel" aria-label="Resumo do frete">
        <div className="delivery-command-main">
          <span className={`delivery-state-dot ${draft.isEnabled ? "is-on" : ""}`} />
          <div>
            <span className="eyebrow">Entrega e frete</span>
            <h2>{deliveryStatusLabel}</h2>
          </div>
        </div>

        <div className="delivery-command-metrics">
          <div>
            <span>4 KM</span>
            <strong>{formatCurrency(previewFreight)}</strong>
          </div>
          <div>
            <span>Retirada</span>
            <strong>{pickupTimeLabel}</strong>
          </div>
          <div>
            <span>Entrega</span>
            <strong>{deliveryTimeLabel}</strong>
          </div>
          <div>
            <span>{formatProviderLabel(settings)}</span>
            <strong>{formatModeLabel(settings)}</strong>
          </div>
        </div>
      </section>

      <section className="surface-card module-form-card delivery-settings-form-card">
        <div className="delivery-form-heading">
          <div>
            <span className="eyebrow">Configuracao rapida</span>
            <h2>Frete pronto para o cliente</h2>
          </div>
          <p>
            O valor muda em tempo real no resumo acima. Salve quando o CEP, a regra por KM e os tempos estiverem certos.
          </p>
        </div>

        {loading ? (
          <p className="loading-state">Carregando configuracao...</p>
        ) : (
          <form className="module-form delivery-settings-form" onSubmit={handleSubmit}>
            <label className={`delivery-switch-row ${draft.isEnabled ? "is-on" : ""}`}>
              <input
                type="checkbox"
                checked={draft.isEnabled}
                onChange={(event) => updateDraft("isEnabled", event.target.checked)}
              />
              <div>
                <strong>Ativar frete automatico por KM</strong>
                <p>Quando desligado, o delivery segue sem frete automatico.</p>
              </div>
            </label>

            <div className="delivery-settings-section">
              <div className="delivery-section-title">
                <span>1</span>
                <strong>Origem da entrega</strong>
              </div>
              <div className="field-group">
                <label htmlFor="deliveryOriginPostalCode">CEP fixo da unidade</label>
                <input
                  id="deliveryOriginPostalCode"
                  value={draft.originPostalCode}
                  onChange={(event) => updateDraft("originPostalCode", normalizeCep(event.target.value))}
                  inputMode="numeric"
                  placeholder="00000-000"
                />
              </div>
            </div>

            <div className="delivery-settings-section">
              <div className="delivery-section-title">
                <span>2</span>
                <strong>Regra do frete</strong>
              </div>
              <div className="module-inline-grid triple">
              <div className="field-group">
                <label htmlFor="deliveryBaseFee">Taxa minima</label>
                <input
                  id="deliveryBaseFee"
                  type="number"
                  min="0"
                  step="0.01"
                  value={draft.baseFee}
                  onChange={(event) => updateDraft("baseFee", event.target.value)}
                  placeholder="8.00"
                />
              </div>

              <div className="field-group">
                <label htmlFor="deliveryBaseDistanceKm">KM inclusos</label>
                <input
                  id="deliveryBaseDistanceKm"
                  type="number"
                  min="0"
                  step="0.01"
                  value={draft.baseDistanceKm}
                  onChange={(event) => updateDraft("baseDistanceKm", event.target.value)}
                  placeholder="3"
                />
              </div>

              <div className="field-group">
                <label htmlFor="deliveryPricePerKm">Valor por KM excedente</label>
                <input
                  id="deliveryPricePerKm"
                  type="number"
                  min="0"
                  step="0.01"
                  value={draft.pricePerKm}
                  onChange={(event) => updateDraft("pricePerKm", event.target.value)}
                  placeholder="2.50"
                />
              </div>
              </div>
              <p className="field-hint">
                No exemplo de 4 KM, {previewChargedDistanceKm.toLocaleString("pt-BR", { maximumFractionDigits: 2 })} KM entram como excedente.
              </p>
            </div>

            <div className="delivery-settings-section">
              <div className="delivery-section-title">
                <span>3</span>
                <strong>Tempo que aparece no pedido</strong>
              </div>
              <div className="module-inline-grid">
              <div className="field-group">
                <label htmlFor="pickupEstimatedMinutes">Tempo para retirada</label>
                <input
                  id="pickupEstimatedMinutes"
                  value={draft.pickupEstimatedMinutes}
                  onChange={(event) => updateDraft("pickupEstimatedMinutes", normalizeEstimatedMinutes(event.target.value))}
                  inputMode="numeric"
                  placeholder="Ex.: 25"
                />
              </div>

              <div className="field-group">
                <label htmlFor="deliveryEstimatedMinutes">Tempo para entrega</label>
                <input
                  id="deliveryEstimatedMinutes"
                  value={draft.deliveryEstimatedMinutes}
                  onChange={(event) => updateDraft("deliveryEstimatedMinutes", normalizeEstimatedMinutes(event.target.value))}
                  inputMode="numeric"
                  placeholder="Ex.: 50"
                />
              </div>
              </div>
              <p className="field-hint">Deixe vazio para ocultar o tempo daquela modalidade.</p>
            </div>

            <div className="delivery-save-strip">
              <div className="field-group">
                <label htmlFor="deliveryOwnerPassword">Senha owner</label>
                <input
                  id="deliveryOwnerPassword"
                  type="password"
                  value={draft.password}
                  onChange={(event) => updateDraft("password", event.target.value)}
                  placeholder="Digite a senha para salvar"
                  autoComplete="current-password"
                />
              </div>
              <button className="primary-link button-link" type="submit" disabled={isSaving}>
                {isSaving ? "Salvando..." : "Salvar frete"}
              </button>
            </div>
          </form>
        )}
      </section>

      {successMessage ? <p className="module-feedback success">{successMessage}</p> : null}
      {errorMessage ? <p className="module-feedback error">{errorMessage}</p> : null}
    </section>
  );
}
