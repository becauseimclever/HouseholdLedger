# Delivery Plan

## Purpose and Status

Break the [Global Requirements](../requirements.md) into independently testable
outcomes while keeping the user's easy intend-record-reflect loop central.
This is the delivery baseline, not a blanket claim of readiness. B01-B04 now
have local implementation and executable evidence described below; later
slices remain planned. Existing behavior should be verified and reused rather
than rebuilt.

The first requirements-gathering round closed on October 15, 2026. The
[remaining feature-level decisions](../requirements.md#decisions-to-resolve-within-delivery-slices)
must be resolved before their affected behaviors are enabled, not through
another mandatory global planning round. Local implementation of B01-B04 does
not by itself establish the B04 deployable baseline.

Slice identifiers are planning references, not feature numbers. Create or
update a focused feature specification when a slice starts and needs one,
using the [Feature Index](../features/README.md) for numbering. Do not create
dozens of speculative feature documents in advance.

The stages express priority, not blanket dependencies. The prerequisite column
lists actual enabling outcomes. A slice can be divided into smaller safe PRs;
split again when its acceptance outcome cannot be reviewed or tested clearly.
No stage requires a long-lived branch or a separate approval ceremony.

## First Deployable Baseline

The first milestone is a securely accessible, persistent self-hosted deployment
of the existing usable calendar workflow, not completion of all requirements.
Until verified, do not describe the current application as this baseline.

| Slice | Observable outcome and requirements | Prerequisites | Acceptance evidence |
| --- | --- | --- | --- |
| B01: PR validation | Contributors have a repeatable build/test gate and PR-only trunk workflow. DELIVERY-001 through DELIVERY-004. | None | Run build and resource-free tests in CI; exercise a deliberately failing check; verify repository rules require PRs and applicable checks. Configure remote rules only with explicit authorization. |
| B02: Persistent container host | The existing API, WASM frontend, and PostgreSQL run through a documented Docker deployment on ARM64 with explicit migrations. HOST-002, UPGRADE-001 through UPGRADE-004. | None | Fresh deployment, readiness, persisted record after container replacement, real PostgreSQL migration test, and clean failure on invalid configuration. Verify on Pi 4 2 GB; state separately which services ran there. |
| B03: Authorized household access | The one configured owner signs in with Authentik and other identities cannot access ledger data. AUTH-001 through AUTH-004. | None | Backend authentication/authorization tests plus an actual OIDC login journey. Anonymous and unauthorized identities cannot read or mutate protected data. No browser-delivered secrets. |
| B04: Baseline deployment proof | The owner can sign in, record and correct activity in the existing calendar, refresh, and see persisted results in the supported deployment. DELIVERY-002, TEST-006. | B01, B02, B03 | End-to-end deployment smoke on the assembled artifact, existing journey regression results, migration/readiness evidence, and documented operator backup/recovery steps. Record remaining requirement gaps honestly. |

B04 establishes the deployability floor. Every subsequent PR must retain it.
Before using real household data, the operator must have a documented,
verified PostgreSQL backup/restore procedure; complete application portability
arrives separately below. This does not add managed backup infrastructure.

The minimum-host topology, supported browser version matrix, and realistic
data-volume/performance targets must be settled and recorded during the
relevant slices. Do not assume that Authentik, PostgreSQL, and the application
all fit on the minimum host without evidence.

### Implementation and Evidence

| Slice | Local implementation | Acceptance still outstanding |
| --- | --- | --- |
| B01 | PR workflow, locked restore/build, resource-free suites, contract comparison, provider/container checks, published HTTP smoke, successful remote baseline CI, and verified required PR/check protection with a failed-check probe | None for the bootstrap governance slice; subsequent code changes still require their own PR checks |
| B02 | Non-root AMD64/ARM64 image, PostgreSQL/key volumes, secret-file configuration, explicit migration/readiness, replacement and dump/restore smoke | Physical Pi 4 2 GB qualification and recorded service topology |
| B03 | Owner-only OIDC code/PKCE session, issuer/subject authorization, antiforgery, sign-out, signed-token middleware and component tests; user-reported real Authentik owner login, unauthorized-user rejection, and logout locally; post-logout recovery notice observed in shared browser | Complete production browser acceptance; a fresh sign-in round trip from the recovery link has not been independently observed |
| B04 | Container persistence/restore proof, production HTTP boundary smoke, existing workflow browser regression, and verified B01 remote controls | Combined authenticated production calendar journey, minimum-host/browser qualification, and operator restore proof |

See [Self-Hosted Deployment](deployment.md) for runnable operations and exact
evidence boundaries. The browser workflow regression deliberately runs in
Development without OIDC; it is not production authentication evidence. Until
the remaining acceptance is recorded, B04 remains **unverified**, and no
deployability floor or full requirements implementation is claimed.

Local verification for this implementation:

| Check | Result |
| --- | --- |
| Release build and scoped C# formatting | Passed; build has zero warnings/errors |
| Resource-free PR gate | 308 passed: 37 Domain, 33 Application, 11 Contracts, 169 Client, 58 API |
| Isolated PostgreSQL provider/migration tests | 9 passed, none skipped |
| Published Production HTTP boundary | 1 passed |
| Published HTTP smoke and Firefox browser journeys | All four cases passed across two disposable database runs: HTTP/workspace/notices together (3), then fresh setup alone (1) |
| AMD64 and emulated ARM64 container smoke | Both passed migration, strict-host readiness, owner gate, replacement persistence, and dump/restore |
| OpenAPI comparison, locked restore, resolved-package vulnerability check, Compose structure | Passed; no reported vulnerable resolved packages |
| Open-source notices browser journey | Passed after repairing its database configuration, stale selectors, settled-layout wait, and shared-output cleanup |

On October 16, 2026, remote baseline
[CI run 38845674849](https://github.com/becauseimclever/HouseholdLedger/actions/runs/38845674849)
passed both jobs for bootstrap commit `a67822e`. Authorized classic protection
on `main` now requires PRs, both named GitHub Actions checks, an up-to-date
branch, and resolved conversations, including for administrators. Force pushes
and branch deletion are disabled; required approvals are zero for the solo
maintainer. Ready-for-review, conflict-free
[probe PR #2](https://github.com/becauseimclever/HouseholdLedger/pull/2)
intentionally failed `Build and resource-free tests` while the PostgreSQL check
passed in [run 38853034049](https://github.com/becauseimclever/HouseholdLedger/actions/runs/38853034049).
GitHub reported its merge state as `BLOCKED`. The PR was closed without merging
and its temporary remote branch was deleted. The later local recovery and test
repairs are not included in that remote baseline CI result.

On October 15, 2026, the user reported successful real Authentik owner login
and calendar access in the local Development host, followed by creation of a
calendar entry that remained visible after a page refresh. This is manual,
user-reported local acceptance evidence, not an independently observed browser
test or production deployment proof. The user subsequently reported that an
unauthorized user could not access the application, corrections persisted after
refresh, and logout worked. Attempting to view the application after logout
reportedly showed an application error. Supplied server logs show protected API
requests correctly returning 401, not a server-side 500. The frontend now offers
an explicit full-document sign-in link on 401/403 without replaying requests or
automatically discarding pending work; initial unauthorized settings loads do
not mount protected pages. The updated Client suite passed 169 tests and the
hosted Release build passed with zero warnings/errors. The user subsequently
reported validation complete, and inspection of the shared local browser showed
the expected "Sign-in required" alert, signed-out explanation, no-replay warning,
and `/auth/login` link instead of a loading screen or application-error banner.
Earlier console errors remain in the browser's event history; this snapshot
does not establish a clean navigation history or independently verify a fresh
sign-in round trip through that link. Production deployment acceptance is deferred
at the user's direction; it is not waived or considered passed. No financial
entry details are retained in this record.

The repaired notices journey uses the runner's disposable database, an isolated
child-process environment, current semantic navigation assertions, and a wait
for the calendar layout before measuring it. Individual cases no longer delete
the runner-owned shared output root. A combined four-case run still failed
because the fresh-setup journey requires an empty database and the workspace
journey leaves accounts behind. All four cases subsequently passed using the
documented split in [Fresh Setup Regression](../../tests/HouseholdLedger.EndToEndTests/FreshSetupRegression.md):
three shared-safe cases together, then fresh setup in a separate database.
This is not a claim of order-independent execution against one shared database.

## Reliable Everyday Recording

| Slice | Observable outcome and requirements | Prerequisites | Acceptance evidence |
| --- | --- | --- | --- |
| L01: Fixed currency setup | The owner confirms currency once before recording money. MONEY-002 through MONEY-005. | B03 | Unit tests for amount precision and rounding; API rejects later currency changes and writes without setup; keyboard setup journey. Existing-data transition requires an explicit decision before migration. |
| L02: Financial dates and presentation | Entry accepts transaction date or posting date, uses the specified fallback, stores dates/times in UTC, and respects time-zone and locale preferences. DISPLAY-001 through DISPLAY-009. | None | Date precedence, missing-date rejection, UTC storage/input/display round trips, date-only source fidelity, daylight-saving and opening-day boundaries, creation timestamp, and format-preview tests; posting-date-only keyboard journey. |
| L03: Initial account balance | The owner opens an account with a balance, including zero, effective at start-of-day. LEDGER-001 through LEDGER-003, LEDGER-011, LEDGER-012. | L01, L02 | Exact debit/credit results by supported account type; pre-opening history excluded and same-day activity included; negative balances accepted; persistence and calendar journey. |
| L04: Traceable safe edits | A record can be edited without overwriting another tab's changes, and changes have authenticated field history. WRITE-001, WRITE-002, AUDIT-001 through AUDIT-005. | B03 | PostgreSQL competing-write test with one conflict; atomic mutation/audit rollback test; actor attribution and pending-input preservation tests. Apply the shared safeguards to every existing mutable surface before marking complete. |
| L05: Trustworthy connection recovery | Reconnecting establishes the exact attempted action's outcome without duplicate writes. WRITE-003 through WRITE-007. | L04 | Lost-response-after-commit, failure-before-commit, pending/unknown status, and bounded-retry tests; recovery UI offers action-specific next steps. |
| L06: Household language and labels | The owner records money in/out without accounting expertise and can label activity. LEDGER-004, UX-005 through UX-007. | L03, L04 | Plain-language entry maps to correct account-type accounting; optional labels persist; keyboard entry and correction require no advanced setup. |
| L07: Transfers | Money moves between accounts without changing household income or spending. LEDGER-009. | L03, L04 | Exact paired balance changes, no duplicated effect, no income/spending inclusion, atomic failure behavior, and a complete transfer journey. |
| L08: Archived accounts | Confirmed account removal archives its transactions without erasing history. LEDGER-006, CHANGE-004. | L04 | Cancel changes nothing; confirmed removal marks account and transactions deleted; active totals and explicit history distinguish archived data; audit persists. Specify historical report scope explicitly. |
| L09: Last-action undo | The last supported reversible action can be undone once without overwriting newer data. CHANGE-001 through CHANGE-003. | L04, L08 | Begin with a defined ordinary transaction action; verify inverse balance effect, concurrency rejection, new audit event, invalidation, and no redo. State unsupported actions and availability lifetime. |
| L10: Permanent deletion | The owner can permanently remove an account after fresh reauthentication. LEDGER-007, LEDGER-008, AUDIT-006. | L08, B03 | Confirmation alone or stale authentication fails; successful reauthentication permits deletion; live records disappear, audit survives, and undo is unavailable. |

L04 can introduce one audited, concurrency-safe edit first, then extend the
same mechanism through separate PRs. Other slices may depend on the relevant
covered operation, but must not claim repository-wide coverage prematurely.

## A Complete Kakeibo Interval

| Slice | Observable outcome and requirements | Prerequisites | Acceptance evidence |
| --- | --- | --- | --- |
| K01: Personal interval | The owner chooses cadence and start date and sees current and next periods. PHIL-002, PHIL-003, PERIOD-001 through PERIOD-003. | L02, L04 | Daily, weekly, monthly, and anchored two-week boundaries; interval transition preserves historical plans and reflections. Specify gap/overlap handling before implementation. |
| K02: Flexible classification | Activity uses the four top-level classifications with optional subcategories and an honest Unclassified state. CLASS-001, CLASS-004. | L06 | Custom subcategory persistence, no hidden unclassified amounts, no category split, and quick correction tests. |
| K03: Next-interval intention | The owner makes a partial next-period plan and compares relevant actuals separately from account balances. MONEY-007, PHIL-009, PHIL-011. | K01, K02 | Exact allocation/remainder tests; partial plans allowed; saving plans never changes account balances; classifications and account reconciliation show explainable differences. |
| K04: Brief reflection | The owner reflects on the current period and consults that reflection while planning the next. PHIL-010, PHIL-011. | K01, K03 | Optional prompts, incomplete-plan independence, saved reflection persistence, and navigation to the next plan without automatic allocation changes. |
| K05: Gentle attention | Overspending, unallocated funds, incomplete plans, and negative balances receive actionable, dismissible notices. PHIL-007, PHIL-008. | K03, L03 | Dismissal survives refresh; unchanged conditions do not repeat; accepted buffers remain valid; new-period/material-change rules are tested; keyboard dismissal works. |
| K06: Independent income expectations | Separate jobs have independent schedules that do not create actual money. PERIOD-004, PERIOD-006. | K01, L04 | Different pay cycles coexist; expected events never change balances or reflection boundaries. |
| K07: Realized paycheck distribution | One paycheck is allocated across accounts without counting income twice. PERIOD-005. | K06, L03, L04 | Allocation sums reconcile; realized account entries and household income count once; retry/conflict tests. Specify deletion of an allocated account before enabling it. |
| K08: Refunds and reversals | A new related transaction changes balances and applies the agreed same-interval spending rule. LEDGER-010, LEDGER-013. | L03, L04, K01, K03 | Same-interval versus cross-interval cases, transaction/posting fallback, opening cutoff, preserved original, and report-range independence. Define unresolved-original handling explicitly. |

## Portability and Banking Imports

| Slice | Observable outcome and requirements | Prerequisites | Acceptance evidence |
| --- | --- | --- | --- |
| D01: Complete dataset export | The owner downloads a versioned dataset containing all implemented household data, without secrets. DATA-001, DATA-004. | L04 | Export completeness and consistent snapshot tests, authorization denial, secret exclusion, and documented format. Extend coverage whenever new persisted concepts arrive. |
| D02: Dataset restoration | A valid exported dataset restores its meaning without partial replacement or currency relabeling. DATA-002, DATA-003. | D01 | Round-trip comparison including audit, deleted records, identities and dates; incompatible/corrupt import leaves data unchanged. Decide replacement/merge policy before implementation. |
| D03: Export reminder | The owner can configure, acknowledge, or defer a non-blocking export reminder. DATA-005. | D01 | Deterministic schedule/dismissal tests; reminder never claims a backup was verified. |
| I01: CSV interpretation preview | A bank file produces editable parsing and mapping suggestions without writing transactions. IMPORT-001 through IMPORT-003. | L01, L02 | Fixtures for differing bank layouts, date/number ambiguity, debit/credit mappings, and explicit destination/mapping confirmation. |
| I02: Duplicate-safe CSV commit | Reviewed rows become valid account transactions with clear outcomes. IMPORT-004 through IMPORT-008. | I01, L03, L04, L05, K02, L07, K08 | Repeated/overlapping files, concurrency, manual-entry matches, transfer/refund interpretation, legitimate repeated amounts, and row failures. Specify batch atomicity and file/staging limits before implementation. |
| I03: Reusable import profile | A saved bank configuration reduces repeated setup while keeping preview/edit available. IMPORT-002, IMPORT-003. | I02 | Profile reuse on changed headers/layout detects incompatibility rather than silently misparsing. |
| I04: Bulk correction | The owner filters and corrects imported or manual records together. CLASS-002, CLASS-003. | I02 | Suggested/Unclassified review, affected-record preview, conflicts, exact balance effects, audit, and honest partial/all-or-nothing outcomes under a specified batch policy. |

## Understanding and Refinement

| Slice | Observable outcome and requirements | Prerequisites | Acceptance evidence |
| --- | --- | --- | --- |
| R01: Current-period report | The owner understands intended versus actual activity for the reflection interval, with tabular details. REPORT-001, REPORT-002, REPORT-007 through REPORT-010. | K03, K08 | Exact totals, visible cross-period refunds, Unclassified inclusion, and plan/account distinction; empty and incomplete states remain honest. |
| R02: Filtering and drill-down | The owner changes report ranges and opens contributing records. REPORT-003, REPORT-004. | R01 | Month/year-to-date and custom boundaries, preserved filters, totals/detail reconciliation, and keyboard return navigation. |
| R03: Linked chart | One useful line or bar visualization answers a defined question with linked legend focus. REPORT-005 through REPORT-007. | R02 | Legend/data highlighting by keyboard and pointer; equivalent table, theme contrast, and correct data. Add other chart families as separate justified slices, not one chart-suite PR. |
| R04: Explainable prediction | One explicitly defined estimate helps the next plan without becoming an actual. PHIL-004. | K03, K06, R01 | Known-input results, visible assumptions/uncertainty, and zero actual balance or transaction effects. Choose the first prediction question before implementation. |
| U01: System-following themes | Light and dark defaults follow the system and explicit selection overrides it. THEME-001 through THEME-006. | None | System-change/override/refresh tests, all surface states and contrast, plus theme-extension instructions. |
| U02: Supported-client qualification | The core loop works on the dated browser/platform support matrix. BROWSER-001, BROWSER-002, A11Y-001 through A11Y-007. | B04, K04 | Publisher-verified version matrix; real browser journeys, manual keyboard and assistive-technology checks, zoom/reflow and narrow-layout checks; record gaps explicitly. |
| U03: Measured core responsiveness | Core tasks meet agreed user-observed timing targets on representative data and minimum hardware. PHIL-012, UX-008 through UX-010. | B04, K04 | Record workload, topology, targets, input/navigation/save delays, and memory usage; verify no misleading optimistic financial success. Narrow measured improvements take priority over a general benchmark framework. |
| O01: Compatible upgrade proof | A prior deployed dataset upgrades without breaking retained API clients. API-001 through API-004, UPGRADE-001 through UPGRADE-004. | B04 | Real PostgreSQL upgrade fixture and retained-contract checks, image replacement/restart test, deprecation guidance and explicit downgrade limitations. Repeat whenever affected, not only once. |

## Cross-Cutting Acceptance for Every Slice

- Preserve the first deployable baseline once B04 is verified; never knowingly
  merge a broken trunk to be repaired in a follow-up PR.
- Reference applicable global requirement IDs, not only those listed in a row.
  PHIL, UI, DESIGN, TDD, accessibility, and privacy rules apply throughout.
- Start with a failing behavioral test, implement, then refactor. Prefer exact
  unit assertions; add component and real-boundary tests where needed and a
  small representative full-system journey for changed critical workflows.
- Keep authoritative financial rules in the backend. Validate amounts, dates,
  concurrency, authorization, and audit effects at the actual boundary.
- Keep everyday language simple, optional detail out of routine entry, and
  errors actionable. Check complete keyboard workflows, not only isolated
  controls. U02/U03 consolidate evidence, not permission to defer basic
  accessibility or responsiveness until the end.
- Preserve existing behavior through additive migrations and compatible API
  evolution. Any existing-data transformation needs its explicit decision,
  tested upgrade path, and operator instructions before activation.
- Report unavailable browser, hardware, identity-provider, or PostgreSQL checks
  as unverified, not passed. Record completion evidence in the feature or PR;
  do not require a separate audit document for ordinary work.

## Immediate Next Step

Begin B01 by inspecting the current build/test commands and creating the
smallest useful PR validation workflow. No workflow directory was present when
this plan was written; repository protection settings have not been inspected
or changed. B02 and B03 are independent implementation opportunities, not
reasons to create multiple simultaneous branches or agents.
