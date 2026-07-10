"use client";

import Link from "next/link";
import { useEffect, useMemo, useState } from "react";
import {
  deleteAllPaidOrders,
  deleteTodayOrderFlow,
  downloadDailyCashReportPdf,
  getOrders,
  type CustomerOrder,
} from "@/lib/api";
import {
  formatCurrency,
  formatPaymentMethod,
  handleApiError,
  type AsyncVoid,
} from "@/components/modules/module-utils";

type PaymentSlice = {
  label: string;
  total: number;
  count: number;
  percent: number;
};

type RankedItem = {
  key: string;
  label: string;
  detail: string;
  quantity: number;
  total: number;
  percent: number;
};

type TimelinePoint = {
  label: string;
  count: number;
  total: number;
  height: number;
};

const paymentOrder = ["Pix", "Credit", "Debit", "Cash", "Undefined"];

function sumOrders(orders: CustomerOrder[]) {
  return orders.reduce((total, order) => total + order.totalAmount, 0);
}

function getLocalDateKey(value: string) {
  const parts = new Intl.DateTimeFormat("en-CA", {
    timeZone: "America/Sao_Paulo",
    year: "numeric",
    month: "2-digit",
    day: "2-digit",
  }).formatToParts(new Date(value));

  const year = parts.find((part) => part.type === "year")?.value ?? "0000";
  const month = parts.find((part) => part.type === "month")?.value ?? "00";
  const day = parts.find((part) => part.type === "day")?.value ?? "00";

  return `${year}-${month}-${day}`;
}

function todayLocalKey() {
  return getLocalDateKey(new Date().toISOString());
}

function yesterdayLocalKey() {
  const date = new Date();
  date.setDate(date.getDate() - 1);
  return getLocalDateKey(date.toISOString());
}

function formatShortDate(dateKey: string) {
  const [year, month, day] = dateKey.split("-").map(Number);
  return new Intl.DateTimeFormat("pt-BR", { day: "2-digit", month: "short" }).format(
    new Date(year, month - 1, day),
  );
}

function buildPaymentSlices(orders: CustomerOrder[]): PaymentSlice[] {
  const paidOrders = orders.filter((order) => order.paymentStatus === "Paid" && order.status !== "Cancelled");
  const total = Math.max(sumOrders(paidOrders), 1);

  return paymentOrder
    .map((method) => {
      const methodOrders = paidOrders.filter((order) => order.paymentMethod === method);
      const methodTotal = sumOrders(methodOrders);

      return {
        label: formatPaymentMethod(method),
        total: methodTotal,
        count: methodOrders.length,
        percent: Math.round((methodTotal / total) * 100),
      };
    })
    .filter((slice) => slice.count > 0 || slice.label !== "Escolher no caixa");
}

function getPeakHour(orders: CustomerOrder[]) {
  const hourCounts = orders.reduce<Record<string, number>>((result, order) => {
    const hour = new Intl.DateTimeFormat("pt-BR", {
      hour: "2-digit",
      hour12: false,
      timeZone: "America/Sao_Paulo",
    }).format(new Date(order.submittedAtUtc));
    result[hour] = (result[hour] ?? 0) + 1;
    return result;
  }, {});

  const [hour, count] = Object.entries(hourCounts).sort((left, right) => right[1] - left[1])[0] ?? ["--", 0];

  return { hour, count };
}

function buildTimeline(orders: CustomerOrder[]): TimelinePoint[] {
  const buckets = ["08", "10", "12", "14", "16", "18", "20", "22"];
  const bucketData = buckets.map((bucket) => {
    const bucketNumber = Number(bucket);
    const bucketOrders = orders.filter((order) => {
      const hour = Number(
        new Intl.DateTimeFormat("pt-BR", {
          hour: "2-digit",
          hour12: false,
          timeZone: "America/Sao_Paulo",
        }).format(new Date(order.submittedAtUtc)),
      );
      return hour >= bucketNumber && hour < bucketNumber + 2;
    });

    return {
      label: `${bucket}h`,
      count: bucketOrders.length,
      total: sumOrders(bucketOrders.filter((order) => order.paymentStatus === "Paid")),
    };
  });
  const max = Math.max(...bucketData.map((item) => item.count), 1);

  return bucketData.map((item) => ({
    ...item,
    height: Math.max(12, Math.round((item.count / max) * 100)),
  }));
}

