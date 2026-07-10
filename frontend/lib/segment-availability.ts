export type SegmentAvailability = {
  key: string;
  available: boolean;
};

const API = process.env.NEXT_PUBLIC_API_BASE_URL ?? "http://localhost:5097";

export async function fetchSegmentAvailability(): Promise<Map<string, boolean>> {
  try {
    const res = await fetch(`${API}/api/public/segments`, {
      next: { revalidate: 60 },
    });
    if (!res.ok) throw new Error(`HTTP ${res.status}`);
    const data: SegmentAvailability[] = await res.json();
    return new Map(data.map((s) => [s.key, s.available]));
  } catch {
    // Fallback: só restaurante disponível
    return new Map([["restaurant", true]]);
  }
}
