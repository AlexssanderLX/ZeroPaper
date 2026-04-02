import type { Metadata } from "next";
import { DM_Sans, Fraunces } from "next/font/google";
import "./globals.css";

const dmSans = DM_Sans({
  subsets: ["latin"],
  variable: "--font-body",
});

const fraunces = Fraunces({
  subsets: ["latin"],
  variable: "--font-display",
});

export const metadata: Metadata = {
  title: "ZeroPaper | Restaurantes",
  description: "Plataforma para restaurantes com pedidos, cozinha e operacao no mesmo fluxo.",
  manifest: "/site.webmanifest?v=20260328-1",
  icons: {
    icon: [
      { url: "/favicon.ico?v=20260328-1", sizes: "any" },
      { url: "/favicon-16x16.png?v=20260328-1", type: "image/png", sizes: "16x16" },
      { url: "/favicon-32x32.png?v=20260328-1", type: "image/png", sizes: "32x32" },
      { url: "/favicon-48x48.png?v=20260328-1", type: "image/png", sizes: "48x48" },
      { url: "/android-chrome-192x192.png?v=20260328-1", type: "image/png", sizes: "192x192" },
      { url: "/android-chrome-512x512.png?v=20260328-1", type: "image/png", sizes: "512x512" },
    ],
    shortcut: ["/favicon.ico?v=20260328-1"],
    apple: [{ url: "/apple-touch-icon.png?v=20260328-1", sizes: "180x180", type: "image/png" }],
  },
};

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    <html lang="pt-BR">
      <body className={`${dmSans.variable} ${fraunces.variable}`}>
        {children}
      </body>
    </html>
  );
}
