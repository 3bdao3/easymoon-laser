# Finance — GL, Accounting Integration & AR/AP Subledgers

## Product scope

`product.md` lists full accounting ERP replacement as **Out of Scope for V1**. Finance delivers:

- **STEP 17:** GL foundation — CoA, fiscal calendar, manual journals, GL, trial balance
- **STEP 18:** Accounting integration foundation (no auto journals / mappings)
- **STEP 19:** AR / AP **subledger** foundation — balances, statements, aging, Billing→AR posting
- **STEP 20:** Inventory valuation / cost facts live in **Inventory** (provisional FIFO). Finance does **not** own stock qty or invent Inventory/COGS account mappings.
- **STEP 21:** Fixed asset capitalization / depreciation / NBV live in **Assets** (provisional straight-line). Finance receives accounting intents only — no automatic journals or CoA mappings.
- **STEP 22:** Financial & management **reporting** lives in **Reports** façade — reads Finance GL/TB/journals/AR/AP via Application contracts. No second ledger; no invented mappings.

## Boundaries

| In scope | Explicitly out of scope |
|----------|-------------------------|
| CoA, fiscal, manual journals | Default CoA seed |
| Accounting integration intents | Automatic GL from AR/AP / Inventory / Assets |
| AR/AP immutable subledger | Supplier Invoice module |
| Customer statement (PatientId) | CRM Customer master |
| Supplier statement (SupplierId) | PO → AP automatic liability |
| Aging foundation | VAT, FX, credit limits, write-offs |
| Future consumption of Assets depreciation intents | Invented Accumulated Depreciation / Expense CoA mappings |

## Customer & supplier ownership

| Party | Master owner | Finance stores |
|-------|--------------|----------------|
| AR customer | **Patients** (`PatientId`) | `PartyId` + optional display snapshot |
| AP supplier | **Procurement** (`SupplierId`) | `PartyId` + optional display snapshot |

No `FinanceCustomer` / `FinanceSupplier` tables.

## AR / AP subledger (STEP 19)

Unified table `finance.SubledgerTransactions` with `SubledgerType` = `Ar` | `Ap`.

| Direction | AR meaning | AP meaning |
|-----------|------------|------------|
| Debit | Increase receivable | Decrease payable |
| Credit | Decrease receivable | Increase payable |

- Posted transactions are **immutable**; corrections via compensating posts / reversal helper
- Idempotency: unique `(OrganizationId, SourceModule, SourceType, SourceId, EventType, IdempotencyKey)`
- Materialized `finance.SubledgerPartyBalances` updated transactionally (not manually editable)
- Balance = signed sum of movements (also reflected in materialized balance)

### Billing → AR (safe flows)

| Event | AR movement |
|-------|-------------|
| InvoiceIssued | Debit TotalAmount |
| PaymentCaptured | Credit Amount |
| PaymentReversed | Debit Amount |
| InvoiceVoided (issued unpaid) | Credit TotalAmount |

Wired via `BillingArIntegration` → `ISubledgerPostingPort` (Application contract). **No** Billing.Infrastructure → Finance.Infrastructure.

### Procurement → AP

**PurchaseOrder is not AP.** No AP posts from Draft/Submitted/Approved PO.  
AP posting port exists for future Supplier Invoice / obligation sources. Documented: AP source transaction not yet available in product.

### GL

Subledger posts may register STEP 18 accounting intents (`SubledgerPosted`). **No journal entries / account mappings.**

## Aging

Buckets: Current, 1-30, 31-60, 61-90, 91+.  
Open items = positive outstanding by source reference.  
**DueDate:** used when present. Billing Invoice has **no due date** — aging uses `TransactionDate` (documented limitation).

## APIs

- `/api/v1/finance/ar/...` balance, statement, transactions, aging
- `/api/v1/finance/ap/...` balance, statement, transactions, aging

Permissions: `Finance.AR.View`, `Finance.AR.Statement`, `Finance.AP.View`, `Finance.AP.Statement`, `Finance.Aging.View`.

No unrestricted balance-adjustment HTTP endpoints.

## Migrations

- `AddFinanceAndGeneralLedgerModule`
- `AddAccountingIntegrationFoundation`
- `AddFinanceArApSubledgers`

## Unresolved

- AR/AP control accounts & mappings to GL
- Supplier Invoice / AP obligation document
- Invoice due dates / credit terms
- Multi-invoice payment allocation
- Credit/debit notes, refunds, write-offs, bad debt
- Multi-currency / FX
- Outbox (STEP 24)
- Manual adjustment transactions (not product-approved)

See `docs/adr/ADR-014-ar-ap-subledgers.md`.

## Reporting (STEP 22)

Finance remains the source of truth for:

- General ledger / trial balance / journal register (`Reports` → `IGeneralLedgerService` / `ITrialBalanceService` / `IJournalService`)
- AR/AP statements, aging, outstanding (`IArSubledgerQueryService` / `IApSubledgerQueryService`)

Reports permissions (`Reports.Finance.*`, `Reports.AR.*`, `Reports.AP.*`) are separate from operational Finance permissions.  
See `docs/adr/ADR-017-financial-management-reporting.md`.
