"use client";

import { FormEvent, useEffect, useState } from "react";
import {
  createSalesAgent,
  getSellerOrders,
  getSalesAgents,
  updateSalesAgent,
  updateSalesAgentStatus,
  type CustomerOrder,
  type SalesAgent,
} from "@/lib/api";
import { handleApiError, type AsyncVoid } from "@/components/modules/module-utils";

type View = "list" | "detail" | "form";

const STATUS_LABEL: Record<string, string> = {
  Pending: "Pendente",
  InKitchen: "Na cozinha",
  Ready: "Pronto",
  Closed: "Fechado",
  Cancelled: "Cancelado",
};

function fmtCurrency(v: number) {
  return v.toLocaleString("pt-BR", { style: "currency", currency: "BRL" });
}

function fmtDate(iso: string) {
  return new Date(iso).toLocaleString("pt-BR", {
    day: "2-digit",
    month: "2-digit",
    year: "2-digit",
    hour: "2-digit",
    minute: "2-digit",
  });
}

function sellerUrl(code: string) {
  const base =
    typeof window !== "undefined"
      ? window.location.origin
      : (process.env.NEXT_PUBLIC_APP_BASE_URL ?? "");
  return `${base}/v/${code}`;
}

/* ─────────────────────────── LIST ─────────────────────────── */

function SellerCard({
  agent,
  onOpen,
  copiedCode,
  onCopy,
}: {
  agent: SalesAgent;
  onOpen: () => void;
  copiedCode: string | null;
  onCopy: (code: string) => void;
}) {
  const url = sellerUrl(agent.code);
  const copied = copiedCode === agent.code;

  return (
    <div className={`vnd-card${agent.isActive ? "" : " vnd-card--off"}`}>
      <button className="vnd-card-main" onClick={onOpen} type="button">
        <span className="vnd-avatar">{agent.name.charAt(0).toUpperCase()}</span>
        <span className="vnd-card-body">
          <span className="vnd-card-row">
            <strong className="vnd-name">{agent.name}</strong>
            {!agent.isActive && <span className="vnd-badge-off">Inativo</span>}
            {agent.commissionPercent != null && (
              <span className="vnd-badge-commission">{agent.commissionPercent}%</span>
            )}
            {agent.phone && <span className="vnd-badge-phone">{agent.phone}</span>}
          </span>
          <span className="vnd-link-preview">{url}</span>
        </span>
        <span className="vnd-chevron">›</span>
      </button>
      <div className="vnd-card-actions">
        <button
          className={`vnd-btn-copy${copied ? " vnd-btn-copy--ok" : ""}`}
          onClick={() => onCopy(agent.code)}
          disabled={!agent.isActive}
          type="button"
        >
          {copied ? "✓ Copiado" : "Copiar link"}
        </button>
        <button className="vnd-btn-detail" onClick={onOpen} type="button">
          Ver desempenho →
        </button>
      </div>
    </div>
  );
}

