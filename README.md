# ZeroPaper

Workspace organizada em duas frentes:

- `backend/`: API ASP.NET Core
- `frontend/`: interface em Next.js

## Comandos esperados

### Backend

```bash
dotnet build backend/ZeroPaper.csproj
dotnet run --project backend/ZeroPaper.csproj
```

### Frontend

```bash
cd frontend
npm install
npm run dev
```

## Observacoes

- A solucao da raiz (`ZeroPaper.slnx`) aponta para `backend/ZeroPaper.csproj`.
- O backend ainda depende de `ConnectionStrings:DefaultConnection` para subir corretamente.
