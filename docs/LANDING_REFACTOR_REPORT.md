# Landing Refactor Report — v3

**Data:** 2026-07-04  
**Branch:** `codex/customer-profile-backend`

---

## Objetivo

Refatorar completamente a home pública do ZeroPaper de uma página focada em restaurante para uma landing page moderna, modular e genérica que:

- Comunica o ZeroPaper como plataforma modular para pequenos negócios
- Remove cards de preço da home principal
- Apresenta segmentos, módulos e como funciona
- Cria rotas dedicadas para planos e segmentos
- Redesenha a navbar com visual glass/pill flutuante
- Melhora o visual hero para ser multi-negócio
- Adiciona seções: Segmentos, Módulos, Como Funciona, Benefícios

---

## Arquivos alterados

| Arquivo | Ação |
|---|---|
| `frontend/app/page.tsx` | Refatoração completa |
| `frontend/components/public-site-header.tsx` | Navbar completamente reescrita |
| `frontend/app/globals.css` | CSS da navbar nova e de todas as seções novas |
| `frontend/lib/landing-data.ts` | Dados atualizados com `platformModules`, `howItWorksSteps`, `benefits` |

---

## Arquivos criados

| Arquivo | Rota |
|---|---|
| `frontend/app/segmentos/page.tsx` | `/segmentos` |
| `frontend/app/segmentos/restaurantes/page.tsx` | `/segmentos/restaurantes` |
| `frontend/app/planos/page.tsx` | `/planos` |

---

## Componentes alterados

### `PublicSiteHeader`
- Classe nova: `.zpnav-*` (em vez de `.zp-home-header`)
- Visual pill flutuante com glass: `border-radius: 999px`, `backdrop-filter: blur(22px)`
- Burger menu mobile a partir de 900px
- Drawer lateral com transição suave
- Links: Segmentos, Recursos, Como funciona
- Botão "Entrar" discreto, "Começar agora" com gradiente verde

### `Home` (page.tsx)
Seções na nova ordem:
1. **Hero** — badge, headline 3 linhas, subheadline, 2 CTAs, painel de dashboard genérico (sem cozinha/mesa como foco)
2. **Flow strip** — chips genéricos
3. **Segmentos** — 6 cards com status e CTA por segmento
4. **Módulos** — 8 cards de módulos universais
5. **Como funciona** — 4 passos em grid
6. **Benefícios** — 6 cards de benefício
7. **Nota de planos** — frase + link para `/planos` (sem cards de preço)
8. **Final CTA** — headline genérico + 2 CTAs

---

## Rotas criadas/alteradas

| Rota | Antes | Depois |
|---|---|---|
| `/` | Home focada em restaurante + planos | Home genérica modular, sem planos |
| `/segmentos` | ❌ não existia | Lista de segmentos com status e CTAs |
| `/segmentos/restaurantes` | ❌ não existia | Planos Essencial/Operação/Gestão + features |
| `/planos` | ❌ não existia | Seletor de segmento → planos |

---

## Endpoints criados/alterados

**Nenhum endpoint backend foi criado ou alterado.**  
**Nenhuma migration foi criada.**  
**Nenhum arquivo backend foi alterado.**

---

## Por que planos saíram da home

A home é o topo do funil: deve criar desejo e orientar o visitante. Cards de preço neste ponto forçam uma decisão antes de o visitante entender o produto. Com o modelo multi-segmento, cada segmento tem planos diferentes — não faz sentido exibir apenas os de restaurante na home genérica.

Os planos ficam em `/segmentos/restaurantes` (único segmento com planos publicados) e em `/planos` como seletor.

---

## Onde os planos ficam agora

- `/segmentos/restaurantes` — planos Essencial/Operação/Gestão com cards completos
- `/planos` — seletor por segmento (outros segmentos direcionam para contato)
- Home (`/`) — apenas a frase: "Os planos variam conforme o tipo de negócio" + link `/planos`

---

## Build

```
npm run build
```

Resultado: ✓ Compiled in 13.7s | 54 páginas geradas sem erro.  
Novas rotas estáticas: `/planos`, `/segmentos`, `/segmentos/restaurantes`.

---

## Deploy

- **Data/hora:** 2026-07-04 ~18:55 UTC
- **Backup em:** `/opt/zeropaper/backups/20260704-155331/frontend`
- **Serviço reiniciado:** `zeropaper-frontend` (PID 1200834)
- **Startup:** ✓ Ready in 255ms

---

## Smoke tests

```
/ -> 200                    ✓
/login -> 200               ✓
/app/impressao -> 200       ✓
/segmentos -> 200           ✓
/segmentos/restaurantes -> 200   ✓
/planos -> 200              ✓
```

---

## Rollback

```bash
systemctl stop zeropaper-frontend
rm -rf /opt/zeropaper/frontend
cp -a /opt/zeropaper/backups/20260704-155331/frontend /opt/zeropaper/frontend
chown -R zeropaper:zeropaper /opt/zeropaper/frontend
systemctl start zeropaper-frontend
```

---

## Riscos pendentes

- **Links de WhatsApp** nos CTAs de segmentos sem plano (`href="/segmentos/varejo"` etc.) levam para páginas de segmento que ainda não existem — retornarão 404 até que as páginas sejam criadas. Recomendado criar páginas stub ou redirecionar para `/planos` no curto prazo.
- **Navbar CSS antigo** (`.zp-home-header`) ainda existe no globals.css mas não é mais renderizado. Pode ser removido em refatoração futura para limpar o arquivo.
- **Responsividade mobile** validada por CSS, mas deve ser testada visualmente nos breakpoints 375px e 414px.
- **Meta tags OG** atualizadas mas não testadas em preview (WhatsApp, LinkedIn).
- **Outros segmentos** (`/segmentos/varejo`, `/segmentos/pet-shop` etc.) não têm páginas ainda — criam 404.

## Próximos passos recomendados

1. Criar páginas stub para os segmentos restantes (ou redirecionar para `/planos`).
2. Substituir número de WhatsApp placeholder nos CTAs de contato.
3. Testar visualmente no mobile físico.
4. Avaliar remoção do CSS legado `.zp-home-*` do globals.css.
