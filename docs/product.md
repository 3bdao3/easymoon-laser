# Clinic Management System — Product Requirements Document (PRD)

| Attribute | Value |
|-----------|--------|
| Document | `docs/product.md` |
| Status | Draft — Requirements Analysis |
| Version | 1.0 |
| Product | Clinic Management System (CMS) |
| Target Stack (documented) | ASP.NET Core Web API, C#, EF Core, SQL Server; Angular, TypeScript |
| Architecture Intent | Clean Architecture, modular, SOLID, multi-branch / multi-tenant ready |
| Implementation | **Not started** — this document only |

---

## 1. Product Vision

Build a **production-ready Clinic Management System** that models real clinic operations end-to-end: patient journey, clinical care, scheduling, queue, billing, inventory, procurement, assets, reporting, and controlled integrations.

The product is **not** a simple CRUD tool. It is a workflow-driven operational platform with:

- Explicit business rules and state machines
- Role-based security and auditability
- Financial integrity (invoices, payments, refunds, commissions)
- Clinical record integrity and access control
- Inventory and procurement controls
- A foundation that can evolve into **multi-branch** and later **multi-tenant** without rewriting core domain models

V1 focuses on a **single clinic / single organization** deployment that is structurally ready for branch and tenant expansion.

---

## 2. Product Goals

### Primary goals (V1)

1. Digitize the full patient operational journey from registration through payment and follow-up.
2. Enforce appointment, queue, visit, prescription, and billing workflows with clear statuses and transitions.
3. Provide secure, role-based access for clinic staff with auditable actions on sensitive data.
4. Maintain financial correctness: invoices, payments, refunds, discounts, balances, and doctor commissions.
5. Support clinical documentation: visits, vitals, diagnosis, treatment, prescriptions, attachments, timeline.
6. Manage inventory and procurement with stock visibility, batches/expiry awareness, and supplier tracking.
7. Deliver operational and financial reports for day-to-day clinic management.
8. Establish Clean Architecture module boundaries that support future multi-branch and multi-tenant growth.

### Non-goals (V1)

- Full multi-tenant SaaS isolation and billing for tenants
- Cross-branch advanced logistics / transfer automation as a primary feature
- Hospital-grade EMR / inpatient / OR / ICU workflows
- Insurance claims adjudication engines (unless later confirmed)
- Patient self-service mobile app as a primary delivery channel (portal may be future)

---

## 3. Target Users

| User group | Description | Primary needs |
|------------|-------------|---------------|
| Clinic owners / administrators | Business and system governance | Settings, users, roles, reports, financial oversight |
| Reception / front desk | First operational touchpoint | Registration, appointments, check-in, queue, invoices/payments |
| Doctors | Clinical care providers | Schedule, queue, visits, diagnosis, prescriptions, notes |
| Nurses / clinical assistants | Clinical support | Vitals, check-in support, queue, assist visits |
| Cashiers / billing staff | Revenue collection | Invoices, payments, refunds, outstanding balances |
| Pharmacists / dispensary (if clinic has internal pharmacy) | Medicine handling | Prescriptions, stock of medicines (scope depends on inventory model) |
| Inventory / store keepers | Stock control | Items, warehouses, adjustments, receiving |
| Procurement officers | Purchasing | Suppliers, POs, receiving, supplier balances |
| Finance / accountants | Financial control | Revenue, expenses, commissions, reports |
| Operations managers | Cross-module oversight | Dashboards, doctor utilization, queue KPIs |
| IT / system admins | Technical administration | Users, permissions, audit, integrations, settings |

**Note:** External patients are **actors** in the business process but are not assumed to be authenticated system users in V1 unless a patient portal is later confirmed.

---

## 4. Actors / Roles

### 4.1 Human actors

| Actor | Typical system role(s) | Notes |
|-------|------------------------|-------|
| System Administrator | `SystemAdmin` | Full configuration; user/role/permission management |
| Clinic Administrator | `ClinicAdmin` | Day-to-day admin within clinic scope |
| Receptionist | `Reception` | Patients, appointments, check-in, queue entry |
| Doctor | `Doctor` | Clinical modules; limited financial visibility |
| Nurse | `Nurse` | Vitals, queue assist, limited clinical documentation |
| Cashier | `Cashier` | Invoices, payments, refunds (policy-controlled) |
| Inventory Manager | `InventoryManager` | Stock, adjustments, warehouses |
| Procurement Officer | `ProcurementOfficer` | Suppliers, POs, receiving |
| Accountant / Finance | `Finance` | Financial reports, expenses, commissions review |
| Operations Manager | `OperationsManager` | Cross-operational reports and dashboards |
| Auditor (read-only) | `Auditor` | Audit logs and read-only sensitive reports |

### 4.2 System actors

| Actor | Purpose |
|-------|---------|
| Notification Service | Sends SMS / Email / WhatsApp / in-app notifications |
| Scheduler / Background Jobs | Reminders, expiry alerts, overdue balances, report generation |
| Integration Gateways | External SMS/Email/WhatsApp providers |
| Audit Writer | Immutable (append-only) recording of security and business events |

### 4.3 Role model principles

- Roles are **permission bundles**, not hard-coded feature switches in UI alone.
- A user may have **one or more roles**.
- Permissions are evaluated at API level (authoritative), not only in the UI.
- Doctor-specific clinical access may additionally be constrained by **assignment** (e.g., own patients/visits) — exact policy is an open question for V1.

---

## 5. System Scope

### 5.1 In scope (V1 product boundaries)

The system manages clinic operations for **one organization** (single legal entity), typically **one physical clinic / branch** in V1, with data model hooks for future branches.

In-scope domains:

