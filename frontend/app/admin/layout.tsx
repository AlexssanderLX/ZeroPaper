import { AppSessionProvider } from "@/components/app-session-provider";

export default function AdminLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return <AppSessionProvider>{children}</AppSessionProvider>;
}
