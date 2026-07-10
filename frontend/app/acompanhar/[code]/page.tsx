const API_BASE_URL = process.env.NEXT_PUBLIC_API_BASE_URL ?? "http://localhost:5097";

type TrackedAdditional = {
  groupName: string;
  optionName: string;
  unitPrice: number;
};

type TrackedItem = {
  name: string;
  quantity: number;
  totalPrice: number;
  notes?: string | null;
  additionalSelections: TrackedAdditional[];
};

type TrackingResponse = {
  found: boolean;
  message?: string | null;
  restaurantName?: string | null;
  order?: {
    number: number;
    status: string;
    paymentStatus: string;
    paymentMethod: string;
    totalAmount: number;
    totalItemQuantity: number;
    submittedAtUtc: string;
    isEdited: boolean;
    editedAtUtc?: string | null;
    items: TrackedItem[];
  } | null;
};

const statusLabels: Record<string, string> = {
  Pending: "Recebido",
  InKitchen: "Em preparo",
  Ready: "Pronto",
  Delivered: "Finalizado",
  Cancelled: "Cancelado",
};

const paymentLabels: Record<string, string> = {
  Pending: "Aguardando",
  Paid: "Pago",
  Cancelled: "Cancelado",
  Pix: "Pix",
  Credit: "Credito",
  Debit: "Debito",
  Cash: "Dinheiro",
  Unspecified: "A combinar",
};

const money = new Intl.NumberFormat("pt-BR", {
  style: "currency",
  currency: "BRL",
});

const dateTime = new Intl.DateTimeFormat("pt-BR", {
  dateStyle: "short",
  timeStyle: "short",
  timeZone: "America/Sao_Paulo",
});

function formatDate(value?: string | null) {
  if (!value) {
    return "";
  }

  const date = new Date(value);
  return Number.isNaN(date.getTime()) ? "" : dateTime.format(date);
}

export default async function OrderTrackingPage({
  params,
}: {
  params: Promise<{ code: string }>;
}) {
  const { code } = await params;
  const response = await fetch(
    `${API_BASE_URL}/api/public/delivery-links/${encodeURIComponent(code)}/tracking`,
    { cache: "no-store" },
  );

  const data = response.ok ? ((await response.json()) as TrackingResponse) : null;
  const order = data?.order ?? null;

  return (
    <main className="tracking-page">
      <section className="tracking-shell">
        <div className="tracking-kicker">ZeroPaper</div>
        <h1>Acompanhar pedido</h1>
        <p className="tracking-muted">
          {data?.restaurantName ? data.restaurantName : "Confira o status atualizado do seu pedido."}
        </p>

        {!data?.found || !order ? (
          <div className="tracking-card">
            <strong>Nao encontrei nenhum pedido para este numero.</strong>
            <p>Quando houver um pedido ativo neste telefone, ele aparece aqui.</p>
          </div>
        ) : (
          <>
            <div className="tracking-status-card">
              <div>
                <span>Pedido</span>
                <strong>#{order.number}</strong>
              </div>
              <div>
                <span>Status</span>
                <strong>{statusLabels[order.status] ?? order.status}</strong>
              </div>
              <div>
                <span>Total</span>
                <strong>{money.format(order.totalAmount)}</strong>
              </div>
            </div>

            <div className="tracking-card tracking-summary">
              <div>
                <span>Pagamento</span>
                <strong>{paymentLabels[order.paymentMethod] ?? order.paymentMethod}</strong>
              </div>
              <div>
                <span>Situacao</span>
                <strong>{paymentLabels[order.paymentStatus] ?? order.paymentStatus}</strong>
              </div>
              <div>
                <span>Recebido em</span>
                <strong>{formatDate(order.submittedAtUtc)}</strong>
              </div>
            </div>

            {order.isEdited ? (
              <p className="tracking-note">Pedido atualizado pela unidade{order.editedAtUtc ? ` em ${formatDate(order.editedAtUtc)}` : ""}.</p>
            ) : null}

            <section className="tracking-items">
              <h2>Itens do pedido</h2>
              {order.items.map((item, index) => (
                <article className="tracking-item" key={`${item.name}-${index}`}>
                  <div className="tracking-item-main">
                    <strong>
                      {item.quantity}x {item.name}
                    </strong>
                    <span>{money.format(item.totalPrice)}</span>
                  </div>
                  {item.additionalSelections.length > 0 ? (
                    <ul>
                      {item.additionalSelections.map((additional, additionalIndex) => (
                        <li key={`${additional.optionName}-${additionalIndex}`}>
                          {additional.optionName}
                          {additional.unitPrice > 0 ? ` + ${money.format(additional.unitPrice)}` : ""}
                        </li>
                      ))}
                    </ul>
                  ) : null}
                  {item.notes ? <p>{item.notes}</p> : null}
                </article>
              ))}
            </section>
          </>
        )}
      </section>
    </main>
  );
}
