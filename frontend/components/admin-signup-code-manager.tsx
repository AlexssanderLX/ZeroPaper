"use client";

import { FormEvent, useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import {
  ApiError,
  confirmCurrentPassword,
  createSignupCode,
  deactivateAdminUser,
  deleteAdminUser,
  getAdminUsers,
  reactivateAdminUser,
  getSignupCodes,
  type AdminUser,
  type CreateSignupCodeResult,
  type SignupCode,
} from "@/lib/api";
import { useAppSession } from "@/components/app-session-provider";

const GENERATED_CODE_KEY = "zp.admin.generated-code";
const GENERATED_CODE_TTL_MS = 5 * 60 * 1000;

function formatDate(value: string) {
  return new Intl.DateTimeFormat("pt-BR", {
    dateStyle: "short",
    timeStyle: "short",
  }).format(new Date(value));
}

function formatCountdown(value: number) {
  const totalSeconds = Math.max(0, Math.ceil(value / 1000));
  const minutes = Math.floor(totalSeconds / 60);
  const seconds = totalSeconds % 60;
  return `${minutes}:${seconds.toString().padStart(2, "0")}`;
}

function formatOptionalDate(value?: string | null) {
  return value ? formatDate(value) : "Sem registro";
}

function formatRole(value: string) {
  const map: Record<string, string> = {
    Root: "Root",
    Owner: "Dono",
    Manager: "Gerencia",
    Employee: "Equipe",
  };

  return map[value] ?? value;
}

function resolveCodeStatus(code: SignupCode) {
  const expiresAt = new Date(code.expiresAtUtc).getTime();

  if (expiresAt <= Date.now()) {
    return { label: "Expirado", tone: "cancelled", isAvailable: false };
  }

  if (code.isActive) {
    return { label: "Disponivel", tone: "available", isAvailable: true };
  }

  return { label: "Utilizado", tone: "warning", isAvailable: false };
}

function resolvePresenceStatus(user: AdminUser) {
  if (user.isOnlineNow) {
    return { label: "Online", tone: "available" };
  }

  return { label: "Offline", tone: "inactive" };
}

type SensitiveAction =
  | { type: "reveal-code" }
  | { type: "deactivate-user"; user: AdminUser }
  | { type: "reactivate-user"; user: AdminUser }
  | { type: "delete-user"; user: AdminUser };

function getSensitiveActionCopy(action: SensitiveAction | null) {
  if (!action) {
    return {
      title: "",
      description: "",
      buttonLabel: "",
    };
  }

  switch (action.type) {
    case "reveal-code":
      return {
        title: "Confirmar senha",
        description: "Digite sua senha root para mostrar o codigo gerado.",
        buttonLabel: "Mostrar codigo",
      };
    case "deactivate-user":
      return {
        title: "Desativar conta",
        description: `Confirme sua senha para desativar ${action.user.fullName}.`,
        buttonLabel: "Desativar conta",
      };
    case "reactivate-user":
      return {
        title: "Reativar conta",
        description: `Confirme sua senha para reativar ${action.user.fullName}.`,
        buttonLabel: "Reativar conta",
      };
    case "delete-user":
      return {
        title: "Excluir conta",
        description: `Confirme sua senha para excluir ${action.user.fullName}. Essa acao remove o registro da plataforma.`,
        buttonLabel: "Excluir conta",
      };
  }
}

export function AdminSignupCodeManager() {
  const router = useRouter();
  const { session, clearSession } = useAppSession();
  const [codes, setCodes] = useState<SignupCode[]>([]);
  const [users, setUsers] = useState<AdminUser[]>([]);
  const [createdCode, setCreatedCode] = useState<CreateSignupCodeResult | null>(null);
  const [showCreatedCode, setShowCreatedCode] = useState(false);
  const [createdCodeExpiresAt, setCreatedCodeExpiresAt] = useState<number | null>(null);
  const [remainingCodeMs, setRemainingCodeMs] = useState(0);
  const [loading, setLoading] = useState(true);
  const [processingUserId, setProcessingUserId] = useState<string | null>(null);
  const [sensitiveAction, setSensitiveAction] = useState<SensitiveAction | null>(null);
  const [confirmPassword, setConfirmPassword] = useState("");
  const [confirmingSensitiveAction, setConfirmingSensitiveAction] = useState(false);
  const [confirmErrorMessage, setConfirmErrorMessage] = useState("");
  const [errorMessage, setErrorMessage] = useState("");
  const [pageMessage, setPageMessage] = useState("");

  async function loadAdminData() {
    setLoading(true);

    try {
      const codesResponse = await getSignupCodes(session.token);
      setCodes(codesResponse);

      try {
        const usersResponse = await getAdminUsers(session.token);
        setUsers(usersResponse);
        setPageMessage("");
      } catch (error) {
        if (error instanceof ApiError && error.status === 404) {
          setUsers([]);
          setPageMessage("A lista de contas ainda nao esta disponivel nesta instancia da API.");
          return;
        }

        throw error;
      }

      setErrorMessage("");
      setPageMessage("");
    } catch (error) {
      if (error instanceof ApiError && error.status === 401) {
        await clearSession();
        return;
      }

      setPageMessage("Nao foi possivel carregar o painel admin agora.");
    } finally {
      setLoading(false);
    }
  }

  async function handleUnauthorizedAwareError(error: unknown) {
    if (error instanceof ApiError && error.status === 401) {
      await clearSession();
      return true;
    }

    return false;
  }

  useEffect(() => {
    if (session.profile !== "admin") {
      router.replace("/login");
      return;
    }

    void loadAdminData();
  }, [router, session.profile, session.token]);

  useEffect(() => {
    const rawStoredValue = window.sessionStorage.getItem(GENERATED_CODE_KEY);

    if (!rawStoredValue) {
      return;
    }

    try {
      const parsedValue = JSON.parse(rawStoredValue) as {
        code: CreateSignupCodeResult;
        expiresAt: number;
      };

      if (parsedValue.expiresAt <= Date.now()) {
        window.sessionStorage.removeItem(GENERATED_CODE_KEY);
        return;
      }

      setCreatedCode(parsedValue.code);
      setCreatedCodeExpiresAt(parsedValue.expiresAt);
    } catch {
      window.sessionStorage.removeItem(GENERATED_CODE_KEY);
    }
  }, []);

  useEffect(() => {
    if (!createdCodeExpiresAt) {
      setRemainingCodeMs(0);
      return;
    }

    const tick = () => {
      const remaining = createdCodeExpiresAt - Date.now();

      if (remaining <= 0) {
        setCreatedCode(null);
        setCreatedCodeExpiresAt(null);
        setShowCreatedCode(false);
        setRemainingCodeMs(0);
        window.sessionStorage.removeItem(GENERATED_CODE_KEY);
        return;
      }

      setRemainingCodeMs(remaining);
    };

    tick();
    const interval = window.setInterval(tick, 1000);
    return () => window.clearInterval(interval);
  }, [createdCodeExpiresAt]);

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    const formData = new FormData(event.currentTarget);
    const label = String(formData.get("label") ?? "").trim();
    const boundEmail = String(formData.get("boundEmail") ?? "").trim().toLowerCase();
    if (!label) {
      setErrorMessage("Informe um nome para o codigo.");
      return;
    }

    try {
      const response = await createSignupCode(session.token, {
        label,
        boundEmail: boundEmail || undefined,
      });

      setCreatedCode(response);
      setShowCreatedCode(false);
      const expiresAt = Date.now() + GENERATED_CODE_TTL_MS;
      setCreatedCodeExpiresAt(expiresAt);
      window.sessionStorage.setItem(
        GENERATED_CODE_KEY,
        JSON.stringify({
          code: response,
          expiresAt,
        }),
      );
      setErrorMessage("");
      (event.currentTarget as HTMLFormElement).reset();
      await loadAdminData();
    } catch (error) {
      if (await handleUnauthorizedAwareError(error)) {
        return;
      }

      setErrorMessage("Nao foi possivel gerar o codigo agora.");
    }
  }

  async function handleDeactivateUser(user: AdminUser) {
    if (user.role !== "Root") {
      setConfirmPassword("");
      setConfirmErrorMessage("");
      setSensitiveAction({ type: "deactivate-user", user });
    }
  }

  async function handleReactivateUser(user: AdminUser) {
    if (user.role !== "Root") {
      setConfirmPassword("");
      setConfirmErrorMessage("");
      setSensitiveAction({ type: "reactivate-user", user });
    }
  }

  async function handleDeleteUser(user: AdminUser) {
    if (user.role !== "Root") {
      setConfirmPassword("");
      setConfirmErrorMessage("");
      setSensitiveAction({ type: "delete-user", user });
    }
  }

  function handleRequestCodeReveal() {
    setConfirmPassword("");
    setConfirmErrorMessage("");
    setSensitiveAction({ type: "reveal-code" });
  }

  function closeSensitiveAction() {
    if (confirmingSensitiveAction) {
      return;
    }

    setSensitiveAction(null);
    setConfirmPassword("");
    setConfirmErrorMessage("");
  }

  async function handleSensitiveActionSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    if (!sensitiveAction) {
      return;
    }

    if (!confirmPassword.trim()) {
      setConfirmErrorMessage("Digite sua senha para continuar.");
      return;
    }

    try {
      setConfirmingSensitiveAction(true);

      const confirmation = await confirmCurrentPassword(session.token, {
        password: confirmPassword,
      });

      if (!confirmation.confirmed) {
        setConfirmErrorMessage("Senha incorreta.");
        return;
      }

      if (sensitiveAction.type === "reveal-code") {
        setShowCreatedCode(true);
        setPageMessage("Codigo liberado nesta tela.");
      }

      if (sensitiveAction.type === "deactivate-user") {
        setProcessingUserId(sensitiveAction.user.id);
        const updatedUser = await deactivateAdminUser(session.token, sensitiveAction.user.id);
        setUsers((currentValue) => currentValue.map((item) => (item.id === updatedUser.id ? updatedUser : item)));
        setPageMessage("Conta desativada com sucesso.");
      }

      if (sensitiveAction.type === "reactivate-user") {
        setProcessingUserId(sensitiveAction.user.id);
        const updatedUser = await reactivateAdminUser(session.token, sensitiveAction.user.id);
        setUsers((currentValue) => currentValue.map((item) => (item.id === updatedUser.id ? updatedUser : item)));
        setPageMessage("Conta reativada com sucesso.");
      }

      if (sensitiveAction.type === "delete-user") {
        setProcessingUserId(sensitiveAction.user.id);
        await deleteAdminUser(session.token, sensitiveAction.user.id);
        setUsers((currentValue) => currentValue.filter((item) => item.id !== sensitiveAction.user.id));
        setPageMessage("Conta excluida com sucesso.");
      }

      setSensitiveAction(null);
      setConfirmPassword("");
      setConfirmErrorMessage("");
    } catch (error) {
      if (await handleUnauthorizedAwareError(error)) {
        return;
      }

      setConfirmErrorMessage("Nao foi possivel validar essa acao agora.");
    } finally {
      setConfirmingSensitiveAction(false);
      setProcessingUserId(null);
    }
  }

  if (session.profile !== "admin") {
    return null;
  }

  const accountsUnavailable = pageMessage.includes("lista de contas");
  const availableCodesCount = codes.filter((item) => resolveCodeStatus(item).isAvailable).length;
  const usersWithActiveSessionCount = users.filter((item) => item.hasActiveSession).length;
  const onlineUsersCount = users.filter((item) => item.isOnlineNow).length;
  const summaryItems = [
    { value: availableCodesCount, label: "Liberacoes" },
    { value: users.length, label: "Contas" },
    { value: usersWithActiveSessionCount, label: "Acessos" },
    { value: onlineUsersCount, label: "Online" },
  ];

  return (
    <main className="page-shell app-shell">
      <section className="surface-card admin-header-card">
        <div className="admin-header-row">
          <div className="admin-header-copy">
            <span className="eyebrow">ZeroPaper Root</span>
            <h1 className="admin-title">Admin</h1>
          </div>

          <div className="admin-stat-row">
            {summaryItems.map((item) => (
              <article key={item.label} className="admin-stat-chip">
                <strong>{item.value}</strong>
                <span>{item.label}</span>
              </article>
            ))}
          </div>
        </div>
      </section>

      {pageMessage && !accountsUnavailable ? (
        <section className="surface-card admin-inline-note">
          <span className="eyebrow">Contas</span>
          <p>{pageMessage}</p>
        </section>
      ) : null}

      <section className="admin-panel-grid">
        <section className="surface-card module-form-card">
          <span className="eyebrow">Novo codigo</span>
          <h2>Liberar cadastro</h2>
          <form className="module-form" onSubmit={handleSubmit}>
            <div className="field-group">
              <label htmlFor="label">Identificacao</label>
              <input id="label" name="label" placeholder="Ex.: Restaurante do bairro" />
            </div>

            <div className="field-group">
              <label htmlFor="boundEmail">Email vinculado</label>
              <input id="boundEmail" name="boundEmail" type="email" placeholder="opcional" />
            </div>

            <button className="primary-link button-link" type="submit">
              Gerar codigo
            </button>
          </form>

          {createdCode ? (
            <div className="module-empty-state generated-code-card">
              <div className="entity-head">
                <div>
                  <strong>Codigo gerado</strong>
                  <p>Visivel aqui por {formatCountdown(remainingCodeMs)}.</p>
                </div>
                <span className="status-chip available">5 min</span>
              </div>
              <div className="toolbar-actions compact">
                <button
                  className="ghost-link button-link"
                  type="button"
                  onClick={() => {
                    if (showCreatedCode) {
                      setShowCreatedCode(false);
                      return;
                    }

                    handleRequestCodeReveal();
                  }}
                >
                  {showCreatedCode ? "Ocultar codigo" : "Desocultar codigo"}
                </button>
              </div>
              <p className="generated-code-value">{showCreatedCode ? createdCode.rawCode : "codigo oculto"}</p>
            </div>
          ) : null}

          {errorMessage ? <p className="module-feedback error">{errorMessage}</p> : null}
        </section>

        <section className="surface-card module-list-card">
          <div className="module-section-head">
            <span className="eyebrow">Liberacoes</span>
            <strong>{codes.length} codigos</strong>
          </div>

          {loading ? (
            <p className="loading-state">Carregando codigos...</p>
        ) : codes.length === 0 ? (
          <div className="module-empty-state">
            <strong>Nenhum codigo gerado.</strong>
          </div>
        ) : (
            <div className="module-card-list">
              {codes.map((code) => {
                const status = resolveCodeStatus(code);

                return (
                  <article key={code.id} className="module-entity-card interactive-card">
                    <div className="entity-head">
                      <div>
                        <h3>{code.label}</h3>
                        <p>{code.boundEmail || "Sem email vinculado"}</p>
                      </div>
                      <span className={`status-chip ${status.tone}`}>{status.label}</span>
                    </div>

                    <div className="entity-meta-grid admin-meta-line">
                      <span>Criado em {formatDate(code.createdAtUtc)}</span>
                    </div>
                  </article>
                );
              })}
            </div>
          )}
        </section>
      </section>

      <section className="surface-card module-list-card">
        <div className="module-section-head">
          <span className="eyebrow">Contas</span>
          <strong>{accountsUnavailable ? "Indisponivel" : `${users.length} usuarios`}</strong>
        </div>

        {loading ? (
          <p className="loading-state">Carregando contas...</p>
        ) : users.length === 0 ? (
          <div className="module-empty-state">
            <strong>{accountsUnavailable ? "Contas indisponiveis agora." : "Nenhuma conta cadastrada ainda."}</strong>
            {accountsUnavailable ? <p>Reinicie a API para liberar esta lista.</p> : null}
          </div>
        ) : (
          <div className="module-card-list">
            {users.map((user) => {
              const presenceStatus = resolvePresenceStatus(user);
              const isEnabled = user.isActive && user.isCompanyActive;
              const isRoot = user.role === "Root";
              const isProcessing = processingUserId === user.id;
              const accessLabel = user.hasActiveSession ? "Acesso aberto" : "Pronta para login";

              return (
                <article key={user.id} className="module-entity-card interactive-card">
                  <div className="entity-head">
                    <div>
                      <h3>{user.fullName}</h3>
                      <p>{user.email}</p>
                    </div>
                    <div className="entity-status-stack">
                      <span className={`status-chip ${isEnabled ? "available" : "inactive"}`}>
                        {isEnabled ? "Conta ativa" : "Conta inativa"}
                      </span>
                      <span className={`status-chip ${presenceStatus.tone}`}>{presenceStatus.label}</span>
                    </div>
                  </div>

                  <div className="entity-meta-grid admin-meta-line">
                    <span>{user.restaurantName}</span>
                    <span>{formatRole(user.role)}</span>
                    <span>{accessLabel}</span>
                    <span>{user.activeSessionCount} acessos abertos</span>
                    <span>Ultimo login {formatOptionalDate(user.lastLoginAtUtc)}</span>
                    <span>Ultima atividade {formatOptionalDate(user.lastSeenAtUtc)}</span>
                  </div>

                  <div className="toolbar-actions compact admin-card-actions">
                    <button
                      className="ghost-link button-link"
                      type="button"
                      disabled={isRoot || isProcessing}
                      onClick={() => void (user.isActive ? handleDeactivateUser(user) : handleReactivateUser(user))}
                    >
                      {isProcessing ? "Processando..." : user.isActive ? "Desativar" : "Reativar"}
                    </button>
                    <button
                      className="ghost-link button-link admin-danger-button"
                      type="button"
                      disabled={isRoot || isProcessing}
                      onClick={() => void handleDeleteUser(user)}
                    >
                      Excluir
                    </button>
                  </div>
                </article>
              );
            })}
          </div>
        )}
      </section>

      {sensitiveAction ? (
        <div className="admin-modal-backdrop" role="presentation">
          <section className="surface-card admin-sensitive-modal" role="dialog" aria-modal="true">
            <span className="eyebrow">Confirmacao</span>
            <h2>{getSensitiveActionCopy(sensitiveAction).title}</h2>
            <p>{getSensitiveActionCopy(sensitiveAction).description}</p>

            <form className="module-form" onSubmit={handleSensitiveActionSubmit}>
              <div className="field-group">
                <label htmlFor="confirmPassword">Sua senha</label>
                <input
                  id="confirmPassword"
                  name="confirmPassword"
                  type="password"
                  value={confirmPassword}
                  onChange={(event) => setConfirmPassword(event.currentTarget.value)}
                  placeholder="Digite sua senha root"
                  autoComplete="current-password"
                />
              </div>

              {confirmErrorMessage ? <p className="module-feedback error">{confirmErrorMessage}</p> : null}

              <div className="toolbar-actions compact admin-modal-actions">
                <button className="primary-link button-link" type="submit" disabled={confirmingSensitiveAction}>
                  {confirmingSensitiveAction ? "Validando..." : getSensitiveActionCopy(sensitiveAction).buttonLabel}
                </button>
                <button className="ghost-link button-link" type="button" onClick={closeSensitiveAction}>
                  Cancelar
                </button>
              </div>
            </form>
          </section>
        </div>
      ) : null}
    </main>
  );
}
