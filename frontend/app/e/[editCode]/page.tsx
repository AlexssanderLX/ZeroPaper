export default async function EditShortLinkPage() {
  return (
    <main className="page-shell public-shell">
      <section className="surface-card public-card ambient-panel public-menu-card">
        <div className="public-table-header">
          <span className="eyebrow">Pedido protegido</span>
          <h1 className="public-title">A edicao por link foi desativada</h1>
          <p className="public-title-support">
            O pedido confirmado agora entra direto no fluxo da unidade. Para corrigir algum detalhe,
            fale com o atendimento da loja ou monte um novo pedido pelo link oficial.
          </p>
        </div>

        <div className="surface-card public-success-card public-order-complete">
          <span className="eyebrow">Operacao segura</span>
          <h2>O ZeroPaper nao altera pedidos ja enviados por link externo.</h2>
          <p>
            Essa regra evita duplicidade e confusao entre cozinha, caixa e entrega.
          </p>
          <div className="toolbar-actions public-success-actions">
            <a className="primary-link button-link" href="/">
              Voltar ao inicio
            </a>
          </div>
        </div>
      </section>
    </main>
  );
}
