# ZeroPaper - Relatorio Tecnico de Stack, Infra, APIs e Seguranca

Atualizado em: 2026-06-30

Este documento descreve as decisoes tecnicas observadas no codigo e na documentacao operacional do projeto ZeroPaper. Nao contem senhas, tokens, connection strings, chaves privadas nem valores de `.env`.

## 1. Resumo executivo

O ZeroPaper e um SaaS multi-tenant para pequenos negocios, hoje focado em restaurantes. A arquitetura separa backend ASP.NET Core, frontend Next.js, banco MySQL/MariaDB e servicos externos para IA, WhatsApp, pagamento online, distancia de entrega e impressao local.

O desenho atual prioriza:

- isolamento por `TenantId` e `CompanyId`;
- sessoes proprias armazenadas no banco, sem confiar em `CompanyId` vindo do frontend;
- backend como autoridade para plano, modulos e empresa ativa;
- publicacao simples em VPS com Nginx, systemd e processos locais;
- integracoes externas atras de services dedicados;
- protecao de segredos persistidos via ASP.NET Data Protection;
- deploy backend self-contained para evitar dependencia de runtime .NET instalado na VPS.

## 2. Stack principal

### Backend

- Plataforma: ASP.NET Core / C#.
- Target: `.NET 8.0`.
- ORM: Entity Framework Core 8.
- Banco: MySQL/MariaDB via `Pomelo.EntityFrameworkCore.MySql`.
- PDF/relatorios: QuestPDF.
- API: Controllers REST sob `/api/...`.
- Autenticacao: sessao propria via bearer token opaco.
- Persistencia: EF Core migrations.
- Uploads: servidos pelo backend via `/uploads`.
- Data Protection: chaves persistidas em filesystem.

Pacotes principais observados em `backend/ZeroPaper.csproj`:

- `Microsoft.AspNetCore.OpenApi`
- `Microsoft.EntityFrameworkCore`
- `Microsoft.EntityFrameworkCore.Design`
- `Pomelo.EntityFrameworkCore.MySql`
- `QuestPDF`

### Frontend

- Framework: Next.js 15.
- Runtime UI: React 19.
- Linguagem: TypeScript.
- Icons: `lucide-react`.
- QR Code: `qrcode`.
- Build de producao: Next standalone.
- Headers de seguranca definidos em `frontend/next.config.ts`.

Scripts principais:

- `npm run dev`
- `npm run build`
- `npm run start`

### Ferramenta local de impressao

- Projeto: `tools/ZeroPaper.PrintAgent`.
- Stack: Windows Forms em C#.
- Targets: `net8.0-windows` e `net48`.
- Comunicacao com backend: endpoints `/api/print-agent/...`.
- Autenticacao do agente: header `X-ZP-Agent-Key`.

## 3. Infraestrutura de producao observada

Baseado em `docs/ZEROPAPER_VPS_RUNTIME_MAP.md` e arquivos Nginx do repo.

### VPS

- Sistema operacional: Ubuntu 22.04.5 LTS.
- Reverse proxy: Nginx.
- Backend interno: `http://127.0.0.1:5097`.
- Frontend interno: `http://127.0.0.1:3000`.
- Banco: MariaDB local em `127.0.0.1:3306`.
- Usuario operacional dos servicos: `zeropaper`.
- Dominio principal documentado: `zeropaperflow.com.br`.

### Systemd

Servicos esperados:

- `zeropaper-backend.service`
- `zeropaper-frontend.service`
- `nginx.service`
- `mariadb.service`
- `docker.service`, usado para Evolution.

Backend roda como binario:

```text
/opt/zeropaper/backend/ZeroPaper
```

Frontend roda como standalone:

```text
/usr/bin/node /opt/zeropaper/frontend/server.js
```

### Nginx

Roteamento:

- `/` -> Next.js em `127.0.0.1:3000`
- `/api/` -> ASP.NET Core em `127.0.0.1:5097`
- `/uploads/` -> backend em `127.0.0.1:5097/uploads/`

