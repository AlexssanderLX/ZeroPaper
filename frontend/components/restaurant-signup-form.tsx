"use client";

import { FormEvent, useRef, useState } from "react";
import { useRouter } from "next/navigation";
import { ApiError, createRestaurantSignup, type PublicCommercialPlan } from "@/lib/api";

type Props = { selectedPlan: PublicCommercialPlan; segment: 1 | 2 };

export function RestaurantSignupForm({ selectedPlan, segment }: Props) {
  const [isSubmitting, setIsSubmitting] = useState(false);
  const isSubmittingRef = useRef(false);
  const [errorMessage, setErrorMessage] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");
  const router = useRouter();

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (isSubmittingRef.current) return;

    const formData = new FormData(event.currentTarget);
    const businessName = String(formData.get("businessName") ?? "").trim();
    const ownerName = String(formData.get("ownerName") ?? "").trim();
    const ownerEmail = String(formData.get("ownerEmail") ?? "").trim().toLowerCase();
    const contactPhone = String(formData.get("contactPhone") ?? "").trim();
    const ownerPassword = String(formData.get("ownerPassword") ?? "").trim();
    const submitter = (event.nativeEvent as SubmitEvent).submitter as HTMLButtonElement | null;
    const registrationFlow = submitter?.value === "pay_now" ? "pay_now" : "pre_registration";

    if (!businessName || !ownerName || !ownerEmail || !contactPhone || !ownerPassword) {
      setErrorMessage("Preencha todos os campos para continuar.");
      return;
    }
    if (ownerPassword.length < 6) {
      setErrorMessage("Crie uma senha com pelo menos 6 caracteres.");
      return;
    }
    if (ownerPassword !== confirmPassword) {
      setErrorMessage("As senhas nao coincidem. Verifique e tente novamente.");
      return;
    }

    setErrorMessage("");
    setIsSubmitting(true);
    isSubmittingRef.current = true;

    try {
      const response = await createRestaurantSignup({
        businessSegment: segment,
        planKey: selectedPlan.key,
        restaurantName: businessName,
        legalName: businessName,
        ownerName,
        ownerEmail,
        ownerPassword,
        contactPhone,
        registrationFlow,
      });
      if (response.checkoutUrl) {
        window.location.assign(response.checkoutUrl);
        return;
      }
      router.push(`/cadastro/confirmacao?segmento=${segment === 2 ? "petshop" : "restaurante"}`);
    } catch (error) {
      isSubmittingRef.current = false;
      setIsSubmitting(false);
      setErrorMessage(error instanceof ApiError ? error.message : "Nao foi possivel enviar o pre-cadastro agora.");
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

  const requiredProps = { required: true, onInvalid, onInput };
  const shortPlanName = selectedPlan.name.replace("ZeroPaper Pet ", "").replace("ZeroPaper ", "");

  return (
    <form className="login-form signup-form" onSubmit={handleSubmit}>
      <div className="field-group">
        <label className="field-label-row" htmlFor="businessName">
          <span>Nome do {segment === 2 ? "pet shop" : "restaurante"}</span>
          <span className="field-requirement">Obrigatorio</span>
        </label>
        <input id="businessName" name="businessName" placeholder={segment === 2 ? "Ex.: Pet Feliz" : "Ex.: Sabor da Casa"} {...requiredProps} />
      </div>
      <div className="field-group">
        <label className="field-label-row" htmlFor="ownerName">
          <span>Seu nome</span>
          <span className="field-requirement">Obrigatorio</span>
        </label>
        <input id="ownerName" name="ownerName" placeholder="Como quer ser chamado" {...requiredProps} />
      </div>
      <div className="field-group">
        <label className="field-label-row" htmlFor="ownerEmail">
          <span>Email de acesso</span>
          <span className="field-requirement">Obrigatorio</span>
        </label>
        <input id="ownerEmail" name="ownerEmail" type="email" placeholder="voce@empresa.com" {...requiredProps} />
      </div>
      <div className="field-group">
        <label className="field-label-row" htmlFor="contactPhone">
          <span>WhatsApp</span>
          <span className="field-requirement">Obrigatorio</span>
        </label>
        <input id="contactPhone" name="contactPhone" type="tel" placeholder="(11) 99999-0000" {...requiredProps} />
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
          minLength={6}
          {...requiredProps}
        />
      </div>
      <div className="field-group">
        <label className="field-label-row" htmlFor="confirmPassword">
          <span>Confirmar senha</span>
          <span className="field-requirement">Repita a senha</span>
        </label>
        <input
          id="confirmPassword"
          name="confirmPassword"
          type="password"
          placeholder="Repita a senha"
          minLength={6}
          value={confirmPassword}
          onChange={(e) => setConfirmPassword(e.target.value)}
          required
        />
      </div>
      {errorMessage ? <p className="form-feedback" role="alert">{errorMessage}</p> : null}
      <button className="primary-link button-link signup-submit" type="submit" value="pay_now" disabled={isSubmitting}>
        {isSubmitting ? "Processando..." : `Pagar e liberar — ${shortPlanName}`}
      </button>
      <button className="ghost-link button-link signup-submit" type="submit" value="pre_registration" disabled={isSubmitting}>
        {isSubmitting ? "Enviando..." : `Solicitar acesso — ${shortPlanName}`}
      </button>
      <p className="signup-form-hint">O plano, valor e modulos serao confirmados com seguranca pelo servidor.</p>
    </form>
  );
}