1. Administration (users, roles, permissions, departments, settings, audit)
2. Patient management and medical record timeline
3. Doctors, nurses, specializations, clinics/departments, working hours
4. Scheduling (slots, holidays, exceptions, conflict detection)
5. Appointments (lifecycle)
6. Queue management
7. Medical visits and clinical documentation
8. Prescriptions
9. Services, packages, pricing, commission rules
10. Financial management (invoices, payments, refunds, discounts, balances, commissions, revenue/expenses)
11. Procurement (suppliers, POs, receiving, supplier balances)
12. Inventory (warehouses, items, stock, batches/expiry, adjustments)
13. Assets (equipment, locations, warranty, maintenance status)
14. Reports and operational dashboards
15. Notifications via SMS / Email / WhatsApp abstractions

### 5.2 System boundaries

```
┌──────────────────────────────────────────────────────────────┐
│                     Clinic Management System                 │
│  Admin │ Patients │ Clinical │ Ops │ Finance │ Supply │ Assets│
└───────────────┬──────────────────────────────┬───────────────┘
                │                              │
        Staff users (Angular)           Notification providers
                │                       (SMS / Email / WhatsApp)
                ▼
        ASP.NET Core API  ──►  SQL Server
```

**Inside boundary:** business workflows, persistence, authorization, auditing, reporting, notification orchestration.

**Outside boundary (V1):**

- External laboratory / radiology information systems (HL7/FHIR full interoperability)
- National health exchange / insurance clearinghouses (unless confirmed later)
- Payment gateway settlement systems (may be future; V1 may be cash/card recorded manually)
- Patient consumer mobile apps
- Accounting ERP general ledger posting (export may be future)
- Hardware queue displays / calling boards (integration interface may be reserved)

### 5.3 Explicit exclusions (V1)

See **Out of Scope for V1** at the end of this document.

---

## 6. Modules

### 6.1 Module catalog

| # | Module | Responsibility |
|---|--------|----------------|
| 1 | Administration | Identity, RBAC, departments, settings, audit access |
| 2 | Patient Management | Registration, profile, history, allergies, attachments, timeline |
| 3 | Doctor & Clinic Management | Staff clinical profiles, specializations, clinics, working hours |
| 4 | Scheduling | Availability, slots, holidays, exceptions, conflict rules |
| 5 | Appointment Management | Appointment lifecycle |
| 6 | Queue Management | Waiting room / call flow |
| 7 | Medical Visits | Encounter/visit clinical documentation |
| 8 | Prescription Management | Rx and items |
| 9 | Services & Packages | Catalog, pricing, packages, commission rules |
| 10 | Financial Management | Invoicing, payments, refunds, balances, commissions, expenses |
| 11 | Procurement | Suppliers, POs, receiving, supplier balances |
| 12 | Inventory | Stock, batches, transactions, min stock |
| 13 | Assets | Equipment/furniture lifecycle metadata |
| 14 | Reports | Analytical and operational reporting |
| 15 | Integrations / Notifications | Outbound messaging and notification delivery |

### 6.2 Module boundaries (ownership)

| Module | Owns | Must not own |
|--------|------|--------------|
| Administration | Users, roles, permissions, settings, audit query | Clinical content, invoices |
| Patient Management | Patient master data, allergies, attachments metadata, timeline aggregation | Visit clinical details (references visits) |
| Doctor & Clinic | Doctor/nurse profiles, specializations, clinic/dept structure, working hours | Appointment booking logic |
| Scheduling | Availability generation/validation | Appointment business status beyond conflict checks |
| Appointments | Appointment aggregate and transitions | Clinical notes, invoice lines |
| Queue | Queue tickets/entries and call state | Diagnosis / Rx |
| Medical Visits | Visit aggregate, vitals, diagnosis, treatment notes | Payment collection |
| Prescriptions | Prescription aggregate | Stock deduction policy may coordinate with Inventory |
| Services & Packages | Service/package catalog and prices | Invoice issuance |
| Financial | Invoice, payment, refund, commission, expense aggregates | Clinical decision data |
| Procurement | Supplier, PO, receipt aggregates | Retail POS |
| Inventory | Stock ledger, batches, warehouses | Supplier master (owned by Procurement; referenced) |
| Assets | Asset registry | Inventory consumables |
| Reports | Read models / queries | Source-of-truth writes |
| Integrations | Delivery adapters, templates, delivery logs | Business rule decisions |

### 6.3 Module dependencies

Dependency direction should point **inward to stable domain concepts** and avoid cycles.

```
Administration ◄── (all modules need identity/authz)

Patient Management ◄── Appointments, Visits, Queue, Finance, Prescriptions, Reports

Doctor & Clinic ◄── Scheduling, Appointments, Visits, Services (commission), Reports

Scheduling ◄── Appointments

Appointments ◄── Queue, Visits, Finance (optional deposit/invoice link)

Queue ◄── Visits (consultation start)

Visits ◄── Prescriptions, Services (ordered), Finance (billable items)

Services & Packages ◄── Finance, Visits, Commissions

Prescriptions ◄── Inventory (optional dispense/stock)

Finance ◄── Reports

Procurement ◄── Inventory (receiving increases stock)

Inventory ◄── Reports, Prescriptions (optional)

Assets ◄── Reports

Integrations ◄── consumed by Appointments, Finance, Inventory alerts, Admin
```

**Allowed coupling:** events/commands across modules (e.g., `AppointmentCheckedIn` → create/update Queue entry).

**Forbidden coupling:** Finance writing clinical notes; Inventory creating appointments; UI bypassing domain rules.

---

## 7. Detailed Functional Requirements

Requirements use IDs for traceability. Priority: **M** = Must (V1), **S** = Should (V1 if feasible), **C** = Could (later).

### 7.1 Administration

| ID | Requirement | Priority |
|----|-------------|----------|
| ADM-01 | Create, update, deactivate users | M |
| ADM-02 | Assign one or more roles to users | M |
| ADM-03 | Define roles and map permissions | M |
| ADM-04 | Manage departments | M |
| ADM-05 | Manage system settings (clinic profile, locale, currency, defaults) | M |
| ADM-06 | View audit logs with filters (actor, entity, date, action) | M |
| ADM-07 | Enforce password / auth policy per settings | M |
| ADM-08 | Soft-disable users without deleting history | M |

