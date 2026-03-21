"use client";

import { useEffect, useMemo, useRef, useState } from "react";
import { useParams } from "next/navigation";
import { APP_BASE_URL } from "@/lib/api";

export default function PrintTableQrPage() {
  const params = useParams<{ publicCode: string }>();
  const imageRef = useRef<HTMLImageElement | null>(null);
  const hasPrintedRef = useRef(false);
  const [imageReady, setImageReady] = useState(false);
  const [imageFailed, setImageFailed] = useState(false);

  const publicCode = params.publicCode;

  const accessUrl = useMemo(() => {
    const baseUrl = APP_BASE_URL || (typeof window !== "undefined" ? window.location.origin : "http://localhost:3000");
    return new URL(`/q/${publicCode}`, baseUrl).toString();
  }, [publicCode]);

  const qrImageUrl = useMemo(
    () =>
      `https://api.qrserver.com/v1/create-qr-code/?size=1400x1400&margin=24&format=png&data=${encodeURIComponent(accessUrl)}`,
    [accessUrl],
  );

  useEffect(() => {
    const image = imageRef.current;

    if (image?.complete) {
      setImageReady(true);
    }
  }, []);

  useEffect(() => {
    if (hasPrintedRef.current || (!imageReady && !imageFailed)) {
      return;
    }

    hasPrintedRef.current = true;

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
            <img
              ref={imageRef}
              className="qr-print-image"
              src={qrImageUrl}
              alt="QR da mesa"
              onLoad={() => setImageReady(true)}
              onError={() => setImageFailed(true)}
              referrerPolicy="no-referrer"
            />
          </div>
        </article>
      </section>
    </main>
  );
}
