# Assets (Fixed Assets) Module

## Scope (STEP 16 + STEP 21)

Fixed asset register (categories, locations, lifecycle, warranty, history) plus **asset accounting foundation**: capitalization, provisional straight-line depreciation schedule/transactions, accumulated depreciation, NBV, disposal financial facts, and STEP 18 accounting intents.

This module is **separate from Inventory** and **does not** create GL journals or invent CoA mappings.

## Boundaries

| Concern | Assets | Finance |
|--------|--------|---------|
| Asset register + financial profile | Owner | — |
| Depreciation calculation / schedule | Owner | — |
| JournalEntry / CoA / fiscal calendar | — | Owner |
| Accounting intents | Calls `IAccountingPostingPort` | Persists intents |

No Assets.Infrastructure → Finance.Infrastructure. Application contract only.

## Operational vs financial lifecycle

| Operational `AssetStatus` | Financial `AssetFinancialStatus` |
|---------------------------|-----------------------------------|
| Active / UnderMaintenance / Retired | Incomplete / Capitalized / Depreciating / FullyDepreciated / Disposed |

Legacy assets without capitalization remain **Incomplete** and cannot depreciate (costs are not fabricated).

## Depreciation (provisional)

- Method: `ProvisionalStraightLine` via `IDepreciationMethod` (**not** product-approved — ADR-016)
- Depreciable base = CapitalizedCost − ResidualValue
- Monthly schedule persisted at capitalization; posted transactions immutable
- Idempotency: `dep:{assetId}:{YYYY-MM}:v{version}`
- NBV = CapitalizedCost − AccumulatedDepreciation; never below residual; never negative

## APIs

- `GET/POST /api/v1/assets/accounting...`
- `GET/POST /api/v1/assets/depreciation...`
- `GET /api/v1/assets/valuation`
- `GET/POST /api/v1/assets/disposals`

## Permissions

`Assets.Accounting.View|Capitalize`, `Assets.Depreciation.View|Post|Schedule`, `Assets.Disposal.View|Process` (+ existing register permissions).

## Unresolved product decisions

- Final depreciation method / frequency / partial-period rules
- Fiscal-period enforcement for depreciation posting
- Impairment, revaluation, tax books
- Automatic capitalization from Procurement/Inventory
- Opening balances for legacy assets
- Whether disposal proceeds are mandatory
- Durable Outbox for accounting intents

## Migrations

- `AddAssetsModule`
- `AlignAssetsPurchaseWarrantyAndHistory`
- `AddAssetAccountingAndDepreciation`

## Reporting (STEP 22)

Asset register / depreciation / valuation reports are read through Reports → `IAssetAccountingService`. Incomplete (non-capitalized) assets appear without fabricated costs. See ADR-017.
