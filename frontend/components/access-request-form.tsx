"use client";

import { FormEvent, useState, useTransition } from "react";
import { ApiError, createAccessRequest } from "@/lib/api";

export function AccessRequestForm() {
  const [isPending, startTransition] = useTransition();
  const [errorMessage, setErrorMessage] = useState("");
  const [successMessage, setSuccessMessage] = useState("");

  function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    const formData = new FormData(event.currentTarget);
    const restaurantName = String(formData.get("restaurantName") ?? "").trim();
    const legalName = String(formData.get("legalName") ?? "").trim();
    const ownerName = String(formData.get("ownerName") ?? "").trim();
    const ownerEmail = String(formData.get("ownerEmail") ?? "").trim().toLowerCase();
    const contactPhone = String(formData.get("contactPhone") ?? "").trim();
    const cityRegion = String(formData.get("cityRegion") ?? "").trim();
    const notes = String(formData.get("notes") ?? "").trim();

    if (!restaurantName || !ownerName || !ownerEmail) {
      setErrorMessage("Informe restaurante, responsavel e email para pedir a liberacao.");
      return;
    }

    setErrorMessage("");
    setSuccessMessage("");

    startTransition(() => {
      void (async () => {
        try {
          const response = await createAccessRequest({
            restaurantName,
            legalName: legalName || undefined,
            ownerName,
            ownerEmail,
            contactPhone: contactPhone || undefined,
            cityRegion: cityRegion || undefined,
            notes: notes || undefined,
          });

          setSuccessMessage(response.message);
          (event.currentTarget as HTMLFormElement).reset();
        } catch (error) {
          if (error instanceof ApiError) {
            setErrorMessage(error.message);
            return;
          }

          setErrorMessage("Nao foi possivel enviar a solicitacao agora.");
        }
      })();
    });
  }

  return (
    <form className="module-form request-access-form" onSubmit={handleSubmit}>
      <div className="field-group">
        <label htmlFor="requestRestaurantName">Nome do restaurante</label>
        <input id="requestRestaurantName" name="restaurantName" placeholder="Ex.: Casa do Bairro" />
      </div>

      <div className="field-group">
        <label htmlFor="requestLegalName">Razao social</label>
        <input id="requestLegalName" name="legalName" placeholder="Opcional" />
      </div>

      <div className="module-inline-grid">
        <div className="field-group">
          <label htmlFor="requestOwnerName">Responsavel</label>
          <input id="requestOwnerName" name="ownerName" placeholder="Seu nome" />
        </div>
        <div className="field-group">
          <label htmlFor="requestOwnerEmail">Email</label>
          <input id="requestOwnerEmail" name="ownerEmail" type="email" placeholder="voce@empresa.com" />
        </div>
      </div>

      <div className="module-inline-grid">
        <div className="field-group">
          <label htmlFor="requestPhone">Telefone</label>
          <input id="requestPhone" name="contactPhone" placeholder="(11) 99999-0000" />
        </div>
        <div className="field-group">
          <label htmlFor="requestRegion">Cidade/Bairro</label>
          <input id="requestRegion" name="cityRegion" placeholder="Sua regiao" />
        </div>
      </div>

      <div className="field-group">
        <label htmlFor="requestNotes">Observacoes</label>
        <input id="requestNotes" name="notes" placeholder="Tipo de operacao, quantidade de mesas, observacoes..." />
      </div>

      {successMessage ? <p className="module-feedback success">{successMessage}</p> : null}
      {errorMessage ? <p className="module-feedback error">{errorMessage}</p> : null}

      <button className="ghost-link button-link" type="submit" disabled={isPending}>
        {isPending ? "Enviando..." : "Solicitar verificacao"}
      </button>
    </form>
  );
}
