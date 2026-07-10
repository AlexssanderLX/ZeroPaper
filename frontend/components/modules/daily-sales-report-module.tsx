"use client";

import { useCallback, useEffect, useState } from "react";
import {
  ApiError,
  USE_MOCK_DAILY_SALES_REPORT,
  getDailySalesReport,
  todayReportDateParam,
  yesterdayReportDateParam,
  type DailySalesReport,
} from "@/lib/daily-sales-report";
import {
  formatCurrency,
  formatDateTime,
  type AsyncVoid,
} from "@/components/modules/module-utils";
import styles from "@/components/modules/daily-sales-report-module.module.css";

type ViewState =
  | "loading"
  | "ready"
  | "empty"
  | "error"
  | "forbidden"
  | "unavailable";

function formatReferenceDate(value: string) {
  const match = /^(\d{4})-(\d{2})-(\d{2})$/.exec(value);
  if (!match) return value;
  const [, year, month, day] = match;
  return new Intl.DateTimeFormat("pt-BR", {
    day: "2-digit",
    month: "long",
    year: "numeric",
  }).format(new Date(Number(year), Number(month) - 1, Number(day)));
}

export function DailySalesReportModule({
  token,
  onUnauthorized,
}: {
  token: string;
  onUnauthorized: AsyncVoid;
}) {
  const [selectedDate, setSelectedDate] = useState(() => todayReportDateParam());
  const [report, setReport] = useState<DailySalesReport | null>(null);
  const [state, setState] = useState<ViewState>("loading");
  const [errorMessage, setErrorMessage] = useState("");

  const loadReport = useCallback(
    async (date: string) => {
      setState("loading");
      setErrorMessage("");
      try {
        const data = await getDailySalesReport(token, date);
        setReport(data);
        setState(data.hasDetailedData ? "ready" : "empty");
      } catch (error) {
        if (error instanceof ApiError) {
          if (error.status === 401) { await onUnauthorized(); return; }
          if (error.status === 403) { setState("forbidden"); return; }
          if (error.status === 404 || error.status >= 500) { setState("unavailable"); return; }
          setErrorMessage(error.message);
          setState("error");
          return;
        }
        setState("unavailable");
      }
    },
    [token, onUnauthorized],
  );

  useEffect(() => { void loadReport(selectedDate); }, [loadReport, selectedDate]);

  const maxDate = todayReportDateParam();
  const todayDate = todayReportDateParam();
  const yesterdayDate = yesterdayReportDateParam();

  return (
    <section className={styles.workspace}>

      {/* Date toolbar */}
      <div className={`surface-card ${styles.dateBar}`}>
        <div className={styles.dateBarLeft}>
          <button
            type="button"
            className={`${styles.presetBtn} ${selectedDate === todayDate ? styles.presetActive : ""}`}
            onClick={() => setSelectedDate(todayDate)}
          >
            Hoje
          </button>
          <button
            type="button"
            className={`${styles.presetBtn} ${selectedDate === yesterdayDate ? styles.presetActive : ""}`}
            onClick={() => setSelectedDate(yesterdayDate)}
          >
            Ontem
          </button>
          <div className={styles.dateDivider} />
          <div className={styles.dateField}>
            <label htmlFor="daily-report-date">Outra data</label>
            <input
              id="daily-report-date"
              type="date"
              value={selectedDate}
              max={maxDate}
              onChange={(e) => setSelectedDate(e.target.value)}
            />
          </div>
        </div>
        <button
          type="button"
          className="ghost-link button-link"
          onClick={() => void loadReport(selectedDate)}
          disabled={state === "loading"}
        >
          {state === "loading" ? "Carregando..." : "Atualizar"}
        </button>
      </div>

      {USE_MOCK_DAILY_SALES_REPORT ? (
        <p className="module-feedback">
          Dados de exemplo (mock local). Defina NEXT_PUBLIC_REPORTS_USE_MOCK=false para usar a API real.
        </p>
      ) : null}

      {state === "loading" ? <LoadingState /> : null}

      {state === "error" ? (
        <section className={`surface-card ${styles.banner} ${styles.bannerError}`}>
          <h3>Nao foi possivel carregar o relatorio</h3>
          <p>{errorMessage || "Tente novamente em instantes."}</p>
          <button type="button" className="primary-link button-link" onClick={() => void loadReport(selectedDate)}>
            Tentar novamente
          </button>
        </section>
      ) : null}

      {state === "forbidden" ? (
        <section className={`surface-card ${styles.banner} ${styles.bannerWarning}`}>
          <h3>Sem permissao para este relatorio</h3>
          <p>Seu acesso atual nao inclui os relatorios de vendas desta unidade.</p>
        </section>
      ) : null}

      {state === "unavailable" ? (
        <section className={`surface-card ${styles.banner} ${styles.bannerWarning}`}>
          <h3>Relatorio temporariamente indisponivel</h3>
          <p>Nao conseguimos falar com o servico de relatorios agora. Tente novamente em alguns minutos.</p>
          <button type="button" className="ghost-link button-link" onClick={() => void loadReport(selectedDate)}>
            Tentar novamente
          </button>
        </section>
      ) : null}

      {state === "empty" && report ? (
        <section className={`surface-card ${styles.banner}`}>
          <h3>Sem dados para {formatReferenceDate(report.referenceDate)}</h3>
          <p>Nao houve vendas registradas nesta data, ou os dados detalhados ja expiraram.</p>
        </section>
      ) : null}

      {state === "ready" && report ? <ReportContent report={report} /> : null}
    </section>
  );
}

