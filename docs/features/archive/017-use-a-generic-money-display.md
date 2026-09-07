# Feature 017: Use a Generic Money Display

## Status

Status: Blocked by Feature 016's financial semantics correction.

- Planned: 2026-09-05.
- Implemented: 2026-09-06.
- Validated: 2026-09-06.
- Reopened: 2026-09-07 because restoring a named currency also restores
  Feature 016's misleading denomination behavior.
- Depends on Feature 016's persisted global display-currency setting and shared
  application-wide money formatter.

## Outcome

A user can choose `No currency` in the global money setting. After saving, all
monetary values use the generic currency sign `¤` instead of a named currency
symbol, while retaining the existing USD-style two-decimal numeric treatment.

## User Flow

1. The user opens Settings.
2. The user chooses `No currency` from the Global money display dropdown.
3. The user saves and sees confirmation.
4. Every current monetary value changes from its prior named currency format to
   the generic form, such as `¤82.45`.
5. Refresh and application restart preserve the generic display.
6. Choosing and saving a named currency restores that named format globally.

## Generic Display Contract

- `No currency` is an explicit persisted option, not a missing setting, empty
  string, or null. Absence of a settings row continues to mean USD.
- The generic sign is Unicode currency sign `¤` (`U+00A4`). It is not `$`, an
  invented icon, or omitted currency context.
- Numeric treatment follows the application's existing USD amount convention:
  two decimal places, midpoint behavior inherited from current decimal values,
  and `en-US` grouping and decimal separators. Only the symbol becomes generic.
- Stored transaction amounts, calculations, validation, amount filters, and API
  contracts remain unchanged. `No currency` does not mean zero, unknown amount,
  conversion, or mixed-currency accounting.
- Accessible monetary text includes the same generic sign and numeric value;
  no display relies on icon recognition or color.

## Responsibility Boundaries

| Layer | Responsibility |
| --- | --- |
| Domain/Application | Accept the explicit generic-display value alongside Feature 016's named values. |
| Infrastructure/API | Extend the settings constraint, then persist and return the value through the existing settings contract. |
| Client | Add the dropdown option and format all monetary surfaces consistently as `¤` plus USD-style numeric text. |

No new endpoint, table, transaction field, or runtime dependency is introduced.
One additive migration extends Feature 016's database check constraint to allow
the explicit generic-display code.

## Acceptance Criteria

| Criterion | Observable outcome |
| --- | --- |
| AC-01: Explicit option | The Global money display dropdown includes one clearly labeled `No currency` option distinct from USD and from unavailable settings data. |
| AC-02: Generic application-wide format | Saving No currency immediately displays values such as `¤82.45` on every current calendar, inspector, account-history, form, confirmation, and filter surface. |
| AC-03: USD-style treatment | Generic display consistently uses two decimal places and `en-US` numeric separators while stored decimals and backend calculations remain unchanged. |
| AC-04: Durable reversible choice | Refresh and restart preserve No currency; saving a named currency restores that currency format everywhere. |
| AC-05: Honest failure behavior | A failed or obsolete save leaves the prior persisted display active and does not treat a missing/invalid value as No currency. |
| AC-06: Accessible responsive display | The generic sign and amount are available as text, remain distinguishable in all states, and do not clip or overlap at supported viewports. |
| AC-07: Bounded slice | No blank currency display, custom symbol, per-account override, conversion, mixed-currency accounting, locale preference, or new persistence shape is introduced. |

## Implementation Notes

1. Model the option as a named enum/value in the existing setting contract,
   such as `None`, while keeping USD as the absence default.
2. Extend the shared formatter; do not add per-component `¤` branches.
3. Keep numeric parsing and backend query values independent of display text.

No new runtime dependency is expected.

## Validation

- Application and API tests cover acceptance, persistence, rejection of unknown
  values, and distinction from the absent-row USD default.
- Shared formatter and Client component tests cover positive values, zero,
  grouping, two decimals, every money surface, failed saves, and switching back
  to a named currency.
- One hosted browser journey saves No currency, verifies generic presentation
  across Calendar, inspector, and account history, refreshes, then restores USD.

## Definition of Done

The feature is complete when No currency is a durable, reversible global choice
that formats every monetary value with `¤` and USD-style numeric treatment,
never changes ledger numbers, remains distinct from default/missing settings,
and passes focused cross-layer and hosted browser validation.

## Deferred Work

Custom symbols, symbol placement settings, accounting-negative formats, broader
locale controls, per-account currency, exchange rates, conversion, and
mixed-currency totals remain separate outcomes.

## Decisions

| Date | Decision | Reason |
| --- | --- | --- |
| 2026-09-05 | Use `¤` as the generic symbol. | Unicode defines it specifically as a currency sign without naming a currency. |
| 2026-09-05 | Preserve USD-style two-decimal treatment. | The user requested values be treated as USD while replacing the dollar sign. |
| 2026-09-05 | Persist No currency explicitly. | Missing configuration must continue to produce the requested USD default. |
| 2026-09-06 | Persist No currency as ISO 4217 code `XXX`. | The standard three-character code preserves the existing contract and remains distinct from absent-row USD; the Client presents it as No currency and `¤`. |

## Dependencies

- [Feature 016](016-configure-global-display-currency.md) supplies the Settings
  route, global persistence contract, supported named currencies, and shared
  money formatter.
