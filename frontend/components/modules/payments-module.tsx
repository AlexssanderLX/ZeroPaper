"use client";

import { useEffect, useState } from "react";
import {
  disconnectMercadoPago,
  getMercadoPagoStatus,
  startMercadoPagoConnection,
  type MercadoPagoStatus,
} from "@/lib/api";
import { formatDateTime, handleApiError, type AsyncVoid } from "@/components/modules/module-utils";

function readCallbackOutcome(): "connected" | "error" | null {
  if (typeof window === "undefined") {
    return null;
  }

  const value = new URLSearchParams(window.location.search).get("mp");
  if (value !== "connected" && value !== "error") {
    return null;
  }

  const url = new URL(window.location.href);
  url.searchParams.delete("mp");
  window.history.replaceState({}, "", `${url.pathname}${url.search}`);
  return value;
}

export function PaymentsModule({ token, onUnauthorized }: { token: string; onUnauthorized: AsyncVoid }) {
  const [status, setStatus] = useState<MercadoPagoStatus | null>(null);
  const [loading, setLoading] = useState(true);
  const [isConnecting, setIsConnecting] = useState(false);
  const [isDisconnecting, setIsDisconnecting] = useState(false);
  const [successMessage, setSuccessMessage] = useState("");
  const [errorMessage, setErrorMessage] = useState("");

  async function loadStatus() {
    setLoading(true);
    try {
      const response = await getMercadoPagoStatus(token);
      setStatus(response);
      setErrorMessage("");
    } catch (error) {
      await handleApiError(error, onUnauthorized, setErrorMessage, "Nao foi possivel carregar o status do Mercado Pago.");
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    const outcome = readCallbackOutcome();
    if (outcome === "connected") {
      setSuccessMessage("Conta Mercado Pago conectada com sucesso.");
    } else if (outcome === "error") {
      setErrorMessage("Nao foi possivel concluir a conexao com o Mercado Pago. Tente novamente.");
    }

    void loadStatus();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [token]);

  async function handleConnect() {
    try {
      setIsConnecting(true);
      setErrorMessage("");
      const response = await startMercadoPagoConnection(token);
      if (!response.authorizationUrl) {
        setErrorMessage("Nao foi possivel iniciar a conexao agora.");
        setIsConnecting(false);
        return;
      }

      window.location.href = response.authorizationUrl;
    } catch (error) {
      await handleApiError(error, onUnauthorized, setErrorMessage, "Nao foi possivel iniciar a conexao com o Mercado Pago.");
      setIsConnecting(false);
    }
  }

  async function handleDisconnect() {
    const confirmed = window.confirm(
      "Tem certeza que deseja desconectar a conta Mercado Pago? Os clientes deixarao de pagar online ate reconectar.",
    );

    if (!confirmed) {
      return;
    }

    try {
      setIsDisconnecting(true);
      await disconnectMercadoPago(token);
      setSuccessMessage("Conta Mercado Pago desconectada.");
      setErrorMessage("");
      await loadStatus();
    } catch (error) {
      await handleApiError(error, onUnauthorized, setErrorMessage, "Nao foi possivel desconectar a conta agora.");
    } finally {
      setIsDisconnecting(false);
    }
  }

  if (loading) {
    return (
      <section className="zppay-card zppay-state">
        <p>Carregando status do pagamento online...</p>
      </section>
    );
  }

  const connected = Boolean(status?.connected);
  const configured = Boolean(status?.configured);

  return (
    <div className="zppay-layout">
      {successMessage ? <p className="module-feedback success">{successMessage}</p> : null}
      {errorMessage ? <p className="module-feedback error">{errorMessage}</p> : null}

      <section className="zppay-card">
        <header className="zppay-card-head">
          <div>
            <span className="zppay-eyebrow">Mercado Pago</span>
            <h3>{connected ? "Conta conectada" : "Receba pagamentos online"}</h3>
          </div>
          <span className={`zppay-status ${connected ? "is-on" : "is-off"}`}>
            {connected ? "Conectada" : "Desconectada"}
          </span>
        </header>

        <p className="zppay-lead">
          Conecte a conta Mercado Pago da unidade para aceitar Pix e cartao direto no pedido. O valor cai na sua propria
          conta e as taxas do Mercado Pago sao cobradas de voce &mdash; o ZeroPaper nao retem nada por transacao.
        </p>

        {!configured ? (
          <div className="zppay-notice">
            A integracao com o Mercado Pago ainda nao foi habilitada na plataforma. Fale com o suporte para liberar.
          </div>
        ) : connected ? (
          <>
            <dl className="zppay-details">
              <div>
                <dt>Conta (ID)</dt>
                <dd>{status?.accountUserId || "-"}</dd>
              </div>
              <div>
                <dt>Ambiente</dt>
                <dd>{status?.liveMode ? "Producao (recebendo de verdade)" : "Teste / sandbox"}</dd>
              </div>
              <div>
                <dt>Conectada em</dt>
                <dd>{status?.connectedAtUtc ? formatDateTime(status.connectedAtUtc) : "-"}</dd>
              </div>
            </dl>

            <div className="zppay-actions">
              <button
                className="zppay-btn zppay-btn-danger"
                type="button"
                onClick={() => void handleDisconnect()}
                disabled={isDisconnecting}
              >
                {isDisconnecting ? "Desconectando..." : "Desconectar conta"}
              </button>
            </div>
          </>
        ) : (
          <div className="zppay-actions">
            <button
              className="zppay-btn zppay-btn-primary"
              type="button"
              onClick={() => void handleConnect()}
              disabled={isConnecting}
            >
              {isConnecting ? "Abrindo Mercado Pago..." : "Conectar conta Mercado Pago"}
            </button>
            <small className="zppay-hint">
              Voce sera levado ao Mercado Pago para autorizar. Use a conta que deve receber os pagamentos da unidade.
            </small>
          </div>
        )}
      </section>
    </div>
  );
}
