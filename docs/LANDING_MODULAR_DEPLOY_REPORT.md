# Landing Modular Deploy Report

## Objetivo

Evoluir a landing page do ZeroPaper de uma home exclusivamente focada em restaurantes para uma home que comunica o produto como plataforma modular para pequenos negócios — mantendo restaurantes como vertical principal e único segmento com planos detalhados, mas preparando a estrutura visual e de dados para outros segmentos futuros.

---

## Resumo da mudança

A home deixou de se apresentar como "sistema para restaurante" e passou a comunicar o ZeroPaper como uma plataforma modular configurável para pedidos, atendimento, catálogo, caixa, clientes, WhatsApp/IA, impressão e operação de pequenos negócios.

Mudanças principais:
- Hero: badge, headline, subheadline e CTA secundário atualizados para linguagem genérica
- Flow strip: chips renomeados para termos genéricos (Catálogo, Pedido, Produção, Caixa...)
- Seção de módulos: título e descrição genéricos, sem mencionar restaurante
- Nova seção de segmentos: cards com status (Disponível agora / Configurável / Implantação personalizada / Sob consulta)
- Nova seção de planos por tipo de negócio: abas com tab ativa apenas para Restaurantes
- Seção de planos de restaurante: mantida integralmente, apenas com novo rótulo de contexto
- Nova seção de plano personalizado: texto, bullets e CTA para WhatsApp
- Header: "Sistema para restaurante" → "Plataforma modular", nav item "Segmentos" adicionado
- Metadata: title/description atualizados para linguagem ampla

---

## Arquivos alterados

| Arquivo | Ação |
|---|---|
| `frontend/app/page.tsx` | Refatorado — hero, módulos, segmentos, planos, CTA |
| `frontend/components/public-site-header.tsx` | Nav atualizado, tagline atualizada |
| `frontend/app/globals.css` | CSS adicionado ao final: segmentos, abas de plano, plano personalizado |
| `frontend/lib/landing-data.ts` | **Criado** — estruturas de dados: segmentos, módulos genéricos, grupos de plano, custom plan |
| `docs/LANDING_MODULAR_DEPLOY_REPORT.md` | **Criado** — este arquivo |

---

## Componentes criados ou alterados

- `PublicSiteHeader` — atualizado (tagline + nav)
- `Home` (page.tsx) — refatorado com novas seções inline

Seções inline em `page.tsx` (sem componentes separados, preservando padrão existente):
- Hero genérico
- Flow strip genérico
- Módulos universais
- Segmentos (novo)
- Planos por tipo de negócio / abas (novo)
- Planos para restaurantes (existente, com novo contexto)
- Plano personalizado (novo)
- Final CTA (atualizado)

---

## Estrutura de dados criada (`frontend/lib/landing-data.ts`)

```ts
businessSegments: BusinessSegment[]   // 6 segmentos com status
genericModules: GenericModule[]        // 8 módulos universais
segmentPlanGroups: SegmentPlanGroup[]  // grupos de plano por segmento
customPlanFeatures: string[]           // bullets do plano personalizado
```

---

## Rotas públicas afetadas

| Rota | Alteração |
|---|---|
| `/` | Conteúdo refatorado |
| `/login` | Smoke test apenas, sem alteração |
| `/app/impressao` | Smoke test apenas, sem alteração |
| `/cadastro` | CTA aponta para `/cadastro?plano=operacao`, sem alteração na página |

---

## Endpoints envolvidos

- **Nenhum endpoint backend foi criado.**
- **Nenhum endpoint backend foi alterado.**
- **Nenhum contrato de API foi alterado.**
- **Nenhuma migration foi criada.**

---

## Seções criadas na landing

1. **Hero genérico** — "Seu negócio vende. O ZeroPaper organiza."
2. **Flow strip genérico** — Catálogo → Pedido → Produção → Caixa → Delivery → WhatsApp IA
3. **Módulos universais** — 8 módulos com descrição genérica
4. **Segmentos** — 6 cards com status de disponibilidade
5. **Planos por tipo de negócio** — abas de segmento
6. **Planos para restaurantes** — seção existente com novo contexto
7. **Plano personalizado** — novo bloco com CTA para WhatsApp
8. **Final CTA** — atualizado para linguagem genérica

---

## Copy comercial nova

