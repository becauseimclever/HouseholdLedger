# Feature 036: Describe an Expense

## Status

Complete. Approved and validated 2026-10-04.

## Outcome

A user can recognize why an expense was recorded when revisiting it later.

## Acceptance Criteria

- Expense creation and correction accept an optional description up to 200
  characters, trimming surrounding whitespace and treating empty text as absent.
- Descriptions survive persistence and appear in day detail and expense history.
- Account search includes descriptions alongside existing searchable fields.
- Existing records and clients without descriptions continue to work.
- Brief classification guidance helps entry without prescribing household choices.

## Validation

Domain, API, persistence, and Client tests cover optional text, limits,
correction, display, and search.

The isolated Firefox setup journey persisted "Weekly groceries" and verified it
in the day inspector and the account's fourth history column after reload.
