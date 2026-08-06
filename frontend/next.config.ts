import type { NextConfig } from "next";

const isDevelopment = process.env.NODE_ENV !== "production";

const securityHeaders = [
  {
    key: "X-Content-Type-Options",
    value: "nosniff",
  },
  {
    key: "X-Frame-Options",
    value: "DENY",
  },
  {
    key: "Referrer-Policy",
    value: "no-referrer",
  },
  {
    key: "Permissions-Policy",
    value: "camera=(), microphone=(), geolocation=()",
  },
  {
    key: "Content-Security-Policy",
    value: [
      "default-src 'self'",
      "base-uri 'self'",
      "frame-ancestors 'none'",
      "object-src 'none'",
      "form-action 'self'",
      `script-src 'self' 'unsafe-inline'${isDevelopment ? " 'unsafe-eval'" : ""} https://static.cloudflareinsights.com`,
      "style-src 'self' 'unsafe-inline'",
      "img-src 'self' data: blob: https:",
      "font-src 'self' data:",
      `connect-src 'self'${isDevelopment ? " http://localhost:5097" : ""} https://cloudflareinsights.com https://*.cloudflareinsights.com`,
      "media-src 'self' data: blob: https:",
    ].join("; "),
  },
  {
    key: "Strict-Transport-Security",
    value: "max-age=31536000; includeSubDomains; preload",
  },
];

const nextConfig: NextConfig = {
  reactStrictMode: true,
  poweredByHeader: false,
  output: "standalone",
  distDir: process.env.NEXT_DIST_DIR ?? ".next",
  async rewrites() {
    if (!isDevelopment) return [];

    return [
      {
        source: "/api/:path*",
        destination: "http://127.0.0.1:5097/api/:path*",
      },
    ];
  },
  async headers() {
    return [
      {
        source: "/:path*",
        headers: securityHeaders,
      },
    ];
  },
};

export default nextConfig;
