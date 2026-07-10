# ZeroPaper VPS Runtime Map

Atualizado em: 2026-06-21.

Este documento descreve como o ZeroPaper esta rodando na VPS de producao. Ele nao contem senhas, tokens, connection strings nem chaves privadas.

## Resumo Executivo

- Host: `216.238.105.103`
- Dominio principal: `zeropaperflow.com.br`
- Sistema: Ubuntu 22.04.5 LTS
- Usuario operacional dos servicos: `zeropaper`
- Reverse proxy: Nginx
- Backend: ASP.NET Core publicado como binario self-contained em `/opt/zeropaper/backend`
- Frontend: Next.js standalone em `/opt/zeropaper/frontend`
- Banco: MariaDB 10.6 local, escutando apenas em `127.0.0.1:3306`
- Backend interno: `http://127.0.0.1:5097`
- Frontend interno: `http://127.0.0.1:3000`
- Uploads persistentes: `/var/lib/zeropaper/uploads`
- Data Protection persistente: `/var/lib/zeropaper/dataprotection`
- Arquivo de ambiente do backend: `/etc/zeropaper/backend.env`
- Arquivo de ambiente do frontend: `/etc/zeropaper/frontend.env`

## Estado Atual Observado

Servicos principais:

```text
zeropaper-backend.service   active/running
zeropaper-frontend.service  active/running
nginx.service               active/running
mariadb.service             active/running
docker.service              active/running
```

Portas locais:

```text
80/443        Nginx publico
22            SSH
127.0.0.1:5097 backend ZeroPaper
127.0.0.1:3000 frontend Next.js
127.0.0.1:3306 MariaDB
127.0.0.1:18080 Evolution API via Docker
```

Smoke local observado na VPS:

```text
401 http://127.0.0.1:5097/api/workspace/overview
401 http://127.0.0.1:5097/api/workspace/printing
200 http://127.0.0.1:3000/
200 http://127.0.0.1:3000/login
200 http://127.0.0.1:3000/app/impressao
```

Esses `401` no backend sao esperados sem token de workspace.

## Systemd

Backend:

```ini
[Unit]
Description=ZeroPaper Backend
After=network.target mariadb.service
Wants=mariadb.service

[Service]
WorkingDirectory=/opt/zeropaper/backend
EnvironmentFile=/etc/zeropaper/backend.env
ExecStart=/opt/zeropaper/backend/ZeroPaper
Restart=always
RestartSec=5
User=zeropaper
Group=zeropaper
KillSignal=SIGINT
SyslogIdentifier=zeropaper-backend

[Install]
WantedBy=multi-user.target
```

Frontend:

```ini
[Unit]
Description=ZeroPaper Frontend
After=network.target zeropaper-backend.service
Wants=zeropaper-backend.service

[Service]
WorkingDirectory=/opt/zeropaper/frontend
EnvironmentFile=/etc/zeropaper/frontend.env
ExecStart=/usr/bin/node /opt/zeropaper/frontend/server.js
Restart=always
RestartSec=5
User=zeropaper
Group=zeropaper
SyslogIdentifier=zeropaper-frontend

[Install]
WantedBy=multi-user.target
```

Comandos uteis:

```bash
systemctl status zeropaper-backend zeropaper-frontend --no-pager
journalctl -u zeropaper-backend -n 120 --no-pager
journalctl -u zeropaper-frontend -n 120 --no-pager
systemctl restart zeropaper-backend
systemctl restart zeropaper-frontend
```

## Nginx

Arquivo ativo:

```text
/etc/nginx/sites-enabled/zeropaper -> /etc/nginx/sites-available/zeropaper
```

Rotas:

```nginx
location / {
    proxy_pass http://127.0.0.1:3000;
}

location /api/ {
    proxy_pass http://127.0.0.1:5097;
}

location /uploads/ {
    proxy_pass http://127.0.0.1:5097/uploads/;
}
```

Certificados configurados:

```text
/etc/ssl/zeropaper/zeropaper.crt
/etc/ssl/zeropaper/zeropaper.key
```

Existe tambem:

```text
/etc/nginx/conf.d/zeropaper-client-body-size.conf
```

Conteudo observado:

```nginx
client_max_body_size 20m;
```

Antes de mexer no Nginx:

```bash
nginx -t
systemctl reload nginx
```