### 7.2 Patient Management

| ID | Requirement | Priority |
|----|-------------|----------|
| PAT-01 | Register patient with unique clinic MRN / patient code | M |
| PAT-02 | Capture demographics and contact information | M |
| PAT-03 | Maintain medical history summary | M |
| PAT-04 | Maintain allergies with severity/notes | M |
| PAT-05 | Upload/manage attachments (referral, ID, reports) with access control | M |
| PAT-06 | View patient timeline (appointments, visits, invoices, prescriptions) | M |
| PAT-07 | Search patients by name, phone, MRN, national ID (if collected) | M |
| PAT-08 | Prevent duplicate registration via configurable matching rules | S |
| PAT-09 | Merge duplicate patients (controlled, audited) | C |

### 7.3 Doctor & Clinic Management

| ID | Requirement | Priority |
|----|-------------|----------|
| DOC-01 | Manage doctor profiles linked to users | M |
| DOC-02 | Manage nurse profiles linked to users | M |
| DOC-03 | Manage specializations | M |
| DOC-04 | Manage clinics / departments association | M |
| DOC-05 | Define doctor working hours templates | M |
| DOC-06 | Assign doctor permissions beyond global role (if needed) | S |
| DOC-07 | Activate/deactivate clinical staff | M |

### 7.4 Scheduling

| ID | Requirement | Priority |
|----|-------------|----------|
| SCH-01 | Define working days and time slot duration per doctor/clinic | M |
| SCH-02 | Generate or validate available slots | M |
| SCH-03 | Manage holidays (clinic-wide) | M |
| SCH-04 | Manage doctor exceptions (leave, extra hours) | M |
| SCH-05 | Detect schedule conflicts (double booking, outside hours, holiday) | M |
| SCH-06 | Block slots manually | S |

### 7.5 Appointment Management

| ID | Requirement | Priority |
|----|-------------|----------|
| APT-01 | Create appointment for patient + doctor + service/slot | M |
| APT-02 | Confirm appointment | M |
| APT-03 | Reschedule appointment with conflict checks | M |
| APT-04 | Cancel appointment with reason | M |
| APT-05 | Mark no-show | M |
| APT-06 | Check-in patient | M |
| APT-07 | Enforce allowed status transitions | M |
| APT-08 | Send reminders (integration) | S |
| APT-09 | Walk-in appointment creation | M |

### 7.6 Queue Management

| ID | Requirement | Priority |
|----|-------------|----------|
| QUE-01 | Add checked-in patient to waiting queue | M |
| QUE-02 | Support priority levels | M |
| QUE-03 | Call next / call specific patient | M |
| QUE-04 | Mark in consultation | M |
| QUE-05 | Mark completed / skipped / returned to waiting | M |
| QUE-06 | Maintain queue history | M |
| QUE-07 | Filter queue by doctor / department / clinic | M |

### 7.7 Medical Visits

| ID | Requirement | Priority |
|----|-------------|----------|
| VIS-01 | Create visit from appointment/queue or direct | M |
| VIS-02 | Capture chief complaint, symptoms, examination | M |
| VIS-03 | Capture diagnosis (structured and/or free text — see open questions) | M |
| VIS-04 | Capture treatment plan and clinical notes | M |
| VIS-05 | Record vital signs | M |
| VIS-06 | Record follow-up recommendation / next visit suggestion | M |
| VIS-07 | Close visit with completion rules | M |
| VIS-08 | Restrict edit after close per policy | M |
| VIS-09 | Link visit to ordered services and prescriptions | M |

### 7.8 Prescription Management

| ID | Requirement | Priority |
|----|-------------|----------|
| RX-01 | Create prescription for a visit/patient | M |
| RX-02 | Add prescription items (medicine, dosage, frequency, duration, instructions) | M |
| RX-03 | Maintain medicines catalog (may overlap Inventory items) | M |
| RX-04 | Print / export prescription | S |
| RX-05 | Void / amend prescription under audit rules | M |
| RX-06 | Optional stock reservation/dispense integration | S |

### 7.9 Services & Packages

| ID | Requirement | Priority |
|----|-------------|----------|
| SVC-01 | Manage medical services and categories | M |
| SVC-02 | Manage pricing (base price; future price lists) | M |
| SVC-03 | Manage packages and package items | M |
| SVC-04 | Define doctor commission rules per service/package | M |
| SVC-05 | Activate/deactivate services | M |

### 7.10 Financial Management

| ID | Requirement | Priority |
|----|-------------|----------|
| FIN-01 | Create invoice from visit/services/packages | M |
| FIN-02 | Support invoice line items, discounts, taxes (if enabled) | M |
| FIN-03 | Record payments (methods configurable) | M |
| FIN-04 | Support partial payments and outstanding balances | M |
| FIN-05 | Process refunds with authorization rules | M |
| FIN-06 | Calculate doctor commissions from eligible paid services | M |
| FIN-07 | Track revenue summaries | M |
| FIN-08 | Record expenses | M |
| FIN-09 | Prevent silent invoice mutation after payment (credit note / adjustment flow) | M |
| FIN-10 | Patient statement / balance inquiry | M |

### 7.11 Procurement

| ID | Requirement | Priority |
|----|-------------|----------|
| PRC-01 | Manage suppliers | M |
| PRC-02 | Create purchase orders with items | M |
| PRC-03 | Receive goods against PO (partial receive allowed) | M |
| PRC-04 | Update supplier balances (purchases, payments to supplier) | M |
| PRC-05 | PO statuses and approval (simple approval optional) | S |

### 7.12 Inventory

