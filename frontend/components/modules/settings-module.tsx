"use client";

import { ChangeEvent, FormEvent, useEffect, useState } from "react";
import {
  changeOwnerPassword,
  deleteAlertSound,
  deleteCompanyLogo,
  generateOwnerShortcutAccess,
  getCompanySettings,
  getOwnerProfile,
  revokeOwnerShortcutAccess,
  updateAlertSettings,
  updateCompanySettings,
  updateOwnerProfile,
  uploadAlertSound,
  uploadCompanyLogo,
  type AlertSettings,
  type CompanySettings,
  type OwnerProfile,
} from "@/lib/api";
import { useAppSession } from "@/components/app-session-provider";
import { handleApiError, type AsyncVoid } from "@/components/modules/module-utils";

const EMPTY_PASSWORD_DRAFT = {
  currentPassword: "",
  newPassword: "",
  confirmPassword: "",
};

export function SettingsModule({ token, onUnauthorized }: { token: string; onUnauthorized: AsyncVoid }) {
  const { updateSession } = useAppSession();
  const [settings, setSettings] = useState<CompanySettings | null>(null);
  const [draft, setDraft] = useState<CompanySettings | null>(null);
  const [alertDraft, setAlertDraft] = useState<AlertSettings | null>(null);
  const [profile, setProfile] = useState<OwnerProfile | null>(null);
  const [profileDraft, setProfileDraft] = useState<OwnerProfile | null>(null);
  const [passwordDraft, setPasswordDraft] = useState(EMPTY_PASSWORD_DRAFT);
  const [shortcutPassword, setShortcutPassword] = useState("");
  const [shortcutUrl, setShortcutUrl] = useState("");
  const [loading, setLoading] = useState(true);
  const [isSavingUnit, setIsSavingUnit] = useState(false);
  const [isSavingProfile, setIsSavingProfile] = useState(false);
  const [isChangingPassword, setIsChangingPassword] = useState(false);
  const [isRotatingShortcut, setIsRotatingShortcut] = useState(false);
  const [isRevokingShortcut, setIsRevokingShortcut] = useState(false);
  const [isSavingAlerts, setIsSavingAlerts] = useState(false);
  const [isUploadingSound, setIsUploadingSound] = useState(false);
  const [isUploadingLogo, setIsUploadingLogo] = useState(false);
  const [errorMessage, setErrorMessage] = useState("");
  const [successMessage, setSuccessMessage] = useState("");

  async function loadSettings() {
    setLoading(true);

    try {
      const [settingsResponse, profileResponse] = await Promise.all([getCompanySettings(token), getOwnerProfile(token)]);
      setSettings(settingsResponse);
      setDraft(settingsResponse);
      setAlertDraft(settingsResponse.alerts);
      setProfile(profileResponse);
      setProfileDraft(profileResponse);
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
      updateSession({ restaurantName: response.tradeName });
      setSuccessMessage("Dados da unidade atualizados.");
      setErrorMessage("");
    } catch (error) {
      await handleApiError(error, onUnauthorized, setErrorMessage, "Nao foi possivel salvar os ajustes.");
    } finally {
      setIsSavingUnit(false);
    }
  }

  async function handleProfileSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    if (!profileDraft) {
      return;
    }

    try {
      setIsSavingProfile(true);
      const response = await updateOwnerProfile(token, {
        fullName: profileDraft.fullName,
        email: profileDraft.email,
      });

      setProfile(response);
      setProfileDraft(response);
      updateSession({
        ownerName: response.fullName,
        email: response.email,
        role: response.role,
      });
      setSuccessMessage("Dados do owner atualizados.");
      setErrorMessage("");
    } catch (error) {
      await handleApiError(error, onUnauthorized, setErrorMessage, "Nao foi possivel atualizar os dados do owner.");
    } finally {
      setIsSavingProfile(false);
    }
  }

  async function handlePasswordSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    try {
      setIsChangingPassword(true);
      await changeOwnerPassword(token, passwordDraft);
      setPasswordDraft(EMPTY_PASSWORD_DRAFT);
      setSuccessMessage("Senha atualizada com seguranca.");
      setErrorMessage("");
    } catch (error) {
      await handleApiError(error, onUnauthorized, setErrorMessage, "Nao foi possivel alterar a senha.");
    } finally {
      setIsChangingPassword(false);
    }
  }

  async function handleShortcutGenerate() {
    if (!shortcutPassword.trim()) {
      setErrorMessage("Informe a senha owner para gerar o atalho.");
      return;
    }

    try {
      setIsRotatingShortcut(true);
      const response = await generateOwnerShortcutAccess(token, shortcutPassword);
      setSettings((currentValue) => currentValue ? { ...currentValue, shortcutAccess: response.shortcutAccess } : currentValue);
      setDraft((currentValue) => currentValue ? { ...currentValue, shortcutAccess: response.shortcutAccess } : currentValue);
      setShortcutUrl(response.shortcutUrl);
      setShortcutPassword("");
      setSuccessMessage("Atalho seguro gerado. Copie agora, ele nao sera exibido novamente.");
      setErrorMessage("");
    } catch (error) {
      await handleApiError(error, onUnauthorized, setErrorMessage, "Nao foi possivel gerar o atalho.");
    } finally {
      setIsRotatingShortcut(false);
    }
  }

  async function handleShortcutRevoke() {
    if (!shortcutPassword.trim()) {
      setErrorMessage("Informe a senha owner para revogar o atalho.");
      return;
    }

    try {
      setIsRevokingShortcut(true);
      const response = await revokeOwnerShortcutAccess(token, shortcutPassword);
      setSettings((currentValue) => currentValue ? { ...currentValue, shortcutAccess: response } : currentValue);
      setDraft((currentValue) => currentValue ? { ...currentValue, shortcutAccess: response } : currentValue);
      setShortcutUrl("");
      setShortcutPassword("");
      setSuccessMessage("Atalho automatico revogado.");
      setErrorMessage("");
    } catch (error) {
      await handleApiError(error, onUnauthorized, setErrorMessage, "Nao foi possivel revogar o atalho.");
    } finally {
      setIsRevokingShortcut(false);
    }
  }

  async function handleCopyShortcutUrl() {
    if (!shortcutUrl) {
      return;
    }

    try {
      await navigator.clipboard.writeText(shortcutUrl);
      setSuccessMessage("Link do atalho copiado.");
      setErrorMessage("");
    } catch {
      setErrorMessage("Nao foi possivel copiar automaticamente. Selecione e copie o link manualmente.");
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

  async function handleLogoUpload(event: ChangeEvent<HTMLInputElement>) {
    const file = event.target.files?.[0];

    if (!file) {
      return;
    }

    try {
      setIsUploadingLogo(true);
      const response = await uploadCompanyLogo(token, file);
      setSettings(response);
      setDraft(response);
      setAlertDraft(response.alerts);
      updateSession({ restaurantName: response.tradeName });
      setSuccessMessage("Logo da unidade atualizada.");
      setErrorMessage("");
    } catch (error) {
      await handleApiError(error, onUnauthorized, setErrorMessage, "Nao foi possivel enviar a logo.");
    } finally {
      setIsUploadingLogo(false);
      event.target.value = "";
    }
  }

  async function handleRemoveLogo() {
    try {
      setIsUploadingLogo(true);
      const response = await deleteCompanyLogo(token);
      setSettings(response);
      setDraft(response);
      setAlertDraft(response.alerts);
      setSuccessMessage("Logo removida.");
      setErrorMessage("");
    } catch (error) {
      await handleApiError(error, onUnauthorized, setErrorMessage, "Nao foi possivel remover a logo.");
    } finally {
      setIsUploadingLogo(false);
    }
  }

  function updateAlertDraftField<K extends keyof AlertSettings>(field: K, value: AlertSettings[K]) {
    setAlertDraft((current) => (current ? { ...current, [field]: value } : current));
  }

  function updatePasswordDraftField<K extends keyof typeof EMPTY_PASSWORD_DRAFT>(field: K, value: string) {
    setPasswordDraft((current) => ({ ...current, [field]: value }));
  }

  function formatShortcutDate(value?: string | null) {
    if (!value) {
      return "Ainda nao registrado";
    }

    return new Intl.DateTimeFormat("pt-BR", {
      dateStyle: "short",
      timeStyle: "short",
    }).format(new Date(value));
  }

  const unitInitial = (draft?.tradeName || settings?.tradeName || "Z").trim().slice(0, 1).toUpperCase() || "Z";
  const contactStatus = draft?.contactEmail || draft?.contactPhone ? "Contato visivel" : "Sem contato";
  const logoStatus = draft?.logoUrl ? "Logo ativa" : "Sem logo";
  const shortcutStatus = settings?.shortcutAccess.isEnabled ? "Atalho ativo" : "Sem atalho";
  const activeAlertsCount = alertDraft
    ? Number(alertDraft.enableOrderAlerts) + Number(alertDraft.enableWaiterCallAlerts)
    : 0;

  return (
    <section className="module-body-grid single">
      <section className="surface-card module-form-card settings-hub-shell">
        <div className="settings-hub-hero">
          <div className="settings-hub-title">
            <span className="eyebrow">Ajustes gerais</span>
            <h2>Configure a unidade sem procurar campo</h2>
            <p>Revise identidade, acesso e alertas em blocos visuais. Os controles principais ficam sempre no proprio card.</p>
          </div>

          {!loading && draft && alertDraft ? (
            <div className="settings-hub-status-grid" aria-label="Resumo dos ajustes">
              <article className="settings-hub-status-card primary">
                <span>Unidade</span>
                <strong>{draft.tradeName || "Sem nome"}</strong>
                <small>{contactStatus}</small>
              </article>
              <article className="settings-hub-status-card">
                <span>Marca</span>
                <strong>{logoStatus}</strong>
                <small>Cardapio publico</small>
              </article>
              <article className="settings-hub-status-card">
                <span>Acesso</span>
                <strong>{shortcutStatus}</strong>
                <small>{formatShortcutDate(settings?.shortcutAccess.expiresAtUtc)}</small>
              </article>
              <article className="settings-hub-status-card">
                <span>Alertas</span>
                <strong>{activeAlertsCount}/2 ligados</strong>
                <small>{alertDraft.hasCustomSound ? "Som proprio" : "Som padrao"}</small>
              </article>
            </div>
          ) : null}
        </div>

        {loading || !draft || !alertDraft || !profileDraft ? (
          <p className="loading-state">Carregando ajustes...</p>
        ) : (
          <>
            <form className="module-form settings-config-card settings-unit-card" onSubmit={handleUnitSubmit}>
              <div className="module-section-head">
                <div>
                  <span className="eyebrow">1. Identidade da unidade</span>
                  <h2>Como o cliente ve sua loja</h2>
                </div>
                <span className="status-chip ready">{logoStatus}</span>
              </div>

              <section className="settings-logo-panel">
                <div className="settings-logo-preview" aria-hidden="true">
                  {draft.logoUrl ? <img src={draft.logoUrl} alt="" /> : <span>{unitInitial}</span>}
                </div>
                <div className="settings-logo-copy">
                  <strong>Logo no cardapio do cliente</strong>
                  <p>Aparece no pedido publico junto do nome da loja. Use JPG, PNG ou WEBP ate 3 MB.</p>
                  <div className="toolbar-actions compact settings-logo-actions">
                    <label className={`ghost-link button-link ${isUploadingLogo ? "is-disabled" : ""}`}>
                      {isUploadingLogo ? "Enviando..." : draft.logoUrl ? "Trocar logo" : "Adicionar logo"}
                      <input
                        type="file"
                        accept="image/jpeg,image/png,image/webp"
                        disabled={isUploadingLogo}
                        onChange={(event) => void handleLogoUpload(event)}
                      />
                    </label>
                    {draft.logoUrl ? (
                      <button className="ghost-link button-link destructive-link" type="button" disabled={isUploadingLogo} onClick={() => void handleRemoveLogo()}>
                        Remover logo
                      </button>
                    ) : null}
                  </div>
                </div>
              </section>

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
                  <span className="eyebrow">2. Conta e seguranca</span>
                  <h2>Acesso do owner</h2>
                </div>
              </div>

              <div className="settings-alert-layout">
                <form className="surface-card settings-alert-panel module-form" onSubmit={handleProfileSubmit}>
                  <div className="module-section-head compact-order-column-head">
                    <div className="kitchen-column-copy">
                      <span className="eyebrow">Perfil principal</span>
                      <strong>Nome e email de login</strong>
                    </div>
                  </div>

                  <div className="module-inline-grid">
                    <div className="field-group">
                      <label>Nome do owner</label>
                      <input
                        value={profileDraft.fullName}
                        maxLength={150}
                        required
                        onChange={(event) =>
                          setProfileDraft((current) => (current ? { ...current, fullName: event.target.value } : current))
                        }
                      />
                    </div>
                    <div className="field-group">
                      <label>Email de login</label>
                      <input
                        type="email"
                        value={profileDraft.email}
                        maxLength={180}
                        required
                        onChange={(event) =>
                          setProfileDraft((current) => (current ? { ...current, email: event.target.value } : current))
                        }
                      />
                    </div>
                  </div>

                  <div className="field-group">
                    <label>Perfil</label>
                    <input value={profile?.role || profileDraft.role} disabled />
                  </div>

                  <button className="primary-link button-link" type="submit" disabled={isSavingProfile}>
                    {isSavingProfile ? "Salvando..." : "Salvar owner"}
                  </button>
                </form>

                <form className="surface-card settings-alert-panel module-form" onSubmit={handlePasswordSubmit}>
                  <div className="module-section-head compact-order-column-head">
                    <div className="kitchen-column-copy">
                      <span className="eyebrow">Senha</span>
                      <strong>Trocar com confirmacao</strong>
                    </div>
                  </div>

                  <div className="field-group">
                    <label>Senha atual</label>
                    <input
                      type="password"
                      autoComplete="current-password"
                      value={passwordDraft.currentPassword}
                      required
                      onChange={(event) => updatePasswordDraftField("currentPassword", event.target.value)}
                    />
                  </div>

                  <div className="module-inline-grid">
                    <div className="field-group">
                      <label>Nova senha</label>
                      <input
                        type="password"
                        autoComplete="new-password"
                        value={passwordDraft.newPassword}
                        minLength={8}
                        required
                        onChange={(event) => updatePasswordDraftField("newPassword", event.target.value)}
                      />
                    </div>
                    <div className="field-group">
                      <label>Confirmar nova senha</label>
                      <input
                        type="password"
                        autoComplete="new-password"
                        value={passwordDraft.confirmPassword}
                        minLength={8}
                        required
                        onChange={(event) => updatePasswordDraftField("confirmPassword", event.target.value)}
                      />
                    </div>
                  </div>

                  <button className="primary-link button-link" type="submit" disabled={isChangingPassword}>
                    {isChangingPassword ? "Alterando..." : "Alterar senha"}
                  </button>
                </form>
              </div>
            </section>

            <section className="surface-card settings-alert-panel module-form settings-config-card">
              <div className="module-section-head compact-order-column-head">
                <div className="kitchen-column-copy">
                  <span className="eyebrow">3. Atalho seguro</span>
                  <strong>Entrar sem digitar senha neste dispositivo</strong>
                </div>
                <span className={`status-chip ${settings?.shortcutAccess.isEnabled ? "ready" : "pending"}`}>
                  {settings?.shortcutAccess.isEnabled ? "Ativo" : "Sem atalho"}
                </span>
              </div>

              <div className="settings-alert-metrics">
                <article className="settings-alert-metric">
                  <small>Criado em</small>
                  <strong>{formatShortcutDate(settings?.shortcutAccess.createdAtUtc)}</strong>
                </article>
                <article className="settings-alert-metric">
                  <small>Expira em</small>
                  <strong>{formatShortcutDate(settings?.shortcutAccess.expiresAtUtc)}</strong>
                </article>
                <article className="settings-alert-metric">
                  <small>Ultimo uso</small>
                  <strong>{formatShortcutDate(settings?.shortcutAccess.lastUsedAtUtc)}</strong>
                </article>
              </div>

              <div className="field-group">
                <label>Senha owner para gerar ou revogar</label>
                <input
                  type="password"
                  autoComplete="current-password"
                  value={shortcutPassword}
                  onChange={(event) => setShortcutPassword(event.target.value)}
                  placeholder="Digite sua senha atual"
                />
              </div>

              {shortcutUrl ? (
                <div className="field-group">
                  <label>Link do atalho gerado agora</label>
                  <textarea value={shortcutUrl} readOnly rows={3} />
                  <p className="field-hint">Copie agora. Por seguranca, o ZeroPaper nao mostra esse token novamente.</p>
                </div>
              ) : null}

              <div className="toolbar-actions compact settings-alert-actions">
                <button className="primary-link button-link" type="button" disabled={isRotatingShortcut} onClick={() => void handleShortcutGenerate()}>
                  {isRotatingShortcut ? "Gerando..." : settings?.shortcutAccess.isEnabled ? "Gerar novo atalho" : "Gerar atalho"}
                </button>

                {shortcutUrl ? (
                  <button className="ghost-link button-link" type="button" onClick={() => void handleCopyShortcutUrl()}>
                    Copiar link
                  </button>
                ) : null}

                {settings?.shortcutAccess.isEnabled ? (
                  <button className="ghost-link button-link" type="button" disabled={isRevokingShortcut} onClick={() => void handleShortcutRevoke()}>
                    {isRevokingShortcut ? "Revogando..." : "Revogar atalho"}
                  </button>
                ) : null}
              </div>
            </section>

            <section className="settings-alerts-block">
              <div className="module-section-head">
                <div>
                  <span className="eyebrow">4. Alertas sonoros</span>
                  <h2>Operacao em tempo real</h2>
                </div>
                <span className="status-chip ready">{activeAlertsCount}/2 ativos</span>
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
