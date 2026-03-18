import Link from "next/link";

const accessTypes = [
  {
    title: "Restaurante cliente",
    text: "Para donos, gerentes e equipe que usam o sistema no dia a dia.",
    hint: "Use email e senha cadastrados no sistema.",
  },
  {
    title: "Administracao ZeroPaper",
    text: "Para quem gerencia clientes, onboarding e operacao da plataforma.",
    hint: "Acesso interno da sua operacao.",
  },
];

export default function LoginPage() {
  return (
    <main className="page-shell">
      <section className="top-link-row">
        <Link className="ghost-link" href="/">
          Voltar para a apresentacao
        </Link>
      </section>

      <section className="login-layout">
        <section className="surface-card">
          <div className="section-title">
            <span className="eyebrow">Entrada</span>
            <h1 className="login-title">Escolha o tipo de acesso e entre no sistema</h1>
          </div>

          <div className="access-list">
            {accessTypes.map((item) => (
              <article key={item.title} className="access-card">
                <h2>{item.title}</h2>
                <p>{item.text}</p>
                <span>{item.hint}</span>
              </article>
            ))}
          </div>
        </section>

        <section className="surface-card login-form-card">
          <div className="section-title">
            <span className="eyebrow">Login</span>
            <h2>Entrar</h2>
          </div>

          <form className="login-form">
            <div className="field-group">
              <label htmlFor="accessType">Tipo de acesso</label>
              <select id="accessType" name="accessType" defaultValue="restaurant">
                <option value="restaurant">Restaurante cliente</option>
                <option value="admin">Administracao ZeroPaper</option>
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

            <button className="primary-link button-link" type="submit">
              Entrar
            </button>
          </form>
        </section>
      </section>
    </main>
  );
}
