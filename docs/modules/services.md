# Services & Packages module (STEP 12)

## Responsibility

Org-scoped catalog of billable healthcare services, optional categories, append-only price history, and service bundles (packages). Does **not** create invoices, payments, inventory, or patient package purchases.

## Domain model

| Aggregate / entity | Notes |
|--------------------|--------|
| `HealthcareService` | Billable service (`MedicalService` in product docs). Code, name, category, default price, currency, optional duration, active flag. |
| `ServicePrice` | Append-only rows; current price = row with `EffectiveToUtc == null`. |
| `ServiceCategory` | Maintainable category catalog (not a hardcoded enum). |
| `HealthcarePackage` | Named bundle with code; items reference services by id. |
| `PackageItem` | Unique `(PackageId, ServiceId)`; quantity must be positive. |

## Lifecycles

- **Service**: create (active + initial price row) → update (optional new price row) → activate/deactivate.
- **Category**: same activate/deactivate pattern as medications.
- **Package**: create → add/update/remove items while **active** → deactivate freezes item edits.

## Pricing (billing boundary)

- Price changes **close** the current `ServicePrice` and **open** a new row; `HealthcareService.DefaultPrice` mirrors the current amount.
- **Billing must snapshot** `amount`, `currency`, and optionally `ServicePriceId` on invoice lines at charge time. This module does not recalculate historical charges from today’s catalog price.
- V1: organization-level default price only (no branch/clinic price lists).

## API

| Area | Base route |
|------|------------|
| Services | `/api/v1/services` |
| Packages | `/api/v1/packages` |
| Categories | `/api/v1/service-categories` (uses `Services.*` permissions) |

Standard CRUD/search, activate/deactivate, package item POST/PUT/DELETE. RowVersion on updates; 409 on concurrency.

## Permissions

`Services.View|Create|Update|Activate|Deactivate`, `Packages.View|Create|Update|Activate|Deactivate`.

## Isolation

All queries filter by `OrganizationId` from `IOrganizationContext`. No `BranchId` on catalog entities in V1.

## Application contracts

- `IServiceLookup` — resolve service + current price for future Billing/Visits.
- `IPackageLookup` — list package lines with current unit prices (indicative; billing still snapshots).

## Migration

`AddServicesModule`, schema `services`.

## Unresolved business decisions

- Default currency per organization vs per service (V1 defaults to `EGP` on create).
- Branch/clinic-specific price lists and effective-dated lists beyond org default.
- Package bundle price vs sum of line prices (SVC / product §10.9).
- Commission rules (SVC-04) — out of scope for STEP 12.
- Whether inactive services may remain on existing packages vs block new adds only (V1: block **new** adds; existing lines unchanged).

## Technical debt

- Category management UI is minimal (dropdown + API); no dedicated category admin screen.
- No dedicated permission codes for categories (reuses `Services.*`).