## Backend

Diretorio ativo:

```text
/opt/zeropaper/backend
```

O backend roda como binario:

```text
/opt/zeropaper/backend/ZeroPaper
```

Importante: a VPS nao tem SDK .NET instalado e `dotnet --info` nao lista runtimes compartilhados. Portanto, o backend deve ser publicado como self-contained para `linux-x64`.

Publicacao recomendada local:

```powershell
dotnet publish backend/ZeroPaper.csproj `
  -c Release `
  -r linux-x64 `
  --self-contained true `
  -p:PublishSingleFile=false `
  -o .codex-deploy/backend-release
```

Nao publicar como framework-dependent. Logs recentes mostraram falha quando o binario tentou procurar `Microsoft.NETCore.App 8.0.0` na VPS:

```text
You must install or update .NET to run this application.
No frameworks were found.
```

Depois foi corrigido por um pacote self-contained.

Variaveis do backend existem em:

```text
/etc/zeropaper/backend.env
```

Permissoes observadas:

```text
owner=root:root
mode=600
```

Chaves presentes, sem valores:

```text
ASPNETCORE_ENVIRONMENT
ASPNETCORE_URLS
ConnectionStrings__DefaultConnection
Email__AccessRequests__Recipient
Email__Smtp__Host
Email__Smtp__Password
Email__Smtp__Port
Email__Smtp__SenderEmail
Email__Smtp__SenderName
Email__Smtp__Username
Email__Smtp__UseSsl
Frontend__AllowedOrigins__0
Frontend__AllowedOrigins__1
Frontend__AllowedOrigins__2
Frontend__AllowedOrigins__3
Frontend__AllowedOrigins__4
Frontend__AllowedOrigins__5
OPENAI_API_KEY
RootAccount__Email
RootAccount__Name
RootAccount__Password
WHATSAPP__EVOLUTION__APIKEY
WHATSAPP__EVOLUTION__BASEURL
```

Nao sobrescrever esse arquivo em deploy.

## Frontend

Diretorio ativo:

```text
/opt/zeropaper/frontend
```

O frontend roda via:

```text
/usr/bin/node /opt/zeropaper/frontend/server.js
```

Versao observada:

```text
Node.js v22.22.1
npm 10.9.4
Next.js 15.5.13
```

O formato em producao e Next standalone, com:

```text
/opt/zeropaper/frontend/server.js
/opt/zeropaper/frontend/.next
/opt/zeropaper/frontend/public
/opt/zeropaper/frontend/node_modules
/opt/zeropaper/frontend/package.json
```

Variaveis do frontend existem em:

```text
/etc/zeropaper/frontend.env
```

Permissoes observadas:

```text
owner=root:root
mode=600
```

Chaves presentes, sem valores:

```text
BACKEND_INTERNAL_URL
HOSTNAME
NEXT_PUBLIC_API_BASE_URL
NEXT_PUBLIC_APP_BASE_URL
NODE_ENV
PORT
```

Publicacao recomendada local:

```powershell
cd frontend
npm ci
npm run build
```

Empacotar a saida standalone com:

```text
frontend/.next/standalone/*
frontend/.next/static
frontend/public
```

No destino, garantir que `server.js`, `.next`, `public` e `package.json` fiquem em `/opt/zeropaper/frontend`.

## Banco

Banco observado:

```text
MariaDB 10.6.23
Host: localhost
Porta: 3306
Database: zeropaper_dev
Usuario: presente na connection string
```

Connection string fica somente em:

```text
/etc/zeropaper/backend.env
```

Migration history observada:

```text
Total de migrations: 32
Ultimas migrations:
20260621224151_PrintAgentProfessionalFlow
20260618035247_SalesReportDailyOrderIndex
20260616121858_CurrentProjectModelSync
20260616114931_CompanyTimeZoneId
20260604034952_CustomerOrderPaymentSplits
20260603234211_OrderEditingAndPriceAdjustment
20260603224154_DeliveryEstimatedWaitTimes
20260602034325_MenuCategoryImageFallback
20260602033030_ManualPixConfirmations
20260507011809_CompanyLogoUrl
20260506223348_MenuCatalogAdditionalsAsSource
20260506221220_MenuItemAdditionalGroupMaxSelections
```

As tabelas do fluxo profissional de impressao ja existem na VPS:

```text
printagents
printjobs
```

Antes de qualquer migration:

```bash
STAMP="$(date +%Y%m%d-%H%M%S)"
mkdir -p "/opt/zeropaper/backups/$STAMP"

# Preencher MYSQL_* a partir de /etc/zeropaper/backend.env sem imprimir senha.
mysqldump --single-transaction --quick DATABASE_NAME \
  | gzip > "/opt/zeropaper/backups/$STAMP/db.sql.gz"
```

Preferir gerar script idempotente localmente e revisar antes:

```powershell
dotnet ef migrations script --idempotent `
  --project backend/ZeroPaper.csproj `
  --startup-project backend/ZeroPaper.csproj `
  -o .codex-deploy/migrations.sql
```

## Evolution / WhatsApp

Ha containers Docker ativos:

```text
evolution_api        127.0.0.1:18080->8080/tcp
evolution_redis      redis:7-alpine
evolution_postgres   postgres:15-alpine
```

Arquivos observados:

```text
/opt/evolution-lite/docker-compose.yml
/opt/evolution-lite/.env
```

Nao alterar Evolution durante deploy comum do ZeroPaper.

## Persistencia

Nao apagar nem sobrescrever:

```text
/etc/zeropaper/backend.env
/etc/zeropaper/frontend.env
/var/lib/zeropaper/uploads
/var/lib/zeropaper/dataprotection
/etc/ssl/zeropaper
/opt/evolution-lite
```

Uploads atuais:

```text
/var/lib/zeropaper/uploads/menu
/var/lib/zeropaper/uploads/menu-categories
/var/lib/zeropaper/uploads/logos
```

O backend serve `/uploads/` via Kestrel por tras do Nginx.

## Estrutura de Deploy Atual

Ativos:

```text
/opt/zeropaper/backend
/opt/zeropaper/frontend
```

Backups e artefatos existem em grande quantidade:

```text
/opt/zeropaper/backups
/opt/zeropaper/releases
/opt/zeropaper/backend.backup-*
/opt/zeropaper/backend.bak-*
/opt/zeropaper/frontend.backup-*
/opt/zeropaper/deploy-*
```

Uso de disco observado:

```text
Filesystem: 52G total, 31G usado, 19G livre, 63%
/opt/zeropaper/backups  ~2.0G
/opt/zeropaper/releases ~4.8G
/opt/zeropaper/frontend ~371M
/opt/zeropaper/backend  ~127M
```

Ha muita sobra historica. Limpeza deve ser feita com cautela e sempre mantendo pelo menos:

- release atual;
- backup imediatamente anterior de backend e frontend;
- ultimo backup de banco;
- backups explicitamente marcados por Alexssander como importantes.

## Fluxo Seguro de Deploy

### 1. Validar local

Backend:

```powershell
dotnet build backend/ZeroPaper.csproj
dotnet ef migrations script --idempotent --project backend/ZeroPaper.csproj --startup-project backend/ZeroPaper.csproj
dotnet publish backend/ZeroPaper.csproj -c Release -r linux-x64 --self-contained true -o .codex-deploy/backend-release
```

Frontend:

```powershell
cd frontend
npm ci
npm run build
```

### 2. Preparar pacote

Backend: empacotar conteudo de `.codex-deploy/backend-release`.

Frontend: empacotar Next standalone, `.next/static` e `public`.

### 3. Antes de trocar arquivos na VPS

```bash
STAMP="$(date +%Y%m%d-%H%M%S)"
BACKUP_DIR="/opt/zeropaper/backups/$STAMP"
mkdir -p "$BACKUP_DIR"
cp -a /opt/zeropaper/backend "$BACKUP_DIR/backend"
cp -a /opt/zeropaper/frontend "$BACKUP_DIR/frontend"
# Fazer dump do banco usando credenciais de /etc/zeropaper/backend.env sem imprimir senha.
```

### 4. Aplicar migration

Preferir aplicar script SQL idempotente revisado.

Nao aplicar migration sem backup de banco.

### 5. Trocar backend

```bash
systemctl stop zeropaper-backend
rm -rf /opt/zeropaper/backend
mv /tmp/zeropaper-backend-new /opt/zeropaper/backend
chown -R zeropaper:zeropaper /opt/zeropaper/backend
chmod +x /opt/zeropaper/backend/ZeroPaper
systemctl start zeropaper-backend
```

Smoke:

```bash
curl -s -o /dev/null -w '%{http_code}' http://127.0.0.1:5097/api/workspace/overview
curl -s -o /dev/null -w '%{http_code}' http://127.0.0.1:5097/api/workspace/printing
```

Esperado sem token: `401`.

### 6. Trocar frontend

```bash
systemctl stop zeropaper-frontend
rm -rf /opt/zeropaper/frontend
mv /tmp/zeropaper-frontend-new /opt/zeropaper/frontend
chown -R zeropaper:zeropaper /opt/zeropaper/frontend
systemctl start zeropaper-frontend
```

Smoke:

```bash
curl -s -o /dev/null -w '%{http_code}' http://127.0.0.1:3000/
curl -s -o /dev/null -w '%{http_code}' http://127.0.0.1:3000/login
curl -s -o /dev/null -w '%{http_code}' http://127.0.0.1:3000/app/impressao
```

Esperado: `200`.

### 7. Verificar logs

```bash
systemctl is-active zeropaper-backend
systemctl is-active zeropaper-frontend
journalctl -u zeropaper-backend -n 120 --no-pager
journalctl -u zeropaper-frontend -n 120 --no-pager
```

## Rollback

Rollback de binario:

```bash
systemctl stop zeropaper-backend zeropaper-frontend
rm -rf /opt/zeropaper/backend /opt/zeropaper/frontend
cp -a /opt/zeropaper/backups/STAMP/backend /opt/zeropaper/backend
cp -a /opt/zeropaper/backups/STAMP/frontend /opt/zeropaper/frontend
chown -R zeropaper:zeropaper /opt/zeropaper/backend /opt/zeropaper/frontend
chmod +x /opt/zeropaper/backend/ZeroPaper
systemctl start zeropaper-backend
systemctl start zeropaper-frontend
```

Rollback de banco:

- So fazer se a migration for destrutiva ou se o binario anterior nao conseguir conviver com a schema nova.
- Para migrations aditivas, preferir rollback apenas do binario e manter tabelas/colunas novas ate corrigir.

## Problemas/Riscos Encontrados

1. A VPS nao tem SDK .NET e nao deve depender de runtime compartilhado.
   - Publicar backend sempre self-contained `linux-x64`.

2. Ha muitos backups e artefatos soltos em `/opt/zeropaper`.
   - Isso aumenta risco de confusao em deploy e consome varios GB.
   - Criar politica de retencao antes de limpar.

3. Alguns diretorios de frontend aparecem com dono numerico `197609:197121`.
   - O servico roda como `zeropaper`, mas os arquivos podem ter vindo de pacote criado no Windows.
   - Apos deploy, sempre rodar `chown -R zeropaper:zeropaper /opt/zeropaper/frontend`.

4. Existem logs recentes de falha por pacote framework-dependent.
   - Isso confirma a necessidade de self-contained.

5. O arquivo local `.codex-deploy/.secrets/production-vps.local.json` nao autenticou como `root`.
   - Nao confiar nele sem atualizar/validar.
   - Preferir SSH key ou cofre local padronizado.

6. Producao ja tem `PrintAgentProfessionalFlow` aplicada.
   - Antes de tentar aplicar a mesma migration novamente, verificar `__efmigrationshistory`.

## Checklist Rapido Antes de Qualquer IA Publicar

- [ ] Confirmar se a tarefa e backend, frontend ou ambos.
- [ ] Rodar build local correspondente.
- [ ] Conferir `git diff` para nao levar arquivos sensiveis/locais.
- [ ] Backend publicado self-contained `linux-x64`.
- [ ] Frontend publicado como Next standalone.
- [ ] Fazer backup de `/opt/zeropaper/backend`, `/opt/zeropaper/frontend` e banco.
- [ ] Nao sobrescrever `/etc/zeropaper/*.env`.
- [ ] Nao sobrescrever `/var/lib/zeropaper/uploads`.
- [ ] Aplicar migration somente apos revisar SQL.
- [ ] Reiniciar apenas servicos necessarios.
- [ ] Smoke local via `127.0.0.1`.
- [ ] Verificar `journalctl`.
- [ ] Registrar caminho do backup usado naquele deploy.