Configuracoes relevantes:

- HTTPS configurado no arquivo de dominio.
- `client_max_body_size 16M` nos arquivos versionados.
- Headers `X-Forwarded-*` repassados ao backend/frontend.

### Docker / Evolution

Documentacao operacional indica Evolution API local via Docker:

- Evolution API em `127.0.0.1:18080`.
- Redis e PostgreSQL usados pelo stack Evolution.
- Arquivos operacionais em `/opt/evolution-lite`.

## 4. Estrutura de configuracao

### Configuracao versionada

`backend/appsettings.json` contem defaults publicos e sem segredo real:

- `Frontend:AllowedOrigins`
- `OpenAI:BaseUrl`
- `OpenAI:DefaultModelName`
- `PublicApp:BaseUrl`
- `MercadoPago:ApiBaseUrl`
- `MercadoPago:AuthBaseUrl`
- `WhatsApp:Evolution:BaseUrl`
- `Delivery` providers e parametros

### Configuracao sensivel

Em producao, a documentacao aponta:

- `/etc/zeropaper/backend.env`
- `/etc/zeropaper/frontend.env`

Esses arquivos nao devem ser commitados, copiados para o repo nem sobrescritos em deploy.

Chaves esperadas, sem valores:

- `ConnectionStrings__DefaultConnection`
- `OPENAI_API_KEY`
- `WHATSAPP__EVOLUTION__APIKEY`
- `WHATSAPP__EVOLUTION__BASEURL`
- `RootAccount__...`
- `Email__Smtp__...`
- `NEXT_PUBLIC_API_BASE_URL`
- `NEXT_PUBLIC_APP_BASE_URL`

## 5. Modelo multi-tenant

O sistema usa entidades centrais:

- `Tenant`
- `Company`
- `AppUser`
- `AppSession`
- `Subscription`

O isolamento operacional usa `TenantId` e `CompanyId` em entidades de dominio. O fluxo de workspace resolve `TenantId` e `CompanyId` a partir da sessao autenticada no backend. A regra importante do projeto e: o workspace nao deve confiar em `CompanyId` vindo do frontend.

O `WorkspaceSessionContext` concentra:

- `TenantId`
- `CompanyId`
- `UserId`
- dados do usuario logado;
- nome do restaurante;
- plano;
- flags de modulo;
- flags comerciais como IA, delivery, relatorios, cupons, vendedores.

## 6. Planos e modulos comerciais

Planos observados em codigo:

- `ZeroPaper Essencial`
- `ZeroPaper Operacao`
- `ZeroPaper Gestao`
- personalizado/modular em alguns fluxos admin

Modulos/flags observados:

- Cardapio/menu
- Mesas
- Cozinha
- Caixa
- Estoque
- Delivery
- Impressao
- Chamado de atendente
- IA/WhatsApp
- Cupons
- Relatorios basicos/avancados
- Dashboard de gestao
- Clientes recorrentes
- Vendedores

Decisao arquitetural: o backend resolve plano e modulos pela assinatura ativa, e o frontend usa essa resposta para navegação/renderizacao. Isso reduz risco de liberar modulo apenas por alteracao no cliente.

## 7. Autenticacao e sessao

### Login

O login usa email/nome e senha. Para root/admin ha perfil `admin`; para restaurante ha perfil `restaurant`.

O backend:

- busca usuario;
- valida senha com hash;
- rejeita usuario/empresa inativos;
- cria token aleatorio de 32 bytes;
- armazena somente hash do token em `AppSession`;
- retorna token bruto uma unica vez ao frontend.

### Sessao

- Duracao observada: 12 horas.
- Token enviado como Bearer.
- Token e hasheado no backend antes de consulta.
- Sessao valida exige:
  - token existente;
  - sessao ativa;
  - nao revogada;
  - nao expirada;
  - usuario ativo;
  - empresa ativa.

### Atalho de owner

Existe login por token de atalho:

