# ZeroPaper

ZeroPaper e uma plataforma operacional para restaurantes presenciais.

O foco do produto e reduzir atrito entre `mesa`, `cozinha`, `caixa`, `impressao` e `atendimento`, mantendo a operacao da unidade em um fluxo unico e mais simples de acompanhar.

## Escopo atual

Na fase atual, o sistema cobre os principais pontos do ciclo operacional do restaurante:

- cardapio com categorias, imagens, edicao e controle de disponibilidade
- mesas com QR Code, impressao e gestao do acesso publico
- pedido publico por mesa com experiencia voltada para toque e celular
- fluxo interno de cozinha com status operacionais e reimpressao
- caixa com controle de cobranca, pagamentos e limpeza do fluxo atual
- alertas sonoros para chamados e novos pedidos
- estoque interno da unidade
- relatorio diario de caixa em PDF
- impressao automatica com agente Windows
- implantacao guiada da unidade dentro do painel

## Modulos da unidade

O painel da unidade esta organizado nos seguintes modulos:

- `Implantacao`
- `Cardapio`
- `Estoque`
- `Mesas`
- `Pedidos para a cozinha`
- `Caixa`
- `Impressao`
- `Unidade`

## Fluxo principal

O fluxo principal do ZeroPaper hoje e:

1. a unidade configura cardapio, mesas e impressao
2. o cliente acessa a mesa pelo QR Code
3. o pedido entra no backend e fica visivel para cozinha e caixa
4. a cozinha acompanha o preparo e pode imprimir ou reimprimir
5. o caixa acompanha a cobranca e fecha manualmente o pagamento
6. a unidade acompanha alertas, chamados e impressao em um mesmo painel

## Impressao automatica

O projeto inclui o codigo-fonte do agente Windows de impressao em:

- [tools/ZeroPaper.PrintAgent](C:\Users\Alexssander\Desktop\Programação\ZeroPaper\tools\ZeroPaper.PrintAgent)

O executavel nao fica versionado no Git. Para gerar o agente de forma local e copiar o arquivo para a area publica de download, use:

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\build-print-agent.ps1
```

Esse processo gera o executavel em `frontend/public/downloads` apenas quando necessario, evitando subir binarios grandes para o repositório.

## Estrutura do repositorio

```text
ZeroPaper/
|-- backend/
|-- frontend/
|-- tools/
|   |-- ZeroPaper.PrintAgent/
|   |-- build-print-agent.ps1
|-- infra/
|   |-- nginx/
|-- README.md
```

## Stack

### Backend

- .NET 8
- ASP.NET Core
- Entity Framework Core
- MySQL / MariaDB
- QuestPDF

### Frontend

- Next.js
- React
- TypeScript

### Operacao local

- agente Windows em .NET para impressao automatica
- Nginx como proxy reverso

## Direcao do produto

O ZeroPaper esta sendo lapidado como base operacional de restaurante presencial.

O criterio atual de evolucao do produto e simples:

- fortalecer o fluxo principal antes de abrir novas frentes
- reduzir cliques e ambiguidade operacional
- priorizar estabilidade, responsividade e seguranca
- manter o sistema facil de implantar em unidades pequenas e medias

## Boas praticas adotadas neste repositorio

- artefatos de build e deploy nao ficam versionados
- uploads operacionais nao entram no Git
- arquivos de ambiente locais e de producao nao entram no Git
- o agente de impressao e versionado por codigo-fonte, nao por binario pesado
- configuracoes de infraestrutura ficam separadas da aplicacao

## Observacao

Este README foi escrito para apresentar o produto e a fase atual do projeto sem expor credenciais, rotas sensiveis, chaves privadas ou detalhes operacionais internos da VPS.
