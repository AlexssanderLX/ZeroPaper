"use client";

import { FormEvent, useEffect, useMemo, useState } from "react";
import { useRouter } from "next/navigation";
import {
  ApiError,
  cleanupSignupCodes,
  confirmCurrentPassword,
  createAdminOwner,
  createSignupCode,
  deactivateAdminOwner,
  deactivateAdminUser,
  deleteAdminCompany,
  deleteAdminUser,
  deleteSignupCode,
  getAdminOwners,
  getAdminDashboard,
  hardDeleteAdminOwner,
  reactivateAdminOwner,
  reactivateAdminUser,
  resetAdminOwnerPassword,
  revealAdminMasterPassword,
  rotateAdminMasterPassword,
  updateAdminOwner,
  updateAdminCompanyPlan,
  type AdminCompanyFlow,
  type AdminDashboard,
  type AdminOwner,
  type AdminUser,
  type CreateSignupCodeResult,
  type SignupCode,
  type UpdateAdminCompanyPlanPayload,
} from "@/lib/api";
import { useAppSession } from "@/components/app-session-provider";

const GENERATED_CODE_KEY = "zp.admin.generated-code";
const GENERATED_CODE_TTL_MS = 5 * 60 * 1000;
const PLAN_FEATURES = [
  { key: "includesMenuModule", label: "Cardapio", description: "Catalogo e publicacao do menu.", price: 9.9 },
  { key: "includesTablesModule", label: "Mesas", description: "QR, mesas e ocupacao.", price: 10 },
  { key: "includesKitchenModule", label: "Cozinha", description: "Fluxo operacional da cozinha.", price: 12 },
  { key: "includesCashModule", label: "Caixa", description: "Cobranca e baixa dos pedidos.", price: 12 },
  { key: "includesStockModule", label: "Estoque", description: "Controle de insumos e saldo.", price: 9 },
  { key: "includesDeliveryModule", label: "Delivery", description: "Canal fixo de pedidos para entrega.", price: 10 },
  { key: "includesPrintingModule", label: "Impressao", description: "Fila e agente de impressao.", price: 8 },
  { key: "includesWaiterCallModule", label: "Chamado", description: "Chamada de atendente e alerta.", price: 9 },
  { key: "includesAiAssistantModule", label: "IA", description: "Atendimento inteligente da unidade.", price: 40 },
] as const;

const STANDARD_PLANS = [
  {
    planName: "ZeroPaper Essencial",
    label: "Essencial",
    price: 80,
    maxUsers: 3,
    features: {
      includesMenuModule: true,
      includesTablesModule: true,
      includesKitchenModule: true,
      includesCashModule: true,
      includesStockModule: false,
      includesDeliveryModule: false,
      includesPrintingModule: true,
      includesWaiterCallModule: true,
      includesAiAssistantModule: false,
    },
  },
  {
    planName: "ZeroPaper Operacao",
    label: "Operacao",
    price: 120,
    maxUsers: 5,
    features: {
      includesMenuModule: true,
      includesTablesModule: true,
      includesKitchenModule: true,
      includesCashModule: true,
      includesStockModule: false,
      includesDeliveryModule: true,
      includesPrintingModule: true,
      includesWaiterCallModule: true,
      includesAiAssistantModule: true,
    },
  },
  {
    planName: "ZeroPaper Gestao",
    label: "Gestao",
    price: 180,
    maxUsers: 8,
    features: {
      includesMenuModule: true,
      includesTablesModule: true,
      includesKitchenModule: true,
      includesCashModule: true,
      includesStockModule: false,
      includesDeliveryModule: true,
      includesPrintingModule: true,
      includesWaiterCallModule: true,
      includesAiAssistantModule: true,
    },
  },
] as const;

type PlanFeatureKey = (typeof PLAN_FEATURES)[number]["key"];
type PlanDraft = Omit<UpdateAdminCompanyPlanPayload, "password">;
type OwnerEditorDraft = {
  fullName: string;
  email: string;
};

function formatDate(value: string) {
  return new Intl.DateTimeFormat("pt-BR", {
    dateStyle: "short",
    timeStyle: "short",
    timeZone: "America/Sao_Paulo",
  }).format(new Date(value));
}

function formatOptionalDate(value?: string | null) {
  return value ? formatDate(value) : "Sem registro";
}

function formatCountdown(value: number) {
  const totalSeconds = Math.max(0, Math.ceil(value / 1000));
  const minutes = Math.floor(totalSeconds / 60);
  const seconds = totalSeconds % 60;
  return `${minutes}:${seconds.toString().padStart(2, "0")}`;
}

function formatRole(value: string) {
  const map: Record<string, string> = {
    Root: "Root",
    Owner: "Dono",
    Manager: "Gerencia",
    Employee: "Equipe",
  };

  return map[value] ?? value;
}

function formatCurrency(value: number) {
  return new Intl.NumberFormat("pt-BR", {
    style: "currency",
    currency: "BRL",
    minimumFractionDigits: 0,
    maximumFractionDigits: 2,
  }).format(value);
}

function normalizeAdminConfirmation(value: string) {
  return value
    .normalize("NFD")
    .replace(/[\u0300-\u036f]/g, "")
    .trim()
    .toLowerCase();
}

function createPlanDraft(company: AdminCompanyFlow): PlanDraft {
  return {
    planName: company.planName,
    includesMenuModule: company.includesMenuModule,
    includesTablesModule: company.includesTablesModule,
    includesKitchenModule: company.includesKitchenModule,
    includesCashModule: company.includesCashModule,
    includesStockModule: company.includesStockModule,
    includesDeliveryModule: company.includesDeliveryModule,
    includesPrintingModule: company.includesPrintingModule,
    includesWaiterCallModule: company.includesWaiterCallModule,
    includesAiAssistantModule: company.includesAiAssistantModule,
    maxUsers: company.maxUsers,
  };
}

function calculatePlanDraftPrice(draft: PlanDraft | null) {
  if (!draft) {
    return 0;
  }

  const standardPlan = STANDARD_PLANS.find((plan) => plan.planName === draft.planName);
  if (standardPlan) {
    return standardPlan.price;
  }

  return PLAN_FEATURES.reduce((total, feature) => {
    return draft[feature.key] ? total + feature.price : total;
  }, 0);
}

function buildPlanDraftName(draft: PlanDraft | null) {
  if (!draft) {
    return "Plano";
  }

  const standardPlan = STANDARD_PLANS.find((plan) => plan.planName === draft.planName);
  if (standardPlan) {
    return standardPlan.planName;
  }

  const hasCorePlan =
    draft.includesMenuModule &&
    draft.includesTablesModule &&
    draft.includesKitchenModule &&
    draft.includesCashModule &&
    draft.includesDeliveryModule &&
    draft.includesPrintingModule &&
    draft.includesWaiterCallModule;

  if (hasCorePlan && draft.includesAiAssistantModule) {
    return "ZeroPaper Operacao";
  }

  if (hasCorePlan) {
    return "ZeroPaper Operacao";
  }

  return "ZeroPaper Personalizado";
}

function countEnabledModules(company: AdminCompanyFlow) {
  return PLAN_FEATURES.reduce((total, feature) => {
    return company[feature.key] ? total + 1 : total;
  }, 0);
}

function describePlanModules(company: AdminCompanyFlow) {
  const enabledFeatures = PLAN_FEATURES.filter((feature) => company[feature.key]).map((feature) => feature.label);

  if (enabledFeatures.length === 0) {
    return "Nenhum modulo liberado";
  }

  return enabledFeatures.join(", ");
}

function resolveCodeStatus(code: SignupCode) {
  const expiresAt = new Date(code.expiresAtUtc).getTime();
  if (code.isActive && expiresAt > Date.now() && code.usedCount < code.maxUses) {
    return { label: "Disponivel", tone: "available", sortable: 0 };
  }

  if (code.usedCount >= code.maxUses || (!code.isActive && code.usedCount > 0)) {
    return { label: "Utilizado", tone: "warning", sortable: 1 };
  }

  return { label: "Expirado", tone: "cancelled", sortable: 2 };
}

function resolvePresenceStatus(user: AdminUser) {
  return user.isOnlineNow
    ? { label: "Online", tone: "available" }
    : { label: "Offline", tone: "inactive" };
}

