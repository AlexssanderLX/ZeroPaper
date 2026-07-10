"use client";

import { useState } from "react";

type State = "idle" | "sending" | "sent" | "error";

export function ContactForm() {
  const [state, setState] = useState<State>("idle");
  const [errorMsg, setErrorMsg] = useState("");

  async function handleSubmit(e: React.FormEvent<HTMLFormElement>) {
    e.preventDefault();
    if (state === "sending" || state === "sent") return;

    setState("sending");
    const form = e.currentTarget;
    const data = {
      email: (form.elements.namedItem("email") as HTMLInputElement).value,
      phone: (form.elements.namedItem("phone") as HTMLInputElement).value,
      message: (form.elements.namedItem("message") as HTMLTextAreaElement).value,
    };

    try {
      const res = await fetch("/api/public/contact", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(data),
      });
      if (res.ok) {
        setState("sent");
      } else {
        const body = await res.json().catch(() => ({}));
        setErrorMsg(body.info ?? "Erro ao enviar. Tente novamente.");
        setState("error");
      }
    } catch {
      setErrorMsg("Sem conexao. Verifique sua internet e tente novamente.");
      setState("error");
    }
  }

  if (state === "sent") {
    return (
      <div className="zpld-contact-success">
        <svg width="36" height="36" viewBox="0 0 36 36" fill="none" stroke="var(--zpld-green)" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
          <circle cx="18" cy="18" r="16" />
          <path d="M11 18l5 5 9-9" />
        </svg>
        <strong>Mensagem enviada!</strong>
        <p>Responderei em breve pelo seu email.</p>
      </div>
    );
  }

  return (
    <form className="zpld-contact-form" onSubmit={handleSubmit} noValidate>
      <div className="zpld-contact-row">
        <label className="zpld-contact-field">
          <span>Seu email <em>*</em></span>
          <input
            name="email"
            type="email"
            placeholder="voce@email.com"
            required
            disabled={state === "sending"}
          />
        </label>
        <label className="zpld-contact-field">
          <span>Telefone / WhatsApp</span>
          <input
            name="phone"
            type="tel"
            placeholder="(11) 99999-0000"
            disabled={state === "sending"}
          />
        </label>
      </div>

      <label className="zpld-contact-field">
        <span>Mensagem <em>*</em></span>
        <textarea
          name="message"
          rows={5}
          placeholder="Conte um pouco sobre seu negocio ou tire sua duvida..."
          required
          disabled={state === "sending"}
        />
      </label>

      {state === "error" && (
        <p className="zpld-contact-error">{errorMsg}</p>
      )}

      <button
        type="submit"
        className="zpld-btn zpld-btn-primary"
        disabled={state === "sending"}
        style={{ width: "100%" }}
      >
        {state === "sending" ? "Enviando..." : "Enviar mensagem →"}
      </button>
    </form>
  );
}
