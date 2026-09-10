"use client";

import { useEffect, useState } from "react";
import {
  ensureAdminDemoAccount,
  getAdminDemoAccount,
  revokeAdminDemoLink,
  revokeAdminDemoSessions,
  rotateAdminDemoLink,
  type AdminDemoAccount,
} from "@/lib/api";

type Props = { token: string };

function formatDate(value?: string | null) {
  return value
    ? new Intl.DateTimeFormat("pt-BR", { dateStyle: "short", timeStyle: "short", timeZone: "America/Sao_Paulo" }).format(new Date(value))
    : "Sem registro";
}

export function AdminDemoAccountPanel({ token }: Props) {
  const [status, setStatus] = useState<AdminDemoAccount | null>(null);
  const [accessUrl, setAccessUrl] = useState("");
  const [loading, setLoading] = useState(true);
  const [busy, setBusy] = useState("");
  const [message, setMessage] = useState("");
  const [error, setError] = useState("");

  useEffect(() => {
    getAdminDemoAccount(token)
      .then(setStatus)
      .catch(() => setError("Nao foi possivel consultar a conta de demonstracao."))
      .finally(() => setLoading(false));
  }, [token]);

  async function run(key: string, action: () => Promise<AdminDemoAccount>, success: string) {
    setBusy(key);
    setError("");
    setMessage("");
    try {
      setStatus(await action());
      setMessage(success);
    } catch {
      setError("Nao foi possivel concluir a operacao agora.");
    } finally {
      setBusy("");
    }
  }

  async function generateLink() {
    setBusy("link");
    setError("");
    setMessage("");
    try {
      const response = await rotateAdminDemoLink(token);
      setStatus(response);
      setAccessUrl(response.accessUrl);
      setMessage("Novo link gerado. Qualquer link anterior e suas sessoes foram revogados.");
    } catch {
      setError("Nao foi possivel gerar o link de demonstracao.");
    } finally {
      setBusy("");
    }
  }

  async function copyLink() {
    await navigator.clipboard.writeText(accessUrl);
    setMessage("Link de demonstracao copiado.");
  }

  if (loading) return <p className="loading-state">Carregando conta de demonstracao...</p>;

  if (!status?.exists) {
    return (
      <section className="surface-card admin-demo-card">
        <span className="eyebrow">Primeiro acesso</span>
        <h2>Criar restaurante demonstracao</h2>
        <p>Cria uma unidade isolada no banco com o plano Gestao. Ela nao aceita login convencional.</p>
        {error ? <p className="module-feedback error">{error}</p> : null}
        <button
          className="primary-link button-link"
          type="button"
          disabled={Boolean(busy)}
          onClick={() => void run("create", () => ensureAdminDemoAccount(token), "Conta de demonstracao criada.")}
        >
          {busy === "create" ? "Criando..." : "Criar conta teste"}
        </button>
      </section>
    );
  }

  return (
    <div className="admin-demo-layout">
      <section className="surface-card admin-demo-card admin-demo-status-card">
        <div className="module-section-head">
          <div>
            <span className="eyebrow">Conta teste</span>
            <strong>{status.restaurantName}</strong>
          </div>
          <span className={`status-chip ${status.hasActiveLink ? "available" : "inactive"}`}>
            {status.hasActiveLink ? "Link ativo" : "Sem link"}
          </span>
        </div>
        <div className="admin-demo-metrics">
          <div><span>Plano</span><strong>{status.planName}</strong></div>
          <div><span>Sessoes abertas</span><strong>{status.activeSessionCount}</strong></div>
          <div><span>Link criado</span><strong>{formatDate(status.linkCreatedAtUtc)}</strong></div>
          <div><span>Ultimo acesso</span><strong>{formatDate(status.linkLastUsedAtUtc)}</strong></div>
        </div>
        <p className="admin-section-copy">O acesso por email, senha e atalhos comuns permanece bloqueado para esta unidade.</p>
      </section>

      <section className="surface-card admin-demo-card">
        <span className="eyebrow">Acesso rapido</span>
        <h2>Link da demonstracao</h2>
        <p>Gerar um novo link invalida o anterior e encerra as sessoes que vieram dele.</p>
        {accessUrl ? (
          <div className="admin-demo-link-box">
            <input value={accessUrl} readOnly aria-label="Link de demonstracao" />
            <button className="ghost-link button-link" type="button" onClick={() => void copyLink()}>Copiar</button>
          </div>
        ) : (
          <p className="admin-demo-link-mask">Por seguranca, o link completo aparece somente logo apos ser gerado.</p>
        )}
        <div className="toolbar-actions">
          <button className="primary-link button-link" type="button" disabled={Boolean(busy)} onClick={() => void generateLink()}>
            {busy === "link" ? "Gerando..." : status.hasActiveLink ? "Gerar novo link" : "Gerar link"}
          </button>
          <button
            className="ghost-link button-link admin-danger-button"
            type="button"
            disabled={Boolean(busy) || !status.hasActiveLink}
            onClick={() => void run("revoke", () => revokeAdminDemoLink(token), "Link e sessoes revogados.")}
          >
            {busy === "revoke" ? "Revogando..." : "Quebrar link"}
          </button>
          <button
            className="ghost-link button-link"
            type="button"
            disabled={Boolean(busy) || status.activeSessionCount === 0}
            onClick={() => void run("sessions", () => revokeAdminDemoSessions(token), "Sessoes da demonstracao encerradas.")}
          >
            {busy === "sessions" ? "Encerrando..." : "Encerrar sessoes"}
          </button>
        </div>
        {message ? <p className="module-feedback success">{message}</p> : null}
        {error ? <p className="module-feedback error">{error}</p> : null}
      </section>
    </div>
  );
}
