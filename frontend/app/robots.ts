import type { MetadataRoute } from "next";

const siteUrl = "https://zeropaperflow.com.br";

export default function robots(): MetadataRoute.Robots {
  return {
    rules: {
      userAgent: "*",
      allow: "/",
      disallow: [
        "/admin/",
        "/app/",
        "/login/",
        "/redefinir-senha/",
        "/redefinir-solicitacao/",
        "/cadastro/confirmacao/",
        "/acompanhar/",
        "/agendar/",
        "/d/",
        "/e/",
        "/imprimir/",
        "/media/",
        "/q/",
        "/v/",
      ],
    },
    sitemap: `${siteUrl}/sitemap.xml`,
    host: siteUrl,
  };
}
