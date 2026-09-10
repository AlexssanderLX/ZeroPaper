# ZeroPaper

ZeroPaper e uma plataforma SaaS multiempresa para operacoes de restaurantes e pet shops. O produto centraliza atendimento, pedidos, pagamentos, agenda, gestao e automacoes em uma experiencia responsiva para operadores e clientes.

> O modulo de restaurantes esta operacional. O modulo Pet Shop esta em fase beta e e liberado manualmente, sem checkout ou mensalidade nesta etapa.

## Funcionalidades atuais

### Restaurantes

- cardapio por categorias, imagens, disponibilidade, adicionais e grupos de complementos;
- QR Code por mesa, comandas, canal publico de pedidos e chamado de atendente;
- pedidos locais, retirada e delivery, com edicao, historico e controle de status;
- cozinha com filas de pedidos a fazer e prontos;
- caixa com pedidos a cobrar, pagos, fechamento diario, cupons e divisoes de pagamento;
- cadastro de clientes recorrentes e historico de compras;
- controle de estoque, vendedores e relatorios operacionais;
- impressao manual e automatica por agente Windows;
- pagamento online de pedidos por conta Mercado Pago conectada pela empresa;
- atendimento automatizado por WhatsApp e assistente de IA configurado no backend.

### Pet Shop (beta)

- cadastro de tutores e animais;
- catalogo de servicos;
- agenda interna e agendamento publico;
- acompanhamento de atendimentos e historico;
- acesso beta controlado pela administracao da plataforma.

### Plataforma e administracao

- autenticacao por perfil, recuperacao de senha e sessoes revogaveis;
- isolamento multi-tenant por empresa;
- planos comerciais e liberacao de modulos;
- assinatura da plataforma pelo Mercado Pago;
- painel Root para empresas, owners, acessos, cobrancas e codigos de cadastro;
- isencao de mensalidade por empresa, protegida por nova confirmacao da senha Root e auditoria;
- conta demonstrativa de restaurante acessivel apenas por link revogavel;
- encerramento centralizado de sessoes demonstrativas;
- dashboards e indicadores operacionais.

## Fluxos principais

### Pedido de restaurante

1. A empresa configura cardapio, mesas, delivery e meios de pagamento.
2. O cliente acessa o canal publico por QR Code ou link.
3. O pedido entra na operacao e segue pelos estados da cozinha.
4. O caixa acompanha cobranca, pagamento e fechamento.
5. A impressao automatica pode enviar pedidos ao agente Windows da unidade.

### Conta demonstrativa

1. O Root abre `Conta teste` no painel administrativo.
2. A plataforma cria ou reutiliza a unidade demonstrativa no plano Gestao.
3. Um novo link seguro e gerado; links anteriores podem ser revogados.
4. O visitante entra sem receber credenciais convencionais.
5. O Root pode quebrar o link ou encerrar todas as sessoes abertas.

### Isencao de mensalidade

1. O Root abre `Isencoes` na sidebar administrativa.
2. Seleciona a empresa e confirma a operacao com sua senha.
3. Enquanto isenta, a empresa nao e bloqueada por vencimento e nao gera novo checkout.
4. A cobranca volta a ser exigida assim que o Root retirar a isencao.

## Estrutura do repositorio

```text
ZeroPaper/
|-- backend/                 API ASP.NET Core e migrations
|-- backend.Tests/           testes automatizados do backend
|-- frontend/                aplicacao Next.js
|-- tools/
|   |-- ZeroPaper.PrintAgent/ agente Windows de impressao
|   |-- TestOps/              verificacoes operacionais
|-- infra/                   configuracoes de infraestrutura
|-- .github/workflows/       CI e deploy de producao
|-- README.md
```

## Tecnologias

- .NET 8, ASP.NET Core e Entity Framework Core;
- MySQL/MariaDB;
- Next.js 15, React 19 e TypeScript;
- QuestPDF;
- Mercado Pago;
- OpenAI;
- agente Windows em .NET.

## Executar localmente

### Requisitos

- .NET SDK compativel com `global.json`;
- Node.js 20;
- MySQL ou MariaDB;
- configuracoes locais do backend e frontend fora do Git.

### Banco e backend

```powershell
dotnet restore .\backend\ZeroPaper.csproj
dotnet ef database update --project .\backend\ZeroPaper.csproj --startup-project .\backend\ZeroPaper.csproj
dotnet run --project .\backend\ZeroPaper.csproj
```

### Frontend

```powershell
Set-Location .\frontend
npm ci
npm run dev
```

### Validacao

```powershell
dotnet test .\backend.Tests\ZeroPaper.Tests.csproj
Set-Location .\frontend
npm audit --omit=dev
npm run build
```

## Agente de impressao

O codigo-fonte do agente esta em `tools/ZeroPaper.PrintAgent`. Para gerar o executavel local:

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\build-print-agent.ps1
```

Artefatos compilados e configuracoes da impressora nao devem ser versionados.

## Seguranca e configuracao

- tokens, senhas, chaves e arquivos de ambiente nao pertencem ao repositorio;
- segredos de integracoes devem existir apenas no backend ou no provedor de deploy;
- credenciais armazenadas pela aplicacao usam protecao de dados antes da persistencia;
- links publicos sensiveis usam tokens aleatorios e revogaveis;
- operacoes administrativas criticas exigem sessao Root, confirmacao de senha e rate limit;
- status de pagamentos externos deve ser confirmado pela API ou webhook do provedor.

## Branches e entrega

O fluxo padrao e:

```text
feature/* -> develop -> main -> CI -> Deploy Production (manual)
```

- `feature/*`: desenvolvimento isolado;
- `develop`: integracao das features validadas;
- `main`: codigo aprovado e elegivel para producao;
- pushes e pull requests para `main` executam o CI;
- o deploy de producao e disparado manualmente pelo workflow `Deploy Production`.

O workflow de producao gera os artefatos, cria backup do banco, executa migrations idempotentes e somente depois ativa a nova versao. Nao altere diretamente a VPS quando o workflow puder realizar a entrega com seguranca.

## Estado do projeto

O ZeroPaper esta em evolucao ativa. As prioridades atuais sao estabilidade operacional, seguranca, responsividade, integracoes de pagamento confiaveis e uma implantacao simples para pequenas e medias empresas.
