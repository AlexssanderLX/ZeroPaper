"use client";

import { FormEvent, useState, useTransition } from "react";
import { ApiError, requestPasswordReset } from "@/lib/api";

export function PasswordResetRequestForm() {
  const [isPending, startTransition] = useTransition();
  const [message, setMessage] = useState("");
  const [status, setStatus] = useState<"success" | "error" | null>(null);

  function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    const formData = new FormData(event.currentTarget);
    const email = String(formData.get("email") ?? "").trim().toLowerCase();

    if (!email) {
      setStatus("error");
      setMessage("Informe o email da unidade para continuar.");
      return;
    }

    setMessage("");
    setStatus(null);

    startTransition(() => {
      void (async () => {
        try {
          const response = await requestPasswordReset({ email });
          setStatus("success");
          setMessage(response.message);
        } catch (error) {
          if (error instanceof ApiError) {
            setStatus("error");
            setMessage(error.message);
            return;
          }

          setStatus("error");
          setMessage("Nao foi possivel enviar o link agora.");
        }
      })();
    });
  }

  return (
    <form className="login-form" onSubmit={handleSubmit}>
      <div className="field-group">
        <label htmlFor="resetRequestEmail">Email</label>
        <input id="resetRequestEmail" name="email" type="email" placeholder="voce@empresa.com" />
      </div>

      {message ? <p className={`module-feedback ${status ?? "success"}`}>{message}</p> : null}

      <button className="primary-link button-link" type="submit" disabled={isPending}>
        {isPending ? "Enviando..." : "Receber link de redefinicao"}
      </button>
    </form>
  );
}