- token bruto validado por hash;
- expiracao;
- revogacao;
- restrito a owner ativo.

## 8. Senhas, hashes e protecao criptografica

### Hash de senha

`PasswordHasher` usa:

- PBKDF2;
- SHA-512;
- salt de 16 bytes;
- chave de 32 bytes;
- 100.000 iteracoes;
- comparacao em tempo constante (`CryptographicOperations.FixedTimeEquals`).

Formato:

```text
pbkdf2-sha512$<salt>$<hash>
```

### Tokens e segredos persistidos

ASP.NET Data Protection e usado para proteger segredos armazenados em banco, incluindo:

- tokens da Evolution/WhatsApp;
- segredo de webhook WhatsApp;
- tokens Mercado Pago;
- state OAuth do Mercado Pago com validade limitada.

Chaves de Data Protection sao persistidas em filesystem para sobreviver a restarts/deploys.

## 9. Controles HTTP e seguranca de borda

### Backend

Controles observados em `Program.cs`:

- Kestrel sem `Server` header.
- CORS com allowlist em `Frontend:AllowedOrigins`.
- Rate limiting por politicas:
  - `public-write`: 10/min.
  - `integration-write`: 120/min.
  - `webhook-ingress`: 30000/min com fila.
  - `sensitive-write`: 12/min.
  - `upload-write`: 20/min.
- Headers:
  - `X-Content-Type-Options: nosniff`
  - `X-Frame-Options: DENY`
  - `Referrer-Policy: strict-origin-when-cross-origin`
  - `Permissions-Policy: camera=(), microphone=(), geolocation=()`
  - CSP basica no backend.
- HSTS em ambiente nao desenvolvimento.
- Handler global de excecoes com mensagens genericas para 500.

### Frontend

Headers em `next.config.ts`:

- `X-Content-Type-Options: nosniff`
- `X-Frame-Options: DENY`
- `Referrer-Policy: strict-origin-when-cross-origin`
- `Permissions-Policy`
- `Content-Security-Policy`
- `Strict-Transport-Security`

Decisao relevante: CSP permite `unsafe-inline` para scripts/styles no estado atual. Isso simplifica compatibilidade com Next/estilos, mas deve ser revisto futuramente para endurecimento.

## 10. Uploads e arquivos

Uploads sao tratados pelo backend e servidos por `/uploads`.

Validador `SafeUploadValidator` verifica assinatura binaria de:

- imagens: JPEG, PNG, WebP;
- audio: WAV, MP3, OGG.

Decisao: validar magic bytes e nao confiar somente em extensao enviada pelo cliente.

Risco ainda a acompanhar:

- limites de tamanho devem ser conferidos por endpoint e Nginx;
- antivírus/scan de malware nao foi observado no codigo;
- politicas de retencao de uploads nao aparecem como fluxo formal.

## 11. APIs externas usadas

### OpenAI API

Uso:

- IA de atendimento;
- geracao/teste de respostas;
- templates de atendimento.

Configuracao:

- `OpenAI:BaseUrl`
- `OpenAI:DefaultModelName`
- `OPENAI_API_KEY` por ambiente.

Default observado:

```text
https://api.openai.com/v1/
gpt-5.4-mini
```

Cuidados de seguranca:

- chave nao versionada;
- chamadas encapsuladas em `AiAssistantService`;
- limite de tamanho de mensagem de teste/entrada observado;
- historico/conversas persistidos por empresa.

### Evolution API / WhatsApp

Uso:

- preparar instancia;
- gerar QR code/pareamento;
- configurar webhook;
- processar mensagens recebidas;
- enviar respostas;
- registrar status/conexao.

Configuracao:

- `WhatsApp:Evolution:BaseUrl`
- `WhatsApp:Evolution:ApiKey`
- `WhatsApp:Evolution:DefaultIntegration`

Controles observados:

- Data Protection para tokens e segredo de webhook;
- lock por empresa em processamento inbound;
- protecao contra mensagens antigas/futuras;
- janela contra replay recente;
- limite de rajada de respostas automaticas;
- tratamento de eventos `messages.upsert`, `connection.update`, `qrcode.updated`, `send.message`.

