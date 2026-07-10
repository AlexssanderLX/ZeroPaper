"use client";

import { useAppSession } from "@/components/app-session-provider";
import { AiAssistantModule } from "@/components/modules/ai-assistant-module";
import { WorkspaceModulePage } from "@/components/workspace-module-page";
import { getModuleBySlug } from "@/lib/owner-portal";

const moduleData = getModuleBySlug("atendimento");

export default function AiAssistantConversationsPage() {
  const { session, clearSession } = useAppSession();

  if (!moduleData) {
    return null;
  }

  return (
    <WorkspaceModulePage
      module={moduleData}
      token={session.token}
      onUnauthorized={clearSession}
      backHref="/app/atendimento"
      backLabel="Voltar ao Atendimento"
      heading="Conversas recentes"
      description="Acompanhe o resumo das ultimas mensagens recebidas pela unidade no canal conectado."
    >
      <AiAssistantModule token={session.token} onUnauthorized={clearSession} section="conversations" />
    </WorkspaceModulePage>
  );
}
