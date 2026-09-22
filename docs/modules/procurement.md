# Procurement module (Step 14)

## Scope

Step 14 delivers **Suppliers**, **Purchase Orders** with lines, and lifecycle **Draft → Submitted → Approved** plus **Cancelled**. Tax is always zero (same as Billing). Currency is ISO 4217 (default **EGP**).

**Procurement does not own inventory or stock movements.** Inventory will consume approved/receivable procurement information through Application-level contracts (`ISupplierLookup`, `IPurchaseOrderLookup`) and future events.

**Goods receipt / receiving (PRC-03)** is owned by **Inventory** (Step 15+20). Procurement exposes `IPurchaseOrderReceivingPort` (`ApplyReceipt` / `ReverseReceipt`) and `QuantityReceived` on lines. Inventory reserves PO quantity **before** posting stock (no ambient distributed transaction / DTC), then posts quantity + cost layers atomically on `InventoryDbContext`. If inventory post fails, PO receipt is reversed.

**Supplier balances and payments (PRC-04)** are owned by **Finance AR/AP subledgers** (STEP 19). Procurement keeps Supplier master data; Finance references `SupplierId` only. **Approved Purchase Orders do not create AP liabilities.** AP awaits a future Supplier Invoice / obligation document.

## Reporting (STEP 22)

PO register and receiving summary are operational reports under `/api/v1/reports/procurement/...`. PO totals are labeled **not AP**. See ADR-017.

## Cost snapshot

Line `UnitCost` and `DescriptionSnapshot` are captured at PO draft creation/update time. They are **never recalculated** from a future inventory catalog. Optional `CatalogItemId` is a nullable reference only (no FK to Inventory).

**STEP 20:** Inventory goods receipts use this PO line `UnitCost` (via `UnitCostSnapshot` on receipt lines) as the **receipt cost source** for inventory valuation. Changing a PO line cost later does **not** rewrite historical inventory cost layers.

## Lifecycle

Implemented statuses: `Draft`, `Submitted`, `Approved`, `Cancelled`, plus receiving statuses `Ordered` (reserved), `PartiallyReceived`, and `Received` (updated by Inventory via `ApplyReceipt`).

`Closed` remains future scope.

Approval is **one-level**: Submit then Approve (PRC-05).

## Technical notes

- Schema: `procurement`
- Numbers: `SUP-{yyyy}-{000001}`, `PO-{yyyy}-{000001}` via UPDLOCK/HOLDLOCK sequences
- Money: `decimal(18,2)`, `Money.Round` AwayFromZero
- Concurrency: `RowVersion` on Supplier and PurchaseOrder
- Permissions prefix: `Procurement.*`

## Unresolved product decisions

- Multi-level / amount-based approval workflows
- Branch-specific supplier catalogs
- VAT / tax on purchase orders
- Credit limits and supplier rating
- Return-to-supplier flows

## Technical debt

- Domain events logged only (`LoggingProcurementDomainEventDispatcher`) — no transactional outbox
- GoodsReceipt aggregate belongs in **Inventory**
- Supplier payment runs belong in **Finance**
