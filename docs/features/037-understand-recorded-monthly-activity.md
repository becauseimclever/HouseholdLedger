# Feature 037: Understand Recorded Monthly Activity

## Status

Complete. Approved and validated 2026-10-04.

## Outcome

A user can interpret recorded activity without mistaking expectations,
incomplete records, or account histories for bank balances or actual savings.

## Acceptance Criteria

- Review describes actuals as recorded so far and exposes the signed difference
  between confirmed monthly income and recorded monthly expenses.
- A negative difference does not imply a bank overdraft; opening balances,
  previous-month money, transfers, and actual savings are not inferred.
- Existing nonnegative unallocated remainder retains its meaning.
- Account views explicitly describe expense history rather than all activity.
- A schedule can supply editable receipt suggestions, but never pre-confirm,
  submit, or materialize income automatically.
- Routine empty income detail is progressively disclosed without hiding errors
  or the path to deliberate income confirmation.

## Validation

Application/API tests verify exact signed arithmetic; Client/browser checks
verify honest labels, suggestions, explicit confirmation, and empty/error states.

The isolated Firefox journey verified $0 confirmed monthly income and $82.45
expenses produce signed cashflow -$82.45 while unallocated remainder stays $0.
Schedule suggestions and errors are covered by the 148 passing Client tests.
