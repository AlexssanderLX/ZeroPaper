import { AppSessionProvider } from "@/components/app-session-provider";
import { WorkspaceProvider } from "@/components/workspace-context";
import "./workspace-shell.css";

export default function AppLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    <AppSessionProvider>
      <WorkspaceProvider>{children}</WorkspaceProvider>
    </AppSessionProvider>
  );
}