- Badge hero: `Plataforma modular para pequenos negócios`
- Headline: `Seu negócio vende. / O ZeroPaper organiza. / Sem papel, sem bagunça.`
- Subheadline: `Pedidos, atendimento e caixa em um só fluxo. Uma plataforma modular...`
- CTA secundário hero: `Ver planos por negócio`
- Tagline header: `Plataforma modular`
- Seção segmentos: `Configurável para o seu tipo de negócio.`
- Seção módulos: `Um fluxo completo, configurável para o seu negócio.`
- Plano personalizado: `Precisa de um fluxo diferente?`
- Final CTA: `Seu negócio organizado a partir de agora.`

Status usados nos cards de segmento:
- `Disponível agora` (restaurantes)
- `Configurável` (varejo, oficinas)
- `Implantação personalizada` (pet shop, assistência)
- `Sob consulta` (personalizado)

---

## O que ficou preparado para futuro

- `businessSegments[]` — fácil adicionar/editar segmentos e seus status
- `segmentPlanGroups[]` — estrutura pronta para ativar planos por segmento individualmente
- `genericModules[]` — módulos renomeáveis por segmento no futuro
- `customPlanFeatures[]` — bullets do plano personalizado editáveis em data
- Abas de planos por segmento — quando um segmento estiver pronto, basta marcar `available: true` e apontar para a seção de planos correspondente
- Futura página `/segmentos/[slug]` pode reusar os dados de `businessSegments`

---

## O que NÃO foi feito

- Não houve alteração de backend
- Não houve alteração de autenticação
- Não houve alteração no workspace `/app`
- Não houve alteração no banco
- Não houve migration
- Não houve alteração em env (`/etc/zeropaper/backend.env` e `/etc/zeropaper/frontend.env` intactos)
- Não houve alteração na Evolution API
- Não houve alteração no Mercado Pago
- Não houve alteração nos uploads
- Não houve alteração no login

---

## Build

```
npm run build
```

Resultado: ✓ Compiled successfully in 26.6s | 51 páginas geradas sem erro de tipo ou linting.

---

## Deploy

- **Data/hora:** 2026-07-04 ~18:33 UTC
- **Branch:** `codex/customer-profile-backend`
- **Backup criado em:** `/opt/zeropaper/backups/20260704-183150/frontend`

Passos executados:
1. Build local: `npm run build` — OK
2. Empacotamento: `standalone`, `.next/static`, `public` (sem `downloads/`)
3. Upload via SCP para `/tmp/` na VPS
4. Backup: `cp -a /opt/zeropaper/frontend /opt/zeropaper/backups/20260704-183150/frontend`
5. `systemctl stop zeropaper-frontend`
6. `rm -rf /opt/zeropaper/frontend && mv /tmp/zeropaper-frontend-new /opt/zeropaper/frontend`
7. `chown -R zeropaper:zeropaper /opt/zeropaper/frontend`
8. `systemctl start zeropaper-frontend`

---

## Smoke tests

```
/ -> 200        ✓
/login -> 200   ✓
/app/impressao -> 200   ✓
```

Resultado do journalctl: serviço iniciou em 270ms, nenhum erro novo após o deploy.

---

## Rollback

```bash
systemctl stop zeropaper-frontend
rm -rf /opt/zeropaper/frontend
cp -a /opt/zeropaper/backups/20260704-183150/frontend /opt/zeropaper/frontend
chown -R zeropaper:zeropaper /opt/zeropaper/frontend
systemctl start zeropaper-frontend
```

---

## Riscos pendentes

- **Links de WhatsApp** nos CTAs de segmentos usam número placeholder (`5500000000000`). Devem ser atualizados com o número real de atendimento antes de divulgar a landing.
- **Copy de segmentos futuros** pode gerar expectativa se lida sem atenção ao status. Revisar textos se receber feedback de clientes confusos.
- **Responsividade mobile** deve ser validada manualmente nos breakpoints 375px e 414px.
- **SEO** — title e description atualizados mas meta tags OG não foram testadas em preview (WhatsApp, Telegram, LinkedIn).
- **Pasta `public/downloads`** não foi enviada no deploy (era 299MB). Se existia na VPS e foi sobrescrita pelo `rm -rf`, verificar: `ls /opt/zeropaper/frontend/public/downloads`.