| ID | Requirement | Priority |
|----|-------------|----------|
| INV-01 | Manage warehouses | M |
| INV-02 | Manage items and categories | M |
| INV-03 | Maintain stock balance per warehouse/item | M |
| INV-04 | Record stock transactions (in/out/adjust/transfer-ready) | M |
| INV-05 | Support batches and expiry dates | M |
| INV-06 | Minimum stock alerts | M |
| INV-07 | Stock adjustments with reason and audit | M |
| INV-08 | Prevent negative stock unless setting allows | M |
| INV-09 | Inventory valuation / cost layers / issue cost (provisional method per ADR-015; no auto GL) | M |

### 7.13 Assets

| ID | Requirement | Priority |
|----|-------------|----------|
| AST-01 | Register assets with category and location | M |
| AST-02 | Track warranty and maintenance status | M |
| AST-03 | Assign asset to department/location | M |
| AST-04 | Retire / dispose asset | S |
| AST-05 | Asset capitalization / depreciation / NBV foundation (provisional method per ADR-016; no auto GL) | M |

### 7.14 Reports

| ID | Requirement | Priority |
|----|-------------|----------|
| RPT-01 | Patient reports (registrations, active patients) | M |
| RPT-02 | Appointment reports (status, doctor, no-show rates) | M |
| RPT-03 | Doctor reports (visits, utilization) | M |
| RPT-04 | Revenue and payment reports | M |
| RPT-05 | Inventory and expiry reports | M |
| RPT-06 | Procurement reports | M |
| RPT-07 | Operational dashboards (queue length, today’s appointments, collections) | M |

### 7.15 Integrations

| ID | Requirement | Priority |
|----|-------------|----------|
| INT-01 | Abstraction for SMS / Email / WhatsApp providers | M |
| INT-02 | Appointment confirmation and reminder notifications | S |
| INT-03 | Payment receipt notification | S |
| INT-04 | Low stock / expiry notifications to staff | S |
| INT-05 | Delivery attempt logging and failure visibility | M |
| INT-06 | In-app / bell notifications for staff | S |

---

## 8. Main Business Workflows

### 8.1 Core patient journey (canonical)

```
Patient Registration
  → Appointment Creation
  → Confirmation (optional depending on policy)
  → Check-in
  → Queue Entry
  → Call / In Consultation
  → Medical Visit / Record
  → Diagnosis & Treatment
  → Prescription (optional)
  → Services / Packages charged
  → Invoice
  → Payment (full or partial)
  → Follow-up (appointment or instruction)
```

### 8.2 Workflow: Registration

1. Reception searches for existing patient.
2. If not found, creates patient master record; system assigns MRN.
3. Captures contacts, demographics, allergies (if known), attachments (optional).
4. Patient becomes available for appointment/visit.

### 8.3 Workflow: Appointment booking

1. Select patient, doctor, date, service (optional), slot.
2. System validates: doctor active, slot available, not holiday, no conflict, patient eligible.
3. Appointment created in `Scheduled` (or `PendingConfirmation`).
4. Optional notification sent.
5. Confirmation moves to `Confirmed`.

### 8.4 Workflow: Day-of visit (check-in → queue → consult)

1. Patient arrives; reception checks in appointment → `CheckedIn`.
2. Queue entry created (`Waiting`) with priority rules.
3. Doctor/nurse calls patient → `Called` → `InConsultation`.
4. Visit created/opened and linked to appointment/queue.
5. On consult end: queue → `Completed`; visit may remain open until documentation finished (policy).

### 8.5 Workflow: Clinical documentation

1. Capture complaint, vitals, exam, diagnosis, treatment, notes.
2. Create prescription if needed.
3. Order billable services/packages.
4. Complete visit when required fields satisfied.
5. Timeline updated.

### 8.6 Workflow: Billing & payment

1. Generate invoice from visit services/packages (and possibly appointment fees).
2. Apply discounts per authorization.
3. Accept payments; update balance.
4. If overpay/error: refund flow with approval.
5. Commission accrual based on rules and payment recognition policy.

### 8.7 Workflow: Procurement → inventory

1. Create PO to supplier.
2. Approve/send PO (if approval enabled).
3. Receive items (batch/expiry captured).
4. Stock transactions increase inventory.
5. Supplier balance updated.

### 8.8 Workflow: Walk-in

1. Register or find patient.
2. Create walk-in appointment or direct visit.
3. Check-in / queue as needed.
4. Continue canonical journey.

### 8.9 Workflow: Cancellation / no-show

1. Cancel before visit with reason → free slot.
2. No-show after missed attendance window → status `NoShow`; optional fee policy (open question).
3. Notifications optional.

---

## 9. Business Rules

### 9.1 Cross-cutting

| ID | Rule |
|----|------|
| BR-01 | All write operations require authenticated users. |
| BR-02 | Authorization is permission-based at API/domain level. |
| BR-03 | Soft-delete or deactivate preferred over hard-delete for master data with history. |
| BR-04 | Monetary amounts use defined currency precision; no floating-money ambiguity in domain. |
| BR-05 | Every state transition must be explicit and validated. |
| BR-06 | Sensitive medical and financial changes are auditable. |

### 9.2 Patient

| ID | Rule |
|----|------|
| BR-PAT-01 | MRN is unique within organization (and later within tenant). |
| BR-PAT-02 | Patient cannot be hard-deleted if clinical/financial history exists. |
| BR-PAT-03 | Allergy list must be visible during visit/prescription flows. |

### 9.3 Scheduling & appointments

| ID | Rule |
|----|------|
| BR-APT-01 | Cannot book outside doctor working hours unless exception allows. |
| BR-APT-02 | Cannot book on clinic holiday unless override permission. |
| BR-APT-03 | Double-booking blocked unless explicit overbook permission. |
| BR-APT-04 | Only allowed status transitions may occur (see §11). |
| BR-APT-05 | Reschedule keeps history (old slot released, audit retained). |
| BR-APT-06 | Cancelled/NoShow appointments do not occupy active queue. |

### 9.4 Queue