type SensitiveAction =
  | { type: "reveal-code" }
  | { type: "cleanup-codes" }
  | { type: "delete-code"; code: SignupCode }
  | { type: "edit-plan"; company: AdminCompanyFlow }
  | { type: "reject-user"; user: AdminUser }
  | { type: "reject-signup"; company: AdminCompanyFlow; user: AdminUser }
  | { type: "delete-company"; company: AdminCompanyFlow }
  | { type: "reveal-master"; company: AdminCompanyFlow }
  | { type: "rotate-master"; company: AdminCompanyFlow }
  | { type: "edit-owner"; owner: AdminOwner }
  | { type: "reset-owner-password"; owner: AdminOwner }
  | { type: "deactivate-owner"; owner: AdminOwner }
  | { type: "reactivate-owner"; owner: AdminOwner }
  | { type: "hard-delete-owner"; owner: AdminOwner }
  | { type: "deactivate-user"; user: AdminUser }
  | { type: "reactivate-user"; user: AdminUser }
  | { type: "delete-user"; user: AdminUser };

function getSensitiveActionCopy(action: SensitiveAction | null) {
  if (!action) {
    return { title: "", description: "", buttonLabel: "" };
  }

  switch (action.type) {
    case "reveal-code":
      return {
        title: "Liberar codigo gerado",
        description: "Digite sua senha root para mostrar o codigo gerado nesta tela.",
        buttonLabel: "Liberar codigo",
      };
    case "cleanup-codes":
      return {
        title: "Limpar codigos usados",
        description: "Confirme sua senha root para remover codigos usados, expirados ou ja encerrados.",
        buttonLabel: "Limpar codigos",
      };
    case "delete-code":
      return {
        title: "Remover codigo",
        description: `Confirme sua senha root para remover o codigo ${action.code.label}.`,
        buttonLabel: "Remover codigo",
      };
    case "reject-user":
      return {
        title: "Rejeitar acesso",
        description: `Confirme sua senha root para negar o acesso pendente de ${action.user.fullName}.`,
        buttonLabel: "Rejeitar acesso",
      };
    case "edit-plan":
      return {
        title: "Editar plano da unidade",
        description: `Ajuste os modulos liberados para ${action.company.restaurantName}, reveja o valor mensal e confirme com sua senha root.`,
        buttonLabel: "Salvar plano",
      };
    case "delete-company":
      return {
        title: "Apagar empresa",
        description: `Essa acao remove ${action.company.restaurantName} do painel, bloqueia owners e encerra sessoes. Pedidos e historico ficam preservados para auditoria.`,
        buttonLabel: "Apagar empresa",
      };
    case "reject-signup":
      return {
        title: "Rejeitar pre-cadastro",
        description: `Confirme sua senha root para rejeitar ${action.company.restaurantName}. O login pendente de ${action.user.fullName} sera bloqueado e a unidade sairá do painel ativo.`,
        buttonLabel: "Rejeitar pre-cadastro",
      };
    case "reveal-master":
      return {
        title: "Liberar senha master",
        description: `Confirme sua senha root para revelar a senha master segura da unidade ${action.company.restaurantName}.`,
        buttonLabel: "Liberar senha",
      };
    case "rotate-master":
      return {
        title: "Rotacionar senha master",
        description: `Confirme sua senha root para gerar uma nova senha master segura para ${action.company.restaurantName}.`,
        buttonLabel: "Gerar nova senha",
      };
    case "edit-owner":
      return {
        title: "Editar owner",
        description: `Atualize o nome e email de ${action.owner.fullName}. A alteracao exige sua senha root.`,
        buttonLabel: "Salvar owner",
      };
    case "reset-owner-password":
      return {
        title: "Trocar senha do owner",
        description: `Defina uma nova senha temporaria para ${action.owner.fullName}. As sessoes abertas desse owner serao encerradas.`,
        buttonLabel: "Trocar senha",
      };
    case "deactivate-owner":
      return {
        title: "Desativar owner",
        description: `Confirme sua senha root para desativar ${action.owner.fullName}. O acesso aberto sera encerrado.`,
        buttonLabel: "Desativar owner",
      };
    case "reactivate-owner":
      return {
        title: "Reativar owner",
        description: `Confirme sua senha root para reativar ${action.owner.fullName}.`,
        buttonLabel: "Reativar owner",
      };
    case "hard-delete-owner":
      return {
        title: "Excluir owner definitivamente",
        description: `Essa acao apaga a conta de ${action.owner.fullName} e suas sessoes. A unidade, pedidos, cardapio e historico operacional nao serao apagados.`,
        buttonLabel: "Excluir owner",
      };
    case "deactivate-user":
      return {
        title: "Desativar conta",
        description: `Confirme sua senha root para desativar ${action.user.fullName}.`,
        buttonLabel: "Desativar conta",
      };
    case "reactivate-user":
      return {
        title: "Liberar login",
        description: `Confirme sua senha root para liberar o login de ${action.user.fullName}.`,
        buttonLabel: "Liberar login",
      };
    case "delete-user":
      return {
        title: "Excluir conta",
        description: `Confirme sua senha root para excluir ${action.user.fullName}. Essa acao remove o registro da plataforma.`,
        buttonLabel: "Excluir conta",
      };
  }
}

type AdminSection = "overview" | "empresas" | "acessos" | "codigos";

