# Clinic Management System — Technical Architecture

| Attribute | Value |
|-----------|--------|
| Document | `docs/architecture.md` |
| Status | Draft — Architecture Design |
| Version | 1.0 |
| Source of truth (product) | `docs/product.md` |
| Implementation | **Not started** — architecture only |

---

## How to read this document

This document designs the **technical architecture** for the Clinic Management System described in `docs/product.md`.

It does **not**:

- Confirm open business questions from the PRD as requirements
- Introduce microservices
- Specify implementation code, migrations, controllers, or Angular components

Sections distinguish:

| Label | Meaning |
|-------|---------|
| **Confirmed** | Required by `docs/product.md` |
| **Recommendation** | Architectural choice for V1 |
| **Assumption** | Working assumption until stakeholder decides |
| **Stakeholder decision** | Must be resolved before or during relevant implementation |

---

## 1. Architecture Vision

Deliver a **production-ready, workflow-driven Clinic Management System** as a **modular monolith** on ASP.NET Core + SQL Server with an Angular staff SPA.

The architecture must:

- Encode real clinic workflows and state machines in domain/application logic (not CRUD controllers)
- Preserve **module ownership** and prevent cross-module entity mutation
- Guarantee **financial** and **inventory** transactional integrity
- Enforce **API-authoritative RBAC** and **append-only audit**
- Scale the API horizontally behind a shared database
- Remain **ready** for multi-branch and later multi-tenant without building SaaS complexity in V1

V1 ships as **one deployable API host + one Angular app + one SQL Server database**, internally modularized so teams can evolve modules independently and extract services later if ever justified.

---

## 2. Architectural Principles

1. **Modular monolith first** — one process, many modules; no microservices in V1.
2. **Clean Architecture per module** — dependencies point inward to domain.
3. **Explicit boundaries** — modules own aggregates; others use contracts/events/queries.
4. **Business rules in domain/application** — controllers are thin transport adapters.
5. **State machines are explicit** — no arbitrary status patches from API clients.
6. **API is the security authority** — UI permissions are UX only.
7. **Money and stock are ledgers** — never “edit a number” without a controlled transaction.
8. **Prefer simplicity** — CQRS/events only where they reduce coupling or protect integrity.
9. **Testable by design** — domain rules unit-testable without DB/UI.
10. **Future-ready, not future-built** — `OrganizationId` / `BranchId` hooks; no tenant engine yet.
11. **No vendor lock-in in domain** — SMS/Email/WhatsApp/storage behind ports.
12. **Observability and audit are first-class** — correlation IDs, structured logs, append-only audit.

---

## 3. Architecture Style

### 3.1 Modular Monolith (**Confirmed** + **Recommendation**)

Single solution, single API host, multiple vertical modules. Modules communicate via:

- Application-level interfaces (anti-corruption / query ports)
- In-process **domain events**
- Durable **integration events** / outbox for async side effects
- Shared **BuildingBlocks** for cross-cutting primitives only (not business entities)

### 3.2 Clean Architecture (**Confirmed**)

Each business module follows:

```
API (Host endpoints) → Application → Domain
                         ↑
                   Infrastructure
```

- **Domain**: entities, aggregates, value objects, domain services, domain events, invariants
- **Application**: use cases (commands/queries), DTOs, validation orchestration, authorization hooks, transaction boundaries
- **Infrastructure**: EF Core, file storage, SMS/Email providers, job scheduling adapters
- **Host/API**: composition root, auth middleware, controllers/minimal APIs, Swagger

### 3.3 Domain-driven boundaries (**Recommendation**)

Use DDD tactically where rules are rich:

- Appointments, Queue, Visits, Prescriptions, Invoices, Inventory stock movements, Purchase Orders

Use simpler CRUD-oriented application services where master data is thin:

- Specializations, Asset categories, some lookup catalogs

Do **not** force heavy aggregate patterns on every table.

---

## 4. High-Level System Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                     Angular Staff SPA                           │
│  Core / Shared / Features / Guards / Interceptors / i18n        │
└──────────────────────────────┬──────────────────────────────────┘
                               │ HTTPS + JWT Bearer
                               ▼
┌─────────────────────────────────────────────────────────────────┐
│                 ASP.NET Core Host (Modular Monolith)            │
│  Middleware: AuthN → AuthZ → Correlation → Exception → Audit    │
│  ┌──────────┐ ┌──────────┐ ┌──────────┐ ┌──────────┐           │
│  │ Patients │ │Appointmts│ │  Visits  │ │ Finance  │  ...      │
│  │ Doctors  │ │ Schedule │ │   Queue  │ │Inventory │           │
│  │  Admin   │ │ Services │ │   Rx     │ │  Notif.  │           │
│  └────┬─────┘ └────┬─────┘ └────┬─────┘ └────┬─────┘           │
│       └────────────┴────────────┴────────────┘                 │
│              Domain Events (in-process)                         │
│              Integration Events + Outbox                        │
│              Background Jobs (reminders, retries, alerts)       │
└───────────────┬─────────────────────────────┬───────────────────┘
                │                             │
                ▼                             ▼
         SQL Server                    External Providers
         (single DB)                   SMS / Email / WhatsApp
         + file store                  Object storage (prod)
