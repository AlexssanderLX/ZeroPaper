"use client";

import { FormEvent, useRef, useState, useTransition } from "react";
import { useRouter } from "next/navigation";
import { ApiError, createAccessRequest, createRestaurantSignup, loginPortal } from "@/lib/api";
import { PORTAL_SESSION_KEY, type PortalSession } from "@/lib/owner-portal";

const defaultPlan = {
  planName: "ZeroPaper Base",
  monthlyPrice: 0,
  maxUsers: 0,
};

export function RestaurantSignupForm() {
  const router = useRouter();
  const formRef = useRef<HTMLFormElement | null>(null);
  const [isPending, startTransition] = useTransition();
  const [isRequestPending, startTransitionRequest] = useTransition();
  const [errorMessage, setErrorMessage] = useState("");
  const [requestMessage, setRequestMessage] = useState("");
  const [requestStatus, setRequestStatus] = useState<"success" | "error" | null>(null);

  function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    const formData = new FormData(event.currentTarget);
    const restaurantName = String(formData.get("restaurantName") ?? "").trim();
    const legalName = String(formData.get("legalName") ?? "").trim();
    const ownerName = String(formData.get("ownerName") ?? "").trim();
    const ownerEmail = String(formData.get("ownerEmail") ?? "").trim().toLowerCase();
    const accessCode = String(formData.get("accessCode") ?? "").trim().toUpperCase();
    const ownerPassword = String(formData.get("ownerPassword") ?? "").trim();
    const contactPhone = String(formData.get("contactPhone") ?? "").trim();

    if (!restaurantName || !legalName || !ownerName || !ownerEmail || !ownerPassword || !accessCode) {
      setErrorMessage("Preencha os dados principais para concluir o cadastro.");
      return;
    }

    setErrorMessage("");

    startTransition(() => {
      void (async () => {
        try {
          await createRestaurantSignup({
            restaurantName,
            legalName,
            ownerName,
            ownerEmail,
            accessCode,
            ownerPassword,
            contactPhone: contactPhone || undefined,
            ...defaultPlan,
          });

          const loginResponse = await loginPortal({
            email: ownerEmail,
            password: ownerPassword,
            profile: "restaurant",
          });

          const session: PortalSession = {
            token: loginResponse.token,
            expiresAtUtc: loginResponse.expiresAtUtc,
            email: loginResponse.email,
            profile: "restaurant",
            ownerName: loginResponse.ownerName,
            restaurantName: loginResponse.restaurantName,
            role: loginResponse.role,
          };

          window.sessionStorage.setItem(PORTAL_SESSION_KEY, JSON.stringify(session));
          router.replace("/app");
        } catch (error) {
          if (error instanceof ApiError) {
            setErrorMessage(error.message);
            return;
          }

          setErrorMessage("Nao foi possivel concluir o cadastro agora.");
        }
      })();
    });
  }

  function handleAccessRequest() {
    const formElement = formRef.current;

    if (!formElement) {
      return;
    }

    const formData = new FormData(formElement);
    const restaurantName = String(formData.get("restaurantName") ?? "").trim();
    const legalName = String(formData.get("legalName") ?? "").trim();
    const ownerName = String(formData.get("ownerName") ?? "").trim();
    const ownerEmail = String(formData.get("ownerEmail") ?? "").trim().toLowerCase();
    const contactPhone = String(formData.get("contactPhone") ?? "").trim();

    if (!restaurantName || !ownerName || !ownerEmail) {
      setRequestStatus("error");
      setRequestMessage("Preencha restaurante, responsavel e email para pedir a liberacao.");
      return;
    }

    setRequestMessage("");
    setRequestStatus(null);

    startTransitionRequest(() => {
      void (async () => {
        try {
          const response = await createAccessRequest({
            restaurantName,
            legalName: legalName || undefined,
            ownerName,
            ownerEmail,
            contactPhone: contactPhone || undefined,
          });

          setRequestMessage(response.message);
          setRequestStatus("success");
        } catch (error) {
          if (error instanceof ApiError) {
            setRequestStatus("error");
            setRequestMessage(error.message);
            return;
          }

          setRequestStatus("error");
          setRequestMessage("Nao foi possivel enviar a solicitacao agora.");
        }
      })();
    });
  }

  return (
    <>
      <form ref={formRef} className="login-form signup-form" onSubmit={handleSubmit}>
        <div className="module-inline-grid">
          <div className="field-group">
            <label htmlFor="restaurantName">Nome do restaurante</label>
            <input id="restaurantName" name="restaurantName" placeholder="Ex.: Casa do Bairro" />
          </div>
          <div className="field-group">
            <label htmlFor="legalName">Razao social</label>
            <input id="legalName" name="legalName" placeholder="Ex.: Casa do Bairro LTDA" />
          </div>
        </div>

        <div className="module-inline-grid">
          <div className="field-group">
            <label htmlFor="ownerName">Responsavel</label>
            <input id="ownerName" name="ownerName" placeholder="Seu nome" />
          </div>
          <div className="field-group">
            <label htmlFor="contactPhone">Telefone</label>
            <input id="contactPhone" name="contactPhone" placeholder="(11) 99999-0000" />
          </div>
        </div>

        <div className="module-inline-grid">
          <div className="field-group">
            <label htmlFor="ownerEmail">Email</label>
            <input id="ownerEmail" name="ownerEmail" type="email" placeholder="voce@empresa.com" />
          </div>
          <div className="field-group">
            <label htmlFor="accessCode">Codigo de liberacao</label>
            <input id="accessCode" name="accessCode" placeholder="ZP-0000-0000-0000" />
          </div>
        </div>

        <div className="module-inline-grid">
          <div className="field-group">
            <label htmlFor="ownerPassword">Senha</label>
            <input id="ownerPassword" name="ownerPassword" type="password" placeholder="Crie uma senha" />
          </div>
        </div>

        {errorMessage ? <p className="form-feedback">{errorMessage}</p> : null}

        <button className="primary-link button-link signup-submit" type="submit" disabled={isPending}>
          {isPending ? "Preparando seu acesso..." : "Cadastrar minha unidade"}
        </button>
      </form>

      <div className="request-access-card">
        <span className="eyebrow">Sem codigo</span>
        <strong>Solicite sua liberacao</strong>
        <p>Se voce ainda nao recebeu um codigo, a ZeroPaper usa os dados preenchidos acima para analisar e retornar a liberacao.</p>
        {requestMessage ? <p className={`module-feedback ${requestStatus ?? "success"}`}>{requestMessage}</p> : null}
        <button className="ghost-link button-link" type="button" onClick={handleAccessRequest} disabled={isRequestPending}>
          {isRequestPending ? "Enviando..." : "Solicitar verificacao"}
        </button>
      </div>
    </>
  );
}
