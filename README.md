# ZeroPaper

ZeroPaper e um micro SaaS para restaurantes locais. O sistema foi pensado para centralizar a rotina da unidade em uma plataforma unica, com foco em operacao, cardapio, mesas, QR Code e fluxo de pedidos.

## Proposta do produto

A ideia da ZeroPaper e reduzir atrito no dia a dia do restaurante com uma experiencia simples para quem administra a unidade e pratica para quem faz o pedido.

Hoje o projeto segue esta linha:

- acesso controlado para novas contas
- separacao por empresa dentro da plataforma
- operacao da unidade em ambiente proprio
- cardapio digital com controle de disponibilidade
- mesas com QR Code
- pedidos indo para a cozinha

## Funcoes principais do MVP

### Cadastro e acesso

- cadastro inicial controlado por liberacao
- recuperacao de senha
- area administrativa da plataforma
- area da unidade apos login

### Cardapio

- criacao de categorias
- criacao de itens
- envio de foto do prato
- controle de item disponivel ou oculto

### Mesas

- criacao de mesas
- geracao automatica de QR Code
- download e impressao do QR

### Pedidos

- pedido publico a partir da mesa
- encaminhamento para a cozinha
- atualizacao de status do pedido
- remocao de pedido cancelado

### Unidade

- ajustes basicos da empresa
- area central de operacao do cliente

## Estrutura do projeto

```text
ZeroPaper/
|-- backend/
|-- frontend/
|-- README.md
```

## Tecnologias

### Backend

- .NET
- ASP.NET Core
- Entity Framework Core
- MySQL

### Frontend

- Next.js
- React
- TypeScript

## Como rodar localmente

1. Configurar a conexao com o banco MySQL
2. Aplicar as migrations do backend
3. Subir o backend
4. Configurar a variavel de ambiente do frontend apontando para a API
5. Subir o frontend

## Estado atual

O projeto ja cobre a base do fluxo principal:

- conta root da plataforma
- liberacao de novos acessos
- login e recuperacao de senha
- portal da unidade
- cardapio
- mesas com QR
- pedidos para a cozinha

## Direcao do produto

O foco atual da ZeroPaper e consolidar uma base simples, bonita e funcional para uso real em restaurantes locais, antes de expandir para automacoes mais avancadas.
