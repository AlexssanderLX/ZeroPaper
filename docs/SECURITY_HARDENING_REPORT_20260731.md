# ZeroPaper — relatório de endurecimento de segurança

Data da revisão: 31/07/2026  
Escopo: código local do backend, frontend e testes. Nenhuma alteração, migration ou deploy foi aplicado na VPS.

## Resultado executivo

O projeto recebeu uma política de autenticação oficial e fechada por padrão, sessões web em cookie HttpOnly com proteção CSRF, políticas por função, limites particionados, desativação da senha mestra compartilhada, proteção adicional de uploads e redução da exposição de tokens e dados em logs.

Essas correções reduzem riscos críticos, mas não constituem certificação de segurança. Permanecem dependências de infraestrutura, validação em ambiente de homologação e decisões de produto listadas ao final.

## Vulnerabilidades encontradas e correções

| Severidade | Achado e impacto | Correção aplicada |
|---|---|---|
| Crítica | Autorização manual repetida e ausência de política global permitiam regressões com endpoints administrativos anônimos. | Autenticação ASP.NET Core oficial, política global autenticada por padrão, `[AllowAnonymous]` explícito e políticas `Root`, `Owner` e workspace. Testes enumeram todas as rotas administrativas/workspace e verificam 401/403. |
| Crítica | Senha mestra compartilhada podia autenticar usuários e confirmar ações protegidas; segredo era reversível. | Fallback removido do login e das confirmações do workspace; endpoints de revelar/rotacionar retornam 410; migration elimina hash e cifra existentes. |
| Alta | Bearer web permanecia acessível ao JavaScript em `sessionStorage`, ampliando impacto de XSS. | Sessão web migrou para cookie `HttpOnly`, `Secure` fora de Development e `SameSite=Strict`; resposta não retorna token. Bearer separado continua compatível com agente de impressão. |
| Alta | Requisições mutáveis com cookie poderiam sofrer CSRF. | Cabeçalho `X-ZP-CSRF` obrigatório nas rotas autenticadas mutáveis; CORS restrito com credenciais. |
| Alta | Rate limit compartilhado podia causar bloqueio global de login e não distinguia origens. | Particionamento por IP; proteção progressiva por hash de IP + identificador, sem bloqueio permanente da conta; limites sensíveis usam usuário autenticado ou IP. |
| Alta | Respostas OAuth/pagamentos e trechos de webhook podiam registrar tokens, dados pessoais ou payloads. | Corpos externos e payloads foram removidos dos logs; permanecem apenas status e identificadores internos necessários. |
| Alta | Tokens de rastreamento, cliente e redefinição apareciam em query/path, sujeitos a histórico e logs intermediários. | Rastreamento/cancelamento e identificação de cliente usam `X-ZP-Public-Token`; redefinição usa fragmento removido do histórico assim que consumido. |
| Alta | Diretórios de uploads privados eram expostos pelo middleware estático. | Fotos de pets e alertas exigem autenticação, recebem `private, no-store`, `nosniff` e nome de arquivo sanitizado; conteúdo público recebe cache explícito. |
| Média | Endpoints públicos de agenda aceitavam abuso concentrado. | Limites por IP, empresa/hora e telefone/empresa/hora; payload máximo explícito e resposta 429. |
| Média | Headers permitiam exposição de tecnologia e política de origem ampla. | `X-Powered-By` desativado, CSP reduzida, HSTS preservado fora de Development, `no-referrer`, `nosniff`, `DENY` e `Permissions-Policy`. |
| Média | Sessões válidas podiam acumular indefinidamente durante seu prazo. | Criação de sessão revoga excedentes e mantém no máximo quatro sessões recentes; redefinição de senha continua revogando todas. |
| Funcional | Frontend SSR podia consultar URL pública incorreta e exibir planos indisponíveis; Pet Shop ficava fechado por fallback; CTA principal voltava aos segmentos. | SSR prioriza `BACKEND_INTERNAL_URL`, disponibilidade não usa cache, Pet Shop vem da API como fonte da verdade e “Começar agora” aponta para `/login`. |

## Arquivos alterados

