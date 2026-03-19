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
  const [isAccessDenied, setIsAccessDenied] = useState(false);

  function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    const formData = new FormData(event.currentTarget);
    const email = String(formData.get("email") ?? "").trim().toLowerCase();
    const password = String(formData.get("password") ?? "").trim();
    const profile = String(formData.get("accessType") ?? "restaurant") as AccessProfile;

    if (!email || !password) {
      setIsAccessDenied(false);
      setErrorMessage("Preencha email e senha para continuar.");
      return;
    }

    setIsAccessDenied(false);
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
          if (error instanceof ApiError && error.status === 403) {
            setIsAccessDenied(true);
            setErrorMessage("Acesso negado. Entre em contato com a ZeroPaper.");
            return;
          }

          if (error instanceof ApiError && error.status === 401) {
            setErrorMessage("Email ou senha invalidos.");
            return;
          }

          setErrorMessage("Nao foi possivel entrar agora.");
        }
      })();
    });
  }

  function handleRequiredInvalid(event: FormEvent<HTMLInputElement | HTMLSelectElement>) {
    const input = event.currentTarget;
    input.setCustomValidity("");

    if (input.validity.valueMissing) {
      input.setCustomValidity("Campo obrigatorio.");
      return;
    }

    if ("type" in input && input.type === "email" && input.validity.typeMismatch) {
      input.setCustomValidity("Informe um email valido.");
    }
  }

  function clearRequiredMessage(event: FormEvent<HTMLInputElement | HTMLSelectElement>) {
    event.currentTarget.setCustomValidity("");
  }

  return (
    <form className="login-form" onSubmit={handleSubmit}>
      <p className="login-form-note">Use o email e a senha liberados para o seu acesso.</p>

      <div className="field-group">
        <label className="field-label-row" htmlFor="accessType">
          <span>Perfil</span>
          <span className="field-requirement">Obrigatorio</span>
        </label>
        <select
          id="accessType"
          name="accessType"
          defaultValue="restaurant"
          required
          onInvalid={handleRequiredInvalid}
          onInput={clearRequiredMessage}
        >
          <option value="restaurant">Unidade</option>
          <option value="admin">Operacao</option>
        </select>
      </div>

      <div className="field-group">
        <label className="field-label-row" htmlFor="email">
          <span>Email</span>
          <span className="field-requirement">Obrigatorio</span>
        </label>
        <input
          id="email"
          name="email"
          type="email"
          placeholder="voce@empresa.com"
          required
          autoComplete="email"
          onInvalid={handleRequiredInvalid}
          onInput={clearRequiredMessage}
        />
      </div>

      <div className="field-group">
        <label className="field-label-row" htmlFor="password">
          <span>Senha</span>
          <span className="field-requirement">Obrigatorio</span>
        </label>
        <input
          id="password"
          name="password"
          type="password"
          placeholder="Sua senha"
          required
          autoComplete="current-password"
          onInvalid={handleRequiredInvalid}
          onInput={clearRequiredMessage}
        />
      </div>

      {errorMessage ? <p className="form-feedback">{errorMessage}</p> : null}

      {isAccessDenied ? (
        <a
          className="ghost-link inline-link"
          href="mailto:alexssander.f.almeida2006@gmail.com?subject=ZeroPaper%20-%20reativacao%20de%20acesso"
        >
          Entrar em contato com a ZeroPaper
        </a>
      ) : null}

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
