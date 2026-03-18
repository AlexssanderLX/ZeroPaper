"use client";

import { FormEvent, useState, useTransition } from "react";
import { useRouter } from "next/navigation";
import {
  buildOwnerName,
  PORTAL_SESSION_KEY,
  type AccessProfile,
  type PortalSession,
} from "@/lib/owner-portal";

export function LoginAccessForm() {
  const router = useRouter();
  const [isPending, startTransition] = useTransition();
  const [errorMessage, setErrorMessage] = useState("");

  function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    const formData = new FormData(event.currentTarget);
    const email = String(formData.get("email") ?? "").trim().toLowerCase();
    const password = String(formData.get("password") ?? "").trim();
    const profile = String(formData.get("accessType") ?? "restaurant") as AccessProfile;

    if (!email || !password) {
      setErrorMessage("Preencha email e senha para continuar.");
      return;
    }

    setErrorMessage("");

    const ownerName = buildOwnerName(email) || "Responsavel da unidade";
    const restaurantName =
      profile === "admin"
        ? "Operacao ZeroPaper"
        : email === "teste.bairro@zeropaper.local"
          ? "Restaurante Teste Bairro"
          : `Unidade ${ownerName}`;

    const session: PortalSession = {
      email,
      profile,
      ownerName,
      restaurantName,
    };

    startTransition(() => {
      window.sessionStorage.setItem(PORTAL_SESSION_KEY, JSON.stringify(session));
      router.replace("/app");
    });
  }

  return (
    <form className="login-form" onSubmit={handleSubmit}>
      <div className="field-group">
        <label htmlFor="accessType">Perfil</label>
        <select id="accessType" name="accessType" defaultValue="restaurant">
          <option value="restaurant">Unidade</option>
          <option value="admin">Operacao</option>
        </select>
      </div>

      <div className="field-group">
        <label htmlFor="email">Email</label>
        <input id="email" name="email" type="email" placeholder="voce@empresa.com" />
      </div>

      <div className="field-group">
        <label htmlFor="password">Senha</label>
        <input id="password" name="password" type="password" placeholder="Sua senha" />
      </div>

      {errorMessage ? <p className="form-feedback">{errorMessage}</p> : null}

      <button className="primary-link button-link" type="submit" disabled={isPending}>
        {isPending ? "Entrando..." : "Entrar"}
      </button>
    </form>
  );
}
