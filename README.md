# ZeroPaper

ZeroPaper e um micro SaaS multi-tenant para restaurantes locais. A proposta e vender acesso ao sistema, nao o dominio da empresa cliente. O produto concentra pedidos por QR Code, fluxo de cozinha, organizacao operacional e visibilidade de estoque em um unico ambiente web.

## Objetivo do projeto

- centralizar operacao, pedidos, cozinha e estoque no mesmo sistema
- separar cada empresa por `tenantId`
- permitir acesso web por login e por QR Code
- manter uma base pronta para VPS, DNS e crescimento por unidades

## Estado atual do MVP

### Backend

- API ASP.NET Core em `.NET 10`
- banco MySQL com Entity Framework Core
- onboarding inicial de restaurante
- repositorios e servicos para persistencia e fluxo inicial
- migration inicial do MVP
- protecoes basicas de seguranca ja aplicadas

### Frontend

- landing page da marca
- tela de login
- area privada em `/app`
- lobby do dono da unidade
- modulos iniciais para `mesas`, `pedidos`, `cozinha`, `estoque`, `equipe` e `ajustes`

## Modelagem principal

As entidades base do backend hoje sao:

- `Tenant`: isolamento de cada cliente no sistema
- `Company`: dados da empresa vinculada ao tenant
- `AppUser`: usuarios da empresa
- `Subscription`: plano e estado comercial da conta
- `QrCodeAccess`: acesso publico controlado por token e rota

## Estrutura do repositorio

```text
ZeroPaper/
|-- backend/
|-- frontend/
|-- ZeroPaper.slnx
|-- README.md
```

## Ferramentas e stack de desenvolvimento

### Backend

- `C#`
- `.NET 10`
- `ASP.NET Core`
- `Entity Framework Core`
- `Pomelo.EntityFrameworkCore.MySql`
- `MySQL`
- `dotnet user-secrets`

### Frontend

- `Next.js 15`
- `React 19`
- `TypeScript`
- `next/font`

### Ferramentas de apoio

- `dotnet` CLI
- `npm`
- `git`
- `GitHub`

## Seguranca ja aplicada

- hashing de senha com `PBKDF2`
- `CORS` restrito para frontend
- `rate limiting` no endpoint publico
- `ProblemDetails` e tratamento global de erro
- headers basicos de seguranca
- `ConnectionStrings:DefaultConnection` fora do codigo, via configuracao

## Rotas e fluxo atual

### Fluxo publico

- `/`: apresentacao do produto
- `/login`: entrada da conta

### Fluxo interno

- `/app`: lobby do dono da unidade
- `/app/mesas`
- `/app/pedidos`
- `/app/cozinha`
- `/app/estoque`
- `/app/equipe`
- `/app/ajustes`

Hoje o login do frontend usa uma sessao local temporaria para navegacao do MVP visual. A autenticacao real com o banco ainda nao foi conectada ao front.

## Como rodar localmente

### Backend

1. Configure `ConnectionStrings:DefaultConnection`.
2. Rode:

```bash
dotnet build backend/ZeroPaper.csproj
dotnet run --project backend/ZeroPaper.csproj
```

3. Se precisar aplicar o banco:

```bash
dotnet ef database update --project backend/ZeroPaper.csproj --startup-project backend/ZeroPaper.csproj
```

### Frontend

1. Entre em `frontend/`
2. Instale as dependencias:

```bash
npm install
```

3. Rode o projeto:

```bash
npm run dev
```

Se precisar, use `frontend/.env.example` como base para a configuracao do front.

## O que ja esta pronto para evoluir

- login real ligado ao backend
- mesas com QR Code por unidade
- pedidos ligados a mesas
- fila de cozinha
- controle de estoque por item
- painel administrativo da operacao ZeroPaper

## Observacao importante

O projeto ja esta organizado como workspace unico com frontend e backend separados, mas ainda em fase de fundacao do MVP. A base atual foi pensada para crescer sem quebrar o conceito principal do produto: uma operacao multi-tenant, simples de vender e simples de usar.
