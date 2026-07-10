"use client";

import { useEffect, useState } from "react";
import { getCashClosing, type CashClosingReport } from "@/lib/api";
import { formatCurrency, handleApiError, type AsyncVoid } from "@/components/modules/module-utils";

function todayLocalIso() {
  const now = new Date();
  const year = now.getFullYear();
  const month = String(now.getMonth() + 1).padStart(2, "0");
  const day = String(now.getDate()).padStart(2, "0");
  return `${year}-${month}-${day}`;
}

function formatMethod(method: string) {
  const map: Record<string, string> = {
    Pix: "Pix",
    Cash: "Dinheiro",
    Dinheiro: "Dinheiro",
    Money: "Dinheiro",
    Credit: "Credito",
    CreditCard: "Credito",
    Credito: "Credito",
    Debit: "Debito",
    DebitCard: "Debito",
    Debito: "Debito",
    Other: "Outros",
    Outros: "Outros",
    Undefined: "Nao definido",
  };

  return map[method] ?? method;
}

export function CashClosingModule({ token, onUnauthorized }: { token: string; onUnauthorized: AsyncVoid }) {
  const [date, setDate] = useState(todayLocalIso());
  const [report, setReport] = useState<CashClosingReport | null>(null);
  const [loading, setLoading] = useState(true);
  const [errorMessage, setErrorMessage] = useState("");

  useEffect(() => {
    let isMounted = true;

    async function load() {
      setLoading(true);
      try {
        const response = await getCashClosing(token, date);
        if (isMounted) {
          setReport(response);
          setErrorMessage("");
        }
      } catch (error) {
        if (isMounted) {
          await handleApiError(error, onUnauthorized, setErrorMessage, "Nao foi possivel carregar o fechamento.");
          setReport(null);
        }
      } finally {
        if (isMounted) {
          setLoading(false);
        }
      }
    }

    void load();

    return () => {
      isMounted = false;
    };
  }, [token, date, onUnauthorized]);

  const isToday = date === todayLocalIso();

  const summary = report
    ? [
        { label: "Total vendido", value: formatCurrency(report.totalSold), tone: "good", lead: true },
        { label: "Pedidos", value: String(report.ordersCount), tone: "neutral" },
        { label: "Ticket medio", value: formatCurrency(report.averageTicket), tone: "neutral" },
        { label: "Descontos / cupons", value: formatCurrency(report.discountsTotal), tone: "warning" },
        { label: "Cancelados", value: String(report.cancelledOrdersCount), tone: report.cancelledOrdersCount > 0 ? "danger" : "neutral" },
      ]
    : [];

  return (
    <section className="module-body-grid single">
      <section className="surface-card zpclosing-shell">
        <div className="zpclosing-head">
          <div className="zpclosing-head-copy">
            <span className="eyebrow">Fechamento de caixa</span>
            <h2>Resumo do dia</h2>
          </div>
          <div className="zpclosing-date">
            <input type="date" value={date} max={todayLocalIso()} onChange={(event) => setDate(event.target.value)} />
            {!isToday ? (
              <button className="zpclosing-btn is-ghost" type="button" onClick={() => setDate(todayLocalIso())}>
                Hoje
              </button>
            ) : null}
          </div>
        </div>

        {errorMessage ? <p className="module-feedback error">{errorMessage}</p> : null}

        {loading ? (
          <p className="loading-state">Carregando fechamento...</p>
        ) : !report ? (
          <div className="zpclosing-empty">
            <strong>Sem dados para esta data</strong>
            <p>Selecione outro dia para ver o fechamento.</p>
          </div>
        ) : (
          <>
            <div className="zpclosing-summary">
              {summary.map((item) => (
                <article key={item.label} className={`zpclosing-card is-${item.tone}${item.lead ? " is-lead" : ""}`}>
                  <span className="zpclosing-card-label">{item.label}</span>
                  <strong className="zpclosing-card-value">{item.value}</strong>
                </article>
              ))}
            </div>

            <div className="zpclosing-methods">
              <div className="zpclosing-methods-head">
                <span className="eyebrow">Formas de pagamento</span>
              </div>
              {report.paymentMethods.length === 0 ? (
                <div className="zpclosing-empty zpclosing-empty-inline">
                  <p>Nenhum pagamento registrado neste dia.</p>
                </div>
              ) : (
                <div className="zpclosing-method-list">
                  {report.paymentMethods.map((method) => (
                    <div key={method.method} className="zpclosing-method-row">
                      <div className="zpclosing-method-id">
                        <strong>{formatMethod(method.method)}</strong>
                        <span>{method.ordersCount} pedido(s)</span>
                      </div>
                      <strong className="zpclosing-method-amount">{formatCurrency(method.amount)}</strong>
                    </div>
                  ))}
                </div>
              )}
            </div>
          </>
        )}
      </section>
    </section>
  );
}