```

**Actors:** staff users only in V1 (patients are business entities, not login principals — per PRD).

---

## 5. Solution Structure

### 5.1 Evaluation of options

| Option | Pros | Cons | Verdict |
|--------|------|------|---------|
| A. One giant Domain/Application/Infrastructure | Simple start | Destroys module boundaries; circular coupling inevitable | **Reject** |
| B. Microservice per module | Independent deploy | Overengineered; ops cost; distributed transactions | **Reject for V1** |
| C. Vertical modules each with Domain/Application/Infrastructure + thin Host | Clear ownership; Clean Architecture; extractable later | More projects | **Select** |
| D. Modules as folders inside fewer projects | Fewer csproj | Weak compile-time boundaries | Acceptable only if team enforces analyzers; **prefer C** |

### 5.2 Chosen structure (**Recommendation**)

**Vertical slice modules** under `src/Modules/*`, each with its own Domain / Application / Infrastructure projects (or clearly separated folders with project references that enforce direction). Shared kernels live in `BuildingBlocks`. One `Host` composes everything.

**Why:** Matches PRD module ownership, prevents “shared mutable domain,” keeps financial/clinical boundaries enforceable at compile time, and avoids microservice complexity.

---

## 6. Project Structure

Conceptual layout (names may be refined at solution creation time; **no projects created in this step**):

```
ErpClink/
  docs/
    product.md
    architecture.md
  src/
    BuildingBlocks/
      ErpClink.BuildingBlocks.Domain/           # Entity base, DomainEvent, Result, Money, Guard
      ErpClink.BuildingBlocks.Application/      # Mediator abstractions, ICurrentUser, paging
      ErpClink.BuildingBlocks.Infrastructure/   # EF interceptors helpers, outbox base, clock
      ErpClink.BuildingBlocks.AspNetCore/       # ProblemDetails, auth attributes helpers

    Modules/
      Administration/
        ErpClink.Modules.Administration.Domain/
        ErpClink.Modules.Administration.Application/
        ErpClink.Modules.Administration.Infrastructure/
      Patients/
        ErpClink.Modules.Patients.Domain/
        ErpClink.Modules.Patients.Application/
        ErpClink.Modules.Patients.Infrastructure/
      Doctors/                  # Doctor & Clinic Management
      Scheduling/
      Appointments/
      Queue/
      MedicalVisits/
      Prescriptions/
      Services/                 # Services & Packages
      Finance/
      Procurement/
      Inventory/
      Assets/
      Reports/
      Notifications/            # Integrations / Notifications

    Host/
      ErpClink.Api/             # Composition root, endpoints, DI, jobs host

  tests/
    ErpClink.UnitTests/
    ErpClink.IntegrationTests/
    ErpClink.Api.Tests/
    # Frontend tests live under Angular project later

  frontend/                     # created in later phase
    erpclink-web/               # Angular workspace
```

### 6.1 Project reference rules

- `*.Domain` references **only** `BuildingBlocks.Domain` (and nothing module-foreign).
- `*.Application` references its own `Domain` + `BuildingBlocks.Application`.
- `*.Infrastructure` references its own `Application` + `BuildingBlocks.Infrastructure`.
- `Host` references all module Infrastructure (composition) + AspNetCore building blocks.
- **No** module Domain/Application references another module’s Domain/Infrastructure.
- Cross-module needs go through **published contracts** in Application (interfaces + DTOs) or events.

### 6.2 Optional consolidation

If project count becomes painful, pair thin modules (e.g., Assets) as folders inside a coarser assembly **only if** ownership and namespaces remain strict. Prefer separate projects for Finance, Inventory, Appointments, Visits, Patients, Administration.

---

## 7. Module Structure

Inside each module (logical):

```
Module.X/
  Domain/
    Aggregates/
    Entities/
    ValueObjects/
    Enums/Statuses/
    DomainEvents/
    DomainServices/
    Exceptions/
  Application/
    Commands/
    Queries/
    Contracts/          # public interfaces other modules may depend on (DTOs only)
    EventHandlers/      # domain / integration handlers owned by this module
    Authorization/
    Validation/
    Abstractions/       # IXyzRepository ports
  Infrastructure/
    Persistence/        # DbContext fragment / configurations
    Repositories/
    External/
    Jobs/
```

Public surface of a module to others:

- `Application/Contracts` (read ports, command ports for orchestration — sparingly)
- Domain/integration events (payloads must not leak internal entities)

---

## 8. Dependency Rules

1. **Inward only** within a module: Host → Infra → Application → Domain.
2. **No circular module dependencies.**
3. **No direct modification** of another module’s entities/tables via foreign DbContext “convenience.”
4. Modules may **reference IDs** of other modules’ aggregates (e.g., `PatientId`) as value identifiers.
5. For data owned elsewhere, use:
   - Query contract (`IPatientDirectory.GetSummary(patientId)`)
   - Domain event reaction
   - Integration event / outbox
6. **Reports** may read via dedicated read DbContext / SQL views / projections — never write transactional sources of truth.
7. **Notifications** never decide clinical/financial outcomes; they react to events.
8. Shared code in BuildingBlocks must be **technical**, not clinic business rules.

---

## 9. Module Dependency Matrix

Legend: **R** = may read via contract/query; **E** = may react to events / publish events; **C** = may invoke application contract (command/query port); **—** = no dependency; **X** = forbidden direct write to foreign aggregates.

| From \ To | Admin | Patients | Doctors | Sched | Appt | Queue | Visits | Rx | Services | Finance | Proc | Inv | Assets | Reports | Notif |
|-----------|-------|----------|---------|-------|------|-------|--------|----|----------|---------|------|-----|--------|---------|-------|
| Admin | ● | — | — | — | — | — | — | — | — | — | — | — | — | — | E |
| Patients | R | ● | — | — | E | E | E | E | — | E | — | — | — | E | E |
| Doctors | R | — | ● | E | E | — | E | — | R | — | — | — | — | E | — |
| Scheduling | R | — | R | ● | R/E | — | — | — | — | — | — | — | — | E | — |
| Appointments | R | R/C | R | R/C | ● | E | E | — | R | E | — | — | — | E | E |
| Queue | R | R | R | — | R/E | ● | E | — | — | — | — | — | — | E | E |
| MedicalVisits | R | R | R | — | R/E | R/E | ● | E | R | E | — | — | — | E | E |
| Prescriptions | R | R | R | — | — | — | R | ● | — | — | — | R/E* | — | E | E |
| Services | R | — | R | — | — | — | R | — | ● | R | — | — | — | E | — |
| Finance | R | R | R | — | R | — | R | — | R | ● | — | — | — | E | E |
| Procurement | R | — | — | — | — | — | — | — | — | — | ● | E/C | — | E | E |
| Inventory | R | — | — | — | — | — | — | R* | — | — | R | ● | — | E | E |
| Assets | R | — | — | — | — | — | — | — | — | — | — | — | ● | E | — |
| Reports | R | R | R | R | R | R | R | R | R | R | R | R | R | ● | — |
| Notifications | R | R | — | — | R | R | — | — | — | R | — | R | — | — | ● |

\* Prescriptions ↔ Inventory dispense coupling is **optional** and remains a **stakeholder decision** (PRD open question #7/#8). Architecture keeps an interface + event seam so documentary Rx works without stock deduction.

**Administration** is depended on by all modules for identity/authz (via BuildingBlocks `ICurrentUser` / permission checks), not by referencing Admin domain entities freely.

---

## 10. Domain Layer Strategy

### Goals

- Capture invariants and state transitions close to the data they protect
- Prefer **rich aggregates** for workflow entities
- Keep domains free of EF, HTTP, SMS SDKs

### Patterns

| Pattern | Use |
|---------|-----|
| Aggregate root | Appointment, QueueEntry, Visit, Prescription, Invoice, PurchaseOrder, Stock ledger operations via InventoryItem/Warehouse balances |
| Entity | InvoiceItem, PrescriptionItem, PatientAllergy |
| Value object | Money, DateRange, PhoneNumber, Dosage, PermissionCode |
| Domain event | `AppointmentCheckedIn`, `InvoiceIssued`, `StockAdjusted` |
| Domain service | Slot conflict evaluation spanning schedule rules (when not natural on one aggregate) |
| Enumeration / smart status | Status enums + transition methods (`Confirm()`, `CheckIn()`) — **not** public setters |

### Explicit transitions

Aggregates expose intent methods:

- `appointment.Confirm(...)`
- `appointment.CheckIn(...)`
- `invoice.Issue(...)`
- `invoice.CapturePayment(...)`
- `stock.Receive(...)` / `stock.Adjust(...)`

Application layer calls these methods; API never sets `Status = "Paid"`.

### Anemic model avoidance

Master data may be thinner; money, stock, appointments, visits, and invoices must **not** be anemic.

---

## 11. Application Layer Strategy

### Responsibilities

- Use-case orchestration (one command/query handler ≈ one use case)
- Authorization attributes/policies check before domain mutation
- FluentValidation (or equivalent) for input validation
- Transaction boundary (`IUnitOfWork` / DbContext SaveChanges)
- Mapping to/from contracts/DTOs
- Publishing domain events after successful persistence (or via interceptor/outbox)

### Style (**Recommendation**)

Use **mediator-style** handlers (e.g., MediatR or equivalent custom) **inside modules**, not a single global god-mediator dumping all features together.

Handlers:

- `CreateAppointmentCommand` / `CreateAppointmentHandler`
- `GetPatientTimelineQuery` / handler

No business rules in controllers.

### Cross-module application contracts

Example:

- `ISchedulingAvailabilityService.EnsureSlotAvailable(...)`
- `IPatientReadService.GetPatientHeader(patientId)`
- `IFinanceBillingPort.CreateDraftInvoiceFromVisit(...)` (owned by Finance; Visits raises event or calls port carefully)

Prefer **events** for “something happened”; prefer **query ports** for “I need a fact to validate.”

---

## 12. Infrastructure Layer Strategy

Per-module infrastructure implements ports:

- EF Core mappings and repositories
- File storage adapters
- Notification provider adapters (owned by Notifications module)
- Background job registrations related to the module
- Outbox publisher implementation

### Composition

`ErpClink.Api` registers all modules’ `AddXModule()` extension methods.

### Forbidden in Infrastructure as business home

Infrastructure must not invent clinic rules (e.g., “auto-complete invoice if paid”) — that belongs in domain/application.

---

## 13. API Layer Strategy

### Host responsibilities

- Authentication middleware
- Authorization
- Correlation ID
- Exception → ProblemDetails
- OpenAPI/Swagger
- Health checks
- Module endpoint registration
- Hangfire/dashboard security (if used)

### Controllers / endpoints

- Thin: validate auth → send command/query → return response
- Grouped by module/feature routes: `/api/patients`, `/api/appointments`, ...
- No EF `DbContext` injection into controllers
- No direct status field updates

### Versioning

See §54.

---

## 14. Angular Frontend Architecture

**Confirmed:** Angular + TypeScript staff SPA. **Not implemented in this phase.**

### Recommended structure

```
frontend/erpclink-web/src/app/
  core/                 # singleton services: auth, api, logging, config
  shared/               # UI kit, pipes, directives, reusable forms
  layout/               # shell, sidebar, topbar, breadcrumbs
  features/
    admin/
    patients/
    doctors/
    scheduling/
    appointments/
    queue/
    visits/
    prescriptions/
    services/
    finance/
    procurement/
    inventory/
    assets/
    reports/
    notifications/
  auth/                 # login, token refresh UX
```

### Frontend cross-cutting

| Concern | Approach |
|---------|----------|
| Auth | Store access token securely (memory + refresh strategy TBD); attach via interceptor |
| AuthZ | Route guards + permission directives; mirror API permissions; never trust UI alone |
| API services | Feature services calling typed HTTP clients |
| Interceptors | Auth, correlation ID, error normalization, loading |
| Errors | Central error handler → user-friendly toasts; preserve ProblemDetails |
| i18n | Angular i18n or ngx-translate; AR/EN ready (**stakeholder**: mandatory bilingual?) |
| UI | Professional ERP layout: dense data grids, forms, dashboards; responsive |
| State | Component store / signal-based state per feature; avoid unnecessary global store |
| Dashboard | Widgets call Reports/operational endpoints; permission-filtered |

### UX architecture notes

- Reception-critical flows (search patient → book → check-in → pay) optimized for speed
- Allergy banners on visit/Rx screens via patient header API
- RTL support prepared if Arabic is confirmed

---

## 15. Database Architecture

### V1 strategy (**Recommendation**)

- **One SQL Server database**
- **One primary write model**
- Logical separation by **schema per module** (preferred) or consistent table prefixes
- EF Core: prefer **one DbContext per module** (bounded) with shared connection/transaction coordination when a use case must span modules
- Cross-module **FK constraints optional**; prefer application-level integrity for foreign module IDs to avoid tight physical coupling — **or** limited FKs for critical refs (`PatientId`) where operationally valuable (**decision during detailed design**)

### Schemas (example)

`admin`, `patients`, `doctors`, `scheduling`, `appointments`, `queue`, `visits`, `rx`, `services`, `finance`, `procurement`, `inventory`, `assets`, `reports`, `notif`

### Cross-cutting columns (readiness)

| Column | V1 usage |
|--------|----------|
| `OrganizationId` | Single org value on relevant tables |
| `BranchId` | Default branch; filter-ready |
| `RowVersion` / `xmin`-style | Optimistic concurrency where needed |
| `IsDeleted` / `IsActive` | Soft delete / deactivation per entity policy |
| `CreatedAt`, `CreatedBy`, `ModifiedAt`, `ModifiedBy` | Standard audit fields (in addition to AuditLog) |

### Indexing priorities

- Patients: MRN (unique per org), phone, name search support
- Appointments: doctor + date, patient + date, status
- Queue: doctor/branch + date + status
- Finance: invoice number, patient, status, payment date
- Inventory: item+warehouse, batch expiry
- Audit: timestamp, actor, entity

### Soft delete / deactivation

- Prefer **deactivate** for users, services, doctors
- Prefer **soft delete** flags for correctable master data
- Financial and clinical documents: **void/cancel/amend** workflows — not hard delete

### Multi-tenant DB

**Not** in V1: no shard-per-tenant, no tenant discriminator enforcement engine. See ADR-006.

---

## 16. Entity Ownership Rules

| Module | Owns (write) | Others may |
|--------|--------------|------------|
| Administration | User, Role, Permission, Department, SystemSetting, AuditLog (append via service) | Read permissions via authz infrastructure |
| Patients | Patient, contacts, allergies, attachment metadata, timeline projection | Reference `PatientId` |
| Doctors | Doctor/Nurse profiles, specializations, clinic structure, working hours | Reference doctor/clinic IDs |
| Scheduling | Templates, holidays, exceptions, blocks | Query availability |
| Appointments | Appointment + status/reschedule history | Publish events |
| Queue | QueueEntry + history | Publish events |
| MedicalVisits | Visit, vitals, diagnosis, notes, follow-up | Publish events; reference patient/doctor/appointment |
| Prescriptions | Prescription, items, medicine catalog (or link) | Optional inventory events |
| Services | Services, packages, prices, commission **rules** | Finance reads rules to create commission **entries** |
| Finance | Invoices, payments, refunds, expenses, commission **entries**, balances | — |
| Procurement | Suppliers, POs, goods receipts, supplier payments | Inventory reacts to receiving |
| Inventory | Warehouses, items, batches, stock transactions, balances | — |
| Assets | Asset registry, locations, maintenance status | — |
| Reports | Report read models / snapshots (optional) | Read-only from sources |
| Notifications | Templates, messages, delivery logs, provider config | — |

**Rule:** Module A must not `UPDATE`/`DELETE` Module B’s tables through a shared context “shortcut.”

---

## 17. Aggregate Boundaries

| Aggregate root | Consistency boundary includes | Notes |
|----------------|-------------------------------|-------|
| Patient | Contacts, allergies, attachment metadata | Timeline is projection |
| DoctorProfile | Working hours refs, specializations | User link by id |
| Schedule / availability | Holidays, exceptions as related entities or separate aggregates coordinated by app service | Keep conflict checks transactional with booking |
| Appointment | Status history, reschedule links | References patient/doctor/slot |
| QueueEntry | Status history | One entry per check-in instance |
| Visit | Vitals, diagnoses, notes, follow-up | Closed ⇒ immutable except amend |
| Prescription | Items | Issued/void transitions |
| MedicalService / Package | Package items, prices | Catalog |
| Invoice | Lines, discounts; payments/refunds as related aggregates or child entities with strict rules | Prefer Payment/Refund as related aggregates referencing Invoice |
| PurchaseOrder | PO lines; GoodsReceipt as related aggregate | Receiving posts inventory via events/commands |
| Warehouse stock | StockBalance + StockTransaction + Batch | Ledger-centric |
| Asset | Maintenance status fields | Simple |
| NotificationMessage | Delivery attempts | |

**Payment and Refund** should be modeled so invoice balances recalculate deterministically from recorded movements — not by free-editing a balance field.

---

## 18. Repository Strategy

### Where repositories are appropriate

- Loading aggregates for write use cases
- Enforcing aggregate-oriented queries (`GetAppointmentForUpdate`)
- Encapsulating EF includes needed for invariants

### Avoid

- Giant generic `IRepository<T>` used for every entity as a substitute for design
- Repositories that return `IQueryable` to controllers
- One repository per table blindly

### Recommendation

- Specific repositories per aggregate: `IAppointmentRepository`, `IInvoiceRepository`, `IStockRepository`
- Queries for screens/reports use **read services** / Dapper / EF projections — not forced through write repositories
- `SaveChanges` via unit of work at application boundary

---

## 19. CQRS Strategy

### Decision (**Recommendation**)

Adopt **lightweight CQRS**: separate command and query paths in the application layer **without** mandating separate databases in V1.

| Use commands | Use queries |
|--------------|-------------|
| State transitions | Lists, search, dashboards |
| Financial posts | Patient timeline |
| Stock movements | Availability slots |
| PO receive | Reports |

### Do not introduce in V1 unless needed

- Event sourcing as system of record
- Separate report database from day one
- Full bus topology

Evolve reporting to dedicated read store when SQL contention appears (SCL-05).

---

## 20. Domain Events

### Purpose

In-process notification that **something happened in a module’s domain**, for:

- Updating projections (timeline)
- Triggering sibling module reactions within the same process
- Queuing integration events

### Examples

- `PatientRegistered`
- `AppointmentCheckedIn`
- `QueueEntryCompleted`
- `VisitClosed`
- `PrescriptionIssued`
- `InvoiceIssued` / `PaymentCaptured`
- `GoodsReceived`
- `StockAdjusted`
- `StockBelowMinimum`

### Rules

- Raised by aggregates
- Handled after successful transaction commit (or via outbox to avoid lost messages)
- Handlers must be idempotent where retries exist
- Payloads carry IDs and minimal facts — not deep entity graphs

---

## 21. Integration Events

### Purpose

Cross-boundary **async** communication and external side effects:

- Send SMS/Email/WhatsApp
- Retryable notifications
- Future: export to accounting, other systems

### Pattern (**Recommendation**)

**Transactional outbox** in SQL Server:

1. Business transaction writes outbox row in same DB transaction
2. Background dispatcher publishes to in-process bus / queue
3. Consumers (Notifications, alerts) process with retries

V1 may use an in-process dispatcher + Hangfire without Azure Service Bus. Swap transport later without changing domain.

### Domain vs integration events

| Domain event | Integration event |
|--------------|-------------------|
| Internal, rich enough for modules | Stable contract for async/external |
| Same bounded solution | Versioned carefully |

**STEP 18 foundation:** BuildingBlocks `IIntegrationEvent`; Finance `AccountingTransactionRequested` + `IAccountingPostingPort` with durable org-scoped idempotency. Outbox/Hangfire delivery remains STEP 24. Automatic journal mapping from Billing/Inventory/Procurement/Assets is intentionally **not** implemented (ADR-013).

---

## 22. Background Jobs

### Use cases (**Confirmed** need)

- Appointment reminders (T-24h / T-2h — policy configurable)
- Notification delivery retries
- Low-stock alerts
- Expiry alerts
- Outbox dispatch
- Optional: mark no-shows after window (**business policy open**)

### Recommendation

Host-integrated job library (e.g., **Hangfire** with SQL storage, or **Quartz** + custom). Prefer Hangfire for operational dashboard and easy retries in V1.

Jobs call application commands — not raw SQL business updates.

---

## 23. Authentication Architecture

### PRD status

Auth mechanism is an **open technical decision** in `docs/product.md` (#16). Architecture must choose an explicit V1 recommendation.

### Options evaluated

| Option | Pros | Cons |
|--------|------|------|
| ASP.NET Core Identity + JWT Bearer | Native to stack; fast to ship; full control; fits single org staff login | Must implement refresh, lockout, password policy well |
| External IdP (Entra ID / Auth0 / Keycloak) | Enterprise SSO, MFA, federation | Extra cost/complexity; overkill for single-clinic V1; still need app roles mapping |

### Decision (**Recommendation**) — ADR-002

**V1: ASP.NET Core Identity (in Administration module) + JWT access tokens + refresh tokens.**

Reasons:

- Matches modular monolith and single-organization V1
- Keeps user/role/permission data in-system (PRD Administration ownership)
- Avoids external IdP dependency for first production clinics
- Can add external IdP later via federation without rewriting authorization model

MFA for privileged roles: design claim/flag support; enforcement can follow (**Should** in PRD).

Patients are **not** Identity users in V1.

---

## 24. Authorization Architecture

### Principles (**Confirmed**)

- API authoritative
- User → Roles → Permissions
- UI guards are convenience only

### Enforcement points

1. Endpoint metadata / policies (`[RequirePermission("Appointments.CheckIn")]`)
2. Application handler guards for defense in depth
3. Resource-based checks where needed (e.g., doctor can update **own** open visits) — **policy details are stakeholder decisions**

### Failure behavior

- Unauthenticated → 401
- Unauthorized → 403 + security audit entry

---

## 25. Role & Permission Architecture

### Model

- `Permission` codes as stable strings: `Patients.Read`, `Invoices.Issue`, ...
- `Role` aggregates permissions
- `User` has many roles
- Effective permissions = union of role permissions
- Optional future: branch-scoped role assignments

### Seed permissions (non-exhaustive; align with PRD)

```
Patients.Read, Patients.Create, Patients.Update, Attachments.Manage
Appointments.Create, Appointments.Confirm, Appointments.Reschedule,
Appointments.Cancel, Appointments.CheckIn, Appointments.MarkNoShow
Queue.View, Queue.Call, Queue.ManagePriority
Visits.Create, Visits.Update, Visits.Close
Prescriptions.Issue
Services.Manage, Packages.Manage, CommissionRules.Manage
Invoices.Create, Invoices.Issue, Payments.Capture, Refunds.Approve, Expenses.Manage
Stock.Receive, Stock.Adjust
PO.Create, PO.Approve
Reports.Operational, Reports.Financial, Reports.Clinical
Users.Manage, Roles.Manage, Settings.Manage, Audit.Read
```

### Data scope (**Assumption** until decided)

- V1 default: permission is global within the single organization/branch
- Extension point: `IDataScopeEvaluator` for doctor-owned records later

---

## 26. Audit Architecture

### Requirements (**Confirmed**)

Audit auth events, authz failures, patient/appointment/visit/Rx changes, invoice/payment/refund/discount, stock adjustments, PO approvals, role/permission changes.

### Design (**Recommendation**)

- `AuditLog` owned by Administration (append API used by all modules)
- Writers invoked from application layer and/or EF interceptor for entity change snapshots
- Fields: actor user id, UTC timestamp, action, entity type, entity id, correlation id, optional before/after JSON, IP/user agent (careful with PII)
- **Append-only** in application (no update/delete endpoints)
- Retention via ops jobs / DBA policy (AUD-08)

Security audit vs business audit may share table with `Category` discriminator or separate tables — prefer one store with category for V1 simplicity.

---

## 27. Error Handling

### Strategy

- Domain throws domain exceptions / returns `Result` for expected rule failures
- Application maps to typed errors
- API middleware maps to **RFC 7807 ProblemDetails**
- Unexpected exceptions → 500 with correlation id (no stack traces to clients in production)

### Client contract

Stable `type` / `code` fields for UI (e.g., `appointment.slot_conflict`).

---

## 28. Validation Strategy

| Layer | Validates |
|-------|-----------|
| Angular forms | UX field validation |
| API model binding | Presence/types |
| Application FluentValidation | Command/query input rules |
| Domain | Invariants & state transitions |

Never rely on UI validation alone. Duplicate critical checks in domain (e.g., cannot pay more than balance).

---

## 29. API Response Strategy

### Recommendation

- Success: resource DTO or command result (`{ id, status }`)
- Lists: enveloped page result (see §30)
- Errors: ProblemDetails
- Avoid multiple ad-hoc wrapper formats

Optional standard:

```json
{
  "data": { },
  "meta": { "correlationId": "..." }
}
```

Pick one envelope style and apply consistently in Host.

---

## 30. Pagination / Filtering / Sorting

### Recommendation

- Offset paging for admin grids (`page`, `pageSize` with max cap)
- Keyset/cursor paging later for huge logs if needed
- Explicit filter DTOs per query (no free-form SQL)
- Sorting whitelist per endpoint (prevent injection / expensive sorts)
- Default page size clinic-appropriate (e.g., 20–50)

---

## 31. Logging

- Structured logging (Serilog recommended) to console + sink (file/Seq/App Insights)
- Correlation ID on every request and job
- **Minimize PHI/PII** in logs (SEC-11)
- Log auth failures, integration failures, unhandled exceptions
- Business “audit” ≠ debug logging — use AuditLog for compliance actions

---

## 32. Observability

| Signal | V1 approach |
|--------|-------------|
| Logs | Structured + correlation |
| Metrics | Basic: request duration, job failures, notification failures |
| Traces | Optional OpenTelemetry hooks; enable when ops needs |
| Health | `/health` DB + optional dependency checks |
| Hangfire dashboard | Restricted to SystemAdmin |

---

## 33. Configuration Management

- `appsettings.json` + environment-specific files
- Environment variables in deployment
- Feature flags / clinic settings for business policies stored in Administration `SystemSetting` where runtime-editable
- Strongly typed options pattern (`SmsOptions`, `JwtOptions`, ...)

Business policies that are **stakeholder-open** (auto-confirm, deposit required, commission basis) should be **settings-driven** where practical so code can ship with defaults without pretending the decision is final.

---

## 34. Secrets Management

- **Never** commit secrets
- Development: User Secrets
- Production: Azure Key Vault / environment secret store / Docker secrets
- Connection strings, JWT signing keys, SMS/WhatsApp API keys, storage keys

---

## 35. File / Attachment Storage Strategy

### PRD

Attachment storage tech is **open** (#15).

### Recommendation — ADR-010

- Introduce `IFileStorage` port in BuildingBlocks/Patients
- **Dev:** local filesystem
- **Prod:** object storage (Azure Blob or S3-compatible)
- DB stores **metadata only** (name, content type, size, hash, storage key, patient id, access classification)
- Validate content types/sizes; scan policy later
- Authorized download via API (no public buckets for PHI)

This does not force a cloud vendor in V1 code paths.

---

## 36. Notification Architecture

Owned by **Notifications** module.

Flow:

1. Domain/integration event occurs (e.g., `AppointmentConfirmed`)
2. Notifications handler resolves template + channel preferences + patient contact
3. Creates `NotificationMessage`
4. Dispatches via channel adapters asynchronously
5. Writes `NotificationDeliveryLog`
6. Retries via background jobs

Rules:

- No clinical detail in SMS by default
- Templates per channel
- Clinic policy / opt-out support

---

## 37. SMS Architecture

- Port: `ISmsSender`
- Adapter(s) behind configuration
- Failures logged; retry with backoff
- Rate limiting considerations at adapter

---

## 38. Email Architecture

- Port: `IEmailSender`
- SMTP or provider adapter
- HTML templates sanitized
- Same outbox/retry model

---

## 39. WhatsApp Architecture

- Port: `IWhatsAppSender`
- May be **disabled** until credentials exist (PRD assumption)
- Same message/orchestration pipeline as SMS/Email
- Template rules must respect provider constraints

---

## 40. Financial Transaction Integrity

### Confirmed needs

Decimal money, deterministic totals, controlled refunds, no silent mutation of posted invoices, transactional operations, audit.

### Design

| Concern | Approach |
|---------|----------|
| Money type | `decimal` + currency code value object; org single currency in V1 |
| Totals | Calculated from lines in domain; persist snapshot at issue time |
| Issue | `Draft` → `Issued` locks lines |
| Changes after issue | Credit note / adjustment documents — not edit-in-place |
| Payments | Append payment records; recompute balance |
| Refunds | State machine + permission `Refunds.Approve` |
| Concurrency | RowVersion on Invoice |
| Transactions | Single DB transaction for invoice+payment write |
| Commission | Entries generated by Finance based on Services’ rules; recognition basis is **settings-configurable** pending stakeholder decision |

Tax inclusive/exclusive remains **settings-flagged** (open question).

---

## 41. Inventory Transaction Integrity

### Confirmed needs

Ledger movements; batches/expiry; min stock; warehouses; concurrency; no negative stock unless allowed.

### Design — ADR-008

- **StockTransaction** is the source of truth for movements
- **StockBalance** is a maintained projection consistent with transactions (updated in same DB transaction)
- Never adjust quantity without inserting a ledger row
- Receiving from Procurement publishes/commands inventory receipt with batch/expiry
- Adjustments require reason + permission
- Optimistic concurrency on balance rows
- Transfer transaction types reserved for multi-branch later

---

## 42. Concurrency Strategy

| Area | Strategy |
|------|----------|
| Appointments slot booking | Transaction + unique constraint / conflict check to prevent double book |
| Invoice payments | RowVersion on invoice |
| Stock balances | RowVersion; retry on conflict |
| Queue call | Conditional update on status |
| Settings | Last-write with audit |

Use pessimistic locking only for rare high-contention cases if proven necessary.

---

## 43. Database Transaction Strategy

- Default: one transaction per application command
- Cross-module: use ambient transaction / `TransactionScope` / explicit coordinator **only when strong consistency required** (e.g., PO receive + stock in)
- Prefer eventual consistency via outbox when side effect is notification/reporting
- Financial + stock commands must be ACID for their consistency boundary

---

## 44. Caching Strategy

| Data | Cache? |
|------|--------|
| Permissions for user | Short-lived memory/distributed cache after login |
| System settings | Memory cache with invalidation on change |
| Service price catalog | Optional short cache |
| Stock balances | **No** long cache for writes; read-through OK with care |
| Reports | Optional snapshot cache |

Avoid stale clinical/financial write paths.

---

## 45. Performance Strategy

- Index hot paths (reception search, today’s appointments/queue)
- Projection queries for lists (no heavy graphs)
- Background work off HTTP thread
- Pagination everywhere for collections
- Async IO throughout
- Avoid N+1 via explicit EF includes/projections
- Horizontal scale API; DB vertical scale first

---

## 46. Reporting Architecture

### Confirmed

Reports must not tightly couple to write models.

### V1 recommendation — ADR-011 / ADR-017 (STEP 22)

**Phase A (implemented):** Thin **Reports** module owns read-only report DTOs + `IReportingQueryService`. Implementations **compose owning modules’ Application query ports** (no Infrastructure→Infrastructure). Live SQL via existing services: pagination, filters, projections, `AsNoTracking`. No reporting tables; no fabricated KPIs; unavailable metrics returned explicitly.

**Phase B (later):** optional materialized read models / replica; CSV/Excel export on the same query contracts.

Permission split: granular `Reports.{Domain}.{Report}.View` (not blanket Operational/Financial/Clinical until those product packs exist).

See `docs/adr/ADR-017-financial-management-reporting.md`.

---

## 47. Background Processing Architecture

```
HTTP Request → Command → DB + Outbox
                         ↓
              Background Dispatcher
                         ↓
         Notification / Reminder / Alert Handlers
                         ↓
              Provider adapters + Delivery logs
```

Job types:

- Recurring (reminders scan, expiry scan)
- Delayed (retry)
- Fire-and-forget after outbox

Idempotency keys on notification send attempts.

---

## 48. Multi-Branch Readiness

### Confirmed direction

V1 effectively one branch; model hooks for more.

### Architecture

- `Branch` entity under Doctors/Admin org structure
- `BranchId` on appointments, queues, warehouses, invoices, stock, assets as applicable
- V1 seed one default branch; UI may hide switcher (**stakeholder**: ship switcher in V1?)
- No inter-branch transfer UI required in V1; transaction types reserved

Patient master: default **org-shared** patients with branch of registration recorded — **assumption** until PRD open question resolved.

---

## 49. Multi-Tenant Readiness

### Confirmed

Not building SaaS isolation in V1.

### Architecture — ADR-006

- Introduce `OrganizationId` on tenant-sensitive tables
- V1 single organization row
- **Do not** build tenant resolver middleware, shared-db RLS engines, or per-tenant migrations yet
- Keep authentication local so later IdP-per-tenant is possible
- Coding guideline: never query without org context helper (even if always one org)

---

## 50. Localization / Arabic / English Architecture

### Status

Bilingual mandatory? **Open** (#18). Architecture must be ready.

### Recommendation

- Angular i18n resources for `en` and `ar`
- RTL layout support in shell CSS
- API validation messages via resource codes, not hard-coded English only
- Store user language preference
- Medical content language: free text as entered (no auto-translate)

---

## 51. Time Zone / Currency Strategy

| Topic | V1 approach |
|-------|-------------|
| Time | Persist UTC; org timezone in settings for display/scheduling interpretation |
| Scheduling | Business rules evaluate in clinic local time |
| Currency | Single currency code per Organization; `Money(amount, currency)` |
| Culture | Number/date formats from locale settings |

---

## 52. Security Architecture

- HTTPS only in production
- JWT signed with strong key; short access token lifetime; refresh rotation
- Password hashing via Identity defaults (modern)
- Disabled users cannot refresh
- Least-privilege role seeds
- Attachment authZ
- CORS locked to Angular origin(s)
- Security headers on Host
- Secrets out of source control
- PHI minimization in notifications/logs
- Segregation of duties via permissions (cashier ≠ clinical editor)

---

## 53. OWASP Considerations

Align with OWASP ASVS-inspired practices:

| Risk | Control |
|------|---------|
| Broken auth | Identity + JWT best practices, lockout |
| Broken authz | Permission checks on every mutating endpoint; deny by default |
| Injection | Parameterized EF/SQL; whitelist sorts |
| XSS | Angular escaping; CSP; sanitize HTML email |
| CSRF | Less relevant for pure Bearer API; if cookies used, antiforgery |
| Mass assignment | Command DTOs; no entity bind |
| Sensitive data exposure | HTTPS, limited logs, secure storage |
| Excessive data | Permission-filtered queries; no over-fetch to clients |
| SSRF | Careful webhook/provider URLs |
| File upload | Type/size validation; store outside web root |

---

## 54. API Versioning Strategy

### Recommendation

- URL versioning: `/api/v1/...`
- V1 only initially
- Deprecation policy later via headers
- Integration event contracts versioned independently when published externally

---

## 55. Testing Architecture

Testing pyramid:

1. **Unit** — domain transitions, money/stock calculations, permission evaluators
2. **Application** — handlers with fakes
3. **Integration** — EF + SQL Server (Testcontainers or dedicated test DB)
4. **API** — WebApplicationFactory + auth tests
5. **Frontend** — unit + component; e2e smoke for reception happy path

No business feature implementation in this phase; architecture mandates test projects exist when coding starts.

---

## 56. Unit Testing

Focus:

- Appointment/Queue/Visit/Invoice/PO state machines
- Invoice totaling, payment/refund rules
- Stock ledger invariants (no negative stock)
- Slot conflict domain service
- Permission aggregation

Fast, no IO.

---

## 57. Integration Testing

Focus:

- Module persistence mappings
- Outbox commit atomicity
- PO receive → stock transaction consistency
- Unique MRN constraints
- Audit append behavior

---

## 58. API Testing

Focus:

- AuthN 401 / AuthZ 403
- Appointment transition endpoints reject illegal status jumps
- Pagination contracts
- ProblemDetails shape
- Permission matrix smoke tests

---

## 59. Frontend Testing

Focus:

- Permission directive/guard behavior
- Critical form validation
- Interceptor error handling
- i18n rendering smoke (when AR/EN enabled)
- Lightweight e2e: login → patient search → create appointment

---

## 60. Deployment Architecture

### V1 topology (**Recommendation**)

```
[Browser Angular static files]
        → CDN / IIS / Nginx
[ASP.NET Core API] × N (stateless behind load balancer)
        → SQL Server (HA as budget allows)
        → Object storage
        → SMS/Email/WhatsApp providers
```

API instances share SQL; Hangfire storage in SQL with distributed locks.

---

## 61. Development Environment

- Windows/Linux Docker optional for SQL Server
- .NET SDK + Node/Angular CLI
- User Secrets for JWT/SMS
- Local file storage
- Seed data: roles, permissions, default org/branch, admin user
- Swagger for API exploration

---

## 62. Production Environment

- HTTPS termination
- Managed SQL backups
- Key vault secrets
- Object storage for attachments
- Log aggregation
- Health checks + alerting on job failures
- Restricted Hangfire dashboard
- Scale API instances horizontally as needed

---

## 63. CI/CD Architecture

Pipeline stages (**Recommendation**):

1. Restore / build backend
2. Unit tests
3. Integration tests (with SQL service)
4. Angular build + frontend unit tests
5. Security scan (optional dependency audit)
6. Publish artifacts
7. Deploy to staging → production (approval gate)

No secrets in pipeline logs. Migrations applied via controlled release step **when implementation begins** (not now).

---

## 64. Backup and Restore Strategy

- SQL Server full + differential + log backups (RPO/RTO set with stakeholder)
- Periodic restore drills
- Object storage versioning/soft delete for attachments
- Documented restore runbook

---

## 65. Disaster Recovery Considerations

| Level | V1 practical approach |
|-------|------------------------|
| DB failure | Restore from backup; failover if using SQL HA |
| Region failure | Not required active-active; cold restore plan |
| Ransomware | Offline/offsite backups |
| App failure | Redeploy stateless API; jobs resume from SQL |

Define RPO/RTO numerically with stakeholder (**open ops decision**).

---

## 66. Architecture Decision Records

The following ADRs are recorded in-line as the initial ADR set. They may later be split into `docs/adr/ADR-XXX.md` without changing decisions.

---

### ADR-001 — Modular Monolith

| Field | Content |
|-------|---------|
| **Context** | 15 modules, strong boundaries, V1 team velocity, future extraction possible |
| **Decision** | Build a modular monolith with vertical modules and Clean Architecture layers |
| **Alternatives** | Microservices; single layered giant project |
| **Reasoning** | Meets PRD; avoids distributed complexity; preserves boundaries |
| **Consequences** | Many projects/folders; discipline required; single deployable simplifies ops |

---

### ADR-002 — Authentication

| Field | Content |
|-------|---------|
| **Context** | PRD open question #16; staff-only login; Administration owns users |
| **Decision** | ASP.NET Core Identity + JWT access tokens + refresh tokens for V1 |
| **Alternatives** | External IdP only; cookie MVC auth; IdentityServer as full OIDC server |
| **Reasoning** | Fast, owned data, sufficient for single-org clinics; IdP can be added later |
| **Consequences** | Must implement token hygiene, lockout, password policy; SSO deferred |

---

### ADR-003 — Authorization

| Field | Content |
|-------|---------|
| **Context** | API-authoritative RBAC with fine-grained permissions |
| **Decision** | User → Roles → Permissions; enforce via policies on endpoints and handlers |
| **Alternatives** | Role-only checks; OPA external engine |
| **Reasoning** | Matches PRD; simple; expressible; testable |
| **Consequences** | Permission catalog must be curated; resource scoping added later as needed |

---

### ADR-004 — Database Strategy

| Field | Content |
|-------|---------|
| **Context** | EF Core + SQL Server; transactional integrity; module ownership |
| **Decision** | Single database; schema-per-module; DbContext per module; org/branch columns |
| **Alternatives** | DB-per-module; shared one DbContext for all |
| **Reasoning** | ACID where needed; clear ownership; simple ops |
| **Consequences** | Cross-module transactions need care; reporting must avoid write contention |

---

### ADR-005 — Multi-Branch Readiness

| Field | Content |
|-------|---------|
| **Context** | Future multi-branch; V1 effectively one branch; UI switcher open |
| **Decision** | Model `Branch` + `BranchId` on operational data; seed one branch; no transfer UI in V1 |
| **Alternatives** | Ignore branch until later; full multi-branch UI now |
| **Reasoning** | Ready without overengineering |
| **Consequences** | Queries should include branch context helpers early |

---

### ADR-006 — Multi-Tenant Readiness

| Field | Content |
|-------|---------|
| **Context** | Future SaaS; not V1 scope |
| **Decision** | Add `OrganizationId`; single org in V1; no tenant runtime isolation engine |
| **Alternatives** | Full multi-tenant now; DB-per-tenant |
| **Reasoning** | Avoid premature SaaS complexity |
| **Consequences** | Later tenant work will add resolver/RLS/sharding as needed |

---

### ADR-007 — Financial Money Handling

| Field | Content |
|-------|---------|
| **Context** | Financial integrity critical; tax/commission basis open |
| **Decision** | `decimal` + currency VO; invoice snapshots on issue; payments/refunds as append-only movements; adjustments via documents; settings for tax/commission basis |
| **Alternatives** | Float; editable invoice totals; balance-only field |
| **Reasoning** | Deterministic, auditable, policy-flexible |
| **Consequences** | Slightly more documents/entities; safer money |

---

### ADR-008 — Inventory Ledger

| Field | Content |
|-------|---------|
| **Context** | PRD requires transactional stock; batches/expiry |
| **Decision** | StockTransaction ledger + consistent StockBalance; concurrency tokens; reason-coded adjustments |
| **Alternatives** | Mutable quantity only; event sourcing stock |
| **Reasoning** | Integrity + practical EF model |
| **Consequences** | Every stock change writes ledger row |

---

### ADR-009 — Background Jobs

| Field | Content |
|-------|---------|
| **Context** | Reminders, retries, alerts must not block HTTP |
| **Decision** | Host-integrated Hangfire (SQL storage) calling application commands; outbox for reliability |
| **Alternatives** | Quartz only; Azure Queue first; inline HTTP sends |
| **Reasoning** | Good V1 ops UX; durable; simple |
| **Consequences** | Secure dashboard; monitor poisoned jobs |

---

### ADR-010 — File Storage

| Field | Content |
|-------|---------|
| **Context** | PRD open #15 |
| **Decision** | `IFileStorage` abstraction; local for dev; object storage for production; metadata in DB |
| **Alternatives** | VARBINARY in SQL; local disk in prod |
| **Reasoning** | Portable; safer for PHI scale |
| **Consequences** | Ops must provision blob store in prod |

---

### ADR-011 — Reporting Strategy

| Field | Content |
|-------|---------|
| **Context** | Avoid coupling reports to write models; SCL-05 |
| **Decision** | V1: Reports module with read queries/views; later: materialized read models |
| **Alternatives** | Same handlers as CRUD; separate OLAP from day one |
| **Reasoning** | Simple start, evolvable |
| **Consequences** | Some reporting load on primary DB initially |

---

### ADR-012 — Domain Events / Integration Events

| Field | Content |
|-------|---------|
| **Context** | Cross-module workflows without shared mutable entities |
| **Decision** | In-process domain events + transactional outbox for integration/async side effects |
| **Alternatives** | Direct cross-DbContext updates; immediate external HTTP in request |
| **Reasoning** | Decoupling + reliability |
| **Consequences** | Handlers must be idempotent; eventual consistency for notifications |

---

### ADR-013 — Accounting Integration Foundation

| Field | Content |
|-------|---------|
| **Context** | Future Billing/Inventory/Procurement/Assets → Finance GL without inventing mappings or coupling Infrastructure |
| **Decision** | Finance-owned Application port + durable org-scoped idempotency; no automatic journals until mappings are approved; Outbox deferred to STEP 24 |
| **Alternatives** | Cross-DbContext posts; module-specific JE construction; in-memory idempotency; full Outbox now |
| **Reasoning** | Stable contracts, concurrency-safe dedupe, clear ownership |
| **Consequences** | Source modules may reference Finance.Application later; zero fake CoA; see `docs/adr/ADR-013-accounting-integration-foundation.md` |

---

### ADR-014 — AR / AP Subledgers

| Field | Content |
|-------|---------|
| **Context** | Need party financial history without taking ownership of Invoice/Supplier or inventing GL mappings |
| **Decision** | Unified immutable Finance subledger (AR/AP); PatientId as AR party; SupplierId as AP party; Billing posts AR via Application port; PO ≠ AP |
| **Alternatives** | Balance on Invoice; Finance Customer/Supplier clones; PO as payable; auto GL |
| **Reasoning** | Ownership + auditability + future reconciliation |
| **Consequences** | See `docs/adr/ADR-014-ar-ap-subledgers.md` |

---

### ADR-016 — Fixed Asset Accounting & Depreciation

| Field | Content |
|-------|---------|
| **Context** | Need asset cost/depreciation foundation; product leaves method unresolved |
| **Decision** | Assets-owned financial profile + provisional straight-line; `IAccountingPostingPort` intents only |
| **Alternatives** | Declining balance; auto GL; defer |
| **Reasoning** | Deterministic auditable NBV without inventing CoA |
| **Consequences** | See `docs/adr/ADR-016-asset-accounting-and-depreciation.md` |

---

# Architectural Assumptions

Working assumptions used by this architecture (not silent PRD confirmations of open questions):

1. Modular monolith hosted as one API process is accepted for production V1.
2. Staff authenticate against in-app Identity; patients do not log in.
3. Single organization and one default branch seeded.
4. Single currency per organization.
5. Manual payment capture (no payment gateway required for V1 architecture).
6. Prescriptions are visit-linked by default; inventory dispense is optional behind a port.
7. Notification providers configured per environment; WhatsApp may be off.
8. UTC storage + org timezone settings.
9. Hangfire + SQL is acceptable job infrastructure.
10. Object storage will be available in production for attachments.

---

# Distinction Summary

## Confirmed requirements (from product)

- Modular, Clean Architecture, workflow-driven system (not CRUD-centric)
- Stack: ASP.NET Core, EF Core, SQL Server, Angular, TypeScript
- Fifteen business modules with ownership boundaries in PRD
- Explicit state transitions for core aggregates
- RBAC permissions, API authoritative
- Financial and inventory integrity
- Auditability; notifications abstractions
- Future multi-branch/multi-tenant **readiness**
- Background jobs for async work
- No microservices mandate

## Architectural recommendations (this document)

- Vertical module projects + BuildingBlocks + Host
- ASP.NET Core Identity + JWT for V1
- Single DB, schema-per-module, DbContext-per-module
- Lightweight CQRS; no event sourcing
- Domain events + outbox integration events
- Hangfire for jobs
- Stock ledger + invoice movement model
- `IFileStorage` with blob in production
- Reports read-query module first
- `/api/v1` versioning
- ProblemDetails errors
- AR/EN readiness in Angular

## Assumptions

See **Architectural Assumptions** above.

## Decisions required from stakeholder

Carry forward unresolved PRD open questions that affect architecture knobs (do not treat as decided):

1. Multi-branch UI in V1 vs hooks only  
2. Appointment confirmation mandatory vs auto-confirm  
3. Deposits/prepayments  
4. No-show fees  
5. Diagnosis coding (free text vs ICD)  
6. Nurse edit rights on visits  
7. Pharmacy dispensing in V1  
8. Medicine vs inventory identity  
9. Commission recognition basis  
10. Tax requirements  
11. Payment methods list  
12. Insurance depth  
13. National ID required?  
14. Duplicate merge in V1?  
15. Attachment storage vendor preference (Blob vs S3) — architecture allows either  
16. Auth: **architecture recommends Identity+JWT**; stakeholder may still mandate external IdP  
17. Offline reception needs  
18. Mandatory AR/EN  
19. E-signature  
20. Queue hardware  
21. Expense approval depth  
22. Shared vs per-doctor queue default  
23. Retention / legal hold  
24. Soft delete policies per entity  
25. Meaning of “Clinic” vs Branch in domain language  
26. RPO/RTO targets for DR  

Where practical, these are **settings- or port-driven** so implementation can proceed behind toggles.

---

# Final Architecture Summary

## 1. Final recommended architecture

**Modular monolith** on ASP.NET Core Web API with **Clean Architecture per vertical module**, Angular staff SPA, **single SQL Server database**, **Identity + JWT**, **permission RBAC**, **domain events + outbox**, **Hangfire jobs**, **ledger-based finance & inventory**, **Reports as read side**, **Notifications as integration module**.

## 2. Final solution structure

```
src/BuildingBlocks/*
src/Modules/{Administration|Patients|Doctors|Scheduling|Appointments|Queue|
             MedicalVisits|Prescriptions|Services|Finance|Procurement|
             Inventory|Assets|Reports|Notifications}/{Domain,Application,Infrastructure}
src/Host/ErpClink.Api
tests/*
frontend/erpclink-web (later)
docs/*
```

## 3. Final module boundaries

As owned in `docs/product.md` §6.2 and reinforced here in §16–17: each module exclusively writes its aggregates; cross-module collaboration via contracts/events only.

## 4. Final dependency rules

- Inward Clean Architecture dependencies
- No circular module references
- No foreign aggregate mutation
- BuildingBlocks contain technical shared kernels only
- Host is composition root

## 5. Architectural decisions

ADR-001 … ADR-016 as documented (… provisional inventory FIFO; provisional asset straight-line depreciation without auto GL).

## 6. Remaining stakeholder decisions

Listed above (PRD open questions + RPO/RTO + optional IdP override).

## 7. Risks

| Risk | Mitigation |
|------|------------|
| Module boundary erosion | Compile-time project refs + code review checklist |
| Cross-module transaction complexity | Limit sync consistency; outbox for side effects |
| Primary DB reporting load | Views + later read models |
| Double booking under concurrency | Unique constraints + transactional conflict checks |
| PHI leakage in logs/notifications | Logging policy + SMS content rules |
| Hangfire/dashboard exposure | Admin-only access |
| Overengineering modules | Keep thin modules simple; rich domain only where needed |
| Unresolved business policies | Settings toggles; avoid hardcoding |

## 8. Recommended next implementation step

**Do not implement business features yet.** Next documentation/design step should be:

1. `docs/domain-overview.md` — bounded contexts, aggregates, ubiquitous language (Clinic vs Branch)  
2. `docs/permissions.md` — full permission matrix mapped to roles  
3. Optionally split ADRs into `docs/adr/`  
4. Then: create solution skeleton (projects empty of business logic), BuildingBlocks primitives, Host startup — **only when stakeholder authorizes scaffolding**

Until scaffolding is explicitly requested, remain documentation-only.

---

## Document Control

| Version | Date | Author role | Notes |
|---------|------|-------------|-------|
| 1.0 | 2026-09-13 | Senior Software / .NET Solution Architect | Architecture from `docs/product.md`; no code |