- Pipeline e segurança: `backend/Program.cs`, `backend/Security/ZeroPaperSecurity.cs`, `backend/Security/LoginAttemptProtector.cs`.
- Autenticação e sessão: `backend/Controllers/AuthController.cs`, `backend/Services/AuthSessionService.cs`.
- Autorizações: controllers em `backend/Controllers/Admin*`, `Workspace*`, `PaymentsController.cs` e marcação explícita dos controllers públicos.
- Senha mestra: `backend/Controllers/AdminCompaniesController.cs`, `backend/Services/AuthSessionService.cs`, `backend/Services/WorkspaceService.cs`, migration `20260731214226_DisableSharedMasterPassword`.
- Tokens/logs/abuso: `PasswordResetService.cs`, `PublicPetShopService.cs`, `WhatsAppIntegrationService.cs`, `MercadoPagoService.cs` e controllers públicos relacionados.
- Frontend: `frontend/lib/api.ts`, `daily-sales-report.ts`, `segment-availability.ts`, `reset-password-form.tsx`, `public-site-header.tsx`, `app/page.tsx` e `next.config.ts`.
- Testes: `backend.Tests/Security/AuthorizationBoundaryTests.cs` e dependências de teste.

## Testes e verificações executados

- `dotnet test ZeroPaper.slnx --no-restore` e nova execução do projeto de testes após o caso CSRF: 26 aprovados, 0 falhas.
- `dotnet build ZeroPaper.slnx --no-restore`: sucesso, 0 avisos, 0 erros.
- `dotnet ef migrations has-pending-model-changes`: nenhum desvio de modelo.
- `dotnet list backend/ZeroPaper.csproj package --vulnerable --include-transitive`: nenhum pacote vulnerável nas fontes consultadas.
- `npm audit`: 0 vulnerabilidades.
- `npm run build`: build Next.js 15.5.22 concluído, tipos e 59 páginas validados.
- `git diff --check`: sem erro de whitespace; apenas avisos locais de conversão LF/CRLF.

## Riscos residuais

- O segredo do webhook Evolution/Z-API ainda pode existir na query por limitação de integração do provedor. Deve ser migrado para header/assinatura quando suportado e redigido nos access logs enquanto isso.
- O webhook Mercado Pago faz consulta autoritativa à API, mas a validação criptográfica da assinatura do provedor deve ser implementada e testada com exemplos oficiais.
- Limites públicos em memória não são globais entre múltiplas réplicas e podem ter corrida. Produção distribuída requer contador atômico compartilhado (por exemplo, Redis) e, para agenda/contato, CAPTCHA adaptativo.
- MFA e reautenticação forte para Root ainda não foram implementados. A senha mestra foi eliminada, mas operações Root continuam dependendo da sessão normal.
- Colunas legadas da senha mestra permanecem no schema por compatibilidade, embora os valores sejam apagados e os endpoints desativados. Remoção física exige migration posterior coordenada.
- Uploads privados usam autorização na aplicação; deve-se confirmar que Nginx/Cloudflare não servem diretamente o mesmo diretório, além de permissões POSIX, antivírus e retenção.
- A política CSP do frontend ainda permite estilos inline por compatibilidade atual. Remoção exige nonces/hashes e validação visual completa.
- Não houve pentest dinâmico autenticado, fuzzing, DAST, revisão independente nem teste real via Cloudflare/Nginx.

## Itens que bloqueiam o deploy

1. Revisar e aplicar a migration de limpeza da senha mestra em backup/homologação antes de produção.
2. Configurar e validar `BACKEND_INTERNAL_URL`, origens CORS exatas e HTTPS; confirmar comportamento dos cookies no domínio real.
3. Redigir query strings, `Authorization`, `Cookie`, senhas, tokens e dados pessoais nos logs de Nginx, Cloudflare e serviços de observabilidade; definir retenção e permissões.
4. Verificar que Nginx não expõe diretamente uploads privados e que os diretórios possuem permissões mínimas.
5. Rotacionar segredos potencialmente expostos anteriormente (webhooks, integrações e senha mestra legada) sem registrá-los no repositório.
6. Executar teste de homologação ponta a ponta: login/logout, CSRF, Root/Owner/Employee, agente de impressão, Mercado Pago, WhatsApp, Pet Shop, uploads e isolamento cruzado de tenants.
7. Definir MFA para Root e validação de assinatura do webhook Mercado Pago, ou aceitar formalmente o risco residual antes da exposição pública.