| ID | Rule |
|----|------|
| BR-QUE-01 | Queue entry requires checked-in patient (or authorized walk-in path). |
| BR-QUE-02 | Priority ordering: higher priority before FIFO within same priority (confirm algorithm). |
| BR-QUE-03 | Only one active `InConsultation` per doctor by default (configurable). |

### 9.5 Visits & prescriptions

| ID | Rule |
|----|------|
| BR-VIS-01 | Visit must reference a patient. |
| BR-VIS-02 | Closed visits are immutable except via controlled amendment/addendum. |
| BR-VIS-03 | Prescription requires visit (or approved standalone policy — default: require visit). |
| BR-VIS-04 | Prescription items require medicine, dosage, frequency, duration (or explicit PRN pattern). |

### 9.6 Financial

| ID | Rule |
|----|------|
| BR-FIN-01 | Invoice total = sum(lines) − discounts + taxes (if enabled). |
| BR-FIN-02 | Payments cannot exceed remaining balance unless overpayment policy exists. |
| BR-FIN-03 | Posted invoice lines are not silently edited; use adjustment/credit note. |
| BR-FIN-04 | Refunds require permission and reason; cannot exceed paid amount. |
| BR-FIN-05 | Outstanding balance = invoice total − payments + refunds (net). |
| BR-FIN-06 | Commission recognition follows configured basis (on invoice vs on payment) — must be single policy. |
| BR-FIN-07 | Discounts above threshold require elevated permission. |

### 9.7 Inventory & procurement

| ID | Rule |
|----|------|
| BR-INV-01 | Stock movements are transactional ledger entries; balance is derived/consistent. |
| BR-INV-02 | Expiry-aware items require batch on receive. |
| BR-INV-03 | Cannot issue stock below zero unless allowed by setting. |
| BR-INV-04 | Receiving against PO cannot exceed ordered qty unless over-receive permission. |
| BR-INV-05 | Adjustments require reason code. |

### 9.8 Assets

| ID | Rule |
|----|------|
| BR-AST-01 | Asset codes unique within organization. |
| BR-AST-02 | Retired assets cannot be assigned as active equipment. |

---

## 10. Entity Candidates

> Entity candidates are domain concepts for later modeling. Not a database schema. Names may refine during design.

### 10.1 Administration

- User, Role, Permission, RolePermission, UserRole
- Department
- SystemSetting
- AuditLog / AuditEntry

### 10.2 Patient

- Patient
- PatientContact
- PatientAllergy
- MedicalHistoryItem (or structured history fields)
- PatientAttachment
- PatientTimelineEvent (read model / projection candidate)

### 10.3 Doctor & clinic

- DoctorProfile
- NurseProfile
- Specialization
- Clinic
- ClinicDepartment (or Department already in Admin — clarify ownership)
- DoctorWorkingHours
- DoctorSpecialization

### 10.4 Scheduling

- ScheduleTemplate
- TimeSlot (generated or implicit)
- Holiday
- ScheduleException
- SlotBlock

### 10.5 Appointments

- Appointment
- AppointmentStatusHistory
- AppointmentRescheduleHistory

### 10.6 Queue

- QueueEntry
- QueueStatusHistory
- QueuePriorityRule (optional)

### 10.7 Visits

- Visit / Encounter
- VisitVitalSign
- VisitDiagnosis
- VisitExaminationFinding
- ClinicalNote
- FollowUpPlan

### 10.8 Prescriptions

- Prescription
- PrescriptionItem
- Medicine (catalog; may map to InventoryItem)

### 10.9 Services & packages

- ServiceCategory
- MedicalService
- ServicePrice
- Package
- PackageItem
- DoctorCommissionRule

### 10.10 Finance

- Invoice
- InvoiceItem
- Payment
- PaymentAllocation (if multi-invoice payments)
- Refund
- Discount
- PatientBalance (derived or stored snapshot)
- DoctorCommissionEntry
- Expense
- ExpenseCategory

### 10.11 Procurement

- Supplier
- PurchaseOrder
- PurchaseOrderItem
- GoodsReceipt
- GoodsReceiptItem
- SupplierPayment
- SupplierBalance (derived/snapshot)

### 10.12 Inventory

- Warehouse
- InventoryItem
- ItemCategory
- StockBalance
- StockTransaction
- StockBatch
- StockAdjustment

### 10.13 Assets

- Asset
- AssetCategory
- AssetLocation
- AssetMaintenanceRecord (optional V1 light)

### 10.14 Integrations / notifications

- NotificationTemplate
- NotificationMessage
- NotificationDeliveryLog
- IntegrationProviderConfig

### 10.15 Cross-cutting future readiness (not full multi-tenant in V1)

- Organization (single row in V1)
- Branch (optional single default branch in V1)

---

## 11. Statuses and State Transitions

### 11.1 Appointment

**Statuses (candidate set):**

`Draft` (optional) → `Scheduled` → `Confirmed` → `CheckedIn` → `InProgress` / `Completed` → (terminal)  
Also: `Cancelled`, `NoShow`, `Rescheduled` (or reschedule as new appointment + link)

**Recommended V1 set:**

| Status | Meaning |
|--------|---------|
| Scheduled | Created, awaiting confirmation/arrival |
| Confirmed | Confirmed by clinic/patient |
| CheckedIn | Patient arrived |
| Completed | Visit finished / appointment fulfilled |
| Cancelled | Cancelled before completion |
| NoShow | Patient did not attend |
| Rescheduled | Superseded by another appointment |

**Transitions:**

| From | To | Guard |
|------|----|-------|
| Scheduled | Confirmed | Confirm permission / auto-policy |
| Scheduled | Cancelled | Cancel reason |
| Scheduled | Rescheduled | New slot valid |
| Confirmed | CheckedIn | Same day / policy window |
| Confirmed | Cancelled | Cancel reason |
| Confirmed | NoShow | Marked after window |
| Confirmed | Rescheduled | New slot valid |
| CheckedIn | Completed | Visit completed linkage |
| CheckedIn | Cancelled | Rare; requires permission |
| Any active | Cancelled | Permission + reason (except completed) |

