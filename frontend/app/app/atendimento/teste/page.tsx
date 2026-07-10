"use client";

import { useAppSession } from "@/components/app-session-provider";
import { AiAssistantModule } from "@/components/modules/ai-assistant-module";
import { WorkspaceModulePage } from "@/components/workspace-module-page";
import { getModuleBySlug } from "@/lib/owner-portal";

const moduleData = getModuleBySlug("atendimento");

export default function AiAssistantTestPage() {
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
      heading="Teste controlado"
      description="Envie uma mensagem de teste pelo backend e confira como a unidade esta respondendo hoje."
    >
      <AiAssistantModule token={session.token} onUnauthorized={clearSession} section="test" />
    </WorkspaceModulePage>
  );
}
