import Link from "next/link";
import { BrandMark } from "@/components/brand-mark";
import { ResetPasswordForm } from "@/components/reset-password-form";

export default function ResetPasswordPage() {
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
              <strong>Nova senha</strong>
            </div>
          </div>

          <h1 className="login-title">Crie uma nova senha para voltar ao sistema</h1>
          <p className="body-copy">
            O link enviado por email expira rapido e derruba acessos antigos depois da troca para manter a conta protegida.
          </p>
        </section>

        <section className="surface-card login-form-card">
          <span className="eyebrow">Senha</span>
          <h2 className="form-title">Atualizar acesso</h2>
          <ResetPasswordForm />
        </section>
      </section>
    </main>
  );
}