### 11.2 Queue entry

| Status | Meaning |
|--------|---------|
| Waiting | In queue |
| Called | Called to room |
| InConsultation | With clinician |
| Completed | Done |
| Skipped | Called but not present |
| Cancelled | Removed |

**Transitions:** Waiting → Called → InConsultation → Completed  
Waiting → Skipped → Waiting (requeue) or Cancelled  
Any non-terminal → Cancelled (permission)

### 11.3 Visit

| Status | Meaning |
|--------|---------|
| Open | Documentation in progress |
| InReview | Optional nurse/doctor review |
| Closed | Completed and locked |
| Cancelled | Voided without clinical completion |
| Amended | Closed with addendum process (optional) |

**Transitions:** Open → Closed; Open → Cancelled; Closed → Amended (controlled)

### 11.4 Prescription

| Status | Meaning |
|--------|---------|
| Draft | Being written |
| Issued | Signed/issued |
| PartiallyDispensed | If dispense enabled |
| Dispensed | Fully dispensed |
| Cancelled / Voided | Voided under audit |

### 11.5 Invoice

| Status | Meaning |
|--------|---------|
| Draft | Editable |
| Issued | Finalized for payment |
| PartiallyPaid | Some payments |
| Paid | Fully paid |
| Voided | Cancelled before payment impact |
| Refunded / PartiallyRefunded | After refunds |

### 11.6 Payment / Refund

- Payment: `Captured` (V1 simple); future `Pending` for gateways
- Refund: `Requested` → `Approved` → `Completed` / `Rejected`

### 11.7 Purchase order

`Draft` → `Submitted` → `Approved` → `Ordered` → `PartiallyReceived` → `Received` → `Closed`  
Also: `Cancelled`

### 11.8 Stock / inventory

Balances are not “statuses”; transactions types: `PurchaseIn`, `SaleOut`/`Issue`, `AdjustIn`, `AdjustOut`, `TransferIn/Out` (transfer ready for multi-branch).

### 11.9 Asset

`Active` → `UnderMaintenance` → `Active`  
`Active` → `Retired`

---

## 12. Permissions Concept

### 12.1 Model

- **Permission** = resource + action (e.g., `Patients.Read`, `Patients.Create`, `Invoices.Refund`).
- **Role** = named set of permissions.
- **User** ↔ **Roles** (many-to-many).
- Optional **claims** for doctor-owned data scope.

### 12.2 Permission categories (illustrative)

| Area | Examples |
|------|----------|
| Admin | Users.Manage, Roles.Manage, Settings.Manage, Audit.Read |
| Patients | Patients.Read, Patients.Create, Patients.Update, Attachments.Manage |
| Scheduling | Schedules.Manage, Holidays.Manage |
| Appointments | Appointments.Create, Confirm, Reschedule, Cancel, CheckIn, MarkNoShow |
| Queue | Queue.View, Queue.Call, Queue.ManagePriority |
| Clinical | Visits.Create, Visits.Update, Visits.Close, Prescriptions.Issue |
| Catalog | Services.Manage, Packages.Manage, CommissionRules.Manage |
| Finance | Invoices.Create, Invoices.Issue, Payments.Capture, Refunds.Approve, Expenses.Manage |
| Supply | Suppliers.Manage, PO.Create, PO.Approve, Stock.Adjust, Stock.Receive |
| Assets | Assets.Manage |
| Reports | Reports.Operational, Reports.Financial, Reports.Clinical |

### 12.3 Enforcement principles

1. UI hides unauthorized actions; API rejects unauthorized calls.
2. Elevated actions (refund, large discount, stock adjust, medical amend) require stronger permissions.
3. Future branch scope: permissions evaluated within branch context.
4. Future tenant scope: permissions evaluated within tenant isolation boundary.

---

## 13. Notifications

### 13.1 Channels

- SMS
- Email
- WhatsApp
- In-app (staff)

### 13.2 Trigger events (V1 candidates)

| Event | Audience | Channel (configurable) |
|-------|----------|------------------------|
| Appointment created | Patient | SMS/WhatsApp/Email |
| Appointment confirmed | Patient | SMS/WhatsApp/Email |
| Appointment reminder (T-24h / T-2h) | Patient | SMS/WhatsApp |
| Appointment cancelled / rescheduled | Patient | SMS/WhatsApp/Email |
| Payment received | Patient | SMS/Email |
| Low stock | Inventory role | In-app/Email |
| Batch near expiry | Inventory role | In-app/Email |
| Queue called | Patient/display | Optional |

### 13.3 Requirements

- Templates per event and channel
- Opt-out / clinic policy for patient messaging
- Retry and failure logging
- No patient clinical details in SMS by default (privacy)

---

## 14. Reporting Requirements

### 14.1 Operational

- Today’s appointments by status/doctor
- Queue waiting time and throughput
- No-show and cancellation rates
- Doctor utilization (booked vs available slots)

### 14.2 Clinical / patient

- New patients in period
- Visits by diagnosis/service (respecting privacy)
- Follow-up adherence (basic)

### 14.3 Financial

- Revenue by day/service/doctor/payment method
- Collections vs outstanding balances
- Refunds summary
- Doctor commissions
- Expenses vs revenue (basic P&L-style operational view — not full accounting)

**STEP 22 implementation note:** Reporting foundation exposes Billing registers, AR/AP, GL/TB, inventory valuation/issue-cost, assets, and procurement operational reports from **real module data**. Net profit / EBITDA / cash flow / gross margin are returned as **unavailable** until GL revenue/expense integration exists. Billing totals are **not** claimed as GL revenue. See ADR-017.

### 14.4 Supply

- Stock on hand by warehouse
- Near-expiry batches
- Below minimum stock
- Purchases by supplier
- PO status aging