function buildTopProducts(orders: CustomerOrder[]): RankedItem[] {
  const paidOrders = orders.filter((order) => order.paymentStatus === "Paid" && order.status !== "Cancelled");
  const totals = new Map<string, RankedItem>();
  let totalQuantity = 0;

  paidOrders.forEach((order) => {
    order.items.forEach((item) => {
      const key = item.menuItemId ?? item.name.toLowerCase();
      const current = totals.get(key) ?? {
        key,
        label: item.name,
        detail: item.categoryName || "Sem categoria",
        quantity: 0,
        total: 0,
        percent: 0,
      };
      current.quantity += item.quantity;
      current.total += item.totalPrice;
      totalQuantity += item.quantity;
      totals.set(key, current);
    });
  });

  return [...totals.values()]
    .sort((left, right) => right.total - left.total)
    .slice(0, 5)
    .map((item) => ({
      ...item,
      percent: totalQuantity ? Math.round((item.quantity / totalQuantity) * 100) : 0,
    }));
}

function buildCategoryRanking(orders: CustomerOrder[]): RankedItem[] {
  const paidOrders = orders.filter((order) => order.paymentStatus === "Paid" && order.status !== "Cancelled");
  const totals = new Map<string, RankedItem>();
  const totalRevenue = Math.max(sumOrders(paidOrders), 1);

  paidOrders.forEach((order) => {
    order.items.forEach((item) => {
      const key = item.categoryName || "Sem categoria";
      const current = totals.get(key) ?? {
        key,
        label: key,
        detail: "Categoria",
        quantity: 0,
        total: 0,
        percent: 0,
      };
      current.quantity += item.quantity;
      current.total += item.totalPrice;
      totals.set(key, current);
    });
  });

  return [...totals.values()]
    .sort((left, right) => right.total - left.total)
    .slice(0, 4)
    .map((item) => ({
      ...item,
      percent: Math.round((item.total / totalRevenue) * 100),
    }));
}

function normalizeCustomerKey(order: CustomerOrder) {
  const phone = order.deliveryPhone?.replace(/\D/g, "");
  if (phone && phone.length >= 8) return `phone:${phone}`;

  const name = order.customerName?.trim().toLowerCase();
  if (name && name.length >= 3) return `name:${name}`;

  return null;
}

function buildCustomerStats(orders: CustomerOrder[]) {
  const paidOrders = orders.filter((order) => order.paymentStatus === "Paid" && order.status !== "Cancelled");
  const counts = new Map<string, number>();

  paidOrders.forEach((order) => {
    const key = normalizeCustomerKey(order);
    if (!key) return;
    counts.set(key, (counts.get(key) ?? 0) + 1);
  });

  const identifiedOrders = [...counts.values()].reduce((total, count) => total + count, 0);
  const recurringOrders = [...counts.values()].filter((count) => count > 1).reduce((total, count) => total + count, 0);
  const recurringCustomers = [...counts.values()].filter((count) => count > 1).length;
  const newCustomers = [...counts.values()].filter((count) => count === 1).length;

  return {
    identifiedOrders,
    recurringOrders,
    recurringCustomers,
    newCustomers,
    recurringPercent: identifiedOrders ? Math.round((recurringOrders / identifiedOrders) * 100) : 0,
  };
}

