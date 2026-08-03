import type { MetadataRoute } from "next";

const siteUrl = "https://zeropaperflow.com.br";

const publicPages = [
  { path: "", changeFrequency: "weekly", priority: 1 },
  { path: "/planos", changeFrequency: "weekly", priority: 0.9 },
  { path: "/segmentos", changeFrequency: "monthly", priority: 0.8 },
  { path: "/segmentos/restaurantes", changeFrequency: "monthly", priority: 0.8 },
  { path: "/segmentos/pet-shop", changeFrequency: "monthly", priority: 0.8 },
  { path: "/cadastro", changeFrequency: "monthly", priority: 0.7 },
  { path: "/sobre", changeFrequency: "monthly", priority: 0.6 },
  { path: "/contato", changeFrequency: "monthly", priority: 0.6 },
  { path: "/privacidade", changeFrequency: "yearly", priority: 0.3 },
] as const;

export default function sitemap(): MetadataRoute.Sitemap {
  return publicPages.map(({ path, changeFrequency, priority }) => ({
    url: `${siteUrl}${path}`,
    changeFrequency,
    priority,
  }));
}
