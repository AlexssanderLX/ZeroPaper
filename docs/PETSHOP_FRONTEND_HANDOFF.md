# ZeroPaper Pet Shop — contrato de passagem para a IA do frontend

Este documento é a fonte de verdade do frontend para o módulo Pet Shop. Não deduza o contrato pelas entidades nem envie `TenantId`/`CompanyId`; use somente os DTOs e endpoints descritos aqui.

## Referências imutáveis

- Commit base anterior: `e71c2ab` (`feat(petshop): integrate secure pet and appointment backend`)
- Commit funcional desta etapa: `ba4d543` (`feat(petshop): complete secure backend capabilities`)
- Migration inicial: `20260731145603_PetShopBackendIntegration`
- Migration desta etapa: `20260731153413_CompletePetShopBackend`
- Script idempotente: `docs/sql/20260731_petshop_backend_idempotent.sql`
- A migration não foi aplicada automaticamente em banco compartilhado ou produção.

## Segurança e sessão

- Endpoints `/api/workspace/**` exigem o mesmo header `Authorization` já usado pelo workspace.
- Tenant, empresa, usuário e capabilities são resolvidos no backend pela sessão. Nunca envie ou persista `TenantId` ou `CompanyId` no frontend.
- Toda consulta e toda relação nova validam tenant + empresa. IDs de tutor, pet, serviço, profissional, pedido e bloqueio de outra empresa são rejeitados ou não encontrados.
- O frontend deve usar `capabilities` retornado no overview da sessão para montar a navegação. Não determine módulos pelo nome da empresa nem por flags locais.
- `HasPets` e `HasAppointments` só são habilitados para `BusinessSegment.PetShop`. Recursos de restaurante (mesas, cozinha, delivery e chamadas de garçom) permanecem separados.
- Upload de foto: `multipart/form-data`, campo `file`, máximo 5 MB; conteúdo é validado por assinatura, nome é gerado no servidor e há rate limit.
- O token público de acompanhamento é segredo opaco: mostrar/armazenar somente no dispositivo do cliente, nunca registrar em analytics ou logs. O banco guarda apenas SHA-256, com expiração de 30 dias e revogação no cancelamento.
- Rotas públicas de escrita têm rate limit. Não expõem tutor, telefone, endereço, notas internas, tenant, empresa, usuário ou IDs internos do agendamento.

## Configuração de uma empresa Pet Shop

Um usuário root deve usar o fluxo administrativo existente de alteração de segmento da empresa e definir `BusinessSegment = PetShop` (`2`). Esse fluxo gera automaticamente `PetShopPublicCode` criptograficamente aleatório se ainda não existir. O código aparece nos DTOs administrativos `AdminCompanyFlowDto` e `AdminCompanySegmentDto` para que o painel monte a URL pública; ele não deve ser inventado pelo frontend.

Depois da sessão ser renovada, consulte o overview do workspace. O objeto `capabilities` contém:

```json
{
  "hasCustomerProfiles": true,
  "hasCatalog": true,
  "hasPets": true,
  "hasAppointments": true,
  "hasOnlinePayments": true,
  "hasWhatsApp": false,
  "hasAiAssistant": false,
  "hasCoupons": false,
  "hasReports": false,
  "hasPrinting": false,
  "hasDelivery": false,
  "hasTables": false,
  "hasKitchen": false,
  "hasWaiterCalls": false
}
```

Os valores ligados ao plano comercial continuam dependendo das flags existentes; o exemplo é ilustrativo. Para Pet Shop, pets e agenda são habilitados pelo segmento nesta versão.

## Enums e serialização

ASP.NET aceita o valor numérico; respostas podem usar números conforme a configuração JSON atual. Tipar no frontend com estes valores exatos:

- `BusinessSegment`: `Restaurant = 1`, `PetShop = 2`
- `CatalogItemKind`: `Product = 1`, `Service = 2`
- `PetSpecies`: `Dog = 1`, `Cat = 2`, `Other = 3`
- `PetSize`: `Small = 1`, `Medium = 2`, `Large = 3`
- `AppointmentStatus`: `Requested = 1`, `Confirmed = 2`, `InProgress = 3`, `Completed = 4`, `Cancelled = 5`, `NoShow = 6`

## Endpoints autenticados

### Tutores (`/api/workspace/customers`)

- `GET ?search=&page=1&pageSize=25` → `{ items: CustomerProfileDto[], page, pageSize, total }`
- `GET /{customerId}` → `CustomerProfileDto`
- `POST` → `201 CustomerProfileDto`
- `PUT /{customerId}` → `200 CustomerProfileDto`

Criação:

```json
{
  "phoneNumber": "11999999999",
  "name": "Ana",
  "zipCode": "00000000",
  "street": "Rua A",
  "number": "10",
  "neighborhood": "Centro",
  "complement": null
}
```

Atualização aceita todos os campos acima menos `phoneNumber`. O DTO de resposta contém `id`, os mesmos dados, `createdAtUtc`, `updatedAtUtc` e `lastOrderAtUtc`. Telefone duplicado na mesma empresa gera conflito.

