import { redirect } from "next/navigation";

export default function OrdersFinishedRedirectPage() {
  redirect("/app/finalizados");
}
