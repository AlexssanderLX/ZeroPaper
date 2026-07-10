"use client";

import { FormEvent, useEffect, useRef, useState } from "react";
import QRCode from "qrcode";
import {
  ensureDeliveryTable,
  getAiAssistantSettings,
  prepareWhatsAppConnection,
  testAiAssistant,
  updateAiAssistantSettings,
  type AiAssistantSettings,
  type AiAssistantTestResult,
  type WhatsAppConnectionSnapshot,
} from "@/lib/api";
import { formatDateTime, handleApiError, type AsyncVoid } from "@/components/modules/module-utils";

const DEFAULT_TEST_MESSAGE =
  "Oi! Quero pedir e preciso de uma orientacao simples para seguir ao link oficial da unidade.";
const WHATSAPP_QR_REFRESH_SECONDS = 25;

const ALL_SERVICE_DAY_VALUES = [0, 1, 2, 3, 4, 5, 6];

function getSelectedServiceDays(settings: Pick<AiAssistantSettings, "serviceDays"> | null) {
  if (!settings || settings.serviceDays == null) return ALL_SERVICE_DAY_VALUES;
  return settings.serviceDays.filter((d) => ALL_SERVICE_DAY_VALUES.includes(d));
}

function normalizePhoneDraft(value: string) {
  const digits = value.replace(/\D/g, "");
  return digits.length <= 13 ? digits : digits.slice(0, 13);
}

function hasStaleWhatsAppActivity(settings: AiAssistantSettings | null) {
  if (!settings?.isWhatsAppConnected) return false;
  const latest = settings.whatsAppLastIncomingAtUtc || settings.whatsAppLastOutgoingAtUtc;
  if (!latest) return false;
  return Date.now() - new Date(latest).getTime() > 72 * 60 * 60 * 1000;
}

export type AiAssistantModuleSection =
  | "status"
  | "hours"
  | "guide"
  | "base"
  | "texts"
  | "whatsapp"
  | "test"
  | "conversations";

