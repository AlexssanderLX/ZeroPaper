"use client";

import { useAppSession } from "@/components/app-session-provider";
import { AiAssistantModule } from "@/components/modules/ai-assistant-module";
import { WorkspaceModulePage } from "@/components/workspace-module-page";
import { getModuleBySlug } from "@/lib/owner-portal";

const moduleData = getModuleBySlug("atendimento");

export default function AiAssistantGuidePage() {
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
      heading="Como fechar certo"
      description="Veja o papel do site, do WhatsApp e da unidade para o cliente pedir sem confusao."
    >
      <AiAssistantModule token={session.token} onUnauthorized={clearSession} section="guide" />
    </WorkspaceModulePage>
  );
}
