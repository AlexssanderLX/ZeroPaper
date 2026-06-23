# ZeroPaper Print Agent API Contract

## Workspace panel endpoints

All workspace endpoints require the normal workspace `Authorization` header.

### `GET /api/workspace/printing`

Returns printing settings, agent status, counters, recent order jobs and the last agent error.

Important fields:

- `enableAutomaticPrinting` / `autoPrintEnabled`: controls whether the agent can claim jobs.
- `hasAgentToken`: true when a token was generated.
- `agentOnline`: true when the last heartbeat is recent.
- `lastSeenAtUtc`: last agent heartbeat.
- `lastError` and `lastErrorAtUtc`: latest error reported by the agent.
- `pendingJobs`, `failedJobs`, `printedJobs`: order print jobs plus technical jobs.

### `PATCH /api/workspace/printing`

Updates printing settings.

```json
{
  "enableAutomaticPrinting": true,
  "paperProfile": "Thermal80mm",
  "ordersPerPage": 1
}
```

### `POST /api/workspace/printing/agent-token`

Rotates the print-agent token. The raw token is returned only once.

Legacy alias: `POST /api/workspace/printing/agent-key`.

```json
{
  "agentToken": "generated-secret-token",
  "agentKey": "generated-secret-token",
  "printing": {}
}
```

### `POST /api/workspace/printing/test-job`

Creates a test print job for the current company.

```json
{
  "notes": "Teste da impressora do caixa"
}
```

Response:

```json
{
  "jobId": "00000000-0000-0000-0000-000000000000",
  "status": "Pending",
  "queuedAtUtc": "2026-06-21T12:00:00Z",
  "printing": {}
}
```

## Windows agent endpoints

The agent must send the token in one of these forms:

- `X-ZP-Agent-Token: <token>` preferred.
- `X-ZP-Agent-Key: <token>` legacy.
- `Authorization: Bearer <token>` accepted.

The token is stored only as a SHA-256 hash in the database. A token always resolves to exactly one active company, so all jobs and settings are company-scoped server-side.

### `POST /api/print-agent/register`

Registers or refreshes an agent installation.

```json
{
  "agentName": "PDV Windows",
  "printerName": "EPSON TM-T20",
  "appVersion": "1.0.0"
}
```

### `POST /api/print-agent/heartbeat`

Refreshes online status and printer metadata. Returns `204`.

### `POST /api/print-agent/jobs/claim-next`

Claims the next pending job if `autoPrintEnabled` is true. Returns `204` when there is no job or automatic printing is off.

Legacy alias: `POST /api/print-agent/orders/claim-next`.

```json
{
  "agentName": "PDV Windows",
  "printerName": "EPSON TM-T20"
}
```

Response includes both `jobId` and legacy `orderId`. For order jobs both ids are the same. For test jobs, `orderId` is the technical job id for legacy compatibility.

```json
{
  "jobId": "00000000-0000-0000-0000-000000000000",
  "orderId": "00000000-0000-0000-0000-000000000000",
  "jobKind": "Order",
  "isTest": false,
  "paperProfile": "Thermal80mm",
  "ordersPerPage": 1,
  "restaurantName": "Restaurante",
  "tableName": "Mesa 1",
  "items": []
}
```

### `POST /api/print-agent/jobs/{jobId}/printed`

Marks an order job or technical job as printed. Returns `204`.

Legacy alias: `POST /api/print-agent/orders/{orderId}/complete`.

### `POST /api/print-agent/jobs/{jobId}/error`

Marks an order job or technical job as failed and stores the latest agent error for the panel. Returns `204`.

Legacy alias: `POST /api/print-agent/orders/{orderId}/fail`.

```json
{
  "agentName": "PDV Windows",
  "printerName": "EPSON TM-T20",
  "errorMessage": "Printer offline"
}
```

### Batch aliases

- `POST /api/print-agent/jobs/printed-batch`
- `POST /api/print-agent/jobs/error-batch`
- Legacy: `orders/complete-batch` and `orders/fail-batch`.

Batch bodies still use `orderIds` for compatibility; the list may contain order ids or technical `printJob` ids.
