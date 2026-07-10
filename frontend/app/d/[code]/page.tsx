import { redirect } from "next/navigation";

const API_BASE_URL = process.env.NEXT_PUBLIC_API_BASE_URL ?? "http://localhost:5097";

type DeliveryShortLinkResponse = {
  found?: boolean;
  publicCode?: string;
  customerToken?: string;
};

export default async function DeliveryShortLinkPage({
  params,
}: {
  params: Promise<{ code: string }>;
}) {
  const { code } = await params;

  if (!code) {
    redirect("/");
  }

  const response = await fetch(
    `${API_BASE_URL}/api/public/delivery-links/${encodeURIComponent(code)}`,
    { cache: "no-store" },
  );

  if (!response.ok) {
    redirect("/");
  }

  const data = (await response.json()) as DeliveryShortLinkResponse;
  if (!data.found || !data.publicCode || !data.customerToken) {
    redirect("/");
  }

  redirect(`/q/${data.publicCode}?cliente=${encodeURIComponent(data.customerToken)}`);
}
