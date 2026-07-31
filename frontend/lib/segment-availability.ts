export type SegmentAvailability = {
  key: string;
  available: boolean;
};

const API = process.env.BACKEND_INTERNAL_URL ?? process.env.NEXT_PUBLIC_API_BASE_URL ?? "http://localhost:5097";

export async function fetchSegmentAvailability(): Promise<Map<string, boolean>> {
  try {
    const res = await fetch(`${API}/api/public/segments`, {
      cache: "no-store",
    });
    if (!res.ok) throw new Error(`HTTP ${res.status}`);
    const data: SegmentAvailability[] = await res.json();
    return new Map(data.map((s) => [s.key, s.available]));
  } catch {
    // Falha fechada: nenhum novo segmento e anunciado sem confirmacao do backend.
    return new Map([["restaurant", true]]);
  }
}
