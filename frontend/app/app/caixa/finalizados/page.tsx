import { redirect } from "next/navigation";

export default function CashFinishedRedirectPage() {
  redirect("/app/finalizados");
}