### 14.5 Dashboard KPIs (V1)

- Appointments today
- Patients in queue
- Collections today
- Outstanding total
- Low-stock count

Reports are **read-only**, permission-filtered, and exportable (CSV/Excel — format decision later).

---

## 15. Integration Requirements

| Integration | V1 approach |
|-------------|-------------|
| SMS | Provider-agnostic port + one adapter configurable |
| Email | SMTP or provider port + adapter |
| WhatsApp | Provider-agnostic port + adapter (may be disabled until credentials exist) |
| Payment gateway | Out of scope for automated capture unless confirmed; manual payment recording in V1 |
| Accounting export | Future |
| Lab/RIS/PACS | Future |
| National ID / insurance eligibility | Future / open |

**Technical expectation (documented intent):** Integrations behind interfaces/adapters so domain does not depend on vendor SDKs.

---

## 16. Non-Functional Requirements

| ID | Category | Requirement |
|----|----------|-------------|
| NFR-01 | Reliability | Prevent data loss for clinical and financial transactions; use ACID transactions for money/stock |
| NFR-02 | Performance | Common reception searches and today’s queue/appointments responsive under normal clinic load |
| NFR-03 | Usability | Reception flows completable with minimal clicks; critical alerts (allergies) visible |
| NFR-04 | Maintainability | Clean Architecture modules; clear boundaries; testable domain rules |
| NFR-05 | Testability | Business rules and state transitions unit-testable without UI |
| NFR-06 | Observability | Structured logs; correlation IDs for requests; integration delivery logs |
| NFR-07 | Availability | Target clinic business hours continuity; exact SLA TBD |
| NFR-08 | Localization | i18n-ready UI strings; timezone/currency settings at organization level |
| NFR-09 | Accessibility | Reasonable keyboard use for reception forms (detail level TBD) |
| NFR-10 | Backup | DB backup/restore strategy required for production (ops procedure) |

---

## 17. Security Requirements

| ID | Requirement |
|----|-------------|
| SEC-01 | Authentication required for all API endpoints except explicit health/public endpoints |
| SEC-02 | Authorization via roles/permissions on every protected operation |
| SEC-03 | Password hashing with modern algorithm; no plaintext secrets in DB |
| SEC-04 | HTTPS only in production |
| SEC-05 | Protect against common API threats (OWASP ASVS-aligned practices) |
| SEC-06 | Least privilege default roles |
| SEC-07 | Segregation: cashiers cannot alter clinical notes; doctors cannot approve arbitrary refunds (unless dual-role) |
| SEC-08 | Attachment access control and content-type validation |
| SEC-09 | Secrets (SMS keys, etc.) in secure configuration, not source control |
| SEC-10 | Session/token expiry and revocation for disabled users |
| SEC-11 | PHI/PII minimization in logs and notifications |
| SEC-12 | Optional MFA for privileged roles (Should) |

Exact identity mechanism (JWT bearer, cookie, Identity server, etc.) is a **documented technical decision still to be made** — not assumed beyond ASP.NET Core secure API design.

---

## 18. Audit Requirements

| ID | Requirement |
|----|-------------|
| AUD-01 | Audit authentication successes/failures and permission denials (security audit) |
| AUD-02 | Audit create/update/deactivate on patients, appointments, visits, prescriptions |
| AUD-03 | Audit invoice issue, payment capture, refund, discount overrides |
| AUD-04 | Audit stock adjustments and PO approvals |
| AUD-05 | Audit role/permission changes |
| AUD-06 | Audit entries include: actor, timestamp, action, entity type/id, before/after or diff summary, correlation id |
| AUD-07 | Audit logs are append-only for users (no edit/delete in V1 app) |
| AUD-08 | Audit retention policy configurable (ops) |

---

## 19. Scalability Requirements

| ID | Requirement |
|----|-------------|
| SCL-01 | Support a single busy clinic’s concurrent staff users in V1 without redesign |
| SCL-02 | Stateless API tier for horizontal scale-out |
| SCL-03 | Database as system of record; indexes for MRN, phone, appointment date, queue date |
| SCL-04 | Background jobs for reminders/notifications so request threads stay light |
| SCL-05 | Reporting queries must not lock transactional tables excessively (read models/replicas later if needed) |
| SCL-06 | Design entities with optional `OrganizationId` / `BranchId` for future scale-out of org structure |
| SCL-07 | Avoid chatty cross-module DB joins in write paths; prefer explicit contracts/events |

V1 does **not** require multi-region active-active.

---

## 20. Future Expansion

### 20.1 Multi-branch

- Multiple branches under one organization
- Branch-scoped schedules, queues, warehouses, cashiers
- Shared patient master vs branch-specific preferences (decision later)
- Inter-branch stock transfer

### 20.2 Multi-tenant

- Tenant isolation (data + config + users)
- Tenant-level branding/settings
- SaaS onboarding and subscription billing
- Stricter noisy-neighbor protections

### 20.3 Product expansions

- Patient portal / mobile app
- Online booking
- Insurance claims
- Lab/radiology integrations
- E-prescription regulatory connectors
- Full accounting/ERP sync
- Advanced EMR (care plans, chronic disease programs)
- Telemedicine visits
- Hardware queue displays / token printers
- AI-assisted coding/documentation (carefully governed)

### 20.4 V1 readiness without overengineering

- Include `Organization` (single) and nullable/default `Branch`
- No tenant router/custom schemas yet
- No distributed microservices mandate; modular monolith preferred initially

---

## 21. Assumptions

