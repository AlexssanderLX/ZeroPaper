import type { Metadata } from "next";
import Link from "next/link";
import { PublicSiteHeader } from "@/components/public-site-header";
import { ElectricBg } from "@/components/electric-bg";
import { PrivacyMobileToc } from "@/components/privacy-mobile-toc";

export const metadata: Metadata = {
  title: "Politica de Privacidade | ZeroPaper",
  description: "Como o ZeroPaper coleta, usa e protege seus dados. Seus direitos garantidos pela LGPD.",
  alternates: { canonical: "/privacidade" },
};

const toc = [
  { id: "s1",  label: "Quem somos" },
  { id: "s2",  label: "Dados coletados" },
  { id: "s3",  label: "Finalidade e base legal" },
  { id: "s4",  label: "Compartilhamento" },
  { id: "s5",  label: "Armazenamento e seguranca" },
  { id: "s6",  label: "Retencao de dados" },
  { id: "s7",  label: "Seus direitos (LGPD)" },
  { id: "s8",  label: "Cookies" },
  { id: "s9",  label: "Menores de idade" },
  { id: "s10", label: "Alteracoes" },
  { id: "s11", label: "Contato" },
];

const rights = [
  { icon: "👁", title: "Acesso",          desc: "Saber quais dados temos sobre voce e como os usamos." },
  { icon: "✏️", title: "Retificacao",     desc: "Corrigir dados incompletos, inexatos ou desatualizados." },
  { icon: "🗑", title: "Eliminacao",      desc: "Pedir a exclusao de dados tratados com base em consentimento." },
  { icon: "📦", title: "Portabilidade",   desc: "Receber seus dados em formato estruturado e transferivel." },
  { icon: "ℹ️", title: "Informacao",      desc: "Saber com quais entidades compartilhamos seus dados." },
  { icon: "🚫", title: "Oposicao",        desc: "Opor-se a tratamento que viola a legislacao vigente." },
  { icon: "🔓", title: "Revogacao",       desc: "Retirar o consentimento dado anteriormente a qualquer momento." },
  { icon: "⚖️", title: "Peticao",         desc: "Peticionar a ANPD contra o controlador de seus dados." },
];

