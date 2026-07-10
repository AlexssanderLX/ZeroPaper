import Link from "next/link";
import { BrandMark } from "@/components/brand-mark";

const navLinks = [
  { label: "Segmentos", href: "/segmentos" },
  { label: "Contato", href: "/contato" },
  { label: "Sobre", href: "/sobre" },
];

export function PublicSiteHeader() {
  return (
    <>
      <input
        className="zpnav-toggle"
        id="zpnav-menu"
        type="checkbox"
        aria-hidden="true"
      />

      <header className="zpnav" role="banner">
        {/* Brand */}
        <Link className="zpnav-brand" href="/">
          <BrandMark small variant="full" priority />
          <span className="zpnav-brand-text">
            <strong>ZeroPaper</strong>
            <small>Plataforma modular</small>
          </span>
        </Link>

        {/* Desktop nav */}
        <nav className="zpnav-links" aria-label="Navegacao principal">
          {navLinks.map((link) => (
            <Link key={link.href} href={link.href} className="zpnav-link">
              {link.label}
            </Link>
          ))}
        </nav>

        {/* Desktop actions */}
        <div className="zpnav-actions">
          <Link className="zpnav-login" href="/login">
            Entrar
          </Link>
          <Link className="zpnav-cta" href="/cadastro?plano=operacao">
            Comecar agora
          </Link>
        </div>

        {/* Mobile hamburger */}
        <label className="zpnav-burger" htmlFor="zpnav-menu" aria-label="Abrir menu">
          <span /><span /><span />
        </label>
      </header>

      {/* Mobile drawer */}
      <label className="zpnav-backdrop" htmlFor="zpnav-menu" aria-hidden="true" />
      <aside className="zpnav-drawer" aria-label="Menu mobile">
        <div className="zpnav-drawer-head">
          <Link className="zpnav-brand" href="/">
            <BrandMark small variant="full" />
            <span className="zpnav-brand-text">
              <strong>ZeroPaper</strong>
              <small>Plataforma modular</small>
            </span>
          </Link>
          <label className="zpnav-drawer-close" htmlFor="zpnav-menu" aria-label="Fechar menu">
            ✕
          </label>
        </div>

        <nav className="zpnav-drawer-nav">
          {navLinks.map((link) => (
            <Link key={link.href} href={link.href} className="zpnav-drawer-link">
              {link.label}
            </Link>
          ))}
        </nav>

        <div className="zpnav-drawer-actions">
          <Link className="zpnav-drawer-login" href="/login">
            Entrar na conta
          </Link>
          <Link className="zpnav-cta zpnav-drawer-cta" href="/cadastro?plano=operacao">
            Comecar agora
          </Link>
        </div>
      </aside>
    </>
  );
}
