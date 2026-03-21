import Link from "next/link";
import { BrandMark } from "@/components/brand-mark";
import { PasswordResetRequestForm } from "@/components/password-reset-request-form";

export default function PasswordResetRequestPage() {
  return (
    <main className="page-shell">
      <section className="top-link-row">
        <Link className="ghost-link" href="/login">
          Voltar para o login
        </Link>
      </section>

      <section className="login-layout">
        <section className="surface-card login-copy-card ambient-panel subtle">
          <div className="brand-lockup compact">
            <BrandMark small variant="full" />
            <div className="brand-copy">
              <span className="eyebrow">ZeroPaper</span>
              <strong>Recuperar acesso</strong>
            </div>
          </div>

          <h1 className="login-title">Receba um link seguro para redefinir a senha</h1>
          <p className="body-copy">
            Informe o email da unidade e, se ele estiver cadastrado, a ZeroPaper envia um link temporario para criar uma nova senha.
          </p>
        </section>

        <section className="surface-card login-form-card">
          <span className="eyebrow">Senha</span>
          <h2 className="form-title">Redefinir acesso</h2>
          <PasswordResetRequestForm />
        </section>
      </section>
    </main>
  );
}
