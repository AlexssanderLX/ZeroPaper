import type { Metadata } from "next";
import { DM_Sans, Fraunces } from "next/font/google";
import "./globals.css";

const dmSans = DM_Sans({
  subsets: ["latin"],
  variable: "--font-body",
  display: "swap",
});

const fraunces = Fraunces({
  subsets: ["latin"],
  variable: "--font-display",
  display: "swap",
});

export const metadata: Metadata = {
  metadataBase: new URL("https://zeropaperflow.com.br"),
  title: "ZeroPaper | Plataforma modular para pequenos negocios",
  description: "Pedidos, atendimento, caixa e operacao em um so fluxo. Configuravel por modulos para restaurantes, varejo, pet shops e mais.",
  manifest: "/site.webmanifest?v=20260328-1",
  icons: {
    icon: [
      { url: "/favicon.ico?v=20260704-2", sizes: "any" },
      { url: "/favicon-16x16.png?v=20260704-2", type: "image/png", sizes: "16x16" },
      { url: "/favicon-32x32.png?v=20260704-2", type: "image/png", sizes: "32x32" },
      { url: "/favicon-48x48.png?v=20260704-2", type: "image/png", sizes: "48x48" },
      { url: "/android-chrome-192x192.png?v=20260704-2", type: "image/png", sizes: "192x192" },
      { url: "/android-chrome-512x512.png?v=20260704-2", type: "image/png", sizes: "512x512" },
    ],
    shortcut: ["/favicon.ico?v=20260704-2"],
    apple: [{ url: "/apple-touch-icon.png?v=20260704-2", sizes: "180x180", type: "image/png" }],
  },
};

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    <html lang="pt-BR">
      <head>
        {/* Detecta dispositivo fraco antes do primeiro render para evitar RAFs pesados */}
        <script dangerouslySetInnerHTML={{ __html: `(function(){var l=false;try{if(navigator.hardwareConcurrency&&navigator.hardwareConcurrency<=4)l=true;if(navigator.deviceMemory&&navigator.deviceMemory<=2)l=true;if(window.matchMedia('(prefers-reduced-motion:reduce)').matches)l=true;var c=navigator.connection;if(c&&(c.effectiveType==='2g'||c.effectiveType==='slow-2g'))l=true;}catch(e){}if(l){document.documentElement.dataset.perf='lite';window.__zpLite=true;}else{document.documentElement.dataset.perf='full';}})();` }} />
      </head>
      <body className={`${dmSans.variable} ${fraunces.variable}`}>
        {children}
      </body>
    </html>
  );
}
