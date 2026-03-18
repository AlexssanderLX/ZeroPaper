import { notFound } from "next/navigation";
import { ModuleScreen } from "@/components/module-screen";
import { getModuleBySlug, ownerModules } from "@/lib/owner-portal";

export function generateStaticParams() {
  return ownerModules.map((module) => ({ module: module.slug }));
}

export default async function ModulePage({
  params,
}: {
  params: Promise<{ module: string }>;
}) {
  const { module: moduleSlug } = await params;
  const module = getModuleBySlug(moduleSlug);

  if (!module) {
    notFound();
  }

  return <ModuleScreen module={module} />;
}
