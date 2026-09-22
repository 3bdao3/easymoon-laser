# ADR-017 — Financial & Management Reporting (STEP 22)

## Status

Accepted (STEP 22)

## Context

ErpClink already owns financial and operational facts in module boundaries (Finance GL/AR/AP, Billing, Inventory valuation, Assets, Procurement). Users need production reporting without inventing a second ledger, CoA mappings, automatic journals, or fabricated KPIs.

Architecture ADR-011 suggested a Reports module with read projections. STEP 22 implements a **thin reporting façade** that reuses **Application query contracts** from owning modules.

## Decision

1. **Reports.Application** defines report DTOs and `IReportingQueryService`.
2. **Reports.Infrastructure** implements the façade by composing existing Application services only (Finance, Billing, Inventory, Assets, Procurement).
3. **No** Reports.Infrastructure → other module Infrastructure references.
4. **No** reporting tables / materializations in STEP 22 — live queries with pagination, filters, `AsNoTracking` in source services.
5. Host exposes `/api/v1/reports/...` with dedicated `Reports.*.View` permissions.
6. Management summary returns **available** metrics from real sources and explicit **unavailable** metrics (NetProfit, EBITDA, CashFlow, GrossMargin) with reasons.
7. Export (CSV/Excel) deferred — no existing export infrastructure; same query contracts will back exports later.
8. Caching deferred — avoid org-sensitive cache keys until needed.

## Source-of-truth rules

| Report | Owner / source | Must not claim |
|--------|----------------|----------------|
| GL / Trial Balance / Journals | Finance posted journals | Draft as posted |
| AR / AP statement & aging | Finance subledger | PO as AP |
| Invoice / Payment registers | Billing | Tax/methods not stored |
| Stock / valuation / issue cost | Inventory | Selling price as cost; GL COGS |
| Asset register / depreciation / NBV | Assets | Recalculate history with today’s config |
| PO / receiving | Procurement / Inventory GR | PO totals as AP liability |
| Management KPIs | Mixed, labeled by `Source` | Fake profit / margin |

## Date semantics

- Report filters use `DateOnly` inclusive `FromDate` / `ToDate` where the owning query does.
- Stock movements filter `CreatedAtUtc` with inclusive From day and exclusive next-day To (Inventory convention).
- AR aging: `DueDate` when present; otherwise `TransactionDate` (Billing invoices have no due date).

## Isolation & security

- Organization isolation enforced by owning module services / current user context (server-side).
- Branch filters passed through where owning queries support them.
- Angular filters are UX only; permissions enforced on API.

## Consequences

- Billing totals and posted GL revenue may diverge until accounting integration posts journals — reports expose both without reconciling.
- Inventory issue cost is labeled **not GL COGS**.
- Management billing aggregates may note partial pages until dedicated SQL aggregates are added.
- Account activity = General Ledger filtered by `accountId`.

## Unresolved

- Dedicated SQL aggregate endpoints for management billing totals (avoid page-limited sums).
- Excel/CSV export reuse once export infrastructure exists.
- Optional read replicas / materialized reporting tables if volume requires.
- Chart series APIs (not invented in STEP 22).
