"use client";

import { FormEvent, useState, useTransition } from "react";
import { useRouter } from "next/navigation";
import Link from "next/link";
import { ApiError, loginPortal } from "@/lib/api";
import { PORTAL_SESSION_KEY, type AccessProfile, type PortalSession } from "@/lib/owner-portal";

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

    startTransition(() => {
      void (async () => {
        try {
          const response = await loginPortal({
            email,
            password,
            profile,
          });

          const session: PortalSession = {
            token: response.token,
            expiresAtUtc: response.expiresAtUtc,
            email: response.email,
            profile,
            ownerName: response.ownerName,
            restaurantName: response.restaurantName,
            role: response.role,
          };

          window.sessionStorage.setItem(PORTAL_SESSION_KEY, JSON.stringify(session));
          router.replace(profile === "admin" ? "/admin" : "/app");
        } catch (error) {
          if (error instanceof ApiError && error.status === 401) {
            setErrorMessage("Email ou senha invalidos.");
            return;
          }

          setErrorMessage("Nao foi possivel entrar agora.");
        }
      })();
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

      <div className="form-link-row">
        <Link className="ghost-link inline-link" href="/redefinir-solicitacao">
          Esqueci minha senha
        </Link>
        <Link className="ghost-link inline-link" href="/cadastro">
          Cadastrar unidade
        </Link>
      </div>
    </form>
  );
}
