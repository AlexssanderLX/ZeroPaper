"use client";

import { useAppSession } from "@/components/app-session-provider";
import { AiAssistantModule } from "@/components/modules/ai-assistant-module";
import { WorkspaceModulePage } from "@/components/workspace-module-page";
import { getModuleBySlug } from "@/lib/owner-portal";

const moduleData = getModuleBySlug("atendimento");

export default function AiAssistantWhatsAppPage() {
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
      heading="WhatsApp da unidade"
      description="Acompanhe o canal atual, gere QR para conectar um numero e troque o WhatsApp da unidade quando precisar."
    >
      <AiAssistantModule token={session.token} onUnauthorized={clearSession} section="whatsapp" />
    </WorkspaceModulePage>
  );
}