export default function PrivacidadePage() {
  return (
    <main className="zpld zpld-privacy" id="privacidade-page">
      <ElectricBg />

      <div className="zpld-bg" aria-hidden="true">
        <span className="zpld-orb zpld-orb-b" />
        <div className="zpld-grid" />
      </div>

      <PublicSiteHeader />

      {/* ── HERO ───────────────────────────────────────────── */}
      <section className="zpld-priv-hero">
        <div className="zpld-priv-hero-inner">
          <Link href="/sobre" className="zpld-breadcrumb">← Sobre o ZeroPaper</Link>

          <div className="zpld-priv-shield" aria-hidden="true">
            <svg width="56" height="56" viewBox="0 0 56 56" fill="none">
              <path d="M28 4L6 13v14c0 15.8 9.3 25.8 22 29 12.7-3.2 22-13.2 22-29V13L28 4z"
                fill="rgba(109,223,157,0.1)" stroke="#6ddf9d" strokeWidth="1.5" />
              <path d="M20 28l5.5 5.5L36 23" stroke="#6ddf9d" strokeWidth="2"
                strokeLinecap="round" strokeLinejoin="round" />
            </svg>
          </div>

          <div className="zpld-priv-badge">LGPD · Lei 13.709/2018</div>

          <h1 className="zpld-priv-h1">Politica de Privacidade</h1>
          <p className="zpld-priv-sub">
            Transparencia total sobre como tratamos seus dados.<br />
            Seus direitos sao nossa responsabilidade.
          </p>

          <div className="zpld-priv-meta">
            <span><strong>Versao:</strong> 1.0</span>
            <span className="zpld-priv-sep" />
            <span><strong>Vigencia:</strong> julho de 2026</span>
            <span className="zpld-priv-sep" />
            <span><strong>Controlador:</strong> ZeroPaper</span>
          </div>
        </div>
      </section>

      {/* TOC mobile sticky */}
      <PrivacyMobileToc />

      {/* ── LAYOUT: TOC + CONTEUDO ─────────────────────────── */}
      <div className="zpld-priv-layout">

        {/* Sidebar TOC */}
        <aside className="zpld-priv-toc" aria-label="Indice">
          <p className="zpld-priv-toc-label">Nesta pagina</p>
          <nav>
            {toc.map((t, i) => (
              <a key={t.id} href={`#${t.id}`} className="zpld-priv-toc-item">
                <span className="zpld-priv-toc-n">{String(i + 1).padStart(2, "0")}</span>
                {t.label}
              </a>
            ))}
          </nav>
        </aside>

        {/* Artigos */}
        <div className="zpld-priv-content">

          <article id="s1" className="zpld-priv-section">
            <div className="zpld-priv-section-tag">01</div>
            <h2>Quem somos</h2>
            <p>
              O <strong>ZeroPaper</strong> e uma plataforma SaaS desenvolvida e operada por
              Alexssander Ferreira de Almeida, com sede no Brasil. Atuamos como
              <strong> controlador dos dados</strong> dos usuarios que se cadastram diretamente
              na plataforma. Os dados de clientes finais de cada negocio (pedidos, mesas,
              consumo) sao de responsabilidade do proprio estabelecimento, que atua como
              controlador independente.
            </p>
            <p>
              Nos reservamos o papel de <strong>operador</strong> ao processar esses dados
              em nome do negocio conforme instruções do proprio estabelecimento.
            </p>
          </article>

          <article id="s2" className="zpld-priv-section">
            <div className="zpld-priv-section-tag">02</div>
            <h2>Dados que coletamos</h2>
            <div className="zpld-priv-table-wrap">
              <table className="zpld-priv-table">
                <thead>
                  <tr><th>Categoria</th><th>Exemplos</th><th>Origem</th></tr>
                </thead>
                <tbody>
                  <tr>
                    <td>Identificacao</td>
                    <td>Nome, e-mail, telefone</td>
                    <td>Cadastro</td>
                  </tr>
                  <tr>
                    <td>Acesso</td>
                    <td>Senha (hash), IP, dispositivo</td>
                    <td>Login</td>
                  </tr>
                  <tr>
                    <td>Negocio</td>
                    <td>Nome do estabelecimento, CNPJ, segmento</td>
                    <td>Configuracao</td>
                  </tr>
                  <tr>
                    <td>Operacao</td>
                    <td>Pedidos, mesas, cardapio, movimentos de caixa</td>
                    <td>Uso da plataforma</td>
                  </tr>
                  <tr>
                    <td>Pagamento</td>
                    <td>Registro de transacoes (sem dados de cartao)</td>
                    <td>Integracao Mercado Pago</td>
                  </tr>
                  <tr>
                    <td>Tecnico</td>
                    <td>Logs de erro, horarios de acesso, versao do app</td>
                    <td>Automatico</td>
                  </tr>
                </tbody>
              </table>
            </div>
            <p className="zpld-priv-note">
              Nao coletamos dados sensiveis (origem racial, saude, biometria, dados religiosos
              ou politicos) nem dados de menores de 18 anos.
            </p>
          </article>

          <article id="s3" className="zpld-priv-section">
            <div className="zpld-priv-section-tag">03</div>
            <h2>Finalidade e base legal</h2>
            <div className="zpld-priv-pills-grid">
              {[
                { base: "Execucao de contrato", fin: "Criar e manter sua conta, processar pedidos e operar os modulos contratados." },
                { base: "Interesse legitimo", fin: "Prevencao de fraudes, seguranca da plataforma, suporte tecnico e melhorias no produto." },
                { base: "Obrigacao legal", fin: "Cumprimento de obrigacoes fiscais, tributarias e determinacoes de autoridades competentes." },
                { base: "Consentimento", fin: "Envio de comunicacoes de marketing, novidades e atualizacoes do produto (revogavel a qualquer momento)." },
              ].map((r) => (
                <div key={r.base} className="zpld-priv-pill">
                  <span className="zpld-priv-pill-label">{r.base}</span>
                  <p>{r.fin}</p>
                </div>
              ))}
            </div>
          </article>

          <article id="s4" className="zpld-priv-section">
            <div className="zpld-priv-section-tag">04</div>
            <h2>Compartilhamento de dados</h2>
            <p>
              Seus dados <strong>nao sao vendidos</strong>. Compartilhamos apenas quando
              necessario para operar o servico:
            </p>
            <ul className="zpld-priv-list">
              <li><strong>Mercado Pago</strong> — processamento de pagamentos via Checkout Pro. Sujeito a Politica de Privacidade do Mercado Pago.</li>
              <li><strong>Provedor de hospedagem (VPS)</strong> — armazenamento dos dados em servidor dedicado no Brasil.</li>
              <li><strong>Evolution API / WhatsApp</strong> — envio de notificacoes operacionais ao estabelecimento. Apenas os dados necessarios para envio da mensagem.</li>
              <li><strong>Autoridades publicas</strong> — quando exigido por lei, ordem judicial ou regulacao aplicavel.</li>
            </ul>
            <p>
              Todos os terceiros sao contratualmente obrigados a proteger seus dados e a
              utiliza-los apenas para a finalidade especifica para a qual foram compartilhados.
            </p>
          </article>

          <article id="s5" className="zpld-priv-section">
            <div className="zpld-priv-section-tag">05</div>
            <h2>Armazenamento e seguranca</h2>
            <div className="zpld-priv-security-grid">
              {[
                { icon: "🔐", title: "Criptografia em transito", desc: "TLS 1.2+ em todas as conexoes entre cliente e servidor." },
                { icon: "🔑", title: "Senhas com hash", desc: "Nenhuma senha e armazenada em texto plano. Utilizamos hash seguro com salt." },
                { icon: "🛡", title: "Controle de acesso", desc: "Cada usuario acessa apenas os dados do seu proprio workspace." },
                { icon: "📋", title: "Logs de auditoria", desc: "Acoes administrativas sao registradas com timestamp e IP." },
                { icon: "💾", title: "Backups regulares", desc: "Backups automatizados do banco de dados com retencao de seguranca." },
                { icon: "🌐", title: "Servidor no Brasil", desc: "Dados hospedados em VPS dedicado com acesso restrito via SSH." },
              ].map((i) => (
                <div key={i.title} className="zpld-priv-sec-card">
                  <span>{i.icon}</span>
                  <strong>{i.title}</strong>
                  <p>{i.desc}</p>
                </div>
              ))}
            </div>
          </article>

          <article id="s6" className="zpld-priv-section">
            <div className="zpld-priv-section-tag">06</div>
            <h2>Retencao de dados</h2>
            <p>
              Mantemos seus dados enquanto sua conta estiver ativa ou pelo periodo necessario
              para cumprir obrigacoes legais.
            </p>
            <ul className="zpld-priv-list">
              <li><strong>Dados de conta:</strong> mantidos durante a vigencia do contrato e por ate 5 anos apos o encerramento (obrigacao fiscal).</li>
              <li><strong>Dados operacionais (pedidos, caixa):</strong> mantidos enquanto o estabelecimento estiver ativo; exportaveis a pedido.</li>
              <li><strong>Logs de acesso:</strong> retidos por 6 meses para fins de seguranca e auditoria.</li>
              <li><strong>Apos solicitacao de exclusao:</strong> eliminamos os dados em ate 30 dias, exceto os que tenhamos obrigacao legal de manter.</li>
            </ul>
          </article>

          {/* ── DIREITOS: secao destacada ─────────── */}
          <article id="s7" className="zpld-priv-section zpld-priv-rights-section">
            <div className="zpld-priv-section-tag zpld-priv-section-tag-green">07</div>
            <div className="zpld-priv-rights-head">
              <h2>Seus direitos garantidos pela LGPD</h2>
              <p>
                A Lei Geral de Protecao de Dados (Lei 13.709/2018) garante a voce os
                seguintes direitos. Para exercer qualquer um deles, entre em contato conosco.
                Respondemos em ate <strong>15 dias uteis</strong>.
              </p>
            </div>
            <div className="zpld-priv-rights-grid">
              {rights.map((r) => (
                <div key={r.title} className="zpld-priv-right-card">
                  <span className="zpld-priv-right-icon">{r.icon}</span>
                  <strong>{r.title}</strong>
                  <p>{r.desc}</p>
                </div>
              ))}
            </div>
            <div className="zpld-priv-rights-cta">
              <Link href="/contato" className="zpld-btn-primary">
                Exercer um direito →
              </Link>
            </div>
          </article>

          <article id="s8" className="zpld-priv-section">
            <div className="zpld-priv-section-tag">08</div>
            <h2>Cookies e tecnologias similares</h2>
            <p>Utilizamos apenas cookies tecnicos e essenciais:</p>
            <ul className="zpld-priv-list">
              <li><strong>Cookie de sessao:</strong> mantém seu login ativo durante o uso do painel. Expirado ao fechar o navegador ou apos inatividade.</li>
              <li><strong>Token de autenticacao:</strong> armazenado em cookie HttpOnly com flag Secure para prevenir acesso via JavaScript.</li>
            </ul>
            <p className="zpld-priv-note">
              Nao utilizamos cookies de rastreamento, analytics de terceiros, pixels de
              redes sociais ou qualquer tecnologia de perfilamento comportamental.
            </p>
          </article>

          <article id="s9" className="zpld-priv-section">
            <div className="zpld-priv-section-tag">09</div>
            <h2>Menores de idade</h2>
            <p>
              O ZeroPaper e destinado exclusivamente a maiores de 18 anos. Nao coletamos
              intencionalmente dados de menores. Caso identifiquemos que dados de um menor
              foram coletados sem o devido consentimento parental, procederemos com a
              eliminacao imediata. Se voce acredita que isso ocorreu, entre em contato.
            </p>
          </article>

          <article id="s10" className="zpld-priv-section">
            <div className="zpld-priv-section-tag">10</div>
            <h2>Alteracoes nesta politica</h2>
            <p>
              Podemos atualizar esta politica periodicamente. Alteracoes relevantes serao
              comunicadas por e-mail ou por aviso visivel na plataforma com antecedencia
              minima de 15 dias. O uso continuado apos a vigencia da nova versao implica
              aceite dos termos atualizados.
            </p>
            <p>
              O historico de versoes ficara disponivel para consulta mediante solicitacao.
            </p>
          </article>

          <article id="s11" className="zpld-priv-section">
            <div className="zpld-priv-section-tag">11</div>
            <h2>Contato e Encarregado de Dados</h2>
            <p>
              Para solicitacoes relacionadas a privacidade, exercicio de direitos ou duvidas
              sobre esta politica, entre em contato:
            </p>
            <div className="zpld-priv-contact-box">
              <div className="zpld-priv-contact-item">
                <span>✉️</span>
                <div>
                  <strong>E-mail</strong>
                  <a href="mailto:alexssander.f.almeida2006@gmail.com">alexssander.f.almeida2006@gmail.com</a>
                </div>
              </div>
              <div className="zpld-priv-contact-item">
                <span>💬</span>
                <div>
                  <strong>WhatsApp</strong>
                  <a href="https://wa.me/5511977936534" target="_blank" rel="noopener noreferrer">+55 11 97793-6534</a>
                </div>
              </div>
              <div className="zpld-priv-contact-item">
                <span>🏢</span>
                <div>
                  <strong>Autoridade Nacional</strong>
                  <a href="https://www.gov.br/anpd" target="_blank" rel="noopener noreferrer">ANPD — gov.br/anpd</a>
                </div>
              </div>
            </div>
          </article>

        </div>
      </div>

      {/* ── CTA FINAL ─────────────────────────────────────── */}
      <div className="zpld-priv-footer-cta">
        <p>Tem duvidas sobre seus dados?</p>
        <Link href="/contato" className="zpld-btn-primary">Falar com a equipe</Link>
        <Link href="/sobre" className="zpld-btn-ghost">Voltar ao inicio</Link>
      </div>

    </main>
  );
}
