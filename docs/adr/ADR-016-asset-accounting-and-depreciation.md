# ADR-016 — Fixed Asset Accounting & Depreciation (STEP 21)

| Field | Content |
|-------|---------|
| **Status** | Accepted (provisional method) |
| **Date** | 2026-09-14 |
| **Context** | Assets module is a register only. Product AST-* requirements and `docs/modules/assets.md` list depreciation / GL capitalization as future/unresolved. STEP 18 provides accounting intents without journals. |
| **Decision** | Assets owns financial profile, schedule, depreciation transactions, NBV. Use `IDepreciationMethod` with **Provisional Straight-Line** monthly schedule. Call `IAccountingPostingPort` for intents only. Keep operational `AssetStatus` separate from `AssetFinancialStatus`. |
| **Alternatives** | Declining balance; units of production; defer entirely; auto GL with invented CoA. |
| **Reasoning** | Deterministic, auditable foundation mirroring Inventory ADR-015 pattern without inventing business or GL mappings. |
| **Consequences** | Product must still approve method, frequency, partial-period rules, fiscal-period enforcement, disposal proceeds policy, opening balances for legacy assets. |

## Explicit non-goals

- Automatic JournalEntry / CoA mappings
- Tax depreciation, impairment, revaluation, leasing
- Automatic capitalization from PO/GR
- Durable Outbox (STEP 18 limitation remains)

## Rounding

Money scale 2, AwayFromZero. Final schedule period absorbs remainder so sum(schedule) = depreciable base.