### Z-API

Ainda ha endpoints e base URL configurada no client legado:

- webhook receive;
- status de mensagem;
- conectado/desconectado.

Pelo contexto atual, Evolution parece ser o caminho principal para testes/operacao local.

### Mercado Pago

Uso:

- OAuth por empresa;
- conexao/desconexao de conta;
- checkout publico de pedido;
- webhook publico de pagamento.

Configuracao:

- `MercadoPago:ApiBaseUrl`
- `MercadoPago:AuthBaseUrl`
- `ClientId`
- `ClientSecret`

Controles:

- OAuth state protegido com Data Protection e validade de 15 minutos;
- access/refresh tokens protegidos antes de persistir;
- checkout valida pedido, status e empresa conectada.

### Nominatim / OpenStreetMap

Uso:

- distancia aproximada de entrega.

Configuracao:

- `Delivery:Approximate:BaseUrl`
- `RouteFactor`
- `MinimumDistanceKm`
- `MaximumDistanceKm`
- `UserAgent`

### Google Routes API

Uso previsto:

- distancia/rota de entrega quando provider Google estiver configurado.

Configuracao:

- `Delivery:GoogleMaps:BaseUrl`
- `Delivery:GoogleMaps:ApiKey`

### SMTP

Uso:

- notificacao de solicitacao de acesso/pre-cadastro.

Configuracao sensivel em ambiente:

- host;
- porta;
- usuario;
- senha;
- remetente;
- destinatario.

## 12. APIs internas principais

### Autenticacao

- `POST /api/auth/login`
- `POST /api/auth/shortcut-login`
- `POST /api/auth/logout`
- `POST /api/auth/password/request-reset`
- `POST /api/auth/password/reset`
- `POST /api/auth/confirm-password`

### Admin

- `/api/admin/dashboard`
- `/api/admin/users`
- `/api/admin/owners`
- `/api/admin/signup-codes`
- `/api/admin/companies`

Usos:

- root/admin;
- gerenciamento de users/owners;
- liberacao/rejeicao de login;
- planos/modulos;
- senha master de unidade.

### Workspace

Base:

- `/api/workspace`

Inclui:

- overview;
- cardapio;
- mesas;
- pedidos;
- caixa/pagamentos;
- clientes/perfil;
- estoque;
- cupons;
- equipe;
- settings;
- delivery;
- chamados;
- relatorios;
- fechamento de caixa.

### IA e WhatsApp

- `/api/workspace/ai`
- `/api/workspace/ai/status`
- `/api/workspace/ai/generate-template`
- `/api/workspace/ai/test`
- `/api/workspace/ai/whatsapp/prepare`
- `/api/integrations/whatsapp/evolution/{instanceId}/events`

### Publico

- `/api/public/tables/{publicCode}`
- `/api/public/tables/{publicCode}/orders`
- `/api/public/tables/{publicCode}/coupons/validate`
- `/api/public/customer-profile/{code}`
- `/api/public/delivery-links/{code}`
- `/api/public/edit-links/{editCode}`
- `/api/public/seller-link/{code}`
- `/api/public/payments/mercadopago/webhook`

### Impressao

- `/api/print-agent/register`
- `/api/print-agent/heartbeat`
- `/api/print-agent/jobs/claim-next`
- `/api/print-agent/orders/claim-next`
- endpoints de sucesso/falha unitarios e em lote.

## 13. Banco e entidades principais

Entidades centrais observadas:

- `Tenant`
- `Company`
- `AppUser`
- `AppSession`
- `Subscription`
- `DiningTable`
- `QrCodeAccess`
- `MenuCategory`
- `MenuItem`
- `MenuItemAdditionalGroup`
- `CustomerOrder`
- `OrderItem`
- `CustomerOrderPayment`
- `DeliveryCustomerProfile`
- `CustomerOrderHistory`
- `Coupon`
- `ManualPixConfirmation`
- `PrintAgent`
- `PrintJob`
- `WhatsAppConversation`
- `WhatsAppMessage`
- `SalesAgent`

