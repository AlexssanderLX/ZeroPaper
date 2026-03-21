# ZeroPaper

ZeroPaper e um micro SaaS para restaurantes locais.

O projeto foi pensado para reduzir atrito na operacao do dia a dia, concentrando cardapio, mesas, QR Code, pedidos e acompanhamento interno em uma unica plataforma.

## Objetivo do MVP

Esta versao do MVP existe para validar a base do produto em uso real.

Hoje o foco da ZeroPaper e:

- facilitar o atendimento por mesa
- transformar o QR Code em porta de entrada do pedido
- organizar o fluxo entre cliente, cozinha e caixa
- dar ao dono da unidade um painel simples para operar

## O que o sistema ja cobre

### Plataforma

- administracao central da ZeroPaper
- liberacao controlada de novos cadastros
- recuperacao de acesso
- organizacao por unidade dentro da plataforma

### Unidade

- painel principal da unidade
- ambiente proprio para operacao
- identidade visual aplicada ao portal

### Cardapio

- organizacao por categorias
- cadastro e edicao de produtos
- imagem do produto
- controle de disponibilidade
- limpeza e manutencao do cardapio

### Mesas e QR Code

- criacao e ajuste de mesas
- geracao automatica de QR Code por mesa
- visualizacao e impressao do QR
- folha dedicada para impressao

### Pedido publico

- acesso do cliente pelo QR Code
- escolha de itens por toque
- observacoes do pedido
- confirmacao de envio
- continuidade para um novo pedido

### Operacao interna

- pedidos para a cozinha
- atualizacao de status do pedido
- caixa com pedidos a cobrar e pagos
- marcacao de pagamento
- controle interno de encerramento do pedido

## Experiencia do produto

O MVP foi dividido em tres experiencias principais:

- administracao da plataforma
- operacao da unidade
- pedido publico pela mesa

## Estrutura do repositorio

```text
ZeroPaper/
|-- backend/
|-- frontend/
|-- README.md
```

## Stack

### Backend

- .NET
- ASP.NET Core
- Entity Framework Core
- MySQL

### Frontend

- Next.js
- React
- TypeScript

## Direcao atual

Neste momento, a ZeroPaper esta sendo lapidada como base operacional de restaurante.

O objetivo nao e cobrir tudo de uma vez, e sim fechar muito bem o fluxo principal:

- cardapio
- mesa
- QR Code
- pedido
- cozinha
- caixa

## Observacao

Este README descreve o MVP de forma intencionalmente simples.

Ele foi escrito para apresentar a proposta e as funcoes do produto, sem expor detalhes internos, rotas ou configuracoes sensiveis.
