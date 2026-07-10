"use client";

import { useEffect, useState } from "react";
import { useParams } from "next/navigation";
import { ApiError, getPublicSellerLink, type PublicSellerLink } from "@/lib/api";
import { PublicTableOrder } from "@/components/public-table-order";

type PageState =
  | { status: "loading" }
  | { status: "unavailable" }
  | { status: "error"; retry: () => void }
  | { status: "ready"; link: PublicSellerLink };

export default function SellerLinkPage() {
  const { code } = useParams<{ code: string }>();
  const [state, setState] = useState<PageState>({ status: "loading" });

  function load() {
    if (!code) return;
    setState({ status: "loading" });
    let cancelled = false;

    void (async () => {
      try {
        const link = await getPublicSellerLink(code);
        if (!cancelled) setState({ status: "ready", link });
      } catch (err) {
        if (cancelled) return;
        if (err instanceof ApiError && (err.status === 404 || err.status === 410)) {
          setState({ status: "unavailable" });
        } else {
          setState({ status: "error", retry: load });
        }
      }
    })();

    return () => { cancelled = true; };
  }

  // eslint-disable-next-line react-hooks/exhaustive-deps
  useEffect(load, [code]);

  if (state.status === "loading") {
    return (
      <main className="sel-link-status">
        <p className="sel-link-status-text">Carregando loja...</p>
      </main>
    );
  }

  if (state.status === "unavailable") {
    return (
      <main className="sel-link-status">
        <div className="sel-link-unavailable">
          <h1>Link nao encontrado</h1>
          <p>Confira se o link esta correto ou peca um novo link para o estabelecimento.</p>
        </div>
      </main>
    );
  }

  if (state.status === "error") {
    return (
      <main className="sel-link-status">
        <p className="sel-link-status-text">Nao foi possivel carregar o link.</p>
        <button className="sel-link-retry" onClick={state.retry}>
          Tentar novamente
        </button>
      </main>
    );
  }

  const { link } = state;
  return (
    <PublicTableOrder publicCode={link.cashTablePublicCode} sellerCode={code} sellerName={link.sellerName} />
  );
}
