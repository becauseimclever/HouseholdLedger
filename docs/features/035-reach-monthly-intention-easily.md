# Feature 035: Reach Monthly Intention Easily

## Status

Complete. Approved and validated 2026-10-04.

## Outcome

A user can reach and navigate monthly planning without surprising calendar
controls or losing an unsaved intention.

## Acceptance Criteria

- Primary navigation names the Calendar and exposes Plan and review.
- Calendar presentation uses Day, Week, and Month labels; Go to today changes
  the selected date to the local current day.
- Arrow keys move within the calendar; Tab exits normally in DOM order.
- Unsaved monthly plan values are preserved as an explicitly unsaved draft or
  protected by a navigation warning.
- Monthly planning remains reachable on narrow viewports without traversing
  the entire month grid.

## Validation

Client tests and keyboard/browser checks cover date movement, focus order,
navigation discovery, and unsaved values. No ledger records change merely
because presentation or navigation changes.

Real Firefox checks verified normal Tab exit without changing the selected date
and dismissed an unsaved-plan navigation warning without losing the edited
value. The existing desktop/mobile workspace journey also passed after updating
its expected labels, navigation count, and account-history loading checks.
