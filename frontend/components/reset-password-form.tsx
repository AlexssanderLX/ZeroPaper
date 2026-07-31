"use client";

import { FormEvent, useEffect, useState, useTransition } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { ApiError, resetPassword } from "@/lib/api";

export function ResetPasswordForm() {
  const router = useRouter();
  const [token, setToken] = useState("");
  const [isPending, startTransition] = useTransition();
  const [message, setMessage] = useState("");
  const [status, setStatus] = useState<"success" | "error" | null>(null);

  useEffect(() => {
    const params = new URLSearchParams(window.location.hash.replace(/^#/, ""));
    setToken(params.get("token")?.trim() ?? "");
    window.history.replaceState(null, "", "/redefinir-senha");
  }, []);

  function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    const formData = new FormData(event.currentTarget);
    const newPassword = String(formData.get("newPassword") ?? "").trim();
    const confirmPassword = String(formData.get("confirmPassword") ?? "").trim();

    if (!token) {
      setStatus("error");
      setMessage("O link de redefinicao esta incompleto ou expirou.");
      return;
    }

    if (newPassword.length < 8) {
      setStatus("error");
      setMessage("Crie uma senha com pelo menos 8 caracteres.");
      return;
    }

    if (newPassword !== confirmPassword) {
      setStatus("error");
      setMessage("As senhas precisam ser iguais.");
      return;
    }

    setMessage("");
    setStatus(null);

    startTransition(() => {
      void (async () => {
        try {
          await resetPassword({
            token,
            newPassword,
          });

          setStatus("success");
          setMessage("Senha atualizada com sucesso. Voce ja pode entrar com a nova senha.");
          setTimeout(() => router.replace("/login"), 900);
        } catch (error) {
          if (error instanceof ApiError) {
            setStatus("error");
            setMessage("Nao foi possivel redefinir a senha com esse link.");
            return;
          }

          setStatus("error");
          setMessage("Nao foi possivel redefinir a senha agora.");
        }
      })();
    });
  }

  return (
    <form className="login-form" onSubmit={handleSubmit}>
      <div className="field-group">
        <label className="field-label-row" htmlFor="newPassword">
          <span>Nova senha</span>
          <span className="field-requirement">Obrigatorio</span>
        </label>
        <input
          id="newPassword"
          name="newPassword"
          type="password"
          placeholder="Crie uma nova senha"
          required
          minLength={8}
        />
      </div>

      <div className="field-group">
        <label className="field-label-row" htmlFor="confirmPassword">
          <span>Confirmar senha</span>
          <span className="field-requirement">Obrigatorio</span>
        </label>
        <input
          id="confirmPassword"
          name="confirmPassword"
          type="password"
          placeholder="Repita a nova senha"
          required
          minLength={8}
        />
      </div>

      {message ? <p className={`module-feedback ${status ?? "success"}`}>{message}</p> : null}

      <button className="primary-link button-link" type="submit" disabled={isPending}>
        {isPending ? "Atualizando..." : "Salvar nova senha"}
      </button>

      <div className="form-link-row single">
        <Link className="ghost-link inline-link" href="/login">
          Voltar para o login
        </Link>
      </div>
    </form>
  );
}
