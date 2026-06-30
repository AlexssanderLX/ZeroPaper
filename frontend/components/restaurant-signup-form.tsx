"use client";

import { FormEvent, useRef, useState } from "react";
import { useRouter } from "next/navigation";
import { ApiError, createRestaurantSignup } from "@/lib/api";
import type { CommercialPlan } from "@/lib/commercial-plans";

export function RestaurantSignupForm({ selectedPlan }: { selectedPlan: CommercialPlan }) {
  const [isSubmitting, setIsSubmitting] = useState(false);
  const isSubmittingRef = useRef(false);
  const [errorMessage, setErrorMessage] = useState("");
  const router = useRouter();

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (isSubmittingRef.current) {
      return;
    }

    const formData = new FormData(event.currentTarget);
    const ownerName = String(formData.get("ownerName") ?? "").trim();
    const ownerEmail = String(formData.get("ownerEmail") ?? "").trim().toLowerCase();
    const contactPhone = String(formData.get("contactPhone") ?? "").trim();
    const ownerPassword = String(formData.get("ownerPassword") ?? "").trim();

    if (!ownerName || !ownerEmail || !contactPhone || !ownerPassword) {
      setErrorMessage("Preencha todos os campos para continuar.");
      return;
    }

    if (ownerPassword.length < 6) {
      setErrorMessage("Crie uma senha com pelo menos 6 caracteres.");
      return;
    }

    setErrorMessage("");
    setIsSubmitting(true);
    isSubmittingRef.current = true;

    try {
      await createRestaurantSignup({
        restaurantName: ownerName,
        legalName: ownerName,
        ownerName,
        ownerEmail,
        ownerPassword,
        contactPhone,
        planName: selectedPlan.name,
        monthlyPrice: selectedPlan.monthlyPrice,
        maxUsers: selectedPlan.maxUsers,
      });

      router.push("/cadastro/confirmacao");
    } catch (error) {
      isSubmittingRef.current = false;
      setIsSubmitting(false);
      if (error instanceof ApiError) {
        setErrorMessage(error.message);
        return;
      }
      setErrorMessage("Nao foi possivel enviar o pre-cadastro agora.");
    }
  }

  function onInvalid(event: FormEvent<HTMLInputElement>) {
    const input = event.currentTarget;
    input.setCustomValidity("");
    if (input.validity.valueMissing) input.setCustomValidity("Campo obrigatorio.");
    else if (input.validity.typeMismatch) input.setCustomValidity("Informe um email valido.");
  }

  function onInput(event: FormEvent<HTMLInputElement>) {
    event.currentTarget.setCustomValidity("");
  }

  return (
    <form className="login-form signup-form" onSubmit={handleSubmit}>
      <div className="field-group">
        <label className="field-label-row" htmlFor="ownerName">
          <span>Seu nome</span>
          <span className="field-requirement">Obrigatorio</span>
        </label>
        <input
          id="ownerName"
          name="ownerName"
          placeholder="Como quer ser chamado"
          required
          onInvalid={onInvalid}
          onInput={onInput}
        />
      </div>

      <div className="field-group">
        <label className="field-label-row" htmlFor="ownerEmail">
          <span>Email de acesso</span>
          <span className="field-requirement">Obrigatorio</span>
        </label>
        <input
          id="ownerEmail"
          name="ownerEmail"
          type="email"
          placeholder="voce@empresa.com"
          required
          onInvalid={onInvalid}
          onInput={onInput}
        />
      </div>

      <div className="field-group">
        <label className="field-label-row" htmlFor="contactPhone">
          <span>WhatsApp</span>
          <span className="field-requirement">Obrigatorio</span>
        </label>
        <input
          id="contactPhone"
          name="contactPhone"
          type="tel"
          placeholder="(11) 99999-0000"
          required
          onInvalid={onInvalid}
          onInput={onInput}
        />
      </div>

      <div className="field-group">
        <label className="field-label-row" htmlFor="ownerPassword">
          <span>Senha</span>
          <span className="field-requirement">Minimo 6 caracteres</span>
        </label>
        <input
          id="ownerPassword"
          name="ownerPassword"
          type="password"
          placeholder="Crie uma senha"
          required
          minLength={6}
          onInvalid={onInvalid}
          onInput={onInput}
        />
      </div>

      {errorMessage ? <p className="form-feedback">{errorMessage}</p> : null}

      <button className="primary-link button-link signup-submit" type="submit" disabled={isSubmitting}>
        {isSubmitting ? "Enviando..." : `Solicitar acesso — ${selectedPlan.name.replace("ZeroPaper ", "")}`}
      </button>

      <p className="signup-form-hint">
        Vou revisar e liberar seu acesso em breve.
      </p>
    </form>
  );
}