Decisoes de modelagem:

- entidades de negocio possuem `TenantId`;
- varias tabelas tambem possuem `CompanyId`;
- soft active via `IsActive`;
- timestamps `CreatedAtUtc` e `UpdatedAtUtc`;
- enums persistidos como inteiros em EF;
- historico/registro separado para pedidos deletados e relatorios.

## 14. Frontend e experiencia

Areas principais:

- landing/cadastro;
- login;
- admin root;
- workspace `/app`;
- cardapio publico `/q/[publicCode]`;
- delivery/link curto `/d/[code]`;
- edicao publica `/e/[editCode]`;
- acompanhamento `/acompanhar/[code]`;
- impressao via `/imprimir/mesa/[publicCode]`.

Decisao de frontend:

- estado de sessao local pelo provider;
- chamadas centralizadas em `frontend/lib/api.ts`;
- layout modular por pagina/feature;
- headers de seguranca em Next config;
- build standalone para VPS.

## 15. Decisoes de deploy

### Backend

Como a VPS nao depende de runtime .NET instalado, o backend deve ser publicado como self-contained:

```powershell
dotnet publish backend/ZeroPaper.csproj -c Release -r linux-x64 --self-contained true
```

### Frontend

Usar Next standalone:

```powershell
cd frontend
npm ci
npm run build
```

Empacotar:

- `.next/standalone`
- `.next/static`
- `public`

### Backups

Antes de deploy:

- backup de `/opt/zeropaper/backend`;
- backup de `/opt/zeropaper/frontend`;
- dump do banco antes de migrations;
- nunca sobrescrever `/etc/zeropaper/*.env`;
- nunca sobrescrever `/var/lib/zeropaper/uploads`;
- nunca sobrescrever Data Protection.

## 16. Decisoes de seguranca positivas

- Tokens de sessao opacos, aleatorios e hasheados no banco.
- Sessoes revogaveis e com expiracao.
- Hash de senha com PBKDF2-SHA512 e salt.
- Confirmacao de senha para acoes sensiveis.
- `CompanyId` e `TenantId` resolvidos pelo backend a partir da sessao.
- CORS allowlist.
- Rate limiting por tipo de endpoint.
- Headers de seguranca no backend e frontend.
- Data Protection para tokens sensiveis em banco.
- Upload com validacao por assinatura.
- Webhooks e integrações em services dedicados.
- Frontend nao deve ser fonte de autorizacao.
- Admin/root separado de workspace de restaurante.
- Public endpoints usam codigos/tokens especificos, nao IDs internos.

## 17. Riscos e pontos de melhoria

### Migrations e producao

Risco observado recentemente: backend publicado pode esperar colunas que a VPS ainda nao tem. Isso quebrou telas que carregavam pedidos.

Recomendacao:

- gerar script SQL idempotente em todo deploy com migration;
- aplicar somente apos backup;
- registrar migration aplicada;
- smoke test de endpoints que consultam tabelas alteradas.

### CSP

Frontend usa `unsafe-inline`. Funciona, mas reduz endurecimento contra XSS.

Recomendacao:

- avaliar nonce/hash no futuro;
- reduzir fontes permitidas conforme necessidade real.

### Logs

Handler de 500 retorna mensagem generica para cliente, positivo. Mas logs podem conter trechos de queries e metadados.

Recomendacao:

- garantir que payloads com dados pessoais nao sejam logados;
- revisar logs de webhooks para evitar telefone/mensagem sensivel.

### Rate limit de cadastro

Ja existe politica de rate limit, mas duplicidade tambem precisa de idempotencia/constraint em banco para cenario multi-instancia.

Recomendacao:

- criar chave unica/idempotency key para pre-cadastro por email/empresa quando o produto amadurecer;
- manter protecao frontend contra duplo clique.

