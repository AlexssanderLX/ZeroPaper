"use client";

import { FormEvent, useEffect, useState } from "react";
import {
  ApiError,
  configureAdminPlatformBilling,
  createAdminSubscriptionCheckout,
  disconnectAdminPlatformBilling,
  getAdminPlatformBilling,
  syncAdminSubscription,
  markAdminSubscriptionPaid,
  type AdminCompanyFlow,
  type AdminPlatformBillingStatus,
  type AdminSubscriptionCheckout,
} from "@/lib/api";

type Props = { token: string; companies: AdminCompanyFlow[] };

export function AdminPlatformBillingPanel({ token, companies }: Props) {
  const [status, setStatus] = useState<AdminPlatformBillingStatus | null>(null);
  const [accessToken, setAccessToken] = useState("");
  const [rootPassword, setRootPassword] = useState("");
  const [showSecrets, setShowSecrets] = useState(false);
  const [busy, setBusy] = useState("");
  const [message, setMessage] = useState("");
  const [error, setError] = useState("");
  const [checkouts, setCheckouts] = useState<Record<string, AdminSubscriptionCheckout>>({});

  useEffect(() => {
    void getAdminPlatformBilling(token)
      .then(setStatus)
      .catch(() => setError("Nao foi possivel consultar a conta recebedora."));
  }, [token]);

  function describeError(value: unknown) {
    return value instanceof ApiError ? value.message : "Nao foi possivel concluir a operacao.";
  }

  async function handleConfigure(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setBusy("configure"); setError(""); setMessage("");
    try {
      const next = await configureAdminPlatformBilling(token, { accessToken: accessToken.trim(), password: rootPassword });
      setStatus(next); setAccessToken(""); setRootPassword("");
      setMessage("Conta Mercado Pago validada e salva com criptografia no servidor.");
    } catch (value) { setError(describeError(value)); }
    finally { setBusy(""); }
  }

  async function handleDisconnect() {
    if (!rootPassword || !window.confirm("Desconectar a conta recebedora da plataforma?")) return;
    setBusy("disconnect"); setError(""); setMessage("");
    try {
      await disconnectAdminPlatformBilling(token, rootPassword);
      setStatus({ configured: false, provider: "MercadoPago", liveMode: false });
      setRootPassword(""); setMessage("Conta recebedora desconectada.");
    } catch (value) { setError(describeError(value)); }
    finally { setBusy(""); }
  }

  async function handleCreate(company: AdminCompanyFlow) {
    if (!rootPassword) { setError("Informe sua senha root para gerar o link."); return; }
    setBusy(`create:${company.companyId}`); setError(""); setMessage("");
    try {
      const checkout = await createAdminSubscriptionCheckout(token, company.companyId, rootPassword);
      setCheckouts((current) => ({ ...current, [company.companyId]: checkout }));
      setRootPassword(""); setMessage(`Link mensal criado para ${company.restaurantName}.`);
    } catch (value) { setError(describeError(value)); }
    finally { setBusy(""); }
  }

  async function handleSync(company: AdminCompanyFlow) {
    setBusy(`sync:${company.companyId}`); setError(""); setMessage("");
    try {
      const checkout = await syncAdminSubscription(token, company.companyId);
      setCheckouts((current) => ({ ...current, [company.companyId]: checkout }));
      setMessage(`Status de ${company.restaurantName}: ${checkout.mercadoPagoStatus ?? "desconhecido"}.`);
    } catch (value) { setError(describeError(value)); }
    finally { setBusy(""); }
  }

  async function handleMarkPaid(company: AdminCompanyFlow) {
    if (!rootPassword) { setError("Informe sua senha root para marcar o pagamento."); return; }
    if (!window.confirm(`Confirmar uma mensalidade paga para ${company.restaurantName}?`)) return;
    setBusy(`paid:${company.companyId}`); setError(""); setMessage("");
    try {
      const result = await markAdminSubscriptionPaid(token, company.companyId, rootPassword);
      setRootPassword("");
      setMessage(`Pagamento registrado. Acesso valido ate ${result.paidThroughUtc ? new Date(result.paidThroughUtc).toLocaleDateString("pt-BR") : "a proxima renovacao"}.`);
    } catch (value) { setError(describeError(value)); }
    finally { setBusy(""); }
  }

  return (
    <div className="admin-billing-stack">
      <section className="surface-card module-list-card">
        <div className="module-section-head">
          <div><span className="eyebrow">Conta recebedora</span><strong>Mercado Pago da plataforma</strong></div>
          <span className={`status-chip ${status?.configured ? (status.liveMode ? "available" : "warning") : "inactive"}`}>
            {status?.configured ? (status.liveMode ? "Producao" : "Teste") : "Nao configurada"}
          </span>
        </div>
        {status?.configured ? (
          <div className="admin-modal-chips">
            <span className="admin-modal-chip is-muted">Conta {status.accountUserId}</span>
            <span className="admin-modal-chip is-muted">{status.accountEmail || "E-mail nao informado"}</span>
          </div>
        ) : null}
        <p className="admin-section-copy">Use o Access Token de producao da sua aplicacao Mercado Pago. O token nunca volta pela API nem aparece novamente no painel.</p>
        <form className="admin-billing-form" onSubmit={handleConfigure}>
          <div className="field-group">
            <label htmlFor="platformAccessToken">Access Token Mercado Pago</label>
            <input id="platformAccessToken" type={showSecrets ? "text" : "password"} value={accessToken} onChange={(event) => setAccessToken(event.target.value)} required autoComplete="off" />
          </div>
          <div className="field-group">
            <label htmlFor="platformRootPassword">Senha root para confirmar a acao</label>
            <input id="platformRootPassword" type={showSecrets ? "text" : "password"} value={rootPassword} onChange={(event) => setRootPassword(event.target.value)} required autoComplete="current-password" />
          </div>
          <div className="toolbar-actions compact">
            <button className="ghost-link button-link" type="button" onClick={() => setShowSecrets((value) => !value)}>{showSecrets ? "Ocultar" : "Ver"}</button>
            <button className="primary-link button-link" type="submit" disabled={busy === "configure"}>{busy === "configure" ? "Validando..." : "Validar e salvar"}</button>
            {status?.configured ? <button className="ghost-link button-link admin-danger-button" type="button" disabled={busy === "disconnect"} onClick={() => void handleDisconnect()}>Desconectar</button> : null}
          </div>
        </form>
        {message ? <p className="module-feedback success" role="status">{message}</p> : null}
        {error ? <p className="module-feedback error" role="alert">{error}</p> : null}
      </section>

      <section className="surface-card module-list-card">
        <div className="module-section-head"><div><span className="eyebrow">Assinaturas mensais</span><strong>Links por empresa</strong></div></div>
        <p className="admin-section-copy">O cliente autoriza a cobranca no checkout oficial do Mercado Pago. Use Sincronizar para consultar o estado diretamente na API.</p>
        <div className="module-card-list">
          {companies.map((company) => {
            const checkout = checkouts[company.companyId] ?? (company.platformBillingCheckoutUrl ? {
              companyId: company.companyId,
              subscriptionId: "",
              planName: company.planName,
              monthlyPrice: company.monthlyPrice,
              mercadoPagoStatus: company.platformBillingStatus,
              checkoutUrl: company.platformBillingCheckoutUrl,
              statusUpdatedAtUtc: company.platformBillingStatusUpdatedAtUtc,
            } : undefined);
            return (
              <article className="module-entity-card" key={company.companyId}>
                <div className="entity-head"><div><h3>{company.restaurantName}</h3><p>{company.ownerEmail}</p></div><span className="status-chip warning">R$ {company.monthlyPrice}/mes</span></div>
                <p className="admin-section-copy">Status: {checkout?.mercadoPagoStatus ?? "sem assinatura"} · Pago ate: {company.paidThroughUtc ? new Date(company.paidThroughUtc).toLocaleDateString("pt-BR") : "nao pago"}</p>
                <div className="toolbar-actions compact">
                  <button className="primary-link button-link" type="button" disabled={!status?.configured || Boolean(busy)} onClick={() => void handleCreate(company)}>{busy === `create:${company.companyId}` ? "Criando..." : "Gerar link"}</button>
                  <button className="ghost-link button-link" type="button" disabled={!status?.configured || Boolean(busy)} onClick={() => void handleSync(company)}>{busy === `sync:${company.companyId}` ? "Consultando..." : "Sincronizar"}</button>
                  <button className="ghost-link button-link" type="button" disabled={Boolean(busy)} onClick={() => void handleMarkPaid(company)}>{busy === `paid:${company.companyId}` ? "Confirmando..." : "Marcar pago"}</button>
                  {checkout?.checkoutUrl ? <a className="ghost-link inline-link" href={checkout.checkoutUrl} target="_blank" rel="noopener noreferrer">Abrir checkout</a> : null}
                </div>
              </article>
            );
          })}
        </div>
      </section>
    </div>
  );
}