export function AdminSignupCodeManager() {
  const router = useRouter();
  const { session, clearSession } = useAppSession();
  const [dashboard, setDashboard] = useState<AdminDashboard | null>(null);
  const [owners, setOwners] = useState<AdminOwner[]>([]);
  const [createdCode, setCreatedCode] = useState<CreateSignupCodeResult | null>(null);
  const [showCreatedCode, setShowCreatedCode] = useState(false);
  const [createdCodeExpiresAt, setCreatedCodeExpiresAt] = useState<number | null>(null);
  const [remainingCodeMs, setRemainingCodeMs] = useState(0);
  const [revealedMasterPasswords, setRevealedMasterPasswords] = useState<Record<string, string>>({});
  const [loading, setLoading] = useState(true);
  const [submitting, setSubmitting] = useState(false);
  const [processingKey, setProcessingKey] = useState<string | null>(null);
  const [sensitiveAction, setSensitiveAction] = useState<SensitiveAction | null>(null);
  const [planDraft, setPlanDraft] = useState<PlanDraft | null>(null);
  const [ownerEditorDraft, setOwnerEditorDraft] = useState<OwnerEditorDraft | null>(null);
  const [newOwnerPassword, setNewOwnerPassword] = useState("");
  const [hardDeleteConfirmationText, setHardDeleteConfirmationText] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");
  const [confirmingSensitiveAction, setConfirmingSensitiveAction] = useState(false);
  const [confirmErrorMessage, setConfirmErrorMessage] = useState("");
  const [errorMessage, setErrorMessage] = useState("");
  const [pageMessage, setPageMessage] = useState("");
  const [activeSection, setActiveSection] = useState<AdminSection>("overview");
  const [selectedCompanyId, setSelectedCompanyId] = useState<string | null>(null);

  const codes = dashboard?.codes ?? [];
  const users = dashboard?.users ?? [];
  const companies = dashboard?.companies ?? [];
  const summary = dashboard?.summary;

  const sortedCodes = useMemo(
    () =>
      [...codes].sort((left, right) => {
        const leftStatus = resolveCodeStatus(left);
        const rightStatus = resolveCodeStatus(right);
        if (leftStatus.sortable !== rightStatus.sortable) {
          return leftStatus.sortable - rightStatus.sortable;
        }

        return new Date(right.createdAtUtc).getTime() - new Date(left.createdAtUtc).getTime();
      }),
    [codes],
  );
  const sortedUsers = useMemo(
    () =>
      [...users].sort((left, right) => {
        const leftPending = left.role !== "Root" && !left.isActive;
        const rightPending = right.role !== "Root" && !right.isActive;

        if (leftPending !== rightPending) {
          return leftPending ? -1 : 1;
        }

        if (left.role === "Root" || right.role === "Root") {
          return left.role === "Root" ? -1 : 1;
        }

        return `${left.restaurantName} ${left.fullName}`.localeCompare(`${right.restaurantName} ${right.fullName}`);
      }),
    [users],
  );
  const pendingSignupUsers = sortedUsers.filter((user) => user.role !== "Root" && !user.isActive);
  const ownerCountsByCompany = useMemo(() => {
    return owners.reduce<Record<string, { total: number; active: number }>>((accumulator, owner) => {
      const currentValue = accumulator[owner.companyId] ?? { total: 0, active: 0 };
      accumulator[owner.companyId] = {
        total: currentValue.total + 1,
        active: currentValue.active + (owner.isActive ? 1 : 0),
      };
      return accumulator;
    }, {});
  }, [owners]);

  async function loadAdminData() {
    setLoading(true);
    try {
      const dashboardResponse = await getAdminDashboard(session.token);
      setDashboard(dashboardResponse);
      setErrorMessage("");

      try {
        const ownerResponse = await getAdminOwners(session.token);
        setOwners(ownerResponse);
      } catch (ownerError) {
        if (ownerError instanceof ApiError && ownerError.status === 401) {
          await clearSession();
          return;
        }

        setOwners([]);
        setPageMessage("Painel carregado, mas o controle de owners ainda nao respondeu nesta instancia.");
      }
    } catch (error) {
      if (error instanceof ApiError && error.status === 401) {
        await clearSession();
        return;
      }

      setPageMessage("Nao foi possivel carregar o painel admin agora.");
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    if (session.profile !== "admin") {
      router.replace("/login");
      return;
    }

    void loadAdminData();
  }, [router, session.profile, session.token]);

  useEffect(() => {
    if (typeof window === "undefined") {
      return;
    }

    const rawStoredValue = window.sessionStorage.getItem(GENERATED_CODE_KEY);
    if (!rawStoredValue) {
      return;
    }

    try {
      const parsedValue = JSON.parse(rawStoredValue) as { code: CreateSignupCodeResult; expiresAt: number };
      if (parsedValue.expiresAt <= Date.now()) {
        window.sessionStorage.removeItem(GENERATED_CODE_KEY);
        return;
      }

      setCreatedCode(parsedValue.code);
      setCreatedCodeExpiresAt(parsedValue.expiresAt);
    } catch {
      window.sessionStorage.removeItem(GENERATED_CODE_KEY);
    }
  }, []);

  useEffect(() => {
    if (!createdCodeExpiresAt) {
      setRemainingCodeMs(0);
      return;
    }

    const tick = () => {
      const remaining = createdCodeExpiresAt - Date.now();
      if (remaining <= 0) {
        setCreatedCode(null);
        setCreatedCodeExpiresAt(null);
        setShowCreatedCode(false);
        setRemainingCodeMs(0);
        if (typeof window !== "undefined") {
          window.sessionStorage.removeItem(GENERATED_CODE_KEY);
        }
        return;
      }

      setRemainingCodeMs(remaining);
    };

    tick();
    const interval = window.setInterval(tick, 1000);
    return () => window.clearInterval(interval);
  }, [createdCodeExpiresAt]);

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const formData = new FormData(event.currentTarget);
    const label = String(formData.get("label") ?? "").trim();
    const boundEmail = String(formData.get("boundEmail") ?? "").trim().toLowerCase();

    if (!label) {
      setErrorMessage("Informe um nome para o codigo.");
      return;
    }

    try {
      setSubmitting(true);
      const response = await createSignupCode(session.token, {
        label,
        boundEmail: boundEmail || undefined,
      });

      setCreatedCode(response);
      setShowCreatedCode(true);
      const expiresAt = Date.now() + GENERATED_CODE_TTL_MS;
      setCreatedCodeExpiresAt(expiresAt);
      window.sessionStorage.setItem(GENERATED_CODE_KEY, JSON.stringify({ code: response, expiresAt }));
      setPageMessage("Codigo gerado com sucesso. Copie e envie para o cliente dentro de 5 minutos.");
      setErrorMessage("");
      (event.currentTarget as HTMLFormElement).reset();
      await loadAdminData();
    } catch (error) {
      if (error instanceof ApiError && error.status === 401) {
        await clearSession();
        return;
      }

      setErrorMessage(error instanceof Error ? error.message : "Nao foi possivel gerar o codigo agora.");
    } finally {
      setSubmitting(false);
    }
  }

  async function handleCreateOwner(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const form = event.currentTarget;
    const formData = new FormData(form);
    const companyId = String(formData.get("companyId") ?? "").trim();
    const fullName = String(formData.get("fullName") ?? "").trim();
    const email = String(formData.get("email") ?? "").trim().toLowerCase();
    const ownerPassword = String(formData.get("ownerPassword") ?? "");
    const rootPassword = String(formData.get("rootPassword") ?? "");

    if (!companyId || !fullName || !email || !ownerPassword || !rootPassword) {
      setErrorMessage("Preencha unidade, owner, email, senha temporaria e sua senha root.");
      return;
    }

    try {
      setSubmitting(true);
      await createAdminOwner(session.token, {
        companyId,
        fullName,
        email,
        ownerPassword,
        rootPassword,
      });
      form.reset();
      setPageMessage("Owner criado com sucesso.");
      setErrorMessage("");
      await loadAdminData();
    } catch (error) {
      if (error instanceof ApiError && error.status === 401) {
        await clearSession();
        return;
      }

      setErrorMessage(error instanceof Error ? error.message : "Nao foi possivel criar o owner agora.");
    } finally {
      setSubmitting(false);
    }
  }

  function openSensitiveAction(action: SensitiveAction) {
    setPlanDraft(action.type === "edit-plan" ? createPlanDraft(action.company) : null);
    setOwnerEditorDraft(action.type === "edit-owner" ? { fullName: action.owner.fullName, email: action.owner.email } : null);
    setNewOwnerPassword("");
    setHardDeleteConfirmationText("");
    setSensitiveAction(action);
    setConfirmPassword("");
    setConfirmErrorMessage("");
  }

  function closeSensitiveAction() {
    if (confirmingSensitiveAction) {
      return;
    }

    setSensitiveAction(null);
    setPlanDraft(null);
    setOwnerEditorDraft(null);
    setNewOwnerPassword("");
    setHardDeleteConfirmationText("");
    setConfirmPassword("");
    setConfirmErrorMessage("");
  }

  async function handleSensitiveActionSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!sensitiveAction) {
      return;
    }

    if (!confirmPassword.trim()) {
      setConfirmErrorMessage("Digite sua senha root para continuar.");
      return;
    }

    try {
      setConfirmingSensitiveAction(true);

      if (sensitiveAction.type === "reveal-code") {
        const confirmation = await confirmCurrentPassword(session.token, { password: confirmPassword });
        if (!confirmation.confirmed) {
          setConfirmErrorMessage("Senha incorreta.");
          return;
        }

        setShowCreatedCode(true);
        setPageMessage("Codigo liberado nesta tela.");
      }

      if (sensitiveAction.type === "cleanup-codes") {
        setProcessingKey("cleanup-codes");
        const confirmation = await confirmCurrentPassword(session.token, { password: confirmPassword });
        if (!confirmation.confirmed) {
          setConfirmErrorMessage("Senha incorreta.");
          return;
        }

        const response = await cleanupSignupCodes(session.token);
        setPageMessage(
          response.deletedCount > 0
            ? `${response.deletedCount} codigo(s) usado(s) ou expirado(s) foram removidos.`
            : "Nao havia codigos usados ou expirados para limpar.",
        );
        await loadAdminData();
      }

      if (sensitiveAction.type === "delete-code") {
        setProcessingKey(`code:${sensitiveAction.code.id}`);
        const confirmation = await confirmCurrentPassword(session.token, { password: confirmPassword });
        if (!confirmation.confirmed) {
          setConfirmErrorMessage("Senha incorreta.");
          return;
        }

        await deleteSignupCode(session.token, sensitiveAction.code.id);
        setPageMessage("Codigo removido com sucesso.");
        await loadAdminData();
      }

      if (sensitiveAction.type === "reject-user") {
        setProcessingKey(`user:${sensitiveAction.user.id}`);
        await deleteAdminUser(session.token, sensitiveAction.user.id, { password: confirmPassword });
        setPageMessage(`${sensitiveAction.user.fullName} teve o acesso pendente rejeitado.`);
        await loadAdminData();
      }

      if (sensitiveAction.type === "edit-plan") {
        if (!planDraft) {
          setConfirmErrorMessage("Monte o plano antes de salvar.");
          return;
        }

        setProcessingKey(`plan:${sensitiveAction.company.companyId}`);
        const response = await updateAdminCompanyPlan(session.token, sensitiveAction.company.companyId, {
          ...planDraft,
          password: confirmPassword,
        });

        setPlanDraft({
          planName: response.planName,
          includesMenuModule: response.includesMenuModule,
          includesTablesModule: response.includesTablesModule,
          includesKitchenModule: response.includesKitchenModule,
          includesCashModule: response.includesCashModule,
          includesStockModule: response.includesStockModule,
          includesDeliveryModule: response.includesDeliveryModule,
          includesPrintingModule: response.includesPrintingModule,
          includesWaiterCallModule: response.includesWaiterCallModule,
          includesAiAssistantModule: response.includesAiAssistantModule,
          maxUsers: response.maxUsers,
        });
        setPageMessage(`Plano de ${response.restaurantName} atualizado para ${response.planName} (${formatCurrency(response.monthlyPrice)}).`);
        await loadAdminData();
      }

      if (sensitiveAction.type === "delete-company" || sensitiveAction.type === "reject-signup") {
        if (sensitiveAction.type === "delete-company") {
          const typedName = normalizeAdminConfirmation(hardDeleteConfirmationText);
          const expectedName = normalizeAdminConfirmation(sensitiveAction.company.restaurantName);
          if (typedName !== expectedName) {
            setConfirmErrorMessage(`Digite o nome da empresa: ${sensitiveAction.company.restaurantName}`);
            return;
          }
        }

        setProcessingKey(`company:${sensitiveAction.company.companyId}`);
        await deleteAdminCompany(session.token, sensitiveAction.company.companyId, {
          password: confirmPassword,
          confirmationText: sensitiveAction.company.restaurantName,
        });
        setSelectedCompanyId(null);
        setPageMessage(
          sensitiveAction.type === "reject-signup"
            ? `${sensitiveAction.company.restaurantName} foi rejeitada e teve o login pendente bloqueado.`
            : `${sensitiveAction.company.restaurantName} foi removida do painel e teve os acessos bloqueados.`,
        );
        await loadAdminData();
      }

      if (sensitiveAction.type === "reveal-master") {
        setProcessingKey(`reveal-master:${sensitiveAction.company.companyId}`);
        const response = await revealAdminMasterPassword(session.token, sensitiveAction.company.companyId, {
          password: confirmPassword,
        });

        setRevealedMasterPasswords((currentValue) => ({
          ...currentValue,
          [response.companyId]: response.rawPassword,
        }));
        setPageMessage(`Senha master liberada para ${response.restaurantName}.`);
        await loadAdminData();
      }

      if (sensitiveAction.type === "rotate-master") {
        setProcessingKey(`rotate-master:${sensitiveAction.company.companyId}`);
        const response = await rotateAdminMasterPassword(session.token, sensitiveAction.company.companyId, {
          password: confirmPassword,
        });

        setRevealedMasterPasswords((currentValue) => ({
          ...currentValue,
          [response.companyId]: response.rawPassword,
        }));
        setPageMessage(`Nova senha master gerada para ${response.restaurantName}.`);
        await loadAdminData();
      }

      if (sensitiveAction.type === "edit-owner") {
        if (!ownerEditorDraft?.fullName.trim() || !ownerEditorDraft.email.trim()) {
          setConfirmErrorMessage("Informe nome e email do owner.");
          return;
        }

        setProcessingKey(`owner:${sensitiveAction.owner.id}`);
        await updateAdminOwner(session.token, sensitiveAction.owner.id, {
          fullName: ownerEditorDraft.fullName,
          email: ownerEditorDraft.email,
          rootPassword: confirmPassword,
        });
        setPageMessage("Owner atualizado com sucesso.");
        await loadAdminData();
      }

      if (sensitiveAction.type === "reset-owner-password") {
        if (!newOwnerPassword.trim()) {
          setConfirmErrorMessage("Informe a nova senha temporaria do owner.");
          return;
        }

        setProcessingKey(`owner:${sensitiveAction.owner.id}`);
        await resetAdminOwnerPassword(session.token, sensitiveAction.owner.id, {
          newPassword: newOwnerPassword,
          rootPassword: confirmPassword,
        });
        setPageMessage("Senha do owner atualizada e sessoes abertas encerradas.");
        await loadAdminData();
      }

      if (sensitiveAction.type === "deactivate-owner") {
        setProcessingKey(`owner:${sensitiveAction.owner.id}`);
        await deactivateAdminOwner(session.token, sensitiveAction.owner.id, { rootPassword: confirmPassword });
        setPageMessage("Owner desativado com sucesso.");
        await loadAdminData();
      }

      if (sensitiveAction.type === "reactivate-owner") {
        setProcessingKey(`owner:${sensitiveAction.owner.id}`);
        await reactivateAdminOwner(session.token, sensitiveAction.owner.id, { rootPassword: confirmPassword });
        setPageMessage("Owner reativado com sucesso.");
        await loadAdminData();
      }

      if (sensitiveAction.type === "hard-delete-owner") {
        setProcessingKey(`owner:${sensitiveAction.owner.id}`);
        await hardDeleteAdminOwner(session.token, sensitiveAction.owner.id, {
          rootPassword: confirmPassword,
          confirmationText: hardDeleteConfirmationText,
        });
        setPageMessage("Owner excluido definitivamente.");
        await loadAdminData();
      }

      if (sensitiveAction.type === "deactivate-user") {
        setProcessingKey(`user:${sensitiveAction.user.id}`);
        await deactivateAdminUser(session.token, sensitiveAction.user.id, { password: confirmPassword });
        setPageMessage("Conta desativada com sucesso.");
        await loadAdminData();
      }

      if (sensitiveAction.type === "reactivate-user") {
        setProcessingKey(`user:${sensitiveAction.user.id}`);
        await reactivateAdminUser(session.token, sensitiveAction.user.id, { password: confirmPassword });
        setPageMessage("Conta reativada com sucesso.");
        await loadAdminData();
      }

      if (sensitiveAction.type === "delete-user") {
        setProcessingKey(`user:${sensitiveAction.user.id}`);
        await deleteAdminUser(session.token, sensitiveAction.user.id, { password: confirmPassword });
        setPageMessage("Conta excluida com sucesso.");
        await loadAdminData();
      }

      setSensitiveAction(null);
      setPlanDraft(null);
      setOwnerEditorDraft(null);
      setNewOwnerPassword("");
      setHardDeleteConfirmationText("");
      setConfirmPassword("");
      setConfirmErrorMessage("");
    } catch (error) {
      if (error instanceof ApiError && error.status === 401) {
        await clearSession();
        return;
      }

      setConfirmErrorMessage(error instanceof Error ? error.message : "Nao foi possivel validar essa acao agora.");
    } finally {
      setConfirmingSensitiveAction(false);
      setProcessingKey(null);
    }
  }

  async function handleCopyValue(value: string, successMessage: string) {
    try {
      await navigator.clipboard.writeText(value);
      setPageMessage(successMessage);
    } catch {
      setPageMessage("Nao foi possivel copiar agora.");
    }
  }

  function hideMasterPassword(companyId: string) {
    setRevealedMasterPasswords((currentValue) => {
      const nextValue = { ...currentValue };
      delete nextValue[companyId];
      return nextValue;
    });
  }

  function handlePlanPreset(planName: string) {
    const selectedPlan = STANDARD_PLANS.find((plan) => plan.planName === planName);
    if (!selectedPlan) {
      return;
    }

    setPlanDraft({
      planName: selectedPlan.planName,
      ...selectedPlan.features,
      maxUsers: selectedPlan.maxUsers,
    });
  }

  if (session.profile !== "admin") {
    return null;
  }

  const companiesActive = companies.filter((company) => company.isCompanyActive).length;
  const companiesInactive = companies.length - companiesActive;
  const aiActiveCount = companies.filter((company) => company.aiEnabled).length;
  const masterPendingCount = companies.filter((company) => !company.hasMasterPassword).length;
  const aiPendingCount = companies.filter((company) => company.aiConfigured && !company.aiEnabled).length;

  const dashboardMetrics: Array<{ label: string; value: number; tone: string; hint: string }> = [
    { label: "Unidades ativas", value: companiesActive, tone: "good", hint: `${companies.length} no total` },
    { label: "Unidades inativas", value: companiesInactive, tone: companiesInactive > 0 ? "danger" : "muted", hint: "Acesso bloqueado" },
    { label: "Pre-cadastros", value: pendingSignupUsers.length, tone: pendingSignupUsers.length > 0 ? "warning" : "muted", hint: "Aguardando liberacao" },
    { label: "IA ativa", value: aiActiveCount, tone: "info", hint: `${summary?.aiInteractionsToday ?? 0} interacoes hoje` },
    { label: "Pedidos hoje", value: summary?.ordersToday ?? 0, tone: "muted", hint: "Todas as unidades" },
    { label: "Senhas master pendentes", value: masterPendingCount, tone: masterPendingCount > 0 ? "warning" : "good", hint: "Gere quando precisar" },
  ];

  const alerts: Array<{ id: string; label: string; tone: string }> = [];
  if (pendingSignupUsers.length > 0) {
    alerts.push({ id: "pending", label: `${pendingSignupUsers.length} pre-cadastro(s) aguardando liberacao`, tone: "warning" });
  }
  if (companiesInactive > 0) {
    alerts.push({ id: "inactive", label: `${companiesInactive} unidade(s) com acesso bloqueado`, tone: "danger" });
  }
  if (masterPendingCount > 0) {
    alerts.push({ id: "master", label: `${masterPendingCount} unidade(s) sem senha master configurada`, tone: "warning" });
  }
  if (aiPendingCount > 0) {
    alerts.push({ id: "ai", label: `${aiPendingCount} unidade(s) com IA configurada mas desligada`, tone: "info" });
  }

  const navSections: Array<{ key: AdminSection; label: string; hint: string; badge?: number }> = [
    { key: "overview", label: "Visao geral", hint: "Resumo e alertas", badge: alerts.length || undefined },
    { key: "empresas", label: "Empresas", hint: "Operacao, plano, IA e senha master", badge: companies.length || undefined },
    { key: "acessos", label: "Acessos", hint: "Quem pode entrar", badge: pendingSignupUsers.length || undefined },
    { key: "codigos", label: "Codigos", hint: "Codigos de cadastro legados", badge: codes.length || undefined },
  ];

  const sectionMeta: Record<AdminSection, { title: string; copy: string }> = {
    overview: { title: "Visao geral", copy: "Resumo operacional das unidades, uso de IA e o que precisa de atencao agora." },
    empresas: { title: "Empresas", copy: "Central de operacao: fluxo do dia, plano, IA e senha master por unidade." },
    acessos: { title: "Acessos", copy: "Controle quem consegue entrar em cada empresa. Crie owners, libere logins e bloqueie contas sem mexer nos pedidos." },
    codigos: { title: "Codigos de cadastro", copy: "Codigos legados de liberacao. Remova os que nao usa mais." },
  };
  const activeMeta = sectionMeta[activeSection];

  function renderCompanyModal() {
    if (!selectedCompanyId) {
      return null;
    }

    const company = companies.find((item) => item.companyId === selectedCompanyId);
    if (!company) {
      return null;
    }

    const revealedPassword = revealedMasterPasswords[company.companyId];
    const isProcessingReveal =
      processingKey === `reveal-master:${company.companyId}` || processingKey === `rotate-master:${company.companyId}`;
    const isProcessingPlan = processingKey === `plan:${company.companyId}`;
    const close = () => setSelectedCompanyId(null);

    const dayFlow = [
      { label: "Pedidos", value: company.ordersToday },
      { label: "Delivery", value: company.deliveryOrdersToday },
      { label: "Pagos", value: company.paidOrdersToday },
      { label: "A cobrar", value: company.pendingPayments },
      { label: "Em aberto", value: company.openOrders },
      { label: "Apagados", value: company.deletedOrdersToday },
      { label: "IA hoje", value: company.aiInteractionsToday },
      { label: "Impressos", value: company.printedToday },
    ];

    return (
      <div className="admin-modal-backdrop" role="presentation" onClick={close}>
        <section
          className="surface-card admin-company-modal"
          role="dialog"
          aria-modal="true"
          onClick={(event) => event.stopPropagation()}
        >
          <div className="admin-company-modal-head">
            <div className="admin-company-modal-id">
              <span className="eyebrow">{company.accessSlug}</span>
              <h2>{company.restaurantName}</h2>
              <p>{company.ownerName} - {company.ownerEmail}</p>
            </div>
            <div className="admin-company-modal-aside">
              <span className={`status-chip ${company.isCompanyActive ? "available" : "inactive"}`}>
                {company.isCompanyActive ? "Ativa" : "Inativa"}
              </span>
              <button className="admin-company-modal-close" type="button" onClick={close} aria-label="Fechar">
                ×
              </button>
            </div>
          </div>

          <div className="admin-company-modal-body">
            <section className="admin-modal-block">
              <span className="admin-modal-block-title">Fluxo do dia</span>
              <div className="admin-modal-metric-grid">
                {dayFlow.map((metric) => (
                  <div key={metric.label}>
                    <strong>{metric.value}</strong>
                    <span>{metric.label}</span>
                  </div>
                ))}
              </div>
            </section>

            <section className="admin-modal-block">
              <span className="admin-modal-block-title">Inteligencia (IA)</span>
              <div className="admin-modal-chips">
                <span className={`admin-modal-chip ${company.aiConfigured ? "is-good" : "is-warn"}`}>
                  {company.aiConfigured ? "API pronta" : "API pendente"}
                </span>
                <span className={`admin-modal-chip ${company.aiEnabled ? "is-good" : "is-muted"}`}>
                  {company.aiEnabled ? "IA ativa" : "IA desligada"}
                </span>
                <span className="admin-modal-chip is-muted">{company.aiModel || "Modelo nao definido"}</span>
                <span className="admin-modal-chip is-muted">
                  Sucesso {company.successfulAiInteractionsToday}/{company.aiInteractionsToday}
                </span>
              </div>
            </section>

            <section className="admin-modal-block">
              <span className="admin-modal-block-title">Plano e dados</span>
              <div className="admin-modal-plan-row">
                <div>
                  <strong>{company.planName}</strong>
                  <span>
                    {formatCurrency(company.monthlyPrice)} / mes - {company.teamMembersCount}/{company.maxUsers} usuarios
                  </span>
                </div>
                <button
                  className="primary-link button-link"
                  type="button"
                  disabled={isProcessingPlan}
                  onClick={() => openSensitiveAction({ type: "edit-plan", company })}
                >
                  {isProcessingPlan ? "Salvando..." : "Gerenciar plano"}
                </button>
              </div>
              <div className="admin-modal-chips">
                <span className="admin-modal-chip is-muted">{countEnabledModules(company)} modulos</span>
                <span className="admin-modal-chip is-muted">Mesas {company.tablesCount}</span>
                <span className="admin-modal-chip is-muted">Cardapio {company.menuItemsCount}</span>
                <span className="admin-modal-chip is-muted">Estoque {company.stockItemsCount}</span>
              </div>
              <p className="admin-modal-modules">{describePlanModules(company)}</p>
            </section>

            <section className="admin-modal-block">
              <span className="admin-modal-block-title">Senha master</span>
              <div className="admin-modal-master-head">
                <span className={`status-chip ${company.hasMasterPassword ? "available" : "warning"}`}>
                  {company.hasMasterPassword ? "Configurada" : "Gerar agora"}
                </span>
                <small>Atualizada {formatOptionalDate(company.masterPasswordRotatedAtUtc)}</small>
              </div>
              {revealedPassword ? (
                <div className="admin-modal-master-reveal">
                  <code>{revealedPassword}</code>
                  <div className="toolbar-actions compact">
                    <button
                      className="ghost-link button-link"
                      type="button"
                      onClick={() => void handleCopyValue(revealedPassword, `Senha master de ${company.restaurantName} copiada.`)}
                    >
                      Copiar
                    </button>
                    <button className="ghost-link button-link" type="button" onClick={() => hideMasterPassword(company.companyId)}>
                      Ocultar
                    </button>
                  </div>
                </div>
              ) : (
                <p className="admin-master-mask">Oculta por seguranca. Libere com sua senha root.</p>
              )}
              <div className="toolbar-actions compact">
                <button className="ghost-link button-link" type="button" disabled={isProcessingReveal} onClick={() => openSensitiveAction({ type: "reveal-master", company })}>
                  {isProcessingReveal ? "Processando..." : "Liberar"}
                </button>
                <button className="ghost-link button-link" type="button" disabled={isProcessingReveal} onClick={() => openSensitiveAction({ type: "rotate-master", company })}>
                  Rotacionar
                </button>
              </div>
            </section>

            <section className="admin-modal-block">
              <span className="admin-modal-block-title">Zona de cuidado</span>
              <p className="admin-section-copy">
                Use quando a empresa for teste ou nao deve mais aparecer no painel. O historico fica preservado.
              </p>
              <div className="toolbar-actions compact">
                <button
                  className="ghost-link button-link admin-danger-button"
                  type="button"
                  disabled={processingKey === `company:${company.companyId}`}
                  onClick={() => openSensitiveAction({ type: "delete-company", company })}
                >
                  {processingKey === `company:${company.companyId}` ? "Apagando..." : "Apagar empresa"}
                </button>
              </div>
            </section>
          </div>
        </section>
      </div>
    );
  }

  return (
    <main className="page-shell app-shell admin-shell">
      <div className="admin-shell-grid">
        <aside className="admin-sidebar">
          <div className="admin-sidebar-brand">
            <span className="eyebrow">ZeroPaper</span>
            <strong>Admin</strong>
          </div>
          <nav className="admin-nav" aria-label="Areas do painel">
            {navSections.map((item) => (
              <button
                key={item.key}
                type="button"
                className={activeSection === item.key ? "admin-nav-item is-active" : "admin-nav-item"}
                aria-current={activeSection === item.key ? "page" : undefined}
                onClick={() => setActiveSection(item.key)}
              >
                <span className="admin-nav-label">{item.label}</span>
                <span className="admin-nav-hint">{item.hint}</span>
                {item.badge ? <em className="admin-nav-badge">{item.badge}</em> : null}
              </button>
            ))}
          </nav>
        </aside>

        <div className="admin-content">
          <header className="admin-content-head">
            <div>
              <span className="eyebrow">ZeroPaper Root</span>
              <h1 className="admin-title">{activeMeta.title}</h1>
              <p className="body-copy">{activeMeta.copy}</p>
            </div>
          </header>

          {pageMessage ? (
            <section className="surface-card admin-inline-note">
              <span className="eyebrow">Painel</span>
              <p>{pageMessage}</p>
            </section>
          ) : null}

          {activeSection === "overview" ? (
            <>
              <section className="admin-metric-grid" aria-label="Indicadores">
                {dashboardMetrics.map((metric) => (
                  <article key={metric.label} className={`admin-metric-card is-${metric.tone}`}>
                    <span className="admin-metric-label">{metric.label}</span>
                    <strong className="admin-metric-value">{metric.value}</strong>
                    <span className="admin-metric-hint">{metric.hint}</span>
                  </article>
                ))}
              </section>

              <section className="surface-card admin-alerts-card">
                <div className="module-section-head">
                  <div>
                    <span className="eyebrow">Precisa de atencao</span>
                    <strong>{alerts.length === 0 ? "Tudo em ordem" : `${alerts.length} alerta(s)`}</strong>
                  </div>
                </div>
                {alerts.length === 0 ? (
                  <div className="module-empty-state">
                    <strong>Nenhum alerta critico agora.</strong>
                    <p>Liberacoes, bloqueios e senhas master estao em dia.</p>
                  </div>
                ) : (
                  <div className="admin-alert-list">
                    {alerts.map((alert) => (
                      <div key={alert.id} className={`admin-alert-row is-${alert.tone}`}>
                        <span className="admin-alert-dot" aria-hidden="true" />
                        <span>{alert.label}</span>
                      </div>
                    ))}
                  </div>
                )}
              </section>
            </>
          ) : null}

      {activeSection === "overview" ? (
      <section className="surface-card module-list-card admin-pending-signups">
        <div className="module-section-head">
          <div>
            <span className="eyebrow">Pre-cadastros</span>
            <strong>{pendingSignupUsers.length} aguardando liberacao</strong>
          </div>
          <p className="admin-section-copy">
            Cada solicitacao ja criou o acesso bloqueado. Revise o contato e libere o login quando estiver pronto para atender.
          </p>
        </div>

        {errorMessage ? <p className="module-feedback error">{errorMessage}</p> : null}

        {loading ? (
          <p className="loading-state">Carregando pre-cadastros...</p>
        ) : pendingSignupUsers.length === 0 ? (
          <div className="module-empty-state">
            <strong>Nenhum pre-cadastro aguardando agora.</strong>
          </div>
        ) : (
          <div className="module-card-list admin-scroll-list">
            {pendingSignupUsers.map((user) => {
              const pendingCompany = companies.find(
                (company) => company.restaurantName === user.restaurantName && company.ownerEmail === user.email,
              );
              const isProcessing = processingKey === `user:${user.id}`;
              const isRejecting = pendingCompany ? processingKey === `company:${pendingCompany.companyId}` : isProcessing;

              return (
                <article key={user.id} className="module-entity-card interactive-card admin-pending-card">
                  <div className="entity-head">
                    <div>
                      <h3>{user.restaurantName}</h3>
                      <p>{user.fullName}</p>
                    </div>
                    <span className="status-chip warning">Aguardando</span>
                  </div>

                  <div className="entity-meta-grid admin-meta-line admin-pending-meta">
                    <span><b>Email</b>{user.email}</span>
                    <span><b>WhatsApp</b>{pendingCompany?.contactPhone || "Nao informado"}</span>
                    <span><b>Plano</b>{pendingCompany?.planName || "Em revisao"}</span>
                  </div>

                  <div className="toolbar-actions compact admin-card-actions">
                    <button
                      className="primary-link button-link"
                      type="button"
                      disabled={isProcessing || isRejecting}
                      onClick={() => openSensitiveAction({ type: "reactivate-user", user })}
                    >
                      {isProcessing ? "Liberando..." : "Liberar login"}
                    </button>
                    <button
                      className="ghost-link button-link admin-danger-button"
                      type="button"
                      disabled={isProcessing || isRejecting}
                      onClick={() =>
                        pendingCompany
                          ? openSensitiveAction({ type: "reject-signup", company: pendingCompany, user })
                          : openSensitiveAction({ type: "reject-user", user })
                      }
                    >
                      {isRejecting ? "Rejeitando..." : "Rejeitar"}
                    </button>
                  </div>
                </article>
              );
            })}
          </div>
        )}
      </section>
      ) : null}

      {activeSection === "codigos" ? (
        <section className="surface-card module-list-card admin-legacy-signup-codes">
          <div className="module-section-head">
            <div>
              <span className="eyebrow">Codigos antigos</span>
              <strong>{codes.length} registros legados</strong>
            </div>
            <div className="toolbar-actions compact">
              <button className="ghost-link button-link" type="button" disabled={loading || codes.length === 0} onClick={() => openSensitiveAction({ type: "cleanup-codes" })}>
                Limpar usados
              </button>
            </div>
          </div>

          {loading ? (
            <p className="loading-state">Carregando codigos...</p>
          ) : sortedCodes.length === 0 ? (
            <div className="module-empty-state">
              <strong>Nenhum codigo gerado.</strong>
            </div>
          ) : (
            <div className="module-card-list admin-scroll-list">
              {sortedCodes.map((code) => {
                const status = resolveCodeStatus(code);
                const isProcessing = processingKey === `code:${code.id}`;

                return (
                  <article key={code.id} className="module-entity-card interactive-card">
                    <div className="entity-head">
                      <div>
                        <h3>{code.label}</h3>
                        <p>{code.boundEmail || "Sem email vinculado"}</p>
                      </div>
                      <span className={`status-chip ${status.tone}`}>{status.label}</span>
                    </div>

                    <div className="entity-meta-grid admin-meta-line">
                      <span>Criado em {formatDate(code.createdAtUtc)}</span>
                      <span>Usos {code.usedCount}/{code.maxUses}</span>
                      <span>Ultimo uso {formatOptionalDate(code.lastUsedAtUtc)}</span>
                    </div>

                    <div className="toolbar-actions compact admin-card-actions">
                      <button className="ghost-link button-link admin-danger-button" type="button" disabled={isProcessing} onClick={() => openSensitiveAction({ type: "delete-code", code })}>
                        {isProcessing ? "Processando..." : "Remover"}
                      </button>
                    </div>
                  </article>
                );
              })}
            </div>
          )}
        </section>
      ) : null}

      {activeSection === "acessos" ? (
      <section className="admin-panel-grid">
        <section className="surface-card module-form-card">
          <span className="eyebrow">Owners</span>
          <h2>Criar owner</h2>
          <p className="admin-section-copy">
            Escolha a empresa, informe o dono e defina uma senha temporaria.
          </p>

          <form className="module-form" onSubmit={handleCreateOwner}>
            <div className="field-group">
              <label htmlFor="ownerCompanyId">Unidade</label>
              <select id="ownerCompanyId" name="companyId" disabled={companies.length === 0}>
                <option value="">Selecione uma unidade</option>
                {companies.map((company) => (
                  <option key={company.companyId} value={company.companyId}>
                    {company.restaurantName}
                  </option>
                ))}
              </select>
            </div>

            <div className="field-group">
              <label htmlFor="ownerFullName">Nome do owner</label>
              <input id="ownerFullName" name="fullName" placeholder="Ex.: Pedro Alves" />
            </div>

            <div className="field-group">
              <label htmlFor="ownerEmail">Email de acesso</label>
              <input id="ownerEmail" name="email" type="email" placeholder="owner@restaurante.com" />
            </div>

            <div className="field-group">
              <label htmlFor="ownerPassword">Senha temporaria do owner</label>
              <input id="ownerPassword" name="ownerPassword" type="password" placeholder="Minimo 8 caracteres" autoComplete="new-password" />
            </div>

            <div className="field-group">
              <label htmlFor="ownerRootPassword">Sua senha root</label>
              <input id="ownerRootPassword" name="rootPassword" type="password" placeholder="Confirme para criar" autoComplete="current-password" />
            </div>

            <button className="primary-link button-link" type="submit" disabled={submitting || companies.length === 0}>
              {submitting ? "Criando..." : "Criar owner"}
            </button>
          </form>
        </section>

        <section className="surface-card module-list-card">
          <div className="module-section-head">
            <div>
              <span className="eyebrow">Controle de owners</span>
              <strong>{owners.length} owners</strong>
            </div>
            <p className="admin-section-copy">
              Owners sao os donos da empresa. Desativar bloqueia o login sem apagar pedidos.
            </p>
          </div>

          {loading ? (
            <p className="loading-state">Carregando owners...</p>
          ) : owners.length === 0 ? (
            <div className="module-empty-state">
              <strong>Nenhum owner cadastrado.</strong>
            </div>
          ) : (
            <div className="module-card-list admin-scroll-list">
              {owners.map((owner) => {
                const isEnabled = owner.isActive && owner.isCompanyActive;
                const isProcessing = processingKey === `owner:${owner.id}`;
                const accessLabel = owner.hasActiveSession ? "Acesso aberto" : "Sem sessao aberta";
                const companyOwnerCount = ownerCountsByCompany[owner.companyId] ?? { total: 0, active: 0 };
                const isOnlyOwnerInCompany = companyOwnerCount.total <= 1;
                const isLastActiveOwnerInCompany = owner.isActive && companyOwnerCount.active <= 1;

                return (
                  <article key={owner.id} className="module-entity-card interactive-card">
                    <div className="entity-head">
                      <div>
                        <h3>{owner.fullName}</h3>
                        <p>{owner.email}</p>
                      </div>
                      <div className="entity-status-stack">
                        <span className={`status-chip ${isEnabled ? "available" : "inactive"}`}>{isEnabled ? "Owner ativo" : "Owner inativo"}</span>
                      </div>
                    </div>

                    <div className="entity-meta-grid admin-meta-line">
                      <span>Empresa: {owner.companyName}</span>
                      <span>{accessLabel}</span>
                      <span>{owner.activeSessionCount} sessao(oes)</span>
                      <span>Ultimo login: {formatOptionalDate(owner.lastLoginAtUtc)}</span>
                    </div>

                    <div className="toolbar-actions compact admin-card-actions">
                      <button className="ghost-link button-link" type="button" disabled={isProcessing} onClick={() => openSensitiveAction({ type: "edit-owner", owner })}>
                        Editar
                      </button>
                      <button className="ghost-link button-link" type="button" disabled={isProcessing} onClick={() => openSensitiveAction({ type: "reset-owner-password", owner })}>
                        Trocar senha
                      </button>
                      <button
                        className="ghost-link button-link"
                        type="button"
                        disabled={isProcessing || isLastActiveOwnerInCompany}
                        title={isLastActiveOwnerInCompany ? "Cadastre ou reative outro owner antes de bloquear o unico acesso ativo da unidade." : undefined}
                        onClick={() => openSensitiveAction({ type: owner.isActive ? "deactivate-owner" : "reactivate-owner", owner })}
                      >
                        {isProcessing ? "Processando..." : owner.isActive ? "Desativar" : "Reativar"}
                      </button>
                      <button
                        className="ghost-link button-link admin-danger-button"
                        type="button"
                        disabled={isProcessing || isOnlyOwnerInCompany}
                        title={isOnlyOwnerInCompany ? "Este e o unico owner da unidade. Cadastre outro owner antes de excluir este acesso." : undefined}
                        onClick={() => openSensitiveAction({ type: "hard-delete-owner", owner })}
                      >
                        {isOnlyOwnerInCompany ? "Unico owner" : "Excluir definitivo"}
                      </button>
                    </div>
                  </article>
                );
              })}
            </div>
          )}
        </section>
      </section>
      ) : null}

      {activeSection === "empresas" ? (
      <section className="surface-card admin-operations-card">
        <div className="module-section-head">
          <div>
            <span className="eyebrow">Operacao das unidades</span>
            <strong>{companies.length} unidades acompanhadas</strong>
          </div>
          <p className="admin-section-copy">
            Fluxo do dia, uso de IA, dados principais e a senha master segura ficam concentrados aqui para configuracao e suporte.
          </p>
        </div>

        {loading ? (
          <p className="loading-state">Carregando operacao das unidades...</p>
        ) : companies.length === 0 ? (
          <div className="module-empty-state">
            <strong>Nenhuma unidade cadastrada ainda.</strong>
          </div>
        ) : (
          <div className="admin-company-cards">
            {companies.map((company) => (
              <article key={company.companyId} className="admin-company-card">
                <div className="admin-company-card-head">
                  <div className="admin-company-card-id">
                    <strong>{company.restaurantName}</strong>
                    <span>{company.ownerName}</span>
                  </div>
                  <span className={`status-chip ${company.isCompanyActive ? "available" : "inactive"}`}>
                    {company.isCompanyActive ? "Ativa" : "Inativa"}
                  </span>
                </div>
                <div className="admin-company-card-metrics">
                  <div><strong>{company.ordersToday}</strong><span>Pedidos hoje</span></div>
                  <div><strong>{company.pendingPayments}</strong><span>A cobrar</span></div>
                  <div><strong>{company.aiInteractionsToday}</strong><span>IA hoje</span></div>
                </div>
                <div className="admin-company-card-foot">
                  <span className="admin-company-plan-chip">
                    {company.planName.replace("ZeroPaper ", "")} - {formatCurrency(company.monthlyPrice)}
                  </span>
                  <button
                    className="primary-link button-link admin-company-manage"
                    type="button"
                    onClick={() => setSelectedCompanyId(company.companyId)}
                  >
                    Gerenciar
                  </button>
                </div>
              </article>
            ))}
          </div>
        )}
      </section>
      ) : null}

      {activeSection === "acessos" ? (
      <section className="surface-card module-list-card">
        <div className="module-section-head">
          <div>
            <span className="eyebrow">Contas</span>
            <strong>{users.length} usuarios</strong>
          </div>
          <p className="admin-section-copy">
            Contas criadas pelo cadastro aparecem aqui. Libere ou bloqueie sem precisar procurar em outra tela.
          </p>
        </div>

        {loading ? (
          <p className="loading-state">Carregando contas...</p>
        ) : sortedUsers.length === 0 ? (
          <div className="module-empty-state">
            <strong>Nenhuma conta cadastrada ainda.</strong>
          </div>
        ) : (
          <div className="module-card-list admin-scroll-list">
            {sortedUsers.map((user) => {
              const presenceStatus = resolvePresenceStatus(user);
              const isEnabled = user.isActive && user.isCompanyActive;
              const isRoot = user.role === "Root";
              const isProcessing = processingKey === `user:${user.id}`;
              const accessLabel = !user.isActive
                ? "Aguardando liberacao"
                : user.hasActiveSession
                  ? "Acesso aberto"
                  : "Pronta para login";

              return (
                <article key={user.id} className="module-entity-card interactive-card">
                  <div className="entity-head">
                    <div>
                      <h3>{user.fullName}</h3>
                      <p>{user.email}</p>
                    </div>
                    <div className="entity-status-stack">
                      <span className={`status-chip ${isEnabled ? "available" : "inactive"}`}>{isEnabled ? "Conta ativa" : "Conta inativa"}</span>
                      <span className={`status-chip ${presenceStatus.tone}`}>{presenceStatus.label}</span>
                    </div>
                  </div>

                  <div className="entity-meta-grid admin-meta-line">
                    <span>Empresa: {user.restaurantName}</span>
                    <span>Tipo: {formatRole(user.role)}</span>
                    <span>{accessLabel}</span>
                    <span>{user.activeSessionCount} sessao(oes)</span>
                    <span>Ultimo login: {formatOptionalDate(user.lastLoginAtUtc)}</span>
                  </div>

                  <div className="toolbar-actions compact admin-card-actions">
                    <button className="ghost-link button-link" type="button" disabled={isRoot || isProcessing} onClick={() => openSensitiveAction({ type: user.isActive ? "deactivate-user" : "reactivate-user", user })}>
                      {isProcessing ? "Processando..." : user.isActive ? "Desativar" : "Liberar login"}
                    </button>
                    <button className="ghost-link button-link admin-danger-button" type="button" disabled={isRoot || isProcessing} onClick={() => openSensitiveAction({ type: "delete-user", user })}>
                      Excluir
                    </button>
                  </div>
                </article>
              );
            })}
          </div>
        )}
      </section>
      ) : null}
        </div>
      </div>

      {renderCompanyModal()}

      {sensitiveAction ? (
        <div className="admin-modal-backdrop" role="presentation">
          <section
            className={`surface-card admin-sensitive-modal${sensitiveAction.type === "edit-plan" ? " admin-plan-modal" : ""}${sensitiveAction.type === "delete-company" || sensitiveAction.type === "reject-signup" ? " admin-delete-company-modal" : ""}`}
            role="dialog"
            aria-modal="true"
          >
            <span className="eyebrow">Confirmacao</span>
            <h2>{getSensitiveActionCopy(sensitiveAction).title}</h2>
            <p>{getSensitiveActionCopy(sensitiveAction).description}</p>

            <form className="module-form" onSubmit={handleSensitiveActionSubmit}>
              {sensitiveAction.type === "edit-plan" && planDraft ? (
                <div className="admin-plan-picker">
                  {STANDARD_PLANS.map((plan) => {
                    const selected = planDraft.planName === plan.planName;
                    const modules = PLAN_FEATURES.filter((feature) => plan.features[feature.key]).map(
                      (feature) => feature.label,
                    );

                    return (
                      <button
                        key={plan.planName}
                        type="button"
                        className={`admin-plan-option${selected ? " is-selected" : ""}`}
                        aria-pressed={selected}
                        onClick={() => handlePlanPreset(plan.planName)}
                      >
                        <span className="admin-plan-option-name">{plan.label}</span>
                        <strong className="admin-plan-option-price">
                          {formatCurrency(plan.price)}
                          <small>/mes</small>
                        </strong>
                        <span className="admin-plan-option-users">Ate {plan.maxUsers} usuarios</span>
                        <span className="admin-plan-option-modules">{modules.join(", ")}</span>
                        {selected ? <span className="admin-plan-option-check">Selecionado</span> : null}
                      </button>
                    );
                  })}
                </div>
              ) : null}

              {sensitiveAction.type === "edit-owner" && ownerEditorDraft ? (
                <>
                  <div className="field-group">
                    <label htmlFor="ownerEditFullName">Nome do owner</label>
                    <input
                      id="ownerEditFullName"
                      value={ownerEditorDraft.fullName}
                      onChange={(event) =>
                        setOwnerEditorDraft((currentValue) =>
                          currentValue ? { ...currentValue, fullName: event.currentTarget.value } : currentValue,
                        )
                      }
                    />
                  </div>

                  <div className="field-group">
                    <label htmlFor="ownerEditEmail">Email de acesso</label>
                    <input
                      id="ownerEditEmail"
                      type="email"
                      value={ownerEditorDraft.email}
                      onChange={(event) =>
                        setOwnerEditorDraft((currentValue) =>
                          currentValue ? { ...currentValue, email: event.currentTarget.value } : currentValue,
                        )
                      }
                    />
                  </div>
                </>
              ) : null}

              {sensitiveAction.type === "reset-owner-password" ? (
                <div className="field-group">
                  <label htmlFor="newOwnerPassword">Nova senha temporaria</label>
                  <input
                    id="newOwnerPassword"
                    type="password"
                    value={newOwnerPassword}
                    onChange={(event) => setNewOwnerPassword(event.currentTarget.value)}
                    placeholder="Minimo 8 caracteres"
                    autoComplete="new-password"
                  />
                </div>
              ) : null}

              {sensitiveAction.type === "hard-delete-owner" ? (
                <div className="module-empty-state">
                  <strong>Confirmacao destrutiva</strong>
                  <p>
                    Para excluir definitivamente, digite <strong>EXCLUIR OWNER</strong>. Esta acao remove a conta e suas sessoes, mas nao remove a unidade nem seus dados operacionais.
                  </p>
                  <div className="field-group">
                    <label htmlFor="hardDeleteOwnerConfirmation">Texto de confirmacao</label>
                    <input
                      id="hardDeleteOwnerConfirmation"
                      value={hardDeleteConfirmationText}
                      onChange={(event) => setHardDeleteConfirmationText(event.currentTarget.value)}
                      placeholder="EXCLUIR OWNER"
                    />
                  </div>
                </div>
              ) : null}

              {sensitiveAction.type === "delete-company" ? (
                <div className="module-empty-state">
                  <strong>Confirmacao da empresa</strong>
                  <p>
                    Digite o nome para liberar o botao: <strong>{sensitiveAction.company.restaurantName}</strong>.
                    Os dados historicos ficam guardados, mas a unidade e os owners deixam de acessar.
                  </p>
                  <div className="field-group">
                    <label htmlFor="deleteCompanyConfirmation">Nome da empresa</label>
                    <input
                      id="deleteCompanyConfirmation"
                      value={hardDeleteConfirmationText}
                      onChange={(event) => setHardDeleteConfirmationText(event.currentTarget.value)}
                      placeholder={sensitiveAction.company.restaurantName}
                      autoFocus
                    />
                  </div>
                </div>
              ) : null}

              <div className="field-group">
                <label htmlFor="confirmPassword">Sua senha root</label>
                <input
                  id="confirmPassword"
                  name="confirmPassword"
                  type="password"
                  value={confirmPassword}
                  onChange={(event) => setConfirmPassword(event.currentTarget.value)}
                  placeholder="Digite sua senha root"
                  autoComplete="current-password"
                />
              </div>

              {confirmErrorMessage ? <p className="module-feedback error">{confirmErrorMessage}</p> : null}

              <div className="toolbar-actions compact admin-modal-actions">
                <button
                  className="primary-link button-link"
                  type="submit"
                  disabled={
                    confirmingSensitiveAction ||
                    (sensitiveAction.type === "delete-company" &&
                      normalizeAdminConfirmation(hardDeleteConfirmationText) !== normalizeAdminConfirmation(sensitiveAction.company.restaurantName))
                  }
                >
                  {confirmingSensitiveAction ? "Validando..." : getSensitiveActionCopy(sensitiveAction).buttonLabel}
                </button>
                <button className="ghost-link button-link" type="button" onClick={closeSensitiveAction}>
                  Cancelar
                </button>
              </div>
            </form>
          </section>
        </div>
      ) : null}
    </main>
  );
}
