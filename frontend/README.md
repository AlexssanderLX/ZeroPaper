# ZeroPaper Frontend

Frontend do MVP ZeroPaper com foco em restaurantes e onboarding conectado ao backend.

## Rodando localmente

```bash
npm install
npm run dev
```

Depois abra `http://localhost:3000`.

## Variavel de ambiente

Crie um `.env.local` com:

```bash
NEXT_PUBLIC_API_BASE_URL=http://localhost:5097
```

## Estrutura

- `app/layout.tsx`: layout global e fontes
- `app/page.tsx`: landing page do produto para restaurantes
- `components/restaurant-onboarding-form.tsx`: formulario integrado ao endpoint de onboarding
- `app/globals.css`: identidade visual e responsividade

## Observacao

Esse front foi montado para refletir o fluxo inicial de venda e cadastro de restaurantes dentro do seu dominio principal.