### Segredos

Segredos estao fora do repo, mas ha muitos artefatos locais/untracked no workspace.

Recomendacao:

- manter `.env`, credenciais, dumps, uploads e backups fora do git;
- revisar `.gitignore`;
- limpar artefatos de deploy antigos com politica de retencao.

### Public endpoints

Endpoints publicos por codigo curto devem garantir:

- expiracao quando aplicavel;
- tokens com entropia suficiente;
- nao exposicao de `CompanyId`, `TenantId`, IDs internos e telefone completo.

### Uploads

Validacao de assinatura existe.

Melhorias futuras:

- limite por arquivo por endpoint;
- normalizacao/reescrita de imagem;
- scan antimalware se houver risco comercial maior.

### Multi-tenant

Padrao atual e bom quando todos os services filtram por `session.CompanyId`.

Risco:

- qualquer endpoint novo que aceite `CompanyId` do frontend pode criar IDOR.

Regra de projeto:

- workspace sempre usa `session.CompanyId`;
- admin/root pode escolher empresa, mas precisa autorizacao root e confirmacao quando sensivel.

## 18. Checklist tecnico de seguranca para novas features

- [ ] Endpoint autenticado quando for workspace/admin.
- [ ] Nao aceitar `CompanyId` do frontend em workspace.
- [ ] Filtrar sempre por `session.CompanyId` e/ou `session.TenantId`.
- [ ] Validar plano/modulo no backend.
- [ ] Validar payload e tamanho maximo.
- [ ] Nao retornar dados sensiveis desnecessarios.
- [ ] Nao logar segredo, token, telefone completo ou payload pessoal.
- [ ] Usar Data Protection para tokens persistidos.
- [ ] Usar hash para tokens de sessao/atalho/reset.
- [ ] Aplicar rate limit se endpoint for publico, sensivel ou webhook.
- [ ] Criar migration revisada e backup antes de producao.
- [ ] Rodar build backend/frontend conforme area alterada.
- [ ] Fazer smoke test pos deploy.

## 19. Arquivos usados como fonte

Principais arquivos analisados:

- `backend/ZeroPaper.csproj`
- `backend/Program.cs`
- `backend/appsettings.json`
- `backend/Data/ZeroPaperDbContext.cs`
- `backend/Services/AuthSessionService.cs`
- `backend/Services/PasswordHasher.cs`
- `backend/Services/SafeUploadValidator.cs`
- `backend/Services/AiAssistantService.cs`
- `backend/Services/WhatsAppIntegrationService.cs`
- `backend/Services/MercadoPagoService.cs`
- `backend/Services/Models/*Options.cs`
- `backend/Controllers/*.cs`
- `frontend/package.json`
- `frontend/next.config.ts`
- `frontend/lib/api.ts`
- `tools/ZeroPaper.PrintAgent/ZeroPaper.PrintAgent.csproj`
- `tools/ZeroPaper.PrintAgent/PrintAgentApiClient.cs`
- `docs/ZEROPAPER_VPS_RUNTIME_MAP.md`
- `infra/nginx/*.conf`

Arquivos propositalmente nao usados:

- `.env`
- logs sensiveis;
- backups;
- dumps de banco;
- uploads;
- arquivos de credenciais.

## 20. Recomendacao final

O ZeroPaper tem uma base tecnica coerente para SaaS pequeno/médio: backend centralizado, tenancy por sessao, banco relacional, frontend standalone, integracoes isoladas em services e deploy simples em VPS.

As prioridades recomendadas agora sao:

1. Formalizar processo de migration em producao antes de todo deploy.
2. Criar checklist automatizado de smoke tests para endpoints criticos.
3. Reduzir risco de artefatos sensiveis no workspace e no git.
4. Endurecer CSP gradualmente.
5. Manter regra absoluta: workspace nunca recebe nem confia em `CompanyId` vindo do frontend.
6. Evoluir idempotencia de fluxos publicos e sensiveis para constraints/transacoes no banco.
