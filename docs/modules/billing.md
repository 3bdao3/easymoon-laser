# Billing & Payments module (STEP 13)

## Responsibility

Org/branch-scoped commercial invoices and manual payment capture against a single invoice. Snapshots catalog prices on lines at charge time. Does **not** implement tax/VAT, GL posting, refunds aggregate, payment gateways, commissions, expenses, or inventory/procurement billing.

**General ledger & AR (STEP 17–19):** Billing remains operational owner of invoices/payments. Finance GL is separate. On Issue / Payment / Void / Payment reverse, Billing calls Finance Application `ISubledgerPostingPort` to post **AR subledger** movements (PatientId as party). **No automatic GL journals.** Multi-invoice payment allocation remains unresolved.

## Domain model

| Aggregate / entity | Notes |
|--------------------|--------|
| `Invoice` | Draft → Issue → Issued / PartiallyPaid / Paid; Void when unpaid (Draft or Issued, PaidAmount = 0). |
| `InvoiceLine` | Historical snapshot: unit price, description, service code, optional `ServicePriceId`, currency. Sources: `Service`, `PackageItem`. |
| `Payment` | Captured → Reversed (never deleted). One `InvoiceId` per payment (no allocation table in V1). |

## Historical snapshot

**Issued invoices must never recalculate amounts from the live Services catalog.** Line `UnitPrice` and totals are fixed at draft line creation (service add or package expansion). Changing catalog prices after issue does not alter issued invoices.

## Lifecycles

- **Invoice**: `CreateDraft` → edit lines/header/discount → `Issue` (assigns `INV-{yyyy}-{000001}`) → `RegisterPayment` on invoice → `PartiallyPaid` / `Paid`. `Void` only if `PaidAmount == 0` and status Draft or Issued.
- **Payment**: `Capture` → optional `Reverse` (updates invoice `PaidAmount`).

## Pricing rules (V1)

- `TaxAmount` is always **0** (no VAT engine).
- Invoice-level `DiscountAmount` only on header; line discounts via `AddServiceLine` / line update.
- **Overpayment rejected** at domain level.
- **Packages**: expanded via `IPackageLookup.GetPackageItemsAsync`; each item becomes a `PackageItem` line with **current** service unit prices snapshotted. No special bundle price — sum of expanded lines.
- **BranchId** on create from `IOrganizationContext`.

## Cross-module ports

- `IPatientLookup` — patient same org, active for new invoices.
- `IMedicalVisitBillingPort` — when `MedicalVisitId` set: same org, patient match; **any visit status allowed** (including cancelled) for V1 unless product tightens later.
- `IServiceLookup` / `IPackageLookup` — resolve prices; inactive service/package cannot be newly added.

## API

| Area | Base route |
|------|------------|
| Invoices | `/api/v1/billing/invoices` |
| Payments | `/api/v1/billing/payments` |

Draft CRUD, issue, void, patient outstanding, payment record/reverse. RowVersion on invoice updates and payment capture; **409** on concurrency.

## Permissions (`Finance.*`)

| Code | Purpose |
|------|---------|
| `Finance.Invoices.View` | Search/get invoices, patient outstanding |
| `Finance.Invoices.Create` | Create draft |
| `Finance.Invoices.Update` | Edit draft/lines |
| `Finance.Invoices.Issue` | Issue draft |
| `Finance.Invoices.Void` | Void unpaid |
| `Finance.Payments.View` | Search/get payments |
| `Finance.Payments.Create` | Record payment |
| `Finance.Payments.Reverse` | Reverse captured payment |

**Roles (seed):** SuperAdmin/Admin — all permissions; Accountant — all `Finance.*` + Patients/Services/Packages/MedicalVisits view; Receptionist — operational billing (view/create/update/issue invoices, view/create payments).

## Reporting (STEP 22)

Invoice register, payment register, and outstanding invoices are exposed under `/api/v1/reports/billing/...` (read-only; `TaxAmount` remains whatever Billing stores — currently 0). Billing totals in management summary are **not** GL revenue. See ADR-017.

## Schema

`billing`: `Invoices`, `InvoiceLines`, `Payments`, `InvoiceNumberSequences`, `PaymentNumberSequences`. Money `decimal(18,2)`.

## Migration

`AddBillingAndPaymentsModule`, schema `billing`.

## Deferred / out of scope

- Refunds aggregate and void-with-payments adjustment flows.
- Tax engine, commission, expenses, GL, procurement/inventory charges.
- Payment gateway integration.
- Multi-invoice payment allocation.

## Unresolved business decisions

- Exact payment method enum for clinic (Cash/Card/BankTransfer/Other are provisional).
- Whether package line input `LineDiscountAmount` should apply per expanded item vs invoice discount only (V1: discount on package expansion lines is 0; use invoice-level discount).
- Linking invoice issue to visit status (currently any status if patient matches).
- Default currency on patient outstanding summary (V1 returns `EGP` label when aggregating mixed-currency is impossible).

## Technical notes

- Sequential numbers use the same SQL pattern as prescriptions (`UPDLOCK` / `HOLDLOCK`).
- `LoggingBillingDomainEventDispatcher` + collector for integration tests and observability.