### Pets (`/api/workspace/pets`)

- `GET ?search=&customerProfileId=&isActive=&page=1&pageSize=25` → `{ items: PetDto[], page, pageSize, total }`
- `GET /{petId}` → `PetDto`
- `POST` → `201 PetDto`
- `PUT /{petId}` → `200 PetDto`
- `PATCH /{petId}/status` com `{ "isActive": false }` → `PetDto`
- `POST /{petId}/photo`, multipart campo `file` → `{ petId, photoUrl }`
- `DELETE /{petId}/photo` → `{ petId, photoUrl: null }`

Criação/edição (na edição omitir `customerProfileId`):

```json
{
  "customerProfileId": "guid",
  "name": "Rex",
  "species": 1,
  "size": 2,
  "breed": "SRD",
  "weightKg": 12.5,
  "birthDate": "2022-05-10",
  "behaviorNotes": null,
  "allergyNotes": null,
  "restrictions": null
}
```

`PetDto` acrescenta `id`, `customerName`, `photoUrl` e `isActive`. Um pet com agendamento futuro ativo não pode ser desativado.

### Catálogo (`/api/workspace/catalog`)

- `GET /items?kind=2` lista itens ativos; `kind` é opcional.
- Use `kind=Service`/`2` para a agenda. Um serviço agendável precisa estar ativo, pertencer à empresa, ter `Kind=Service` e `EstimatedDurationMinutes` válido.
- Criação/edição do catálogo continua usando os endpoints de menu já existentes. Os DTOs existentes expõem `kind` e `estimatedDurationMinutes`.

### Agenda (`/api/workspace/appointments`)

- `GET ?fromUtc=&toUtc=&status=&petId=&customerId=&assignedUserId=` → `AppointmentDto[]`
- `GET /{appointmentId}` → `AppointmentDto`
- `POST` → `201 AppointmentDto`
- `PUT /{appointmentId}/schedule` → `AppointmentDto`
- `PUT /{appointmentId}/notes` → `AppointmentDto`
- `PUT /{appointmentId}/assignee` → `AppointmentDto`
- `PATCH /{appointmentId}/status` → `AppointmentDto`
- `GET /{appointmentId}/history` → `AppointmentHistoryDto[]`
- `PUT /{appointmentId}/order` liga um pedido existente da mesma empresa → `AppointmentDto`
- `POST /{appointmentId}/order` cria pedido pelo snapshot do serviço → `{ appointment, order }`
- `GET /settings` e `PUT /settings`
- `GET /availability?date=2026-08-01&serviceId={guid}&assignedUserId={guid?}`
- `GET /blocks?fromUtc=&toUtc=`, `POST /blocks`, `DELETE /blocks/{blockId}`
- `GET /reports/summary?fromUtc=&toUtc=`
- `GET /professionals` → usuários ativos da mesma empresa

Criar agendamento:

```json
{
  "petId": "guid",
  "menuItemId": "guid",
  "assignedUserId": null,
  "startsAtUtc": "2026-08-01T14:00:00Z",
  "durationMinutes": null,
  "customerNotes": "Banho hipoalergênico"
}
```

Reagendar: `{ "startsAtUtc": "...Z", "durationMinutes": 60 }`. Notas: `{ "customerNotes": null, "internalNotes": null }`. Profissional: `{ "assignedUserId": "guid-ou-null" }`. Status: `{ "status": 2, "cancellationReason": null }`; cancelamento exige motivo.

Criar pedido: `{ "paymentMethod": "Pix", "unitPrice": null, "notes": null }`. Ligar existente: `{ "customerOrderId": "guid" }`. Não é permitido criar dois pedidos para o mesmo agendamento nem criar para cancelado/no-show.

Configuração de agenda:

```json
{
  "serviceDays": "1,2,3,4,5,6",
  "startTime": "08:00",
  "endTime": "18:00",
  "slotIntervalMinutes": 30
}
```

`serviceDays` usa números do `DayOfWeek` .NET: domingo `0` até sábado `6`. A resposta acrescenta `timeZone`. Bloqueio: `{ assignedUserId, startsAtUtc, endsAtUtc, reason }`. Bloqueio sem profissional afeta toda a agenda; com profissional afeta somente ele.

`AppointmentDto`: `id`, `petId`, `petName`, `menuItemId`, `serviceName`, `customerOrderId`, `assignedUserId`, `assignedUserName`, `startsAtUtc`, `endsAtUtc`, `durationMinutes`, `status`, `unitPrice`, `customerNotes`, `internalNotes`, `cancellationReason`.

Disponibilidade retorna `{ date, serviceId, durationMinutes, timeZone, slots: [{ startsAtUtc, endsAtUtc }] }`. Use os slots retornados; não recalcular disponibilidade no frontend.

Histórico retorna `{ id, previousStatus, newStatus, changedByUserId, changedByUserName, changedAtUtc, reason }`. Relatório retorna contagens por status e `linkedRevenue`.

## Endpoints públicos

Base: `/api/public/petshops` (sem Authorization).

