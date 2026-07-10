"use client";

import { useAppSession } from "@/components/app-session-provider";
import { AiAssistantModule } from "@/components/modules/ai-assistant-module";
import { WorkspaceModulePage } from "@/components/workspace-module-page";
import { getModuleBySlug } from "@/lib/owner-portal";

const moduleData = getModuleBySlug("atendimento");

export default function AiAssistantHubPage() {
  const { session, clearSession } = useAppSession();

  if (!moduleData) {
    return null;
  }

  return (
    <WorkspaceModulePage
      module={moduleData}
      token={session.token}
      onUnauthorized={clearSession}
      heading="Atendimento da unidade"
      description="Tudo em uma tela: visao geral, IA, WhatsApp e teste. Troque de area pelas abas, sem abrir paginas separadas."
      showSummary={false}
    >
      <AiAssistantModule token={session.token} onUnauthorized={clearSession} />
    </WorkspaceModulePage>
  );
}
