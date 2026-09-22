# ADR-015 — Inventory Valuation Method (STEP 20)

| Field | Content |
|-------|---------|
| **Status** | Accepted (provisional method) |
| **Date** | 2026-09-14 |
| **Context** | STEP 20 requires inventory cost engine. `docs/modules/inventory.md` and product docs list FIFO/LIFO/valuation as **unresolved**. No approved Chart of Accounts or GL mappings. |
| **Decision** | Introduce `IInventoryValuationMethod` strategy. Default registered implementation is **Provisional FIFO** with explicit cost layers ordered by `ReceiptDate` / `CreatedAtUtc`. Document that this is **not** a finalized business decision. |
| **Alternatives** | Weighted average; standard cost; specific identification; defer costing entirely. |
| **Reasoning** | FIFO is deterministic, auditable, fits receipt `UnitCostSnapshot` and batch association, and supports the STEP 20 acceptance criteria without inventing GL accounts. |
| **Consequences** | Product must still approve FIFO vs weighted average vs standard cost. WeightedAverage enum exists but is not implemented. Legacy stock without cost layers cannot be issued with cost until an approved opening-balance mechanism exists. |

## Rounding

- Quantity: 3 dp (`Quantity.Scale`)
- Unit cost: 6 dp (`InventoryCost.UnitCostScale`)
- Money totals / inventory value: 2 dp (`Money.Scale`)
- Last layer slice consumes remaining value to avoid drift

## No GL

Cost transactions feed future STEP 18 accounting integration only. No automatic journals.