- `GET /{publicCode}` → `{ businessName, logoUrl, timeZone }`
- `GET /{publicCode}/services` → `[{ id, name, description, price, durationMinutes, imageUrl }]`
- `GET /{publicCode}/availability?date=2026-08-01&serviceId={guid}` → mesmo DTO de disponibilidade
- `POST /{publicCode}/appointment-requests` → `PublicAppointmentCreatedDto`
- `GET /appointments/{accessToken}` → `PublicAppointmentTrackingDto`
- `POST /appointments/{accessToken}/cancel` → `PublicAppointmentTrackingDto`

Solicitação pública:

```json
{
  "customerName": "Ana",
  "phoneNumber": "11999999999",
  "petName": "Rex",
  "petSpecies": 1,
  "petSize": 2,
  "petBreed": "SRD",
  "serviceId": "guid",
  "startsAtUtc": "2026-08-01T14:00:00Z",
  "notes": null
}
```

Resposta de criação: `{ accessToken, accessExpiresAtUtc, status, startsAtUtc }`. Salve o token imediatamente. Tracking: `{ petName, serviceName, startsAtUtc, endsAtUtc, status, canCancel }`.

## Status, conflitos e timezone

Transições válidas:

- `Requested → Confirmed`
- `Confirmed → InProgress`
- `InProgress → Completed`
- `Requested → Cancelled`
- `Confirmed → Cancelled`
- `Confirmed → NoShow`
- `Completed`, `Cancelled` e `NoShow` são terminais.

Um atendimento em andamento não pode ser reagendado nem cancelado. O backend impede sobreposição para o mesmo profissional e respeita bloqueios gerais/profissionais; criação e reagendamento usam transação serializável. A disponibilidade é sugestiva: outro cliente pode ocupar o slot antes do POST, portanto trate `409` e recarregue os horários.

Todas as datas de API com horário são UTC ISO-8601 com `Z`. `date` da disponibilidade é a data civil no timezone da empresa. O backend converte expediente local para UTC e devolve `timeZone`; o frontend formata para exibição com esse timezone, sem enviar horário local sem offset.

## Erros HTTP

- `400`: payload, enum, data UTC, intervalo, arquivo ou regra de validação inválidos.
- `401`: sessão ausente/inválida nos endpoints internos.
- `403`: capability/módulo indisponível para a empresa.
- `404`: recurso inexistente, de outra empresa, código público inválido ou token público inválido/expirado/revogado.
- `409`: conflito de agenda, telefone duplicado, transição inválida, relação incompatível, pedido já vinculado ou estado final.
- `429`: rate limit de upload/escrita pública.

Erros são retornados no formato `ProblemDetails` usado pelo backend. Mostre `detail` quando seguro e uma mensagem amigável como fallback.

## Ordem recomendada para o frontend

1. Atualizar tipos do overview e proteger rotas/menu por `capabilities`.
2. Criar shell Pet Shop sem habilitar telas de restaurante.
3. Tutores, depois pets e upload de foto.
4. Catálogo filtrado por `Service` e edição da duração.
5. Agenda interna: settings, profissionais, disponibilidade, CRUD, status, histórico e bloqueios.
6. Pedido/pagamento usando somente os DTOs retornados.
7. Fluxo público por `publicCode`, persistência segura do `accessToken` e tracking/cancelamento.
8. Tratar loading, vazio, 401/403/404/409/429; testes de isolamento visual e build.

Não consulte tabelas, migrations ou entidades para inventar campos. Se algo necessário não estiver neste contrato/API, registre como lacuna de backend em vez de improvisar `CompanyId`, `TenantId`, status ou regras no cliente.

## Aplicação local, build e testes

Faça backup antes da migration. Em ambiente local, a partir da raiz:

```powershell
dotnet ef database update --project backend/ZeroPaper.csproj --startup-project backend/ZeroPaper.csproj
dotnet ef migrations has-pending-model-changes --project backend/ZeroPaper.csproj --startup-project backend/ZeroPaper.csproj
dotnet build ZeroPaper.slnx
dotnet test ZeroPaper.slnx --no-build
```

Alternativa controlada: revisar e aplicar `docs/sql/20260731_petshop_backend_idempotent.sql`. Resultado validado no commit funcional: modelo sem mudanças pendentes, build com 0 erros e 14/14 testes aprovados. Existem 11 warnings de nulabilidade preexistentes no PrintAgent `net48`, sem relação com Pet Shop.

## Limitações conhecidas desta versão

- Capabilities de Pet Shop são preset do segmento, ainda não flags comerciais independentes.
- O fluxo público cria/reutiliza tutor e pet antes de tentar reservar; em conflito de slot esses cadastros podem permanecer válidos para nova tentativa.
- Cancelamento público revoga o token; por não haver usuário autenticado, ele não gera entrada de histórico com `ChangedByUserId` nesta versão.
- A UI deve obter o `PetShopPublicCode` pelo fluxo administrativo autorizado; não existe endpoint público para descobri-lo.
- Não fazer deploy, push ou aplicar migration em produção sem autorização explícita.
