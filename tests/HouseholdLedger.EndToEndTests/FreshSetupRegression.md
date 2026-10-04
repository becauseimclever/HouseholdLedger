# Fresh setup browser regression

Run from the repository root:

```powershell
& tests\HouseholdLedger.EndToEndTests\Run-FreshSetupBrowserRegression.ps1
```

This is one system/end-to-end test, not a unit or component test: it uses the
published ASP.NET host, real pinned Firefox/geckodriver W3C sessions, and
PostgreSQL 18. It proves a representative cross-component setup journey rather
than duplicating exhaustive validation cases from lower-layer tests.

The dedicated runner creates a uniquely named Docker container, a fresh database,
and a dynamically assigned loopback database port. It migrates that database
through the environment-only EF design factory and delegates fresh temporary
publish/profile handling to the existing browser runner. The test host runs in
Production to exclude User Secrets. All data creation occurs through UI forms;
there is no API seeding. The isolated-run marker is required, so do not invoke
this fact with an application database connection. Containers and their volumes,
browser/API/driver processes, profiles, and temporary publish output are cleaned
on success and failure. Existing installed packages are used with `--no-restore`.

Prerequisites: Docker with the approved `postgres:18` image, the existing EF tool,
trusted ASP.NET HTTPS development certificate, and approved local Firefox
153.0.1/geckodriver 0.37.1 binaries in the paths used by
`Run-BrowserEndToEndTests.ps1`. No additional browser/test packages are required.
The generic browser runner still defaults to the original workspace journey;
use the dedicated runner for this fresh-database journey.

To validate the existing desktop/mobile workspace journey with the same isolated
database lifecycle:

```powershell
& tests\HouseholdLedger.EndToEndTests\Run-FreshSetupBrowserRegression.ps1 `
  -TestFilter "FullyQualifiedName~ApiHostRendersAccessibleDesktopCalendarWorkspace"
```

## Covered behavior

- UI creation and full-document reload persistence of BoA Checking, CapOne
  Checking, UHCU Checking, BoA Savings, and UHCU Savings.
- UI biweekly schedule anchored in a prior month, net $2,750, allocated
  $1,000/$1,500/$250 to the three checking accounts. Persisted allocations are
  compared by destination rather than incidental repository ordering.
- Next-month day 9 and 23 expectations with no confirmed receipts; day 8 has no
  expectation. The expectation-month review shows zero actual income.
- Invalid monthly reconciliation ($501 unexpected) disables save and leaves no
  persisted plan. Valid current-month plan: expected $5,500, savings $500, necessities
  $3,500, optional $750, culture $250, unexpected $500. Intended savings is a
  plan target; both savings accounts still have no actual activity.
- Reloaded review shows all planned amounts, zero actual expenses, corresponding
  negative variances, and zero actual income.
- Explicit UI confirmation of the past anchor-date income with the three checking
  destinations. Reloaded receipt-month review shows $2,750 actual income and
  unallocated remainder; the calendar shows exactly one confirmed receipt.
  The current plan month remains at zero actual income with its saved plan.
- Both savings accounts have no transaction activity; neither schedule nor
  receipt allocates money to savings.
- UI entry of the authorized $82.45 Necessities expense on the current month's
  first day against BoA
  Checking, followed by full-document reload checks of inspector, calendar
  total, and the account-history date/amount/classification row. Current-month review
  retains the plan and shows necessities planned $3,500, actual $82.45, variance
  -$3,417.55, actual income $0, and unallocated remainder clamped to $0.

## Durable relative dates

The test captures local today once and derives the current month, next month,
and next-month paydays 9 and 23. The anchor is 42 days before the first expected
payday, always in a prior month and already past even when today is the first
of a month. The confirmed receipt uses that anchor date. The plan and expense
use the current month, with expense dated its first day (never future).
This preserves the original amounts and semantics without relying on the
September/October 2026 dates or changing the production clock.

Calendar navigation, inspector labels, form dates, receipt/review links, and
account-history dates all use these calculated dates. `AddMonths` and `AddDays`
handle month lengths, leap years, and year rollover. Future expectations stay
visible despite the production inspector intentionally hiding past expectations;
historical confirmation remains entirely manual, without schedule association.
There is no expiring execution window or production clock override.

## Validation

Audit follow-up validated October 4, 2026: **1 passed, 0 failed, 0 skipped**
in 19 seconds (isolated runner 29.48 seconds). Added explicit receipt correction
to $2,751, reload, confirmed removal, and monthly-income refresh to zero; expense
description persistence; signed -$82.45 cashflow; saved/reloaded reflection;
normal Tab exit; and cancellation of unsaved-plan abandonment. The existing
desktop/mobile workspace journey separately passed in 7 seconds (runner
17.21 seconds). All owned containers and temporary process/publish resources
were removed. Neither run touched the manual-development database.

Initial follow-up runs exposed outdated test selectors and an asynchronous
deletion assertion that navigated before completion. Assertions now use the
new presentation labels and wait for authoritative UI state before proceeding.

Validated October 4, 2026: **1 passed, 0 failed, 0 skipped**, browser test duration
17 seconds; complete isolated runner 27.29 seconds with relative dates, the
authorized $500 savings/$3,500 necessities plan, and $82.45 expense. This run
used anchor/receipt September 28, 2026, expectations November 9 and 23, 2026,
and an October 1, 2026 expense/current-month plan. Infrastructure build had
zero warnings/errors. The installed EF preview tool warns that it is older than
runtime 10.0.10; migrations nevertheless completed. The initial absent migration
history query is logged as a failed command on a fresh database, then all
migrations apply successfully.

An independent date-arithmetic check passed for all 4,800 months in 2000–2399
(one full Gregorian leap-year cycle): the anchor is prior to the current month,
both expected dates are in the next month, and both are biweekly occurrences.

During development, incorrect test assumptions about creation-only success
status surviving reload, allocation order, and calendar returning to a monthly
page's month were corrected. Past anchor-date expectations were removed from
assertions because they are intentionally hidden; explicit receipt confirmation
still executes. These were harness assumptions, not observed production defects.
