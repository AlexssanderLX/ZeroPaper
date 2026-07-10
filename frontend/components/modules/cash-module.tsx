"use client";

import Link from "next/link";
import { useEffect, useMemo, useState } from "react";
import {
  adjustOrderValue,
  deleteAllPaidOrders,
  deleteOrder,
  deletePaidOrder,
  deleteTodayOrderFlow,
  downloadDailyCashReportPdf,
  getOrders,
  markAllOrdersPaid,
  updateOrderPayment,
  type CustomerOrder,
  type MarkOrderPaidInput,
} from "@/lib/api";
import {
  formatCurrency,
  formatDateTime,
  formatPaymentMethod,
  formatPaymentStatus,
  handleApiError,
  type AsyncVoid,
} from "@/components/modules/module-utils";

type CashViewKey = "overview" | "pending" | "paid" | "reports";
type PaymentDraft = Record<string, Record<string, number>>;
type AdjustmentDraft = {
  order: CustomerOrder;
  finalAmount: string;
  note: string;
};

type CashColumn = {
  key: Exclude<CashViewKey, "overview" | "reports">;
  title: string;
  description: string;
  emptyTitle: string;
  emptyCopy: string;
  total: number;
  items: CustomerOrder[];
};

type PaymentSummary = {
  value: string;
  label: string;
  count: number;
  total: number;
};

const paymentOptions = [
  { value: "Pix", label: "Pix" },
  { value: "Credit", label: "Credito" },
  { value: "Debit", label: "Debito" },
  { value: "Cash", label: "Dinheiro" },
];

function buildDeliverySummary(order: CustomerOrder) {
  if (!order.isDeliveryOrder) {
    return [];
  }

  const lines = [];

  if (order.customerName) {
    lines.push(`Cliente: ${order.customerName}`);
  }

  if (order.deliveryPhone) {
    lines.push(`Telefone: ${order.deliveryPhone}`);
  }

  if (order.fulfillmentType === "Pickup") {
    lines.push("Retirada: no local");
    return lines;
  }

  if (order.deliveryAddress || order.deliveryNumber) {
    let address = [order.deliveryAddress, order.deliveryNumber].filter(Boolean).join(", ");

    if (order.deliveryComplement) {
      address = `${address} (${order.deliveryComplement})`;
    }

    lines.push(`Entrega: ${address}`);
  }

  return lines;
}

function sumOrders(orders: CustomerOrder[]) {
  return orders.reduce((total, order) => total + order.totalAmount, 0);
}

function getDisplayItemQuantity(order: CustomerOrder) {
  const quantity = order.totalItemQuantity || order.items.reduce((total, item) => total + Number(item.quantity ?? 0), 0);
  return Number.isInteger(quantity) ? quantity.toString() : quantity.toLocaleString("pt-BR", { maximumFractionDigits: 2 });
}

function sortOrdersByOldestFirst(orders: CustomerOrder[]) {
  return [...orders].sort((leftOrder, rightOrder) => {
    const submittedDifference =
      new Date(leftOrder.submittedAtUtc).getTime() - new Date(rightOrder.submittedAtUtc).getTime();

    if (submittedDifference !== 0) {
      return submittedDifference;
    }

    return leftOrder.number - rightOrder.number;
  });
}

function buildPaymentSummaries(orders: CustomerOrder[]): PaymentSummary[] {
  const paidOrders = orders.filter((order) => order.paymentStatus === "Paid" && order.status !== "Cancelled");

  return paymentOptions.map((option) => {
    const total = paidOrders.reduce(
      (sum, order) => sum + order.payments.filter((payment) => payment.method === option.value).reduce((paymentSum, payment) => paymentSum + payment.amount, 0),
      0,
    );
    const optionOrders = paidOrders.filter((order) =>
      order.payments.length > 0
        ? order.payments.some((payment) => payment.method === option.value)
        : order.paymentMethod === option.value,
    );

    return {
      value: option.value,
      label: option.label,
      count: optionOrders.length,
      total,
    };
  });
}

function buildPaymentSummary(order: CustomerOrder) {
  if (order.payments.length > 0) {
    return order.payments.map((payment) => `${formatPaymentMethod(payment.method)}: ${formatCurrency(payment.amount)}`).join(" + ");
  }

  return formatPaymentMethod(order.paymentMethod);
}

function isDefinedPaymentMethod(method?: string | null) {
  return Boolean(method && method !== "Undefined");
}

function getPreferredPaymentMethod(order: CustomerOrder) {
  return isDefinedPaymentMethod(order.paymentMethod)
    ? order.paymentMethod
    : isDefinedPaymentMethod(order.requestedPaymentMethod)
      ? order.requestedPaymentMethod
      : "";
}

function getPaymentChangeCopy(order: CustomerOrder) {
  if (
    order.requestedPaymentMethod &&
    order.requestedPaymentMethod !== "Undefined" &&
    order.requestedPaymentMethod !== order.paymentMethod
  ) {
    return `Alterado de ${formatPaymentMethod(order.requestedPaymentMethod)} para ${formatPaymentMethod(order.paymentMethod)} no caixa.`;
  }

  return null;
}

