import { PublicTableOrder } from "@/components/public-table-order";

export default async function PublicTablePage({
  params,
}: {
  params: Promise<{ publicCode: string }>;
}) {
  const { publicCode } = await params;
  return <PublicTableOrder publicCode={publicCode} />;
}
