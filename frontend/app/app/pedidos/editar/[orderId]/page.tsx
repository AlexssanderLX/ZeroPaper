"use client";

import { useEffect, useState } from "react";
import { useParams } from "next/navigation";
import { useAppSession } from "@/components/app-session-provider";
import { PublicTableOrder } from "@/components/public-table-order";
import { getOrder, type CustomerOrder } from "@/lib/api";
import { handleApiError } from "@/components/modules/module-utils";

export default function EditOrderPage() {
  const params = useParams<{ orderId: string }>();
  const { session, clearSession } = useAppSession();
  const [order, setOrder] = useState<CustomerOrder | null>(null);
  const [loading, setLoading] = useState(true);
  const [errorMessage, setErrorMessage] = useState("");

  useEffect(() => {
    let ignore = false;

    async function loadOrderForEdit() {
      try {
        setLoading(true);
        const response = await getOrder(session.token, params.orderId);

        if (ignore) {
          return;
        }

        if (!response.publicCode) {
          setErrorMessage("Este pedido nao possui link de cardapio para edicao.");
          return;
        }

        setOrder(response);
        setErrorMessage("");
      } catch (error) {
        if (!ignore) {
          await handleApiError(error, clearSession, setErrorMessage, "Nao foi possivel abrir o pedido para edicao.");
        }
      } finally {
        if (!ignore) {
          setLoading(false);
        }
      }
    }

    void loadOrderForEdit();

    return () => {
      ignore = true;
    };
  }, [clearSession, params.orderId, session.token]);

  if (loading) {
    return (
      <main className="page-shell">
        <section className="surface-card app-loading-card ambient-panel subtle">
          <span className="eyebrow">Editando pedido</span>
          <h1>Abrindo cardapio</h1>
          <p>Carregando os itens atuais deste pedido.</p>
        </section>
      </main>
    );
  }

  if (errorMessage || !order?.publicCode) {
    return (
      <main className="page-shell">
        <section className="surface-card app-loading-card ambient-panel subtle">
          <span className="eyebrow">Edicao indisponivel</span>
          <h1>Nao foi possivel abrir</h1>
          <p>{errorMessage || "Pedido sem cardapio publico vinculado."}</p>
          <a className="primary-link button-link" href="/app/pedidos/a-fazer">
            Voltar aos pedidos
          </a>
        </section>
      </main>
    );
  }

  return (
    <PublicTableOrder
      publicCode={order.publicCode}
      editOrder={order}
      editToken={session.token}
      editBackHref="/app/pedidos/a-fazer"
      onEditUnauthorized={clearSession}
    />
  );
}
