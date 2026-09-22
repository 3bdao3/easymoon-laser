# ADR-014 — AR / AP Financial Subledgers (STEP 19)

| Field | Content |
|-------|---------|
| **Status** | Accepted |
| **Date** | 2026-09-14 |
| **Context** | Need auditable customer/supplier financial history separate from Billing invoices, Procurement POs, and General Ledger journals. |
| **Decision** | Finance owns a unified immutable `SubledgerTransactions` ledger (`Ar` / `Ap`) with materialized `SubledgerPartyBalances`, Application posting port, and read APIs for balances/statements/aging. AR party identity = **PatientId** (no CRM Customer duplicate). AP party identity = **Procurement SupplierId**. Billing posts AR via `ISubledgerPostingPort` after invoice/payment commit. Purchase Orders do **not** create AP. No automatic GL journals or account mappings. |
| **Alternatives** | (1) Balance fields on Invoice/Supplier; (2) Duplicate Customer/Supplier in Finance; (3) Treat PO as AP; (4) Auto-post GL. |
| **Reasoning** | Preserves module ownership, financial immutability, org-scoped idempotency, and future GL reconciliation readiness without inventing accounting policy. |
| **Consequences** | AP remains empty until a Supplier Invoice (or equivalent obligation) exists. Aging uses `DueDate` when present; Invoice has no due date — age from `TransactionDate`. Multi-currency FX and payment allocation across invoices remain unresolved. |

## Subledger vs GL

AR/AP subledger = party financial history.  
GL = chart/journals/posting rules.  
Bridge = STEP 18 `IAccountingPostingPort` (intent only until mappings approved).
