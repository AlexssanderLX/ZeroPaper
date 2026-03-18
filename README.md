# ZeroPaper

ZeroPaper e um micro SaaS multi-tenant para restaurantes locais. O produto foi pensado para vender acesso a uma plataforma unica, dentro do dominio da ZeroPaper, com operacao da unidade, pedidos por QR Code, cozinha, equipe e estoque no mesmo fluxo.

## O que o produto resolve

- centraliza a rotina da unidade em um unico sistema web
- separa cada empresa por `tenantId`
- permite login do dono, gerencia e equipe
- cria mesas com acesso publico por QR Code
- envia pedidos da mesa direto para a operacao e para a cozinha
- ajuda no controle basico de estoque e acessos internos

## Estado atual do MVP

Hoje o projeto ja possui:

- onboarding de restaurante
- login real com sessao por token
- portal interno da unidade em `/app`
- modulo de `mesas`
- modulo de `pedidos`
- modulo de `cozinha`
- modulo de `estoque`
- modulo de `equipe`
- modulo de `ajustes`
- rota publica de mesa em `/q/{publicCode}`
- fluxo testado com MySQL real

## Estrutura do repositorio

```text
ZeroPaper/
|-- backend/
|-- frontend/
|-- ZeroPaper.slnx
|-- README.md
```

## Stack e ferramentas

### Backend

- `C#`
- `.NET 10`
- `ASP.NET Core`
- `Entity Framework Core`
- `Pomelo.EntityFrameworkCore.MySql`
- `MySQL`

### Frontend

- `Next.js 15`
- `React 19`
- `TypeScript`
- `next/font`

### Ferramentas de desenvolvimento

- `dotnet` CLI
- `dotnet ef`
- `dotnet user-secrets`
- `npm`
- `git`
- `GitHub`

## Modelagem principal

### Base comercial e tenant

- `Tenant`: isolamento logico de cada cliente
- `Company`: dados da empresa/unidade
- `Subscription`: plano comercial da conta
- `AppUser`: usuarios da unidade
- `AppSession`: sessao autenticada por token

### Operacao da unidade

- `DiningTable`: mesa criada pela unidade
- `QrCodeAccess`: acesso publico associado a mesa ou recurso
- `CustomerOrder`: pedido aberto na operacao
- `OrderItem`: itens do pedido
- `StockItem`: insumos e controle basico de estoque

## Fluxos atuais

### Onboarding

1. cria `Tenant`
2. cria `Company`
3. cria usuario dono
4. cria assinatura
5. cria acesso inicial da unidade

### Portal interno

1. usuario faz login em `/login`
2. backend gera sessao autenticada
3. front abre o lobby em `/app`
4. unidade acessa mesas, pedidos, cozinha, estoque, equipe e ajustes

### Fluxo de mesa e QR Code

1. unidade cria uma mesa
2. sistema gera `publicCode`
3. a mesa fica disponivel em `/q/{publicCode}`
4. o link pode ser usado para QR Code no ZeroPaper ou em outra ferramenta
5. o cliente abre a mesa e envia o pedido
6. o pedido entra no fluxo interno e pode seguir para a cozinha

## Rotas principais

### Frontend

- `/`
- `/login`
- `/app`
- `/app/mesas`
- `/app/pedidos`
- `/app/cozinha`
- `/app/estoque`
- `/app/equipe`
- `/app/ajustes`
- `/q/{publicCode}`

### Backend

- `POST /api/onboarding/restaurants`
- `POST /api/auth/login`
- `POST /api/auth/logout`
- `GET /api/workspace/overview`
- `GET /api/workspace/tables`
- `POST /api/workspace/tables`
- `GET /api/workspace/orders`
- `POST /api/workspace/orders`
- `PATCH /api/workspace/orders/{orderId}/status`
- `GET /api/workspace/stock`
- `POST /api/workspace/stock`
- `PUT /api/workspace/stock/{stockItemId}`
- `GET /api/workspace/team`
- `POST /api/workspace/team`
- `GET /api/workspace/settings`
- `PUT /api/workspace/settings`
- `GET /api/public/tables/{publicCode}`
- `POST /api/public/tables/{publicCode}/orders`

## Seguranca aplicada ate agora

- hash de senha com `PBKDF2`
- sessao autenticada por bearer token
- token salvo em hash no banco
- `CORS` restrito para o frontend
- `rate limiting` em rotas publicas de escrita
- headers basicos de seguranca
- tratamento global de erro com `ProblemDetails`
- sem exposicao de senha ou dados internos em resposta publica

## Como rodar localmente

### 1. Configurar banco

Defina `ConnectionStrings:DefaultConnection` via `user-secrets`, variavel de ambiente ou `appsettings`.

Exemplo de estrutura:

```text
server=localhost;port=3306;database=zeropaper_dev;user=SEU_USUARIO;password=SUA_SENHA;SslMode=None
```

### 2. Aplicar banco

```bash
dotnet ef database update --project backend/ZeroPaper.csproj --startup-project backend/ZeroPaper.csproj
```

### 3. Rodar backend

```bash
dotnet build backend/ZeroPaper.csproj
dotnet run --project backend/ZeroPaper.csproj
```

### 4. Rodar frontend

Crie `frontend/.env.local` com:

```bash
NEXT_PUBLIC_API_BASE_URL=http://localhost:5097
```

Depois:

```bash
cd frontend
npm install
npm run dev
```

## Validacao ja feita

Foi validado manualmente no backend:

- migration aplicada no MySQL
- onboarding de restaurante
- login real
- criacao de mesa
- geracao de acesso publico
- criacao de pedido interno
- envio para cozinha
- criacao de item de estoque
- criacao de membro da equipe
- atualizacao de ajustes da unidade
- criacao de pedido pela rota publica da mesa

## Proximos passos naturais

- cardapio real com itens cadastrados
- vinculacao de pedido a produtos do cardapio
- geracao visual de QR Code
- roles mais refinadas por modulo
- dashboard administrativo da ZeroPaper
- deploy em VPS com DNS e dominio
