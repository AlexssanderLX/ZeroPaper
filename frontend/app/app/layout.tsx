import { AppSessionProvider } from "@/components/app-session-provider";

export default function AppLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return <AppSessionProvider>{children}</AppSessionProvider>;
}