function SellerListView({
  agents,
  onOpenDetail,
  onOpenCreate,
  copiedCode,
  onCopy,
  successMsg,
  errorMsg,
  newlyCreated,
}: {
  agents: SalesAgent[];
  onOpenDetail: (a: SalesAgent) => void;
  onOpenCreate: () => void;
  copiedCode: string | null;
  onCopy: (code: string) => void;
  successMsg: string;
  errorMsg: string;
  newlyCreated: SalesAgent | null;
}) {
  const activeCount = agents.filter((a) => a.isActive).length;

  return (
    <div className="vnd-list">
      <div className="vnd-list-header">
        <div>
          <h1 className="vnd-list-title">Vendedores</h1>
          <p className="vnd-list-subtitle">
            Cada vendedor tem um link exclusivo. Pedidos feitos pelo link são atribuídos a ele
            automaticamente.
          </p>
        </div>
        <button className="vnd-btn-primary" onClick={onOpenCreate} type="button">
          + Novo vendedor
        </button>
      </div>

      {successMsg && (
        <div className="vnd-banner vnd-banner--ok">
          <span>✓ {successMsg}</span>
          {newlyCreated && (
            <button
              className={`vnd-btn-copy${copiedCode === newlyCreated.code ? " vnd-btn-copy--ok" : ""}`}
              onClick={() => onCopy(newlyCreated.code)}
              type="button"
            >
              {copiedCode === newlyCreated.code ? "✓ Copiado" : "Copiar link"}
            </button>
          )}
        </div>
      )}

      {errorMsg && <p className="vnd-error">{errorMsg}</p>}

      {agents.length > 0 && (
        <div className="vnd-chips-row">
          <span className="vnd-chip vnd-chip--active">
            {activeCount} ativo{activeCount !== 1 ? "s" : ""}
          </span>
          {agents.length - activeCount > 0 && (
            <span className="vnd-chip">
              {agents.length - activeCount} inativo{agents.length - activeCount !== 1 ? "s" : ""}
            </span>
          )}
        </div>
      )}

      {agents.length === 0 && !errorMsg && (
        <div className="vnd-empty-state">
          <div className="vnd-empty-icon">👤</div>
          <p className="vnd-empty-title">Nenhum vendedor ainda</p>
          <p className="vnd-empty-text">
            Crie um vendedor para gerar um link exclusivo. Toda venda pelo link é atribuída a ele.
          </p>
          <button className="vnd-btn-primary" onClick={onOpenCreate} type="button">
            Criar primeiro vendedor
          </button>
        </div>
      )}

      {agents.length > 0 && (
        <div className="vnd-cards">
          {agents.map((a) => (
            <SellerCard
              key={a.id}
              agent={a}
              onOpen={() => onOpenDetail(a)}
              copiedCode={copiedCode}
              onCopy={onCopy}
            />
          ))}
        </div>
      )}
    </div>
  );
}

/* ─────────────────────────── DETAIL ─────────────────────────── */

