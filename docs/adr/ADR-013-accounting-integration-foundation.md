# ADR-013 — Accounting Integration Foundation (STEP 18)

| Field | Content |
|-------|---------|
| **Status** | Accepted |
| **Date** | 2026-09-14 |
| **Context** | Operational modules (Billing, Procurement, Inventory, Assets) will eventually need to post into Finance GL without violating modular-monolith boundaries or inventing account mappings prematurely. |
| **Decision** | Introduce an Application-level accounting integration foundation owned by Finance: `AccountingPostingRequest`, `IAccountingPostingPort`, `AccountingTransactionRequested`, durable org-scoped idempotency in `finance.AccountingIntegrationRequests`. **Do not** auto-create journals or default CoA mappings in this step. |
| **Alternatives** | (1) Direct cross-module DbContext writes; (2) module-specific events that construct `JournalEntry`; (3) in-memory idempotency; (4) full Outbox+Hangfire now. |
| **Reasoning** | Keeps Finance as sole owner of accounts/journals/posting rules; gives source modules a stable contract; DB uniqueness ensures concurrency-safe idempotency; defers business accounting policy and Outbox to later approved steps. |
| **Consequences** | Source modules may later reference **Finance.Application** only. No journals until explicit mappings exist. Outbox delivery remains STEP 24. Unresolved: revenue/cash/inventory/asset account mappings, VAT, multi-currency GL, AR/AP. |

## Boundary

| Owner | Owns |
|-------|------|
| Finance | Accounts, fiscal calendar, journals, posting/reversal, integration idempotency |
| Billing | Invoices, payments |
| Procurement | Suppliers, POs |
| Inventory | Warehouses, stock, goods receipts |
| Assets | Asset lifecycle |

Cross-module: Application contracts and/or integration events only. No Infrastructure→Infrastructure references. No distributed or cross-DbContext transactions.

## Idempotency

Unique indexes (include `OrganizationId`):

1. `(OrganizationId, SourceModule, SourceType, SourceId, EventType, IdempotencyKey)`
2. `(OrganizationId, IdempotencyKey)`

Statuses: `Pending` | `Processing` | `Succeeded` | `Failed`. STEP 18 accepts requests as `Pending` (registered for future mapping). `Succeeded` reserved for future journal creation. Failed rows may be safely retried to `Pending`.

## Correlation

`CorrelationId` is required on persisted requests and on `AccountingTransactionRequested`. HTTP correlation middleware is still a broader observability concern; callers should pass the request correlation when available.

## Future Outbox (STEP 24)

Intended path:

`Domain / source commit` → Outbox row → Hangfire/worker → `IAccountingPostingPort` / handler → (later) mapping → Finance journal rules.

STEP 18 does **not** implement Outbox publishers or background workers.
