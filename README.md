# ZeroPaper

ZeroPaper e um micro SaaS multi-tenant para restaurantes locais. A proposta do produto e vender acesso a uma plataforma unica, dentro da propria ZeroPaper, com operacao da unidade, cardapio, mesas, pedidos por QR Code e fluxo de cozinha no mesmo sistema.

## O que existe hoje

- area root da plataforma em `/admin`
- cadastro protegido por codigo de liberacao
- solicitacao de acesso por email
- recuperacao de senha por email
- login real com sessao por token
- portal da unidade em `/app`
- cardapio com categorias, itens, foto local e disponibilidade
- mesas com QR Code, download e impressao
- pedidos publicos por mesa em `/q/{publicCode}`
- quadro de pedidos para a cozinha
- ajustes da unidade

## Fluxo atual do produto

### Root ZeroPaper

Voce administra a plataforma em `/admin`.

Hoje a area root permite:

- gerar codigos de liberacao
- ver contas cadastradas
- desativar, reativar e excluir contas
- exigir confirmacao de senha para acoes sensiveis

Os codigos de liberacao:

- expiram em 5 minutos
- sao usados para liberar novo cadastro
- ficam protegidos no fluxo administrativo

### Cadastro da unidade

1. o restaurante recebe um codigo de liberacao
2. acessa `/cadastro`
3. cria a conta inicial da unidade
4. o sistema faz login automatico
5. a pessoa entra direto no portal da unidade

Se a unidade nao tiver codigo, pode solicitar liberacao pelo fluxo publico.

### Portal da unidade

A area logada principal fica em `/app`.

Os modulos principais hoje sao:

- `Cardapio`
- `Mesas`
- `Pedidos para a cozinha`
- `Unidade`

### Mesa e pedido publico

1. a unidade cria a mesa
2. o sistema gera um QR Code e um link publico
3. o QR pode ser baixado ou impresso
4. o cliente abre a mesa em `/q/{publicCode}`
5. escolhe itens do cardapio
6. envia o pedido
7. o pedido entra no quadro da cozinha

## Modulos atuais

### Cardapio

- cria categorias
- cria itens
- envia foto do computador para o prato
- controla se o item esta `Disponivel` ou `Oculto`
- apaga item
- apaga categoria

O controle de disponibilidade ficou centralizado no cardapio. A rota antiga de estoque no front redireciona para o cardapio.

### Mesas

- cria mesa com nome e lugares
- gera QR automaticamente
- mostra o QR por mesa
- permite abrir, copiar, baixar e imprimir o QR

### Pedidos para a cozinha

- lista pedidos por etapa
- move pedido entre status
- conclui pedido
- cancela pedido
- remove pedido cancelado

### Unidade

- ajusta dados da unidade
- mantem o contexto administrativo do restaurante

## Estrutura do repositorio

```text
ZeroPaper/
|-- backend/
|-- frontend/
|-- ZeroPaper.slnx
|-- README.md
```

## Stack

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

### Ferramentas de desenvolvimento

- `dotnet`
- `dotnet ef`
- `dotnet user-secrets`
- `npm`
- `git`
- `GitHub`

## Modelagem principal

### Plataforma

- `Tenant`
- `Company`
- `Subscription`
- `AppUser`
- `AppSession`
- `SignupCode`
- `PasswordResetRequest`

### Operacao

- `DiningTable`
- `QrCodeAccess`
- `MenuCategory`
- `MenuItem`
- `CustomerOrder`
- `OrderItem`

## Rotas principais

### Frontend

- `/`
- `/login`
- `/cadastro`
- `/admin`
- `/app`
- `/app/cardapio`
- `/app/mesas`
- `/app/pedidos`
- `/app/ajustes`
- `/app/estoque` redireciona para `/app/cardapio`
- `/q/{publicCode}`
- `/redefinir-solicitacao`
- `/redefinir-senha`

### Backend

- `POST /api/onboarding/restaurants`
- `POST /api/public/access-requests`
- `POST /api/auth/login`
- `POST /api/auth/logout`
- `POST /api/auth/password/request-reset`
- `POST /api/auth/password/reset`
- `POST /api/auth/confirm-password`
- `GET /api/admin/signup-codes`
- `POST /api/admin/signup-codes`
- `GET /api/admin/users`
- `PATCH /api/admin/users/{userId}/deactivate`
- `PATCH /api/admin/users/{userId}/reactivate`
- `DELETE /api/admin/users/{userId}`
- `GET /api/workspace/overview`
- `GET /api/workspace/menu`
- `POST /api/workspace/menu/categories`
- `POST /api/workspace/menu/items`
- `PATCH /api/workspace/menu/items/{menuItemId}/status`
- `POST /api/workspace/menu/images`
- `DELETE /api/workspace/menu/categories/{categoryId}`
- `DELETE /api/workspace/menu/items/{menuItemId}`
- `GET /api/workspace/tables`
- `POST /api/workspace/tables`
- `GET /api/workspace/orders`
- `POST /api/workspace/orders`
- `PATCH /api/workspace/orders/{orderId}/status`
- `DELETE /api/workspace/orders/{orderId}`
- `GET /api/workspace/settings`
- `PUT /api/workspace/settings`
- `GET /api/public/tables/{publicCode}`
- `POST /api/public/tables/{publicCode}/orders`

## Seguranca atual

- hash de senha com `PBKDF2`
- sessao autenticada por bearer token
- token salvo em hash no banco
- cadastro fechado por codigo de liberacao
- confirmacao de senha para acoes administrativas sensiveis
- recuperacao de senha por email
- `CORS` restrito
- `rate limiting` em rotas publicas de escrita
- tratamento global de erro com `ProblemDetails`

## Como rodar localmente

### 1. Banco

Configure `ConnectionStrings:DefaultConnection`.

Exemplo:

```text
server=localhost;port=3306;database=zeropaper_dev;user=SEU_USUARIO;password=SUA_SENHA;SslMode=None
```

### 2. Aplicar migrations

```bash
dotnet ef database update --project backend/ZeroPaper.csproj --startup-project backend/ZeroPaper.csproj
```

### 3. Rodar backend

```bash
dotnet run --project backend/ZeroPaper.csproj --urls http://localhost:5097
```

### 4. Rodar frontend

Crie `frontend/.env.local`:

```bash
NEXT_PUBLIC_API_BASE_URL=http://localhost:5097
```

Depois:

```bash
cd frontend
npm install
npm run dev
```

## Validacao feita

Ja foram validados no projeto:

- login root
- geracao de codigo de liberacao
- cadastro de nova unidade com codigo
- login automatico apos cadastro
- recuperacao de senha por email
- criacao de categoria e item de cardapio
- upload de foto no cardapio
- controle de disponibilidade do item
- criacao de mesa
- geracao de QR publico
- download e impressao do QR
- pedido publico pela mesa
- fluxo do pedido para a cozinha
- cancelamento e remocao de pedido cancelado

## Direcao atual do MVP

O foco atual do produto esta em:

- cadastro controlado
- operacao da unidade
- cardapio como centro da disponibilidade
- mesas e QR Code
- fluxo simples e direto para cozinha

Os proximos passos naturais sao:

- refinamento dos ajustes da unidade
- mais controle visual e operacional no cardapio
- mais automacao no fluxo da cozinha
- deploy em VPS com DNS e dominio