1. V1 targets **one organization** and effectively **one branch**, with model hooks for more.
2. Staff are internal users; patients are not primary login users in V1.
3. Clinic operates appointment + walk-in hybrid.
4. Currency is single-currency per organization in V1.
5. Clinical coding system (ICD-10 etc.) may start as free text + optional code field unless mandated.
6. Inventory and pharmacy may share item catalog or map Medicine ↔ InventoryItem.
7. Tax may be optional via settings (inclusive/exclusive TBD).
8. Commissions are calculated inside the system; payouts may be manual.
9. WhatsApp/SMS providers will be configured per deployment; system ships with abstractions.
10. Clean Architecture modular monolith is acceptable for V1 (not microservices-first).
11. Regulatory regime (HIPAA/GDPR/local health law) applies in spirit (consent, access, audit); exact certification is not claimed in V1.
12. Arabic/English bilingual UI is likely desired but not confirmed (see open questions).

---

## 22. Open Questions

1. Is V1 strictly single-branch, or must multi-branch switching ship in V1 UI?
2. Exact appointment status model: is `Confirmed` mandatory or can clinics auto-confirm?
3. Are deposits / prepayments required before confirmation?
4. No-show fees: yes/no, and how invoiced?
5. Diagnosis: free text only, ICD catalog, or both?
6. Can nurses create/edit visits or only vitals?
7. Is internal pharmacy dispensing in V1, or prescriptions are documentary only?
8. Medicine catalog vs inventory items: same entity or linked?
9. Commission basis: on issued invoice, on collected payment, or hybrid?
10. Tax handling: required for go-live countries/markets?
11. Payment methods list (cash, card, transfer, insurance co-pay)?
12. Insurance patients: track payer info only, or full claims?
13. Patient unique identity: national ID required?
14. Duplicate patient merge in V1 or later?
15. Attachment storage: DB, local disk, or object storage (S3/Azure Blob)?
16. Auth mechanism: ASP.NET Identity + JWT, or external IdP?
17. Offline / poor-connectivity requirements for reception?
18. Mandatory bilingual (AR/EN) for V1?
19. Electronic signature for prescriptions/visit close?
20. Queue display hardware integration needed in V1?
21. Expense module depth: simple ledger vs approval workflows?
22. Multi-doctor clinics sharing one reception queue vs per-doctor queues — default model?
23. Data retention and medical record legal hold requirements by jurisdiction?
24. Soft delete vs archive policies per entity?
25. Is “Clinic” a physical branch, a specialty unit, or both in domain language?

---

# Traceability Summary

## Confirmed Requirements

- Build a workflow-driven Clinic Management System (not CRUD-only).
- Documented target stack: ASP.NET Core Web API, C#, EF Core, SQL Server, Angular, TypeScript.
- Architecture goals: Clean Architecture, modularity, SOLID, maintainability, testability, secure APIs, future multi-branch/multi-tenant readiness.
- Modules 1–15 as listed in the product brief are in product scope for planning.
- Canonical business flow: Registration → Appointment → Confirmation → Check-in → Queue → Consultation → Medical Record → Diagnosis → Prescription → Services → Invoice → Payment → Follow-up.
- Must define/enforce states, business rules, permissions, audit, financial integrity, inventory controls, reporting, and notification integrations.
- V1 documentation deliverable is this PRD only; **no code, migrations, controllers, or Angular components yet**.
- Financial documents must not be silently mutated after posting; stock and money require transactional integrity.
- RBAC with API-level enforcement; audit of sensitive clinical, financial, security, and inventory actions.
- Integrations are provider-agnostic abstractions for SMS, Email, WhatsApp, plus notification logging.

## Assumptions

- Single organization / single effective branch in V1 with `Organization`/`Branch` hooks.
- Modular monolith is the initial deployment shape.
- Patients are not authenticated end-users in V1.
- Single currency per organization.
- Manual payment recording in V1 (no mandatory payment gateway).
- Prescriptions require a visit by default.
- Commissions calculated in-system; payouts may be external/manual.
- Tax optional via settings until market requirements confirm otherwise.
- WhatsApp may be disabled until provider configured.
- Regulatory compliance is designed for (audit, access control, least privilege) without claiming a specific certification badge in V1.

## Open Questions

- Multi-branch UI in V1 vs hooks only
- Appointment confirmation/deposit/no-show fee policies
- Diagnosis coding standard
- Nurse clinical edit rights
- Pharmacy dispense vs documentary Rx
- Medicine vs inventory identity
- Commission recognition basis
- Tax and insurance depth
- National ID and duplicate merge
- Attachment storage technology
- AuthN token/IdP choice (must be documented before implementation)
- Offline needs, bilingual UI, e-signature
- Queue hardware, expense workflow depth
- Shared vs per-doctor queue defaults
- Retention/legal hold and “Clinic” domain meaning

## Out of Scope for V1

- Implementation of code, EF migrations, API controllers, Angular feature modules/components
- Full multi-tenant SaaS isolation, tenant billing, and self-serve onboarding
- Advanced multi-branch logistics as a primary feature (transfers may be model-ready only)
- Hospital inpatient, ER triage acuity systems, OR scheduling, ICU
- Full insurance claims adjudication / clearinghouse integration
- Patient mobile app / self-service portal as a must-have
- Automated payment gateway settlement (unless later confirmed)
- Lab/RIS/PACS/HL7/FHIR deep interoperability
- General ledger / full accounting ERP replacement (GL foundation, accounting integration contracts, and AR/AP **subledgers** exist — not an ERP replacement; no auto GL mappings; PO is not AP)
- AI diagnosis products
- Marketplace plugins
- Certification projects (HIPAA audit attestation, etc.) as delivery scope unless contracted
- Microservices decomposition mandate

---

## Document Control

| Version | Date | Author role | Notes |
|---------|------|-------------|-------|
| 1.0 | 2026-09-13 | Senior Product Architect / BA | Initial PRD from stakeholder brief; analysis only |

**Next recommended artifacts (not created yet):**

1. `docs/domain-overview.md` — bounded contexts & aggregates  
2. `docs/workflows.md` — detailed state diagrams  
3. `docs/permissions.md` — full permission matrix  
4. Architecture decision records (auth, tenancy hooks, money, inventory ledger)