export function ReportsModule({
  token,
  onUnauthorized,
  variant = "combined",
}: {
  token: string;
  onUnauthorized: AsyncVoid;
  variant?: "combined" | "flow" | "analysis";
}) {
  const [orders, setOrders] = useState<CustomerOrder[]>([]);
  const [loading, setLoading] = useState(true);
  const [exportingDailyReport, setExportingDailyReport] = useState(false);
  const [removingPaidBatch, setRemovingPaidBatch] = useState(false);
  const [removingTodayFlow, setRemovingTodayFlow] = useState(false);
  const [errorMessage, setErrorMessage] = useState("");
  const [successMessage, setSuccessMessage] = useState("");

  async function loadOrders() {
    setLoading(true);

    try {
      const response = await getOrders(token);
      setOrders(response);
      setErrorMessage("");
    } catch (error) {
      await handleApiError(error, onUnauthorized, setErrorMessage, "Nao foi possivel carregar a gestao.");
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    void loadOrders();
  }, [token]);

  async function handleDownloadDailyReport() {
    try {
      setExportingDailyReport(true);
      setSuccessMessage("");
      const file = await downloadDailyCashReportPdf(token);
      const downloadUrl = URL.createObjectURL(file.blob);
      const anchor = document.createElement("a");
      anchor.href = downloadUrl;
      anchor.download = file.fileName;
      document.body.appendChild(anchor);
      anchor.click();
      anchor.remove();
      URL.revokeObjectURL(downloadUrl);
      setErrorMessage("");
      setSuccessMessage("Relatorio diario exportado em PDF.");
    } catch (error) {
      await handleApiError(error, onUnauthorized, setErrorMessage, "Nao foi possivel exportar o relatorio do dia.");
    } finally {
      setExportingDailyReport(false);
    }
  }

  async function handleDeleteAllPaidOrders() {
    const password = window.prompt("Digite a senha do owner para apagar todos os pedidos pagos.");

    if (!password || !window.confirm("Apagar todos os pedidos ja pagos do caixa?")) {
      return;
    }

    try {
      setRemovingPaidBatch(true);
      setSuccessMessage("");
      await deleteAllPaidOrders(token, password);
      await loadOrders();
      setSuccessMessage("Pedidos pagos apagados.");
    } catch (error) {
      await handleApiError(error, onUnauthorized, setErrorMessage, "Nao foi possivel apagar os pedidos pagos.");
    } finally {
      setRemovingPaidBatch(false);
    }
  }

  async function handleDeleteTodayFlow() {
    const password = window.prompt("Digite a senha do owner para apagar todos os pedidos ativos da operacao.");

    if (
      !password ||
      !window.confirm("Apagar todos os pedidos ativos em todo o sistema, incluindo cozinha, caixa, impressao e status atuais?")
    ) {
      return;
    }

    try {
      setRemovingTodayFlow(true);
      setSuccessMessage("");
      await deleteTodayOrderFlow(token, password);
      await loadOrders();
      setSuccessMessage("Pedidos ativos da operacao apagados em todo o sistema.");
    } catch (error) {
      await handleApiError(error, onUnauthorized, setErrorMessage, "Nao foi possivel apagar os pedidos atuais da operacao.");
    } finally {
      setRemovingTodayFlow(false);
    }
  }

  const activeOrders = orders.filter((order) => order.status !== "Cancelled");
  const cancelledOrders = orders.filter((order) => order.status === "Cancelled");
  const paidOrders = activeOrders.filter((order) => order.paymentStatus === "Paid");
  const pendingOrders = activeOrders.filter((order) => order.paymentStatus !== "Paid");
  const deliveryOrders = activeOrders.filter((order) => order.isDeliveryOrder);
  const totalRevenue = sumOrders(paidOrders);
  const pendingRevenue = sumOrders(pendingOrders);
  const totalDiscount = activeOrders.reduce((total, order) => total + order.discountAmount, 0);
  const couponDiscount = activeOrders.reduce((total, order) => total + (order.couponDiscountAmount ?? 0), 0);
  const averageTicket = paidOrders.length ? totalRevenue / paidOrders.length : 0;
  const todayKey = todayLocalKey();
  const yesterdayKey = yesterdayLocalKey();
  const todayPaid = paidOrders.filter((order) => getLocalDateKey(order.submittedAtUtc) === todayKey);
  const yesterdayPaid = paidOrders.filter((order) => getLocalDateKey(order.submittedAtUtc) === yesterdayKey);
  const todayRevenue = sumOrders(todayPaid);
  const yesterdayRevenue = sumOrders(yesterdayPaid);
  const revenueDelta = yesterdayRevenue ? Math.round(((todayRevenue - yesterdayRevenue) / yesterdayRevenue) * 100) : null;
  const paymentSlices = useMemo(() => buildPaymentSlices(activeOrders), [activeOrders]);
  const timeline = useMemo(() => buildTimeline(activeOrders), [activeOrders]);
  const topProducts = useMemo(() => buildTopProducts(activeOrders), [activeOrders]);
  const categoryRanking = useMemo(() => buildCategoryRanking(activeOrders), [activeOrders]);
  const customerStats = useMemo(() => buildCustomerStats(activeOrders), [activeOrders]);
  const peak = useMemo(() => getPeakHour(activeOrders), [activeOrders]);
  const readyOrders = activeOrders.filter((order) => order.status === "Ready").length;
  const inKitchenOrders = activeOrders.filter((order) => order.status === "Pending" || order.status === "InKitchen").length;
  const paidPercent = activeOrders.length ? Math.round((paidOrders.length / activeOrders.length) * 100) : 0;
  const deliveryPercent = activeOrders.length ? Math.round((deliveryOrders.length / activeOrders.length) * 100) : 0;
  const couponOrders = activeOrders.filter((order) => order.couponCode).length;
  const showFlow = variant !== "analysis";
  const showAnalysis = variant !== "flow";

  return (
    <section className="reports-dashboard-workspace reports-management">
      {successMessage ? <p className="module-feedback success">{successMessage}</p> : null}
      {errorMessage ? <p className="module-feedback error">{errorMessage}</p> : null}

      <section className="surface-card reports-hero-card reports-management-hero">
        <div className="reports-hero-copy">
          <span className="eyebrow">{variant === "analysis" ? "Analise de vendas" : "Relatorios"}</span>
          <h2>{variant === "analysis" ? "Veja o que vende e onde ajustar." : "Controle o fluxo do dia sem procurar menu."}</h2>
          <p>
            {variant === "analysis"
              ? "Ticket medio, produtos, horarios, clientes e pagamentos aparecem em blocos curtos para leitura rapida."
              : "Fechamento, relatorio detalhado, exportacao e limpeza ficam juntos para operar com responsabilidade."}
          </p>
        </div>
        <div className="reports-action-panel reports-management-actions">
          <Link className="primary-link button-link" href="/app/caixa/fechamento">
            Fechar caixa
          </Link>
          {variant === "analysis" ? (
            <Link className="ghost-link button-link" href="/app/cupons">
              Gerenciar cupons
            </Link>
          ) : null}
          <Link className="ghost-link button-link" href="/app/caixa/relatorios/diario">
            Relatorio diario
          </Link>
        </div>
      </section>

      {loading ? (
        <section className="surface-card reports-loading-card">
          <p className="loading-state">Carregando gestao...</p>
        </section>
      ) : (
        <>
          {showFlow ? (
          <section className="reports-section-block">
            <div className="reports-block-head">
              <div>
                <span className="eyebrow">Relatorios e fluxo do dia</span>
                <h3>Fechar, exportar e limpar sem procurar menu.</h3>
              </div>
              <Link className="button-link primary-link" href="/app/caixa/fechamento">
                Fechar caixa
              </Link>
            </div>

            <div className="reports-command-grid reports-flow-grid">
              <article className="surface-card reports-command-card">
                <span className="eyebrow">Caixa</span>
                <strong>{pendingOrders.length ? `${pendingOrders.length} a cobrar` : "Caixa em dia"}</strong>
                <p>{pendingOrders.length ? `${formatCurrency(pendingRevenue)} ainda precisa de baixa.` : "Nenhum pedido aberto no caixa."}</p>
                <Link className="button-link ghost-link" href="/app/caixa/a-pagar">
                  Ver caixa
                </Link>
              </article>
              <article className="surface-card reports-command-card">
                <span className="eyebrow">Cozinha</span>
                <strong>{inKitchenOrders ? `${inKitchenOrders} em preparo` : "Sem fila acumulada"}</strong>
                <p>{readyOrders ? `${readyOrders} pedido(s) prontos aguardam finalizacao.` : "Cozinha sem pedido pronto parado."}</p>
                <Link className="button-link ghost-link" href="/app/pedidos">
                  Ver cozinha
                </Link>
              </article>
              <article className="surface-card reports-command-card">
                <span className="eyebrow">Relatorio</span>
                <strong>PDF e historico</strong>
                <p>Exporte o dia ou abra o relatorio detalhado quando precisar conferir os numeros.</p>
                <div className="reports-card-actions">
                  <button className="ghost-link button-link" type="button" disabled={exportingDailyReport} onClick={() => void handleDownloadDailyReport()}>
                    {exportingDailyReport ? "Exportando..." : "Exportar PDF"}
                  </button>
                  <Link className="button-link ghost-link" href="/app/caixa/relatorios/diario">
                    Detalhado
                  </Link>
                </div>
              </article>
              <article className="surface-card reports-command-card reports-danger-card">
                <span className="eyebrow">Fluxo</span>
                <strong>Limpeza do dia</strong>
                <p>Remove da operacao e preserva os dados para analise.</p>
                <div className="reports-card-actions">
                  <button className="ghost-link button-link" type="button" disabled={removingPaidBatch || paidOrders.length === 0} onClick={() => void handleDeleteAllPaidOrders()}>
                    {removingPaidBatch ? "Limpando..." : "Limpar pagos"}
                  </button>
                  <button className="ghost-link button-link destructive-link" type="button" disabled={removingTodayFlow} onClick={() => void handleDeleteTodayFlow()}>
                    {removingTodayFlow ? "Apagando..." : "Apagar fluxo"}
                  </button>
                </div>
              </article>
            </div>
          </section>
          ) : null}

          {showAnalysis ? (
          <section className="reports-section-block">
            <div className="reports-block-head">
              <div>
                <span className="eyebrow">Analise de vendas</span>
                <h3>Indicadores simples para decidir o proximo ajuste.</h3>
              </div>
              <Link className="button-link ghost-link" href="/app/cupons">
                Cupons
              </Link>
            </div>

            <section className="reports-kpi-grid reports-management-kpis reports-analysis-kpis">
              <article className="surface-card reports-kpi-card is-revenue">
                <span>Recebido</span>
                <strong>{formatCurrency(totalRevenue)}</strong>
                <i style={{ width: `${paidPercent}%` }} />
              </article>
              <article className="surface-card reports-kpi-card">
                <span>Hoje</span>
                <strong>{formatCurrency(todayRevenue)}</strong>
                <em>{revenueDelta === null ? "sem base" : `${revenueDelta >= 0 ? "+" : ""}${revenueDelta}%`}</em>
              </article>
              <article className="surface-card reports-kpi-card">
                <span>Ticket medio</span>
                <strong>{formatCurrency(averageTicket)}</strong>
                <em>{paidOrders.length} pagos</em>
              </article>
              <article className="surface-card reports-kpi-card">
                <span>Descontos</span>
                <strong>{formatCurrency(totalDiscount)}</strong>
                <em>{couponOrders} cupons</em>
              </article>
            </section>

            <section className="reports-analysis-grid">
              <article className="surface-card reports-chart-card reports-compact-card">
                <div className="reports-section-head">
                  <div>
                    <span className="eyebrow">Produtos</span>
                    <h3>Mais vendidos</h3>
                  </div>
                  <strong>{topProducts.length}</strong>
                </div>
                <div className="reports-ranking-list">
                  {topProducts.length ? (
                    topProducts.slice(0, 4).map((item, index) => (
                      <div key={item.key} className="reports-ranking-row reports-visual-row">
                        <span className="reports-rank-number">{index + 1}</span>
                        <div className="reports-ranking-copy">
                          <strong>{item.label}</strong>
                          <div className="reports-bar-track">
                            <span style={{ width: `${item.percent}%` }} />
                          </div>
                        </div>
                        <em>{formatCurrency(item.total)}</em>
                      </div>
                    ))
                  ) : (
                    <p className="reports-empty-note">Sem vendas pagas.</p>
                  )}
                </div>
              </article>

              <article className="surface-card reports-chart-card reports-compact-card">
                <div className="reports-section-head">
                  <div>
                    <span className="eyebrow">Pico</span>
                    <h3>Horarios fortes</h3>
                  </div>
                  <strong>{peak.count ? `${peak.hour}h` : "--"}</strong>
                </div>
                <div className="reports-mini-timeline">
                  {timeline.map((item) => (
                    <div key={item.label} className="reports-mini-timeline-item">
                      <span style={{ height: `${item.height}%` }} />
                      <small>{item.label}</small>
                    </div>
                  ))}
                </div>
              </article>

              <article className="surface-card reports-chart-card reports-compact-card">
                <div className="reports-section-head">
                  <div>
                    <span className="eyebrow">Clientes</span>
                    <h3>Recorrencia</h3>
                  </div>
                  <strong>{customerStats.recurringPercent}%</strong>
                </div>
                <div className="reports-mini-metrics">
                  <span>Novos <strong>{customerStats.newCustomers}</strong><i style={{ width: `${Math.max(8, 100 - customerStats.recurringPercent)}%` }} /></span>
                  <span>Recorrentes <strong>{customerStats.recurringCustomers}</strong><i style={{ width: `${Math.max(8, customerStats.recurringPercent)}%` }} /></span>
                  <span>Identificados <strong>{customerStats.identifiedOrders}</strong><i style={{ width: "100%" }} /></span>
                </div>
              </article>

              <article className="surface-card reports-chart-card reports-compact-card">
                <div className="reports-section-head">
                  <div>
                    <span className="eyebrow">Pagamentos</span>
                    <h3>Entrada</h3>
                  </div>
                  <strong>{paidPercent}%</strong>
                </div>
                <div className="reports-payment-bars reports-payment-compact">
                  {paymentSlices.length ? (
                    paymentSlices.slice(0, 4).map((slice) => (
                      <div key={slice.label} className="reports-payment-row reports-visual-row">
                        <div>
                          <strong>{slice.label}</strong>
                          <div className="reports-bar-track">
                            <span style={{ width: `${slice.percent}%` }} />
                          </div>
                        </div>
                        <em>{formatCurrency(slice.total)}</em>
                      </div>
                    ))
                  ) : (
                    <p className="reports-empty-note">Nenhum pagamento baixado.</p>
                  )}
                </div>
              </article>
            </section>

            <section className="reports-analysis-grid reports-analysis-grid-secondary">
              <article className="surface-card reports-chart-card reports-compact-card">
                <div className="reports-section-head">
                  <div>
                    <span className="eyebrow">Categorias</span>
                    <h3>Receita</h3>
                  </div>
                </div>
                <div className="reports-category-list">
                  {categoryRanking.length ? (
                    categoryRanking.slice(0, 4).map((item) => (
                      <div key={item.key} className="reports-category-row reports-visual-row">
                        <div>
                          <strong>{item.label}</strong>
                          <div className="reports-bar-track">
                            <span style={{ width: `${item.percent}%` }} />
                          </div>
                        </div>
                        <em>{formatCurrency(item.total)}</em>
                      </div>
                    ))
                  ) : (
                    <p className="reports-empty-note">Sem categorias pagas ainda.</p>
                  )}
                </div>
              </article>
              <article className="surface-card reports-chart-card reports-compact-card">
                <div className="reports-section-head">
                  <div>
                    <span className="eyebrow">Delivery</span>
                    <h3>Canal</h3>
                  </div>
                  <strong>{deliveryPercent}%</strong>
                </div>
                <div className="reports-mini-metrics">
                  <span>Delivery <strong>{deliveryOrders.length}</strong><i style={{ width: `${Math.max(8, deliveryPercent)}%` }} /></span>
                  <span>Cupons <strong>{formatCurrency(couponDiscount)}</strong><i style={{ width: `${couponOrders ? 70 : 8}%` }} /></span>
                  <span>Cancelados <strong>{cancelledOrders.length}</strong><i style={{ width: `${cancelledOrders.length ? 35 : 8}%` }} /></span>
                </div>
              </article>
            </section>
          </section>
          ) : null}
        </>
      )}
    </section>
  );
}
