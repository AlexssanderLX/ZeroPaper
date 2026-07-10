"use client";

import { useAppSession } from "@/components/app-session-provider";
import { AiAssistantModule } from "@/components/modules/ai-assistant-module";
import { WorkspaceModulePage } from "@/components/workspace-module-page";
import { getModuleBySlug } from "@/lib/owner-portal";

const moduleData = getModuleBySlug("atendimento");

export default function AiAssistantTextsPage() {
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
      heading="Textos e tom da conversa"
      description="Escreva a forma como a unidade recebe, ajuda e conduz o cliente no atendimento."
    >
      <AiAssistantModule token={session.token} onUnauthorized={clearSession} section="texts" />
    </WorkspaceModulePage>
  );
}