function SellerDetailView({
  agent: initial,
  allAgents,
  token,
  onUnauthorized,
  onBack,
  onEdit,
  onToggle,
  copiedCode,
  onCopy,
}: {
  agent: SalesAgent;
  allAgents: SalesAgent[];
  token: string;
  onUnauthorized: AsyncVoid;
  onBack: () => void;
  onEdit: (a: SalesAgent) => void;
  onToggle: (a: SalesAgent) => Promise<void>;
  copiedCode: string | null;
  onCopy: (code: string) => void;
}) {
  const agent = allAgents.find((a) => a.id === initial.id) ?? initial;
  const [orders, setOrders] = useState<CustomerOrder[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const url = sellerUrl(agent.code);
  const copied = copiedCode === agent.code;

  useEffect(() => {
    let mounted = true;
    setLoading(true);
    void (async () => {
      try {
        const data = await getSellerOrders(token, agent.id);
        if (mounted) setOrders(data);
      } catch (err) {
        await handleApiError(
          err,
          onUnauthorized,
          setError,
          "Não foi possível carregar os pedidos.",
        );
      } finally {
        if (mounted) setLoading(false);
      }
    })();
    return () => {
      mounted = false;
    };
  }, [agent.id, token, onUnauthorized]);

  const totalRevenue = orders.reduce((s, o) => s + o.totalAmount, 0);
  const closed = orders.filter((o) => o.status === "Closed");
  const avgTicket = orders.length > 0 ? totalRevenue / orders.length : 0;
  const commission =
    agent.commissionPercent != null ? totalRevenue * (agent.commissionPercent / 100) : null;

  return (
    <div className="vnd-detail">
      {/* nav */}
      <div className="vnd-detail-nav">
        <button className="vnd-btn-back" onClick={onBack} type="button">
          ← Vendedores
        </button>
        <div className="vnd-detail-nav-right">
          <button className="vnd-btn-ghost" onClick={() => onEdit(agent)} type="button">
            Editar
          </button>
          <button
            className={`vnd-btn-ghost${agent.isActive ? " vnd-btn-ghost--danger" : ""}`}
            onClick={() => void onToggle(agent)}
            type="button"
          >
            {agent.isActive ? "Desativar" : "Ativar"}
          </button>
        </div>
      </div>

      {/* profile */}
      <div className="vnd-profile-card">
        <span className="vnd-avatar vnd-avatar--lg">{agent.name.charAt(0).toUpperCase()}</span>
        <div className="vnd-profile-info">
          <div className="vnd-profile-name-row">
            <h2 className="vnd-profile-name">{agent.name}</h2>
            {!agent.isActive && <span className="vnd-badge-off">Inativo</span>}
          </div>
          <div className="vnd-profile-chips">
            {agent.phone && <span className="vnd-badge-phone">{agent.phone}</span>}
            {agent.commissionPercent != null && (
              <span className="vnd-badge-commission">{agent.commissionPercent}% comissão</span>
            )}
          </div>
          <div className="vnd-profile-link-row">
            <span className="vnd-profile-url">{url}</span>
            <button
              className={`vnd-btn-copy${copied ? " vnd-btn-copy--ok" : ""}`}
              onClick={() => onCopy(agent.code)}
              disabled={!agent.isActive}
              type="button"
            >
              {copied ? "✓ Copiado" : "Copiar link"}
            </button>
            {agent.isActive && (
              <a
                className="vnd-btn-open"
                href={url}
                target="_blank"
                rel="noopener noreferrer"
              >
                Abrir ↗
              </a>
            )}
          </div>
        </div>
      </div>

      {/* stats */}
      <div className="vnd-stats">
        <div className="vnd-stat">
          <span className="vnd-stat-n">{orders.length}</span>
          <span className="vnd-stat-l">Pedidos</span>
        </div>
        <div className="vnd-stat">
          <span className="vnd-stat-n">{closed.length}</span>
          <span className="vnd-stat-l">Fechados</span>
        </div>
        <div className="vnd-stat">
          <span className="vnd-stat-n">{fmtCurrency(totalRevenue)}</span>
          <span className="vnd-stat-l">Volume total</span>
        </div>
        <div className="vnd-stat">
          <span className="vnd-stat-n">{fmtCurrency(avgTicket)}</span>
          <span className="vnd-stat-l">Ticket médio</span>
        </div>
        {commission !== null && (
          <div className="vnd-stat vnd-stat--hi">
            <span className="vnd-stat-n">{fmtCurrency(commission)}</span>
            <span className="vnd-stat-l">Comissão est.</span>
          </div>
        )}
      </div>

      {/* orders */}
      <div className="vnd-orders">
        <h3 className="vnd-orders-title">Pedidos deste vendedor</h3>
        {error && <p className="vnd-error">{error}</p>}
        {loading && <p className="vnd-muted">Carregando...</p>}
        {!loading && !error && orders.length === 0 && (
          <p className="vnd-muted">Nenhum pedido atribuído ainda.</p>
        )}
        {orders.length > 0 && (
          <div className="vnd-table-wrap">
            <table className="vnd-table">
              <thead>
                <tr>
                  <th>Nº</th>
                  <th>Cliente</th>
                  <th>Status</th>
                  <th>Total</th>
                  <th>Data</th>
                </tr>
              </thead>
              <tbody>
                {orders.map((o) => (
                  <tr key={o.id}>
                    <td className="vnd-td-num">#{o.number}</td>
                    <td>{o.customerName ?? "—"}</td>
                    <td>
                      <span className={`vnd-status vnd-status--${o.status.toLowerCase()}`}>
                        {STATUS_LABEL[o.status] ?? o.status}
                      </span>
                    </td>
                    <td className="vnd-td-val">{fmtCurrency(o.totalAmount)}</td>
                    <td className="vnd-td-date">{fmtDate(o.submittedAtUtc)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>
    </div>
  );
}

/* ─────────────────────────── FORM ─────────────────────────── */

function SellerForm({
  editAgent,
  onBack,
  onSaved,
  token,
  onUnauthorized,
}: {
  editAgent: SalesAgent | null;
  onBack: () => void;
  onSaved: (a: SalesAgent, isNew: boolean) => void;
  token: string;
  onUnauthorized: AsyncVoid;
}) {
  const [name, setName] = useState(editAgent?.name ?? "");
  const [phone, setPhone] = useState(editAgent?.phone ?? "");
  const [commission, setCommission] = useState(
    editAgent?.commissionPercent != null ? String(editAgent.commissionPercent) : "",
  );
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState("");

  async function submit(e: FormEvent<HTMLFormElement>) {
    e.preventDefault();
    if (saving) return;
    const n = name.trim();
    if (!n) {
      setError("Informe o nome do vendedor.");
      return;
    }
    const pct = commission.trim() ? parseFloat(commission) : null;
    if (pct !== null && (isNaN(pct) || pct < 0 || pct > 100)) {
      setError("Comissão deve ser entre 0 e 100.");
      return;
    }
    setSaving(true);
    setError("");
    try {
      const payload = { name: n, phone: phone.trim() || null, commissionPercent: pct };
      if (editAgent) {
        const updated = await updateSalesAgent(token, editAgent.id, payload);
        onSaved(updated, false);
      } else {
        const created = await createSalesAgent(token, payload);
        onSaved(created, true);
      }
    } catch (err) {
      await handleApiError(err, onUnauthorized, setError, "Não foi possível salvar.");
    } finally {
      setSaving(false);
    }
  }

  return (
    <div className="vnd-form-view">
      <button className="vnd-btn-back" onClick={onBack} type="button">
        ← {editAgent ? "Voltar" : "Vendedores"}
      </button>

      <div className="vnd-form-card">
        <div className="vnd-form-head">
          <span className="vnd-avatar vnd-avatar--md">
            {name.trim() ? name.trim().charAt(0).toUpperCase() : "?"}
          </span>
          <div>
            <h2 className="vnd-form-title">
              {editAgent ? "Editar vendedor" : "Novo vendedor"}
            </h2>
            <p className="vnd-form-sub">
              {editAgent
                ? "Atualize os dados do vendedor."
                : "Crie um vendedor para gerar um link exclusivo."}
            </p>
          </div>
        </div>

        <form className="vnd-form" onSubmit={submit}>
          <label className="vnd-field">
            <span className="vnd-label">Nome *</span>
            <input
              className="vnd-input"
              value={name}
              onChange={(e) => setName(e.target.value)}
              placeholder="Ex: João Silva, Balcão, WhatsApp..."
              required
              autoFocus
            />
          </label>

          <label className="vnd-field">
            <span className="vnd-label">Telefone</span>
            <input
              className="vnd-input"
              type="tel"
              value={phone}
              onChange={(e) => setPhone(e.target.value)}
              placeholder="(11) 99999-0000"
            />
          </label>

          <label className="vnd-field">
            <span className="vnd-label">Comissão %</span>
            <div className="vnd-input-row">
              <input
                className="vnd-input vnd-input--sm"
                type="number"
                min="0"
                max="100"
                step="0.01"
                value={commission}
                onChange={(e) => setCommission(e.target.value)}
                placeholder="0"
              />
              <span className="vnd-input-suffix">%</span>
            </div>
            <span className="vnd-hint">
              Usada para calcular a comissão estimada no desempenho do vendedor.
            </span>
          </label>

          {error && <p className="vnd-error">{error}</p>}

          <div className="vnd-form-actions">
            <button className="vnd-btn-primary" type="submit" disabled={saving}>
              {saving ? "Salvando..." : editAgent ? "Salvar" : "Criar vendedor"}
            </button>
            <button className="vnd-btn-ghost" type="button" onClick={onBack} disabled={saving}>
              Cancelar
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}

/* ─────────────────────────── ROOT ─────────────────────────── */

export function SellersModule({
  token,
  onUnauthorized,
}: {
  token: string;
  onUnauthorized: AsyncVoid;
}) {
  const [agents, setAgents] = useState<SalesAgent[]>([]);
  const [loading, setLoading] = useState(true);
  const [view, setView] = useState<View>("list");
  const [selected, setSelected] = useState<SalesAgent | null>(null);
  const [editing, setEditing] = useState<SalesAgent | null>(null);
  const [errorMsg, setErrorMsg] = useState("");
  const [successMsg, setSuccessMsg] = useState("");
  const [newlyCreated, setNewlyCreated] = useState<SalesAgent | null>(null);
  const [copiedCode, setCopiedCode] = useState<string | null>(null);

  useEffect(() => {
    let mounted = true;
    void (async () => {
      try {
        const data = await getSalesAgents(token);
        if (mounted) setAgents(data);
      } catch (err) {
        await handleApiError(
          err,
          onUnauthorized,
          setErrorMsg,
          "Não foi possível carregar os vendedores.",
        );
      } finally {
        if (mounted) setLoading(false);
      }
    })();
    return () => {
      mounted = false;
    };
  }, [token, onUnauthorized]);

  function copyLink(code: string) {
    void navigator.clipboard.writeText(sellerUrl(code)).then(() => {
      setCopiedCode(code);
      setTimeout(() => setCopiedCode(null), 2500);
    });
  }

  function openCreate() {
    setEditing(null);
    setNewlyCreated(null);
    setErrorMsg("");
    setSuccessMsg("");
    setView("form");
  }

  function openEdit(agent: SalesAgent) {
    setEditing(agent);
    setNewlyCreated(null);
    setErrorMsg("");
    setSuccessMsg("");
    setView("form");
  }

  function openDetail(agent: SalesAgent) {
    setSelected(agent);
    setSuccessMsg("");
    setNewlyCreated(null);
    setView("detail");
  }

  function handleSaved(agent: SalesAgent, isNew: boolean) {
    setAgents((prev) =>
      isNew ? [...prev, agent] : prev.map((a) => (a.id === agent.id ? agent : a)),
    );
    if (isNew) {
      setNewlyCreated(agent);
      setSuccessMsg(`Vendedor "${agent.name}" criado com sucesso!`);
      setView("list");
    } else {
      if (selected?.id === agent.id) setSelected(agent);
      setSuccessMsg(`Vendedor "${agent.name}" atualizado.`);
      setView(selected ? "detail" : "list");
    }
  }

  async function toggleStatus(agent: SalesAgent) {
    try {
      const updated = await updateSalesAgentStatus(token, agent.id, !agent.isActive);
      setAgents((prev) => prev.map((a) => (a.id === agent.id ? updated : a)));
      if (selected?.id === agent.id) setSelected(updated);
    } catch (err) {
      await handleApiError(
        err,
        onUnauthorized,
        setErrorMsg,
        "Não foi possível atualizar o status.",
      );
    }
  }

  if (loading) return <p className="vnd-muted">Carregando vendedores...</p>;

  if (view === "form") {
    return (
      <SellerForm
        editAgent={editing}
        onBack={() => setView(selected ? "detail" : "list")}
        onSaved={handleSaved}
        token={token}
        onUnauthorized={onUnauthorized}
      />
    );
  }

  if (view === "detail" && selected) {
    return (
      <SellerDetailView
        agent={selected}
        allAgents={agents}
        token={token}
        onUnauthorized={onUnauthorized}
        onBack={() => {
          setSelected(null);
          setView("list");
        }}
        onEdit={openEdit}
        onToggle={toggleStatus}
        copiedCode={copiedCode}
        onCopy={copyLink}
      />
    );
  }

  return (
    <SellerListView
      agents={agents}
      onOpenDetail={openDetail}
      onOpenCreate={openCreate}
      copiedCode={copiedCode}
      onCopy={copyLink}
      successMsg={successMsg}
      errorMsg={errorMsg}
      newlyCreated={newlyCreated}
    />
  );
}