function renderOrderTitle(order: CustomerOrder) {
  if (!order.isDeliveryOrder) {
    if (order.salesAgentId && order.salesAgentName) {
      return `Pedido de ${order.salesAgentName}`;
    }
    return `Pedido #${order.number}`;
  }

  if (order.fulfillmentType === "Pickup") {
    return order.customerName ? `Retirada de ${order.customerName}` : "Retirada";
  }

  return order.customerName ? `Delivery de ${order.customerName}` : "Delivery";
}

function renderOrderEyebrow(order: CustomerOrder) {
  if (order.isDeliveryOrder) {
    return order.fulfillmentType === "Pickup" ? "Retirada" : "Delivery";
  }
  if (order.salesAgentId) {
    return "Vendedor";
  }
  return order.tableName || "Local";
}

function renderOrderSubtitle(order: CustomerOrder) {
  if (order.fulfillmentType === "Pickup") {
    return `${order.items.length} item(ns) para retirada`;
  }

  if (order.isDeliveryOrder) {
    return `${order.items.length} item(ns) para entrega`;
  }

  return `${order.tableName} - ${order.items.length} item(ns)`;
}

function getOrderFlowLabel(order: CustomerOrder) {
  if (order.status === "Delivered") {
    return "Concluido";
  }

  if (order.status === "Ready") {
    return "Pronto";
  }

  if (order.status === "InKitchen") {
    return "Em preparo";
  }

  return "Novo";
}

function getOrderPrintCheckLabel(order: CustomerOrder) {
  return order.printStatus === "Printed" ? "Impresso" : "Ainda nao impresso";
}

function getOrderPrintCheckCopy(order: CustomerOrder) {
  if (order.printStatus === "Printed") {
    return order.printedAtUtc ? `Impresso em ${formatDateTime(order.printedAtUtc)}.` : "Pedido marcado como impresso.";
  }

  return "Esse pedido ainda nao foi impresso.";
}

function buildCashColumns(orders: CustomerOrder[]): CashColumn[] {
  const activeOrders = sortOrdersByOldestFirst(orders.filter((order) => order.status !== "Cancelled"));

  return [
    {
      key: "pending",
      title: "A cobrar",
      description: "Pedidos esperando cobranca e confirmacao no caixa.",
      emptyTitle: "Nada para cobrar agora.",
      emptyCopy: "Assim que novos pedidos chegarem, eles vao aparecer aqui para voce fechar no caixa.",
      items: activeOrders.filter((order) => order.paymentStatus !== "Paid"),
      total: sumOrders(activeOrders.filter((order) => order.paymentStatus !== "Paid")),
    },
    {
      key: "paid",
      title: "Pagos",
      description: "Historico rapido dos pedidos ja fechados hoje.",
      emptyTitle: "Nenhum pedido pago por enquanto.",
      emptyCopy: "Quando um pedido for marcado como pago, ele aparece aqui com o horario e a forma de pagamento.",
      items: activeOrders.filter((order) => order.paymentStatus === "Paid"),
      total: sumOrders(activeOrders.filter((order) => order.paymentStatus === "Paid")),
    },
  ];
}

function normalizeCashSearch(value: string) {
  return value
    .toLocaleLowerCase("pt-BR")
    .normalize("NFD")
    .replace(/[\u0300-\u036f]/g, "")
    .trim();
}

function orderMatchesCashSearch(order: CustomerOrder, searchTerm: string) {
  const normalizedSearch = normalizeCashSearch(searchTerm);

  if (!normalizedSearch) {
    return true;
  }

  const searchableText = [
    renderOrderTitle(order),
    renderOrderSubtitle(order),
    order.tableName,
    order.customerName,
    order.deliveryPhone,
    `pedido ${order.number}`,
    `#${order.number}`,
    getOrderFlowLabel(order),
    formatPaymentMethod(order.paymentMethod),
    formatPaymentMethod(order.requestedPaymentMethod),
    formatPaymentStatus(order.paymentStatus),
  ]
    .filter(Boolean)
    .join(" ");

  return normalizeCashSearch(searchableText).includes(normalizedSearch);
}