function LoadingState() {
  return (
    <section className={`surface-card ${styles.skeletonCard}`}>
      <div className={styles.skeletonBar} style={{ width: "40%", height: "28px" }} />
      <div className={styles.skeletonBar} style={{ width: "65%" }} />
      <div className={styles.skeletonBar} style={{ width: "50%" }} />
    </section>
  );
}

function ReportContent({ report }: { report: DailySalesReport }) {
  return (
    <>
      {/* Main summary card */}
      <section className={`surface-card ${styles.summaryCard}`}>
        <div className={styles.summaryTop}>
          <div className={styles.summaryHero}>
            <span className="eyebrow">Total de vendas</span>
            <strong className={styles.heroValue}>{formatCurrency(report.totalSalesAmount)}</strong>
            <p>{formatReferenceDate(report.referenceDate)}</p>
          </div>
          <div className={styles.kpiRow}>
            <div className={styles.kpiItem}>
              <span>Recebido</span>
              <strong>{formatCurrency(report.paidAmount)}</strong>
            </div>
            <div className={styles.kpiItem}>
              <span>A receber</span>
              <strong>{formatCurrency(report.pendingAmount)}</strong>
            </div>
            <div className={styles.kpiItem}>
              <span>Ticket medio</span>
              <strong>{formatCurrency(report.averageTicket)}</strong>
            </div>
          </div>
        </div>
        <div className={styles.counterRow}>
          <span>{report.ordersSubmittedCount} enviados</span>
          <span className={styles.counterPaid}>{report.paidOrdersCount} pagos</span>
          {report.pendingOrdersCount > 0 && (
            <span>{report.pendingOrdersCount} pendentes</span>
          )}
          {report.cancelledOrdersCount > 0 && (
            <span className={styles.counterCancelled}>{report.cancelledOrdersCount} cancelados</span>
          )}
        </div>
      </section>

      {/* Payments + breakdown */}
      <div className={styles.detailGrid}>
        <section className={`surface-card ${styles.detailCard}`}>
          <span className="eyebrow">Pagamentos</span>
          <h3>Formas de pagamento</h3>
          {report.paymentMethods.length ? (
            <div className={styles.paymentList}>
              {report.paymentMethods.map((method) => (
                <div key={method.method} className={styles.paymentRow}>
                  <div className={styles.paymentLabel}>
                    <strong>{method.label}</strong>
                    <span>{method.count} pedido(s)</span>
                  </div>
                  <div className={styles.paymentTrack}>
                    <span style={{ width: `${Math.min(100, Math.max(0, method.percent))}%` }} />
                  </div>
                  <em className={styles.paymentAmount}>{formatCurrency(method.amount)}</em>
                </div>
              ))}
            </div>
          ) : (
            <p className={styles.metaNote}>Nenhuma forma de pagamento registrada nesta data.</p>
          )}
        </section>

        <section className={`surface-card ${styles.detailCard}`}>
          <span className="eyebrow">Financeiro</span>
          <h3>Composicao</h3>
          <dl className={styles.breakdownList}>
            <div className={styles.breakdownRow}>
              <dt>Total bruto</dt>
              <dd>{formatCurrency(report.totalSalesAmount)}</dd>
            </div>
            <div className={`${styles.breakdownRow} ${styles.rowPositive}`}>
              <dt>Recebido</dt>
              <dd>{formatCurrency(report.paidAmount)}</dd>
            </div>
            {report.cancelledAmount > 0 && (
              <div className={`${styles.breakdownRow} ${styles.rowNegative}`}>
                <dt>Cancelado</dt>
                <dd>{formatCurrency(report.cancelledAmount)}</dd>
              </div>
            )}
            {report.discountAmount > 0 && (
              <div className={`${styles.breakdownRow} ${styles.rowNegative}`}>
                <dt>Descontos</dt>
                <dd>{formatCurrency(report.discountAmount)}</dd>
              </div>
            )}
            {report.surchargeAmount > 0 && (
              <div className={styles.breakdownRow}>
                <dt>Acrescimos</dt>
                <dd>{formatCurrency(report.surchargeAmount)}</dd>
              </div>
            )}
            {report.deliveryFreightAmount > 0 && (
              <div className={styles.breakdownRow}>
                <dt>Frete</dt>
                <dd>{formatCurrency(report.deliveryFreightAmount)}</dd>
              </div>
            )}
          </dl>
        </section>
      </div>

      {report.detailExpiresAtUtc ? (
        <p className={styles.metaNote}>
          Dados detalhados disponiveis ate {formatDateTime(report.detailExpiresAtUtc)}.
        </p>
      ) : null}
    </>
  );
}