export function AiAssistantModule({
  token,
  onUnauthorized,
}: {
  token: string;
  onUnauthorized: AsyncVoid;
  section?: AiAssistantModuleSection;
}) {
  const [settings, setSettings] = useState<AiAssistantSettings | null>(null);
  const [draft, setDraft] = useState<AiAssistantSettings | null>(null);
  const [whatsAppConnection, setWhatsAppConnection] = useState<WhatsAppConnectionSnapshot | null>(null);
  const [whatsAppQrImage, setWhatsAppQrImage] = useState<string | null>(null);
  const [whatsAppQrSecondsLeft, setWhatsAppQrSecondsLeft] = useState(0);
  const [requestedWhatsAppPhone, setRequestedWhatsAppPhone] = useState("");
  const [testMessage, setTestMessage] = useState(DEFAULT_TEST_MESSAGE);
  const [testResult, setTestResult] = useState<AiAssistantTestResult | null>(null);
  const [loading, setLoading] = useState(true);
  const [isSaving, setIsSaving] = useState(false);
  const [isPreparingDeliveryTable, setIsPreparingDeliveryTable] = useState(false);
  const [isPreparingWhatsApp, setIsPreparingWhatsApp] = useState(false);
  const [isRefreshingWhatsAppQr, setIsRefreshingWhatsAppQr] = useState(false);
  const [isToggling, setIsToggling] = useState(false);
  const [isTesting, setIsTesting] = useState(false);
  const [errorMessage, setErrorMessage] = useState("");
  const [successMessage, setSuccessMessage] = useState("");
  const whatsAppRef = useRef<HTMLDivElement | null>(null);

  const assistantReady = Boolean(settings?.apiConfigured && draft?.orderingLink);
  const isWhatsAppConnected = Boolean(
    whatsAppConnection?.isConnected || settings?.isWhatsAppConnected,
  );
  const staleActivity = hasStaleWhatsAppActivity(settings);
  const connectedPhone = whatsAppConnection?.connectedPhone || settings?.whatsAppConnectedPhone;

  const shouldRefreshWhatsAppQr = Boolean(
    whatsAppConnection &&
      !whatsAppConnection.isConnected &&
      (whatsAppConnection.qrCodeBase64 || whatsAppConnection.qrCodeText || whatsAppConnection.pairingCode),
  );

  const whatsAppQrCountdownLabel =
    whatsAppQrSecondsLeft > 0
      ? `Renova em ${whatsAppQrSecondsLeft}s`
      : shouldRefreshWhatsAppQr
        ? "Renovando QR..."
        : "";

  function buildAbsoluteOrderingLink(accessUrl: string) {
    if (typeof window !== "undefined") return new URL(accessUrl, window.location.origin).toString();
    return accessUrl;
  }

  async function copyText(value: string | null | undefined, label: string) {
    if (!value) { setErrorMessage(`Ainda nao existe ${label.toLowerCase()} para copiar.`); return; }
    try {
      await navigator.clipboard.writeText(value);
      setSuccessMessage(`${label} copiado.`);
      setErrorMessage("");
    } catch {
      setErrorMessage(`Nao foi possivel copiar ${label.toLowerCase()} agora.`);
    }
  }

  async function loadSettings(showLoading = true) {
    if (showLoading) setLoading(true);
    try {
      const response = await getAiAssistantSettings(token);
      setSettings(response);
      setDraft(response);
      setErrorMessage("");
    } catch (error) {
      await handleApiError(error, onUnauthorized, setErrorMessage, "Nao foi possivel carregar o modulo de atendimento.");
    } finally {
      if (showLoading) setLoading(false);
    }
  }

  useEffect(() => { void loadSettings(); }, [token]);

  useEffect(() => {
    let cancelled = false;
    async function buildQrPreview() {
      if (!whatsAppConnection) { setWhatsAppQrImage(null); return; }
      if (whatsAppConnection.qrCodeBase64) {
        const src = whatsAppConnection.qrCodeBase64.startsWith("data:image")
          ? whatsAppConnection.qrCodeBase64
          : `data:image/png;base64,${whatsAppConnection.qrCodeBase64}`;
        setWhatsAppQrImage(src);
        return;
      }
      if (!whatsAppConnection.qrCodeText) { setWhatsAppQrImage(null); return; }
      try {
        const dataUrl = await QRCode.toDataURL(whatsAppConnection.qrCodeText, { width: 360, margin: 1 });
        if (!cancelled) setWhatsAppQrImage(dataUrl);
      } catch {
        if (!cancelled) setWhatsAppQrImage(null);
      }
    }
    void buildQrPreview();
    return () => { cancelled = true; };
  }, [whatsAppConnection]);

  useEffect(() => {
    if (!whatsAppConnection || (!whatsAppQrImage && !whatsAppConnection.pairingCode)) return;
    whatsAppRef.current?.scrollIntoView({ behavior: "smooth", block: "start" });
  }, [whatsAppConnection, whatsAppQrImage]);

  useEffect(() => {
    if (!shouldRefreshWhatsAppQr || isPreparingWhatsApp) return;
    const id = window.setTimeout(() => void refreshWhatsAppConnectionSilently(), 25000);
    return () => window.clearTimeout(id);
  }, [
    shouldRefreshWhatsAppQr, isPreparingWhatsApp,
    whatsAppConnection?.qrCodeBase64, whatsAppConnection?.qrCodeText,
    whatsAppConnection?.pairingCode, whatsAppConnection?.state,
  ]);

  useEffect(() => {
    if (!shouldRefreshWhatsAppQr) { setWhatsAppQrSecondsLeft(0); return; }
    setWhatsAppQrSecondsLeft(WHATSAPP_QR_REFRESH_SECONDS);
    const id = window.setInterval(() => setWhatsAppQrSecondsLeft((c) => Math.max(c - 1, 0)), 1000);
    return () => window.clearInterval(id);
  }, [
    shouldRefreshWhatsAppQr,
    whatsAppConnection?.qrCodeBase64, whatsAppConnection?.qrCodeText,
    whatsAppConnection?.pairingCode, whatsAppConnection?.state,
  ]);

  function updateDraft<K extends keyof AiAssistantSettings>(field: K, value: AiAssistantSettings[K]) {
    setDraft((c) => (c ? { ...c, [field]: value } : c));
  }

  async function saveSettings(nextDraft: AiAssistantSettings, successText: string) {
    try {
      setIsSaving(true);
      const response = await updateAiAssistantSettings(token, {
        isEnabled: nextDraft.isEnabled,
        model: nextDraft.model,
        systemPrompt: nextDraft.systemPrompt,
        greetingMessage: nextDraft.greetingMessage,
        redirectMessage: nextDraft.redirectMessage,
        fallbackMessage: nextDraft.fallbackMessage,
        orderingLink: nextDraft.orderingLink || undefined,
        pixReceiverName: nextDraft.pixReceiverName || undefined,
        pixKey: nextDraft.pixKey || undefined,
        pixMessage: nextDraft.pixMessage || undefined,
        serviceDays: getSelectedServiceDays(nextDraft),
        serviceStartTime: nextDraft.serviceStartTime || undefined,
        serviceEndTime: nextDraft.serviceEndTime || undefined,
        maxOutputTokens: Number(nextDraft.maxOutputTokens),
        whatsAppEnabled: nextDraft.whatsAppEnabled,
        whatsAppInstanceId: nextDraft.whatsAppInstanceId || undefined,
      });
      setSettings(response);
      setDraft(response);
      setSuccessMessage(successText);
      setErrorMessage("");
      return true;
    } catch (error) {
      await handleApiError(error, onUnauthorized, setErrorMessage, "Nao foi possivel salvar o atendimento.");
      return false;
    } finally {
      setIsSaving(false);
    }
  }

  async function handleToggleEnabled() {
    if (!draft || isToggling) return;
    try {
      setIsToggling(true);
      await saveSettings({ ...draft, isEnabled: !draft.isEnabled },
        draft.isEnabled ? "Atendimento pausado." : "Atendimento ativado.");
    } finally {
      setIsToggling(false);
    }
  }

  async function handleLinkSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!draft) return;
    await saveSettings(draft, "Link e Pix salvos.");
  }

  async function handleHoursSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!draft) return;
    await saveSettings(draft, "Horarios e dias salvos.");
  }

  async function handlePrepareDeliveryTable() {
    if (!draft) return;
    try {
      setIsPreparingDeliveryTable(true);
      const deliveryTable = await ensureDeliveryTable(token);
      const response = await updateAiAssistantSettings(token, {
        isEnabled: draft.isEnabled,
        model: draft.model,
        systemPrompt: draft.systemPrompt,
        greetingMessage: draft.greetingMessage,
        redirectMessage: draft.redirectMessage,
        fallbackMessage: draft.fallbackMessage,
        orderingLink: buildAbsoluteOrderingLink(deliveryTable.accessUrl),
        pixReceiverName: draft.pixReceiverName || undefined,
        pixKey: draft.pixKey || undefined,
        pixMessage: draft.pixMessage || undefined,
        serviceDays: getSelectedServiceDays(draft),
        serviceStartTime: draft.serviceStartTime || undefined,
        serviceEndTime: draft.serviceEndTime || undefined,
        maxOutputTokens: Number(draft.maxOutputTokens),
        whatsAppEnabled: draft.whatsAppEnabled,
        whatsAppInstanceId: draft.whatsAppInstanceId || undefined,
      });
      setSettings(response);
      setDraft(response);
      setSuccessMessage("Mesa de delivery pronta e vinculada ao atendimento.");
      setErrorMessage("");
    } catch (error) {
      await handleApiError(error, onUnauthorized, setErrorMessage, "Nao foi possivel preparar a mesa de delivery.");
    } finally {
      setIsPreparingDeliveryTable(false);
    }
  }

  async function handlePrepareWhatsAppConnection(forceNewSession = false, usePairingCode = false) {
    try {
      setIsPreparingWhatsApp(true);
      const normalizedPhone = normalizePhoneDraft(requestedWhatsAppPhone);
      const shouldForce =
        forceNewSession || (usePairingCode && Boolean(settings?.isWhatsAppConnected || whatsAppConnection?.isConnected));
      const response = await prepareWhatsAppConnection(token, {
        phoneNumber: usePairingCode ? normalizedPhone || undefined : undefined,
        forceNewSession: shouldForce,
      });
      setWhatsAppConnection(response);
      setSuccessMessage(
        response.qrCodeBase64 || response.qrCodeText || response.pairingCode
          ? usePairingCode ? "Codigo de pareamento pronto."
          : shouldForce ? "Novo QR preparado."
          : "QR Code aberto."
        : response.message || "Conexao preparada.",
      );
      setErrorMessage("");
      void loadSettings(false);
    } catch (error) {
      await handleApiError(error, onUnauthorized, setErrorMessage, "Nao foi possivel preparar a conexao do WhatsApp.");
    } finally {
      setIsPreparingWhatsApp(false);
    }
  }

  async function refreshWhatsAppConnectionSilently() {
    try {
      setIsRefreshingWhatsAppQr(true);
      const response = await prepareWhatsAppConnection(token, { forceNewSession: false });
      setWhatsAppConnection(response);
    } catch { /* botao manual disponivel */ } finally {
      setIsRefreshingWhatsAppQr(false);
    }
  }

  async function handleRunTest() {
    if (!settings?.apiConfigured) {
      setErrorMessage("Configure primeiro a OPENAI_API_KEY no backend para liberar o teste.");
      return;
    }
    if (!testMessage.trim()) {
      setErrorMessage("Escreva uma mensagem antes de testar.");
      return;
    }
    try {
      setIsTesting(true);
      const response = await testAiAssistant(token, testMessage.trim());
      setTestResult(response);
      setSuccessMessage("Teste concluido.");
      setErrorMessage("");
    } catch (error) {
      await handleApiError(error, onUnauthorized, setErrorMessage, "Nao foi possivel executar o teste da IA.");
    } finally {
      setIsTesting(false);
    }
  }

  const linkDone = Boolean(draft?.orderingLink);
  const whatsAppDone = isWhatsAppConnected && !staleActivity;

  return (
    <section className="module-body-grid single">
      <section className="surface-card ziai-shell">

        {/* Head */}
        <div className="ziai-head">
          <div className="ziai-head-copy">
            <span className="eyebrow">Atendimento</span>
            <h2>Atendimento com IA</h2>
          </div>
          {settings ? (
            <span className={`zpprint-chip ${assistantReady ? "is-ready" : "is-pending"}`}>
              {assistantReady ? "Pronto" : "Configurar"}
            </span>
          ) : null}
        </div>

        {loading || !draft || !settings ? (
          <p className="loading-state">Carregando configuracao do atendimento...</p>
        ) : (
          <>
            {/* ── Status row ─────────────────────────────────────── */}
            <div className="ziai-status-row">
              <article className={`ziai-stat ${settings.apiConfigured ? "is-good" : "is-danger"}`}>
                <span className="ziai-stat-label">OpenAI</span>
                <strong>{settings.apiConfigured ? "Liberada" : "Pendente"}</strong>
                <small>{settings.apiConfigured ? "Conexao pronta" : "Falar com suporte"}</small>
              </article>

              <article className={`ziai-stat ${draft.isEnabled ? "is-good" : "is-muted"}`}>
                <span className="ziai-stat-label">Atendimento IA</span>
                <strong>{draft.isEnabled ? "Ativo" : "Pausado"}</strong>
                <label className="zpprint-switch">
                  <input
                    type="checkbox"
                    checked={draft.isEnabled}
                    disabled={isToggling}
                    onChange={() => void handleToggleEnabled()}
                  />
                  <span className="zpprint-switch-track" aria-hidden="true">
                    <span className="zpprint-switch-thumb" />
                  </span>
                  <em>{draft.isEnabled ? "Respondendo automaticamente" : "Respostas pausadas"}</em>
                </label>
              </article>

              <article className={`ziai-stat ${settings.whatsAppServerConfigured ? "is-good" : "is-danger"}`}>
                <span className="ziai-stat-label">Servidor WhatsApp</span>
                <strong>{settings.whatsAppServerConfigured ? "Pronto" : "Pendente"}</strong>
                <small>{settings.whatsAppServerConfigured ? "Evolution configurada" : "Falar com suporte"}</small>
              </article>

              <article className={`ziai-stat ${whatsAppDone ? "is-good" : staleActivity ? "is-warning" : "is-muted"}`}>
                <span className="ziai-stat-label">WhatsApp</span>
                <strong>{whatsAppDone ? "Conectado" : staleActivity ? "Revisar" : "Aguardando"}</strong>
                <small>{connectedPhone || (whatsAppDone ? "Ativo" : "Gere o QR abaixo")}</small>
              </article>
            </div>

            {/* ── Passo 1: Link e Pix ────────────────────────────── */}
            <div className="ziai-block">
              <div className="ziai-block-head">
                <span className={`zpprint-step-num ${linkDone ? "is-done" : ""}`}>
                  {linkDone ? "OK" : "1"}
                </span>
                <div>
                  <strong>Link oficial e Pix</strong>
                  <p>O link e para onde a IA leva o cliente. O Pix e opcional.</p>
                </div>
              </div>

              <form className="ziai-block-form" onSubmit={handleLinkSubmit}>
                <div className="field-group">
                  <label htmlFor="ziai-link">Link oficial do pedido</label>
                  <div className="toolbar-actions compact ai-module-actions ai-inline-action">
                    <input
                      id="ziai-link"
                      value={draft.orderingLink || ""}
                      placeholder="https://zeropaperflow.com.br/q/..."
                      onChange={(e) => updateDraft("orderingLink", e.target.value)}
                    />
                    <button
                      className="ghost-link button-link"
                      type="button"
                      onClick={() => void copyText(draft.orderingLink, "Link oficial")}
                    >
                      Copiar
                    </button>
                  </div>
                </div>

                <div className="ziai-inline-grid">
                  <div className="field-group">
                    <label htmlFor="ziai-pix-name">Recebedor do Pix</label>
                    <input
                      id="ziai-pix-name"
                      value={draft.pixReceiverName || ""}
                      placeholder="Nome do recebedor"
                      onChange={(e) => updateDraft("pixReceiverName", e.target.value || null)}
                    />
                  </div>
                  <div className="field-group">
                    <label htmlFor="ziai-pix-key">Chave Pix</label>
                    <input
                      id="ziai-pix-key"
                      value={draft.pixKey || ""}
                      placeholder="CPF, telefone, e-mail ou aleatoria"
                      onChange={(e) => updateDraft("pixKey", e.target.value || null)}
                    />
                  </div>
                </div>

                <div className="field-group">
                  <label htmlFor="ziai-pix-msg">Mensagem do Pix <small>opcional</small></label>
                  <input
                    id="ziai-pix-msg"
                    value={draft.pixMessage || ""}
                    placeholder="Ex.: envie o comprovante no WhatsApp da unidade"
                    onChange={(e) => updateDraft("pixMessage", e.target.value || null)}
                  />
                </div>

                <div className="zpprint-step-actions">
                  <button
                    className="zpprint-btn is-ghost"
                    type="button"
                    disabled={isPreparingDeliveryTable}
                    onClick={() => void handlePrepareDeliveryTable()}
                  >
                    {isPreparingDeliveryTable ? "Preparando..." : "Criar mesa fixa de delivery"}
                  </button>
                  <button className="zpprint-btn is-primary" type="submit" disabled={isSaving}>
                    {isSaving ? "Salvando..." : "Salvar link e Pix"}
                  </button>
                </div>
              </form>
            </div>

            {/* ── Passo 2: WhatsApp ──────────────────────────────── */}
            <div className="ziai-block" ref={whatsAppRef}>
              <div className="ziai-block-head">
                <span className={`zpprint-step-num ${whatsAppDone ? "is-done" : ""}`}>
                  {whatsAppDone ? "OK" : "3"}
                </span>
                <div>
                  <strong>Conectar WhatsApp</strong>
                  <p>
                    {whatsAppDone
                      ? `Numero conectado: ${connectedPhone || "ativo"}`
                      : "Gere o QR Code e leia no WhatsApp do celular da unidade."}
                  </p>
                </div>
              </div>

              <div className="ziai-block-form">
                <div className="ziai-stat-row-mini">
                  <article className="ziai-mini-stat">
                    <small>Ultima entrada</small>
                    <strong>{settings.whatsAppLastIncomingAtUtc ? formatDateTime(settings.whatsAppLastIncomingAtUtc) : "Sem mensagens"}</strong>
                  </article>
                  <article className="ziai-mini-stat">
                    <small>Ultima saida</small>
                    <strong>{settings.whatsAppLastOutgoingAtUtc ? formatDateTime(settings.whatsAppLastOutgoingAtUtc) : "Sem respostas"}</strong>
                  </article>
                </div>

                {staleActivity ? (
                  <div className="zpprint-alert">
                    <strong>Conexao antiga</strong>
                    <p>Nao ha atividade recente. Use &quot;Gerar novo QR e reconectar&quot; para renovar a sessao.</p>
                  </div>
                ) : null}

                <div className="field-group">
                  <label htmlFor="ziai-phone">Numero <small>opcional — so para pareamento por numero</small></label>
                  <input
                    id="ziai-phone"
                    inputMode="numeric"
                    autoComplete="tel"
                    placeholder="5511999999999"
                    value={requestedWhatsAppPhone}
                    onChange={(e) => setRequestedWhatsAppPhone(normalizePhoneDraft(e.target.value))}
                  />
                </div>

                <div className="zpprint-step-actions">
                  <button
                    className="zpprint-btn is-primary"
                    type="button"
                    disabled={isPreparingWhatsApp || !settings.whatsAppServerConfigured}
                    onClick={() => void handlePrepareWhatsAppConnection(false)}
                  >
                    {isPreparingWhatsApp ? "Consultando..." : isWhatsAppConnected ? "Atualizar estado" : "Gerar QR para conectar"}
                  </button>
                  {normalizePhoneDraft(requestedWhatsAppPhone) ? (
                    <button
                      className="zpprint-btn is-ghost"
                      type="button"
                      disabled={isPreparingWhatsApp || !settings.whatsAppServerConfigured}
                      onClick={() => void handlePrepareWhatsAppConnection(false, true)}
                    >
                      {isPreparingWhatsApp ? "Preparando..." : "Gerar codigo por numero"}
                    </button>
                  ) : null}
                  <button
                    className="zpprint-btn is-ghost"
                    type="button"
                    disabled={isPreparingWhatsApp || !settings.whatsAppServerConfigured}
                    onClick={() => void handlePrepareWhatsAppConnection(true)}
                  >
                    {isPreparingWhatsApp ? "Preparando..." : "Gerar novo QR e reconectar"}
                  </button>
                </div>

                {whatsAppQrImage ? (
                  <div className="field-group">
                    <label>QR Code — leia pelo WhatsApp do celular da unidade</label>
                    <div className="ziai-qr-wrap">
                      <img src={whatsAppQrImage} alt="QR Code do WhatsApp da unidade" />
                    </div>
                    <small className="ai-field-hint">
                      {isRefreshingWhatsAppQr
                        ? "Atualizando o QR..."
                        : "O QR expira rapido. Esta tela renova automaticamente enquanto estiver aberta."}
                      {whatsAppQrCountdownLabel ? ` ${whatsAppQrCountdownLabel}` : ""}
                    </small>
                  </div>
                ) : null}

                {whatsAppConnection?.pairingCode ? (
                  <div className="field-group">
                    <label>Codigo de pareamento</label>
                    <div className="toolbar-actions compact ai-module-actions ai-inline-action">
                      <input value={whatsAppConnection.pairingCode} readOnly />
                      <button className="ghost-link button-link" type="button" onClick={() => void copyText(whatsAppConnection.pairingCode, "Codigo de pareamento")}>
                        Copiar
                      </button>
                    </div>
                    {whatsAppQrCountdownLabel ? <small className="ai-field-hint">{whatsAppQrCountdownLabel}</small> : null}
                  </div>
                ) : null}
              </div>
            </div>

            {/* ── Passo 3: Testar ────────────────────────────────── */}
            <div className="ziai-block">
              <div className="ziai-block-head">
                <span className="zpprint-step-num">3</span>
                <div>
                  <strong>Testar a IA</strong>
                  <p>Confira se a resposta esta clara antes de ligar o canal no WhatsApp.</p>
                </div>
              </div>

              <div className="ziai-block-form">
                <div className="field-group">
                  <label>Mensagem de teste</label>
                  <textarea
                    className="ai-textarea"
                    value={testMessage}
                    onChange={(e) => setTestMessage(e.target.value)}
                  />
                </div>
                <div className="zpprint-step-actions">
                  <button
                    className="zpprint-btn is-ghost"
                    type="button"
                    disabled={isTesting}
                    onClick={() => void handleRunTest()}
                  >
                    {isTesting ? "Consultando..." : "Testar IA"}
                  </button>
                  {testResult ? <span className="zpprint-pill is-good">{testResult.model}</span> : null}
                </div>

                {testResult ? (
                  <div className="surface-card ai-test-response-card">
                    <div className="module-section-head compact-order-column-head">
                      <div className="kitchen-column-copy">
                        <span className="eyebrow">Resposta</span>
                        <strong>Gerada em {formatDateTime(testResult.generatedAtUtc)}</strong>
                      </div>
                    </div>
                    <p className="ai-response-copy">{testResult.reply}</p>
                  </div>
                ) : null}
              </div>
            </div>

            {/* ── Conversas recentes ─────────────────────────────── */}
            <div className="ziai-jobs">
              <div className="zpprint-jobs-head">
                <span className="eyebrow">Conversas recentes</span>
              </div>

              {settings.recentWhatsAppConversations.length === 0 ? (
                <div className="zpprint-empty">
                  <strong>Sem conversas ainda</strong>
                  <p>As mensagens recebidas pelo WhatsApp aparecem aqui com telefone, ultima mensagem e horario.</p>
                </div>
              ) : (
                <div className="ziai-convo-list">
                  {settings.recentWhatsAppConversations.map((conv) => (
                    <article key={conv.id} className="ziai-convo">
                      <div className="ziai-convo-head">
                        <div>
                          <strong>{conv.customerName || conv.externalPhone}</strong>
                          <p>{conv.externalPhone}</p>
                        </div>
                        <span className="zpprint-pill">{conv.lastDirection === "Inbound" ? "Cliente" : "Unidade"}</span>
                      </div>
                      <p className="ziai-convo-preview">{conv.lastMessagePreview || "Sem resumo."}</p>
                      <div className="ziai-convo-meta">
                        <span>{conv.messageCount} mensagens</span>
                        <span>{conv.lastInteractionAtUtc ? formatDateTime(conv.lastInteractionAtUtc) : "Sem horario"}</span>
                      </div>
                    </article>
                  ))}
                </div>
              )}
            </div>
          </>
        )}

        {successMessage ? <p className="module-feedback success">{successMessage}</p> : null}
        {errorMessage ? <p className="module-feedback error">{errorMessage}</p> : null}
      </section>
    </section>
  );
}