export function CashModule({
  token,
  onUnauthorized,
  section,
}: {
  token: string;
  onUnauthorized: AsyncVoid;
  section: CashViewKey;
}) {
  const [orders, setOrders] = useState<CustomerOrder[]>([]);
  const [paymentDrafts, setPaymentDrafts] = useState<PaymentDraft>({});
  const [loading, setLoading] = useState(true);
  const [processingOrderId, setProcessingOrderId] = useState("");
  const [removingPaidBatch, setRemovingPaidBatch] = useState(false);
  const [markingAllPaid, setMarkingAllPaid] = useState(false);
  const [removingTodayFlow, setRemovingTodayFlow] = useState(false);
  const [exportingDailyReport, setExportingDailyReport] = useState(false);
  const [errorMessage, setErrorMessage] = useState("");
  const [successMessage, setSuccessMessage] = useState("");
  const [activeOverviewColumn, setActiveOverviewColumn] = useState<"pending" | "paid">("pending");
  const [searchTerm, setSearchTerm] = useState("");
  const [adjustmentDraft, setAdjustmentDraft] = useState<AdjustmentDraft | null>(null);
  const [selectedOrderId, setSelectedOrderId] = useState<string | null>(null);

  async function loadOrders() {
    setLoading(true);

    try {
      const response = await getOrders(token, false, true);
      setOrders(response);
      setPaymentDrafts(
        response.reduce<PaymentDraft>((result, order) => {
          result[order.id] = paymentOptions.reduce<Record<string, number>>((paymentResult, option) => {
            const existingPayment = order.payments.find((payment) => payment.method === option.value);
            const preferredMethod = getPreferredPaymentMethod(order);
            paymentResult[option.value] = existingPayment?.amount ?? (
              preferredMethod === option.value && order.paymentStatus !== "Paid" ? order.totalAmount : 0
            );
            return paymentResult;
          }, {});
          return result;
        }, {}),
      );
      setErrorMessage("");
    } catch (error) {
      await handleApiError(error, onUnauthorized, setErrorMessage, "Nao foi possivel carregar o caixa.");
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    void loadOrders();
  }, [token]);

  useEffect(() => {
    if (selectedOrderId && !orders.some((o) => o.id === selectedOrderId)) {
      setSelectedOrderId(null);
    }
  }, [orders, selectedOrderId]);

  async function handlePaymentUpdate(order: CustomerOrder, paymentStatus: string) {
    const draft = paymentDrafts[order.id] ?? {};
    const payments = paymentOptions
      .map((option) => ({
        method: option.value,
        amount: Number(draft[option.value] ?? 0),
      }))
      .filter((payment) => payment.amount > 0);

    if (paymentStatus === "Paid") {
      const paymentTotal = payments.reduce((total, payment) => total + payment.amount, 0);

      if (payments.length === 0) {
        setErrorMessage("Informe pelo menos uma forma de pagamento.");
        return;
      }

      if (Math.abs(paymentTotal - order.totalAmount) > 0.009) {
        setErrorMessage(`A soma dos pagamentos precisa ser ${formatCurrency(order.totalAmount)}.`);
        return;
      }
    }

    try {
      setProcessingOrderId(order.id);
      setSuccessMessage("");
      const updatedOrder = await updateOrderPayment(
        token,
        order.id,
        paymentStatus,
        payments.length === 1 ? payments[0].method : undefined,
        paymentStatus === "Paid" ? payments : undefined,
      );

      setOrders((currentValue) =>
        currentValue.map((currentOrder) => (currentOrder.id === updatedOrder.id ? updatedOrder : currentOrder)),
      );
      setPaymentDrafts((currentValue) => ({
        ...currentValue,
        [updatedOrder.id]: paymentOptions.reduce<Record<string, number>>((result, option) => {
          result[option.value] = updatedOrder.payments.find((payment) => payment.method === option.value)?.amount ?? 0;
          return result;
        }, {}),
      }));
      setErrorMessage("");
      setSuccessMessage(paymentStatus === "Paid" ? "Pedido marcado como pago." : "Pedido voltou para a lista a cobrar.");
    } catch (error) {
      await handleApiError(error, onUnauthorized, setErrorMessage, "Nao foi possivel atualizar o caixa.");
    } finally {
      setProcessingOrderId("");
    }
  }

  function getBatchPaymentInput(order: CustomerOrder): MarkOrderPaidInput {
    const draft = paymentDrafts[order.id] ?? {};
    const payments = paymentOptions
      .map((option) => ({
        method: option.value,
        amount: Number(draft[option.value] ?? 0),
      }))
      .filter((payment) => payment.amount > 0);
    const paymentTotal = payments.reduce((total, payment) => total + payment.amount, 0);

    if (payments.length > 0 && Math.abs(paymentTotal - order.totalAmount) <= 0.009) {
      return {
        orderId: order.id,
        paymentMethod: payments.length === 1 ? payments[0].method : undefined,
        payments,
      };
    }

    const fallbackMethod = getPreferredPaymentMethod(order) || undefined;

    return {
      orderId: order.id,
      paymentMethod: fallbackMethod,
    };
  }

  async function handleMarkAllPendingPaid(pendingOrders: CustomerOrder[]) {
    const confirmed = window.confirm("Confirmar pagamento de todos os pedidos a cobrar?");

    if (!confirmed) {
      return;
    }

    try {
      setMarkingAllPaid(true);
      setSuccessMessage("");
      const result = await markAllOrdersPaid(token, pendingOrders.map((order) => getBatchPaymentInput(order)));
      await loadOrders();
      setErrorMessage("");

      const baseMessage = `${result.markedCount} pedido(s) marcados como pagos.`;
      setSuccessMessage(result.ignoredCount > 0 ? `${baseMessage} ${result.ignoredCount} pedido(s) nao foram alterados.` : baseMessage);

      if (result.ignoredCount > 0) {
        setErrorMessage(result.ignoredReasons.slice(0, 3).join(" "));
      }
    } catch (error) {
      await handleApiError(error, onUnauthorized, setErrorMessage, "Nao foi possivel marcar todos como pagos.");
    } finally {
      setMarkingAllPaid(false);
    }
  }

  function parseMoneyInput(value: string) {
    const parsedValue = Number(value.trim().replace(",", "."));
    return Number.isFinite(parsedValue) ? parsedValue : Number.NaN;
  }

  async function handleAdjustOrderValue(order: CustomerOrder) {
    setSelectedOrderId(null);
    setAdjustmentDraft({
      order,
      finalAmount: order.totalAmount.toLocaleString("pt-BR", { minimumFractionDigits: 2, maximumFractionDigits: 2 }),
      note: order.priceAdjustmentNote ?? "",
    });
  }

  async function handleConfirmAdjustOrderValue() {
    if (!adjustmentDraft) {
      return;
    }

    const finalAmount = parseMoneyInput(adjustmentDraft.finalAmount);

    if (!Number.isFinite(finalAmount) || finalAmount < 0) {
      setErrorMessage("Informe um valor final valido.");
      return;
    }

    const confirmed = window.confirm(`Confirmar alteracao do valor deste pedido para ${formatCurrency(finalAmount)}?`);

    if (!confirmed) {
      return;
    }

    try {
      const order = adjustmentDraft.order;
      setProcessingOrderId(order.id);
      setSuccessMessage("");
      const updatedOrder = await adjustOrderValue(token, order.id, {
        finalAmount,
        discountAmount: 0,
        surchargeAmount: 0,
        note: adjustmentDraft.note,
      });
      setOrders((currentValue) =>
        currentValue.map((currentOrder) => (currentOrder.id === updatedOrder.id ? updatedOrder : currentOrder)),
      );
      setAdjustmentDraft(null);
      setErrorMessage("");
      setSuccessMessage("Valor do pedido ajustado.");
    } catch (error) {
      await handleApiError(error, onUnauthorized, setErrorMessage, "Nao foi possivel ajustar o valor do pedido.");
    } finally {
      setProcessingOrderId("");
    }
  }

  async function handleDeletePaidOrder(orderId: string) {
    const password = window.prompt("Digite a senha do owner para apagar este pedido pago.");

    if (!password) {
      return;
    }

    const confirmed = window.confirm("Apagar este pedido pago do caixa?");

    if (!confirmed) {
      return;
    }

    try {
      setProcessingOrderId(orderId);
      setSuccessMessage("");
      await deletePaidOrder(token, orderId, password);
      setOrders((currentValue) => currentValue.filter((order) => order.id !== orderId));
      setPaymentDrafts((currentValue) => {
        const nextValue = { ...currentValue };
        delete nextValue[orderId];
        return nextValue;
      });
      setErrorMessage("");
      setSuccessMessage("Pedido pago apagado.");
    } catch (error) {
      await handleApiError(error, onUnauthorized, setErrorMessage, "Nao foi possivel apagar o pedido pago.");
    } finally {
      setProcessingOrderId("");
    }
  }

  async function handleDeleteUnpaidOrder(orderId: string) {
    const confirmed = window.confirm("Apagar este pedido antes do pagamento?");

    if (!confirmed) {
      return;
    }

    try {
      setProcessingOrderId(orderId);
      setSuccessMessage("");
      await deleteOrder(token, orderId);
      setOrders((currentValue) => currentValue.filter((order) => order.id !== orderId));
      setPaymentDrafts((currentValue) => {
        const nextValue = { ...currentValue };
        delete nextValue[orderId];
        return nextValue;
      });
      setErrorMessage("");
      setSuccessMessage("Pedido apagado.");
    } catch (error) {
      await handleApiError(error, onUnauthorized, setErrorMessage, "Nao foi possivel apagar o pedido.");
    } finally {
      setProcessingOrderId("");
    }
  }

  async function handleDeleteAllPaidOrders() {
    const password = window.prompt("Digite a senha do owner para apagar todos os pedidos pagos.");

    if (!password) {
      return;
    }

    const confirmed = window.confirm("Apagar todos os pedidos ja pagos do caixa?");

    if (!confirmed) {
      return;
    }

    try {
      const paidOrderIds = orders.filter((order) => order.paymentStatus === "Paid").map((order) => order.id);
      setRemovingPaidBatch(true);
      setSuccessMessage("");
      await deleteAllPaidOrders(token, password);
      setOrders((currentValue) => currentValue.filter((order) => order.paymentStatus !== "Paid"));
      setPaymentDrafts((currentValue) => {
        const nextValue = { ...currentValue };

        Object.keys(nextValue).forEach((orderId) => {
          if (paidOrderIds.includes(orderId)) {
            delete nextValue[orderId];
          }
        });

        return nextValue;
      });
      setErrorMessage("");
      setSuccessMessage("Pedidos pagos apagados.");
    } catch (error) {
      await handleApiError(error, onUnauthorized, setErrorMessage, "Nao foi possivel apagar os pedidos pagos.");
    } finally {
      setRemovingPaidBatch(false);
    }
  }

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

  async function handleDeleteTodayFlow() {
    const password = window.prompt("Digite a senha do owner para apagar todos os pedidos ativos da operacao.");

    if (!password) {
      return;
    }

    const confirmed = window.confirm(
      "Apagar todos os pedidos ativos em todo o sistema, incluindo cozinha, caixa, impressao e status atuais?",
    );

    if (!confirmed) {
      return;
    }

    try {
      setRemovingTodayFlow(true);
      setSuccessMessage("");
      await deleteTodayOrderFlow(token, password);
      await loadOrders();
      setErrorMessage("");
      setSuccessMessage("Pedidos ativos da operacao apagados em todo o sistema.");
    } catch (error) {
      await handleApiError(error, onUnauthorized, setErrorMessage, "Nao foi possivel apagar os pedidos atuais da operacao.");
    } finally {
      setRemovingTodayFlow(false);
    }
  }

  const columns = useMemo(() => buildCashColumns(orders), [orders]);
  const pendingColumn = columns.find((column) => column.key === "pending");
  const paidColumn = columns.find((column) => column.key === "paid");
  const activeColumn = columns.find((column) => column.key === section) ?? columns[0];
  const pendingCount = pendingColumn?.items.length ?? 0;
  const paidCount = paidColumn?.items.length ?? 0;
  const pendingTotal = sumOrders(pendingColumn?.items ?? []);
  const paidTotal = sumOrders(paidColumn?.items ?? []);
  const paymentSummaries = useMemo(() => buildPaymentSummaries(orders), [orders]);
  const visibleColumn = useMemo(() => {
    const baseColumn = section === "overview"
      ? columns.find((column) => column.key === activeOverviewColumn) ?? columns[0]
      : activeColumn;

    if (!baseColumn) {
      return undefined;
    }

    const filteredItems = baseColumn.items.filter((order) => orderMatchesCashSearch(order, searchTerm));

    return {
      ...baseColumn,
      items: filteredItems,
      total: sumOrders(filteredItems),
    };
  }, [activeColumn, activeOverviewColumn, columns, searchTerm, section]);

  function updatePaymentDraft(orderId: string, method: string, value: string) {
    const parsedValue = parseMoneyInput(value || "0");

    setPaymentDrafts((currentValue) => ({
      ...currentValue,
      [orderId]: {
        ...(currentValue[orderId] ?? {}),
        [method]: Number.isFinite(parsedValue) && parsedValue >= 0 ? parsedValue : 0,
      },
    }));
  }

  function renderOrderCard(order: CustomerOrder) {
    const isPaid = order.paymentStatus === "Paid";

    return (
      <article
        key={order.id}
        className={`cash-compact-card ${isPaid ? "cash-compact-card-paid" : "cash-compact-card-pending"}`}
        onClick={() => setSelectedOrderId(order.id)}
        role="button"
        tabIndex={0}
        onKeyDown={(e) => { if (e.key === "Enter" || e.key === " ") setSelectedOrderId(order.id); }}
      >
        <div className="cash-compact-head">
          <div className="cash-compact-copy">
            <h3>{renderOrderTitle(order)}</h3>
            <p>{renderOrderSubtitle(order)}</p>
          </div>
          <strong className="cash-compact-amount">{formatCurrency(order.totalAmount)}</strong>
        </div>
        <div className="entity-meta-grid cash-compact-meta">
          <span>{getDisplayItemQuantity(order)} item(ns)</span>
          <span>{isPaid ? buildPaymentSummary(order) : "A cobrar"}</span>
          <span>{formatDateTime(order.submittedAtUtc)}</span>
          {order.isDeliveryOrder
            ? <span>{order.fulfillmentType === "Pickup" ? "Retirada" : "Entrega"}</span>
            : <span>Local</span>}
          {order.hasPriceAdjustment ? <span>Valor ajustado</span> : null}
        </div>
      </article>
    );
  }

  function renderOrderModal() {
    const order = orders.find((o) => o.id === selectedOrderId);
    if (!order) return null;

    const isPaid = order.paymentStatus === "Paid";
    const isProcessing = processingOrderId === order.id;
    const deliveryLines = buildDeliverySummary(order);

    return (
      <div className="cash-order-modal-backdrop" onClick={() => setSelectedOrderId(null)}>
        <section
          className="surface-card cash-order-modal"
          onClick={(e) => e.stopPropagation()}
          role="dialog"
          aria-modal="true"
          aria-label={renderOrderTitle(order)}
        >
          {/* Header */}
          <div className="cash-modal-head">
            <div className="cash-modal-head-copy">
              <span className="eyebrow">{renderOrderEyebrow(order)}</span>
              <h2>{renderOrderTitle(order)}</h2>
              <p>Pedido #{order.number} · {formatDateTime(order.submittedAtUtc)}</p>
            </div>
            <div className="cash-modal-head-right">
              <strong className="cash-modal-total">{formatCurrency(order.totalAmount)}</strong>
              <span className={`status-chip ${isPaid ? "ready" : "pending"} cash-payment-chip`}>
                {formatPaymentStatus(order.paymentStatus)}
              </span>
            </div>
            <button
              className="cash-modal-close"
              type="button"
              onClick={() => setSelectedOrderId(null)}
              aria-label="Fechar"
            >
              ×
            </button>
          </div>

          {/* Meta chips */}
          <div className="entity-meta-grid">
            <span>{getDisplayItemQuantity(order)} item(ns)</span>
            <span>{getOrderFlowLabel(order)}</span>
            <span>{getOrderPrintCheckLabel(order)}</span>
            {order.salesAgentName ? <span>Vendedor: {order.salesAgentName}</span> : null}
            {order.couponCode ? <span>Cupom: {order.couponCode}</span> : null}
            {order.isEdited ? <span>Editado</span> : null}
          </div>

          {/* Adjustment notice */}
          {order.hasPriceAdjustment ? (
            <div className="module-empty-state compact-empty-state cash-payment-state-card cash-adjustment-summary-card">
              <strong>Valor alterado</strong>
              <p>Original: {formatCurrency(order.originalTotalAmount)}</p>
              <p>Final: {formatCurrency(order.totalAmount)}</p>
              {order.discountAmount > 0 ? <p>Desconto: {formatCurrency(order.discountAmount)}</p> : null}
              {order.surchargeAmount > 0 ? <p>Acrescimo: {formatCurrency(order.surchargeAmount)}</p> : null}
              {order.priceAdjustmentNote ? <p>{order.priceAdjustmentNote}</p> : null}
            </div>
          ) : null}

          {/* Delivery info */}
          {deliveryLines.length > 0 ? (
            <div className="module-empty-state compact-empty-state cash-payment-state-card">
              <strong>Entrega</strong>
              {deliveryLines.map((line) => (
                <p key={line}>{line}</p>
              ))}
            </div>
          ) : null}

          {/* Notes */}
          {order.notes ? (
            <div className="module-empty-state compact-empty-state cash-payment-state-card">
              <strong>Observacao</strong>
              <p>{order.notes}</p>
            </div>
          ) : null}

          {/* Payment section */}
          {isPaid ? (
            <div className="module-empty-state compact-empty-state cash-payment-state-card">
              <strong>{order.payments.length > 1 ? "Pagamento dividido" : buildPaymentSummary(order)}</strong>
              <p>{order.paidAtUtc ? `Pago em ${formatDateTime(order.paidAtUtc)}` : "Pago no caixa."}</p>
              {order.payments.length > 1 ? <p>{buildPaymentSummary(order)}</p> : null}
              {getPaymentChangeCopy(order) ? <p>{getPaymentChangeCopy(order)}</p> : null}
            </div>
          ) : (
            <div className="field-group cash-payment-field">
              <label>Pagamento</label>
              <div className="cash-split-payment-grid">
                {paymentOptions.map((option) => (
                  <label
                    key={option.value}
                    className={`cash-split-payment-field ${getPreferredPaymentMethod(order) === option.value ? "is-client-choice" : ""}`}
                  >
                    <span>
                      {option.label}
                      {getPreferredPaymentMethod(order) === option.value ? <small>Escolha do cliente</small> : null}
                    </span>
                    <input
                      inputMode="decimal"
                      value={paymentDrafts[order.id]?.[option.value] ? String(paymentDrafts[order.id]?.[option.value]) : ""}
                      onChange={(event) => updatePaymentDraft(order.id, option.value, event.target.value)}
                      placeholder="0,00"
                    />
                  </label>
                ))}
              </div>
              <p className="field-hint">
                Soma: {formatCurrency(Object.values(paymentDrafts[order.id] ?? {}).reduce((total, value) => total + Number(value ?? 0), 0))}
                {" "}de {formatCurrency(order.totalAmount)}
              </p>
            </div>
          )}

          {/* Primary actions */}
          <div className="cash-modal-primary-actions">
            {isPaid ? (
              <button
                className="ghost-link button-link module-action-button cash-modal-action-secondary"
                type="button"
                disabled={isProcessing}
                onClick={() => void handlePaymentUpdate(order, "Pending")}
              >
                {isProcessing ? "Salvando..." : "Voltar a cobrar"}
              </button>
            ) : (
              <button
                className="ghost-link button-link module-action-button module-action-button-primary cash-modal-action-primary"
                type="button"
                disabled={isProcessing}
                onClick={() => void handlePaymentUpdate(order, "Paid")}
              >
                {isProcessing ? "Salvando..." : "Pago"}
              </button>
            )}
          </div>

          {/* Secondary actions */}
          <div className="toolbar-actions compact table-card-actions module-action-row order-card-actions">
            <button
              className="ghost-link button-link module-action-button"
              type="button"
              disabled={isProcessing}
              onClick={() => void handleAdjustOrderValue(order)}
            >
              Ajustar valor
            </button>

            {isPaid ? (
              <button
                className="ghost-link button-link destructive-link module-action-button"
                type="button"
                disabled={isProcessing}
                onClick={() => void handleDeletePaidOrder(order.id)}
              >
                {isProcessing ? "Apagando..." : "Excluir pago"}
              </button>
            ) : (
              <button
                className="ghost-link button-link destructive-link module-action-button"
                type="button"
                disabled={isProcessing}
                onClick={() => void handleDeleteUnpaidOrder(order.id)}
              >
                {isProcessing ? "Apagando..." : "Excluir"}
              </button>
            )}
          </div>
        </section>
      </div>
    );
  }

  function renderCashColumn(column: CashColumn) {
    const hasSearch = normalizeCashSearch(searchTerm).length > 0;

    return (
      <section
        className={`surface-card cash-quick-column cash-quick-column-${column.key} is-active`}
      >
        <div className="module-section-head cash-view-panel-head compact-order-column-head">
          <div className="kitchen-column-copy">
            <div className="cash-column-title-row">
              <span className="eyebrow">{column.title}</span>
              <strong className="cash-column-inline-total">{formatCurrency(column.total)}</strong>
            </div>
            <small>{column.description}</small>
            <small className="cash-column-inline-count">{column.items.length} pedido(s)</small>
          </div>
          {column.key === "pending" && column.items.length > 0 ? (
            <button
              className="ghost-link button-link module-action-button cash-mark-all-paid-button"
              type="button"
              disabled={markingAllPaid}
              onClick={() => void handleMarkAllPendingPaid(column.items)}
            >
              {markingAllPaid ? "Baixando..." : "Marcar todos como pagos"}
            </button>
          ) : null}
        </div>

        <div className="field-group cash-search-field">
          <label htmlFor="cashOrderSearch">Buscar nesta lista</label>
          <input
            id="cashOrderSearch"
            type="search"
            value={searchTerm}
            onChange={(event) => setSearchTerm(event.target.value)}
            placeholder="Ex.: Davi, Mesa 2, telefone..."
          />
        </div>

        {column.items.length === 0 ? (
          <div className="module-empty-state compact-empty-state cash-quick-empty">
            <strong>{hasSearch ? "Nenhum pedido encontrado." : column.emptyTitle}</strong>
            <p>{hasSearch ? "Tente buscar por outro nome, mesa ou telefone." : column.emptyCopy}</p>
          </div>
        ) : (
          <div className="cash-single-list cash-quick-list">
            {column.items.map((order) => renderOrderCard(order))}
          </div>
        )}

      </section>
    );
  }

  function renderOperationPanel() {
    return (
      <>
        <div className="cash-method-grid">
          {paymentSummaries.map((summary) => (
            <article key={summary.value} className="cash-method-card">
              <div className="cash-method-head">
                <strong>{summary.label}</strong>
                <span>{summary.count} pagos</span>
              </div>
              <strong className="cash-method-total">{formatCurrency(summary.total)}</strong>
            </article>
          ))}
        </div>

        <div className="cash-operation-grid">
          <article className="surface-card cash-operation-card">
            <span className="eyebrow">Relatorio diario</span>
            <strong>Exportar PDF</strong>
            <p>Baixe o fechamento do dia com pedidos, pagamentos e totais do caixa.</p>
            <button
              className="ghost-link button-link module-action-button"
              type="button"
              disabled={exportingDailyReport}
              onClick={() => void handleDownloadDailyReport()}
            >
              {exportingDailyReport ? "Exportando PDF..." : "Exportar relatorio"}
            </button>
          </article>

          <article className="surface-card cash-operation-card">
            <span className="eyebrow">Pagos</span>
            <strong>Limpar historico pago</strong>
            <p>Remove do caixa os pedidos ja pagos, sem mexer no que ainda esta aguardando cobranca.</p>
            <button
              className="ghost-link button-link destructive-link module-action-button"
              type="button"
              disabled={removingPaidBatch || paidCount === 0}
              onClick={() => void handleDeleteAllPaidOrders()}
            >
              {removingPaidBatch ? "Apagando pagos..." : "Apagar pagos"}
            </button>
          </article>

          <article className="surface-card cash-operation-card cash-operation-card-danger">
            <span className="eyebrow">Sistema</span>
            <strong>Apagar fluxo atual</strong>
            <p>Limpa pedidos ativos de cozinha, caixa e impressao. Use ao fechar o dia ou quando precisar reiniciar a fila da operacao.</p>
            <button
              className="ghost-link button-link destructive-link module-action-button"
              type="button"
              disabled={removingTodayFlow}
              onClick={() => void handleDeleteTodayFlow()}
            >
              {removingTodayFlow ? "Apagando operacao..." : "Apagar pedidos atuais"}
            </button>
          </article>
        </div>
      </>
    );
  }

  const cashTabs = [
    {
      key: "pending" as const,
      title: "A cobrar",
      count: pendingCount,
      total: pendingTotal,
    },
    {
      key: "paid" as const,
      title: "Pagos",
      count: paidCount,
      total: paidTotal,
    },
  ];

  return (
    <section className="menu-workspace">
      {successMessage ? <p className="module-feedback success">{successMessage}</p> : null}
      {errorMessage ? <p className="module-feedback error">{errorMessage}</p> : null}

      {selectedOrderId ? renderOrderModal() : null}

      {adjustmentDraft ? (
        <div className="cash-adjust-modal-backdrop" role="presentation">
          <section className="surface-card cash-adjust-modal" role="dialog" aria-modal="true" aria-label="Ajustar valor do pedido">
            <div className="module-section-head compact-order-panel-head">
              <div>
                <span className="eyebrow">Ajustar valor</span>
                <h2>{renderOrderTitle(adjustmentDraft.order)}</h2>
                <p>Atual: {formatCurrency(adjustmentDraft.order.totalAmount)}</p>
              </div>
              <button className="ghost-link button-link module-action-button" type="button" onClick={() => setAdjustmentDraft(null)}>
                Fechar
              </button>
            </div>

            <div className="field-group">
              <label htmlFor="cashFinalAmount">Valor final do pedido</label>
              <input
                id="cashFinalAmount"
                inputMode="decimal"
                value={adjustmentDraft.finalAmount}
                onChange={(event) => setAdjustmentDraft((currentValue) => currentValue ? { ...currentValue, finalAmount: event.target.value } : currentValue)}
                autoFocus
              />
              <p className="field-hint">
                O sistema calcula desconto ou acrescimo automaticamente.
              </p>
            </div>

            <div className="field-group">
              <label htmlFor="cashAdjustmentNote">Observacao opcional</label>
              <input
                id="cashAdjustmentNote"
                value={adjustmentDraft.note}
                onChange={(event) => setAdjustmentDraft((currentValue) => currentValue ? { ...currentValue, note: event.target.value } : currentValue)}
                placeholder="Ex.: desconto combinado"
              />
            </div>

            <div className="toolbar-actions compact table-card-actions module-action-row order-card-actions">
              <button className="ghost-link button-link module-action-button" type="button" onClick={() => setAdjustmentDraft(null)}>
                Cancelar
              </button>
              <button
                className="ghost-link button-link module-action-button module-action-button-primary"
                type="button"
                disabled={processingOrderId === adjustmentDraft.order.id}
                onClick={() => void handleConfirmAdjustOrderValue()}
              >
                {processingOrderId === adjustmentDraft.order.id ? "Salvando..." : "Confirmar ajuste"}
              </button>
            </div>
          </section>
        </div>
      ) : null}

      <section className="surface-card module-list-card">
        <div className="module-section-head cash-header compact-order-panel-head">
          <div className="cash-header-copy">
            <h2>Caixa</h2>
          </div>
          {section === "overview" ? (
            <Link className="zpclosing-link-btn" href="/app/caixa/fechamento">
              Fechamento do dia
            </Link>
          ) : null}
        </div>

        <div className="cash-shell">
          <section className="cash-focus-panel">
            {loading ? (
              <p className="loading-state">Carregando caixa...</p>
            ) : section === "reports" ? (
              <>
                <div className="module-section-head cash-view-panel-head compact-order-column-head">
                  <div className="kitchen-column-copy">
                    <strong>Relatorios e operacao</strong>
                    <small>Area para exportacao, limpeza de historico pago e controle do fluxo atual do sistema.</small>
                  </div>
                </div>

                {renderOperationPanel()}
              </>
            ) : section === "overview" ? (
              <>
                <nav className="owner-flow-tabs cash-flow-tabs" aria-label="Trocar area do caixa">
                  {cashTabs.map((tab) => (
                    <div key={tab.key} className="owner-flow-tab-item">
                      <button
                        className={activeOverviewColumn === tab.key ? "owner-flow-tab-button is-active" : "owner-flow-tab-button"}
                        type="button"
                        aria-pressed={activeOverviewColumn === tab.key}
                        onClick={() => setActiveOverviewColumn(tab.key)}
                      >
                        <span>{tab.title}</span>
                        <strong>{tab.count}</strong>
                      </button>
                    </div>
                  ))}
                </nav>

                <div className="cash-quick-board" aria-label="Visao rapida do caixa">
                  {visibleColumn ? renderCashColumn(visibleColumn) : null}
                </div>
              </>
            ) : visibleColumn ? (
              <>
                {renderCashColumn(visibleColumn)}
              </>
            ) : null}
          </section>
        </div>
      </section>
    </section>
  );
}
