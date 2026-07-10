"use client";

import { useEffect, useState } from "react";
import { ApiError, getPublicCustomerProfile, type PublicCustomerProfile } from "@/lib/api";
import { formatCurrency, formatDateTime } from "@/components/modules/module-utils";

type PublicCustomerProfilePanelProps = {
  code: string;
  onClose: () => void;
  asPage?: boolean;
};

function formatOrderStatus(status: string) {
  switch (status) {
    case "Pending":
      return "Recebido";
    case "InKitchen":
      return "Em preparo";
    case "Ready":
      return "Pronto";
    case "Delivered":
      return "Concluido";
    case "Cancelled":
      return "Cancelado";
    default:
      return status || "Registrado";
  }
}

function formatFulfillmentType(value: string) {
  switch (value) {
    case "Delivery":
      return "Entrega";
    case "Pickup":
      return "Retirada";
    case "Local":
      return "Local";
    default:
      return value || "Pedido";
  }
}

function buildAddressLabel(profile: PublicCustomerProfile) {
  const address = profile.primaryAddress;
  if (!address) {
    return null;
  }

  const streetLine = [address.street, address.number].filter(Boolean).join(", ");
  const detailLine = [address.neighborhood, address.complement].filter(Boolean).join(" - ");
  const zipLine = address.zipCode ? `CEP ${address.zipCode}` : "";

  return [streetLine, detailLine, zipLine].filter(Boolean).join(" | ");
}

export function PublicCustomerProfilePanel({ code, onClose, asPage }: PublicCustomerProfilePanelProps) {
  const [profile, setProfile] = useState<PublicCustomerProfile | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [errorMessage, setErrorMessage] = useState("");

  useEffect(() => {
    let ignore = false;

    async function loadProfile() {
      setIsLoading(true);
      setErrorMessage("");

      try {
        const response = await getPublicCustomerProfile(code);
        if (ignore) {
          return;
        }

        if (!response.found) {
          setProfile(response);
          setErrorMessage(response.message || "Perfil nao encontrado ou link invalido.");
          return;
        }

        setProfile(response);
      } catch (error) {
        if (ignore) {
          return;
        }

        if (error instanceof ApiError && error.status === 404) {
          setErrorMessage("Perfil nao encontrado ou link invalido.");
        } else {
          setErrorMessage("Nao foi possivel carregar seu perfil agora.");
        }
      } finally {
        if (!ignore) {
          setIsLoading(false);
        }
      }
    }

    void loadProfile();

    return () => {
      ignore = true;
    };
  }, [code]);

  const addressLabel = profile ? buildAddressLabel(profile) : null;
  const recentOrders = profile?.recentOrders ?? [];

  const panelContent = (
    <>
      <div className="public-customer-profile-head">
        <div>
          <span className="eyebrow">Meu perfil</span>
          <h2 id="publicCustomerProfileTitle">{profile?.customerName ? `Ola, ${profile.customerName.split(" ")[0]}` : "Meus pedidos"}</h2>
        </div>
        <button className="ghost-link button-link" type="button" onClick={onClose}>
          {asPage ? "← Voltar" : "Fechar"}
        </button>
      </div>

      {isLoading ? (
        <p className="loading-state">Carregando seu perfil...</p>
      ) : errorMessage ? (
        <div className="module-empty-state compact-empty-state">
          <strong>{errorMessage}</strong>
          <p>Voce pode continuar usando o cardapio normalmente.</p>
        </div>
      ) : profile?.found ? (
        <>
          <div className="public-customer-profile-summary">
            <div>
              <span>Cliente</span>
              <strong>{profile.customerName || "Cliente"}</strong>
            </div>
            {profile.maskedPhone ? (
              <div>
                <span>Telefone</span>
                <strong>{profile.maskedPhone}</strong>
              </div>
            ) : null}
            <div>
              <span>Loja</span>
              <strong>{profile.businessName || "Unidade"}</strong>
            </div>
          </div>

          {addressLabel ? (
            <div className="public-customer-profile-address">
              <span>Endereco principal</span>
              <p>{addressLabel}</p>
            </div>
          ) : null}

          {profile.hasActiveOrder ? (
            <p className="module-feedback success compact-feedback">Voce tem pedido recente em andamento nesta unidade.</p>
          ) : null}

          <div className="public-customer-profile-orders-head">
            <div>
              <span className="eyebrow">Historico</span>
              <h3>Ultimos pedidos</h3>
            </div>
            <strong>{recentOrders.length}</strong>
          </div>

          {recentOrders.length === 0 ? (
            <div className="module-empty-state compact-empty-state">
              <strong>Nenhum pedido recente.</strong>
              <p>{profile.message || "Quando voce fizer pedidos por este link, eles aparecerao aqui."}</p>
            </div>
          ) : (
            <div className="public-customer-profile-orders">
              {recentOrders.map((order, index) => (
                <article key={`${order.displayCode ?? "pedido"}-${order.createdAt}-${index}`} className="public-customer-profile-order">
                  <div className="public-customer-profile-order-head">
                    <div>
                      <strong>{order.displayCode || `Pedido ${index + 1}`}</strong>
                      <span>{formatDateTime(order.createdAt)}</span>
                    </div>
                    <div>
                      <strong>{formatCurrency(order.total)}</strong>
                      <span>{formatOrderStatus(order.status)} | {formatFulfillmentType(order.fulfillmentType)}</span>
                    </div>
                  </div>

                  <div className="public-customer-profile-items">
                    {order.items.map((item, itemIndex) => (
                      <span key={`${item.name}-${itemIndex}`}>
                        {item.quantity}x {item.name}
                      </span>
                    ))}
                  </div>
                </article>
              ))}
            </div>
          )}
        </>
      ) : (
        <div className="module-empty-state compact-empty-state">
          <strong>Perfil nao encontrado ou link invalido.</strong>
          <p>Voce pode continuar usando o cardapio normalmente.</p>
        </div>
      )}
    </>
  );

  if (asPage) {
    return (
      <section className="public-profile-page-card" aria-labelledby="publicCustomerProfileTitle">
        {panelContent}
      </section>
    );
  }

  return (
    <div className="public-customer-profile-backdrop" role="presentation" onClick={onClose}>
      <section
        className="surface-card public-customer-profile-panel"
        role="dialog"
        aria-modal="true"
        aria-labelledby="publicCustomerProfileTitle"
        onClick={(event) => event.stopPropagation()}
      >
        {panelContent}
      </section>
    </div>
  );
}
