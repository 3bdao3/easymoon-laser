# Inventory module (Steps 15 + 20)

## Scope

Inventory owns **warehouses**, **items/categories**, **stock balances**, **batches/expiry**, **stock movements**, **goods receipts**, and (STEP 20) **inventory valuation / cost layers / issue cost**.

Procurement owns suppliers and purchase orders; Inventory consumes receivable POs via `IPurchaseOrderReceivingPort` (Application only).

**Not in scope:** Assets, automatic GL journals, serial numbers, multi-UOM conversion, warehouse transfers, landed cost, sales returns, invented CoA mappings.

## Valuation (STEP 20)

| Topic | Behavior |
|-------|----------|
| Method | **Provisional FIFO** via `IInventoryValuationMethod` / `FifoInventoryValuationMethod` (ADR-015). **Not** product-approved as final. |
| Receipt cost | `PurchaseOrderLine.UnitCost` → `GoodsReceiptLine.UnitCostSnapshot` → cost layer |
| Issue cost | FIFO consume open layers; Adjust Out uses same engine |
| Adjust In | Requires explicit `UnitCost` (no silent zero / selling price) |
| Legacy stock | Quantities without cost layers are **not** fabricated; issues fail with `inventory.insufficient_costed_stock` if costed qty insufficient |
| GL | No automatic journals / account mappings |

### Tables

- `inventory.InventoryCostLayers`
- `inventory.InventoryCostTransactions`
- `inventory.InventoryCostAllocations`
- `StockBalances.InventoryValue` (materialized; not manually editable)

### APIs

- `GET /api/v1/inventory/valuation`
- `GET /api/v1/inventory/valuation/cost-history`
- `GET /api/v1/inventory/valuation/cost-layers`
- `GET /api/v1/inventory/valuation/issue-costs`

Permissions: `Inventory.Valuation.View`, `Inventory.CostHistory.View`, `Inventory.CostLayers.View`, `Inventory.IssueCost.View`.

## Reporting (STEP 22)

Reports module exposes stock balances, movements, valuation, and issue-cost (**COGS foundation, not GL COGS**) via Inventory Application ports. Legacy uncosted stock is not fabricated. See ADR-017.

## Receiving

- Goods receipt posted in one Inventory transaction (qty + cost + movements) then Procurement receiving port.
- Cancel reverses qty and reverses unconsumed cost layers only (fails if layers partially consumed).

## Schema

- Quantities: `decimal(18,3)`; unit cost layers: `decimal(18,6)`; money: `decimal(18,2)`
- Concurrency: `RowVersion` on balances, batches, cost layers

## Unresolved product decisions

- Final valuation method (FIFO vs weighted average vs standard cost)
- Configurable allow negative stock
- Over-receive permission / tolerance
- Multi-UOM conversion
- Opening inventory valuation for legacy qty
- Landed cost / freight / VAT capitalization
- Warehouse transfers, purchase/sales returns
- FEFO (separate from FIFO costing)
- Automatic GL / COGS account mappings

## Migrations

- `AddInventoryAndStockModule`
- `AddInventoryValuationAndCostAccounting`
