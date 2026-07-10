"use client";

import { useEffect, useMemo, useRef, useState } from "react";
import { useParams } from "next/navigation";
import QRCode from "qrcode";
import { APP_BASE_URL } from "@/lib/api";

export default function PrintTableQrPage() {
  const params = useParams<{ publicCode: string }>();
  const hasPrintedRef = useRef(false);
  const [qrImageUrl, setQrImageUrl] = useState("");
  const [imageReady, setImageReady] = useState(false);
  const [imageFailed, setImageFailed] = useState(false);

  const publicCode = params.publicCode;

  const accessUrl = useMemo(() => {
    const baseUrl = APP_BASE_URL || (typeof window !== "undefined" ? window.location.origin : "http://localhost:3000");
    return new URL(`/q/${publicCode}`, baseUrl).toString();
  }, [publicCode]);

  useEffect(() => {
    let cancelled = false;

    async function buildQrCode() {
      try {
        setImageReady(false);
        setImageFailed(false);

        const dataUrl = await QRCode.toDataURL(accessUrl, {
          width: 1400,
          margin: 2,
        });

        if (!cancelled) {
          setQrImageUrl(dataUrl);
          setImageReady(true);
        }
      } catch {
        if (!cancelled) {
          setQrImageUrl("");
          setImageFailed(true);
        }
      }
    }

    void buildQrCode();

    return () => {
      cancelled = true;
    };
  }, [accessUrl]);

  useEffect(() => {
    if (hasPrintedRef.current || (!imageReady && !imageFailed)) {
      return;
    }

    hasPrintedRef.current = true;

    if (window.parent && window.parent !== window) {
      window.parent.postMessage(
        {
          type: imageReady ? "zeropaper:qr-print-ready" : "zeropaper:qr-print-failed",
        },
        window.location.origin,
      );
      return;
    }

    const timeoutId = window.setTimeout(() => {
      window.focus();
      window.print();
    }, 220);

    return () => window.clearTimeout(timeoutId);
  }, [imageFailed, imageReady]);

  return (
    <main className="qr-print-page">
      <section className="qr-print-sheet">
        <article className="qr-print-card">
          <p className="qr-print-message">Seja bem-vindo! Escaneie para pedir.</p>

          <div className="qr-print-frame">
            {qrImageUrl ? (
              <img
                className="qr-print-image"
                src={qrImageUrl}
                alt="QR da mesa"
                loading="eager"
              />
            ) : (
              <div className="qr-print-fallback">
                <strong>QR indisponivel</strong>
                <p>Tente atualizar a pagina ou use o botao abaixo para imprimir manualmente.</p>
              </div>
            )}
          </div>

          <div className="qr-print-actions no-print">
            <button className="ghost-link button-link" type="button" onClick={() => window.print()}>
              Imprimir agora
            </button>
          </div>
        </article>
      </section>
    </main>
  );
}
