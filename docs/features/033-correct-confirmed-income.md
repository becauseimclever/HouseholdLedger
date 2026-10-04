# Feature 033: Correct Confirmed Income

## Status

Complete. Approved and validated 2026-10-04.

## Outcome

A user can recover from an incorrect income receipt or an uncertain confirmation
without corrupting recorded actuals.

## Acceptance Criteria

- A confirmed receipt can be explicitly edited or permanently removed, with a
  confirmation before removal and authoritative refresh of affected dates.
- Actual dates, amounts, and existing-account allocations remain validated.
- Retrying the same confirmation request records at most one receipt, including
  concurrent requests. Reusing its identity with different data is a conflict.
- Identical legitimate payments remain possible with different request identities.
- Expectations never become receipts without deliberate user confirmation.

## Validation

Application/API tests cover correction, removal, validation, and conflicts.
Real PostgreSQL tests prove durable retry identity and concurrent insertion.
Client tests cover errors, confirmation, and notifications after mutation.

Nine isolated PostgreSQL integration tests passed, including concurrent retries,
conflicts, correction/deletion tombstones, and upgrade preservation. The real
Firefox setup journey confirmed, corrected, reloaded, and removed a receipt,
then verified the monthly actual income returned to zero.
