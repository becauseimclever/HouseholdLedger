# Global Requirements

## Baseline Status

**Round 1 requirements baseline completed: October 15, 2026.**

This closes the initial requirements-gathering round and establishes the
product and engineering direction for delivery. It does not mark the
requirements as implemented, establish a deployable release, or authorize
commits, remote repository changes, or data migrations.

Use the [Delivery Plan](development/delivery-plan.md) to implement and verify
small slices. Retain requirement identifiers as the document evolves; record
material changes explicitly and update affected acceptance criteria rather
than silently changing financial meaning. Feature completion requires
observable evidence, not simply a reference to this baseline.

## Purpose and Scope

This living document defines the cross-cutting requirements for HouseholdLedger.
It covers product philosophy, user experience, technology, code organization,
and testing, and will expand as further requirements are agreed. Stable
requirement identifiers allow feature documents and implementation decisions to
reference individual rules.

These requirements apply across features. Feature specifications define their
own user-observable outcomes within these constraints. This document describes
required standards, not a claim that all existing code has been audited against
them. The [Architecture Overview](architecture/overview.md) describes the current
implementation; [Testing](development/testing.md) provides execution guidance.

Newly agreed requirements take precedence over older feature decisions where
they differ. Historical feature documents retain their original scope and
evidence; adding a requirement here does not implement or verify the change.
In particular, flexible reflection intervals, a system-following default theme,
and balance-tracking accounts with a fixed currency selection are required
direction, not claims about the currently shipped behavior. Balance tracking
supersedes the earlier account-as-destination-only scope; fixed currency
supersedes the older changeable display-currency behavior. Transitioning existing
data and settings requires a separate implementation decision; this document
does not authorize a data migration.

## Product Philosophy and Kakeibo

HouseholdLedger exists to make the Kakeibo method of personal budgeting easy to
follow. Its core loop is to plan with intention, record what actually happened,
and reflect. The system must adapt to the user's household and rhythm rather
than require the user to adopt a rigid budgeting workflow.

The core experience is easy entry, tracking through the user's intervals,
reflection on what happened, and intentional planning for the next interval.
Accounting correctness and technical complexity are the developers'
responsibility, not prerequisites the user must learn to participate.

| ID | Requirement |
| --- | --- |
| PHIL-001 | Support intentional planning, straightforward recording, and meaningful reflection as connected parts of Kakeibo practice. Keep the user in control of financial decisions rather than prescribing goals or judging spending choices. |
| PHIL-002 | Support flexible reflection intervals, including daily, weekly, monthly, and user-chosen periods. Users must be able to reflect over two-week intervals aligned with their pay period rather than being restricted to calendar months or a fixed weekday boundary. |
| PHIL-003 | Reflection must not require a completed plan. Planning targets the next interval while reflection concerns the current interval, as defined under Periods and Income Expectations. Pay schedules and the displayed calendar view must not dictate reflection cadence. |
| PHIL-004 | Support future planning and predictions to help users consider expected income, expenses, and outcomes. Clearly distinguish plans, estimates, and predictions from recorded actuals, and make the assumptions and uncertainty behind predictions understandable. Predictions must not silently create transactions or present expected income or savings as achieved. |
| PHIL-005 | Make adding, correcting, and removing transactions, and managing multiple accounts, as straightforward as possible. Minimize unnecessary steps and preserve clear feedback and safeguards against unintended changes or data loss. |
| PHIL-006 | Keep a simple calendar-based interface at the center of the experience. Dates, recorded activity, future expectations, and the selected period must be easy to understand without surrounding controls overwhelming the ledger. |
| PHIL-007 | Present overspending, unallocated funds, and incomplete plans explicitly as those conditions, with a clear notification inviting attention and a relevant path to review them. Addressing a condition may mean correcting it or consciously accepting it, such as retaining an unallocated buffer or acknowledging an unexpected expense. Do not require users to change valid figures merely to clear a warning; use neutral language rather than judgment or manufactured financial conclusions. |
| PHIL-008 | Make these attention notifications dismissible and non-blocking. Remember dismissal for the same condition and period across routine navigation and refreshes; do not repeatedly interrupt the user about an unchanged condition. Dismissal must not alter the underlying financial state, which remains visible in the relevant plan or report. Define meaningful change and new-period rules before resurfacing a dismissed notification. |
| PHIL-009 | Optimize for sustainable awareness and intentional household decisions, not perfect categorization, exhaustive planning, or maximizing savings. Accounts, imports, predictions, reports, and technical safeguards must support the intend-record-reflect loop rather than become prerequisites for it. |
| PHIL-010 | Help users reflect on what worked, what surprised them, and what they want to try next through brief, optional prompts. Make previous reflections easy to consult while planning the next interval, without automatically changing allocations, prescribing goals, or requiring a questionnaire or score. |
| PHIL-011 | Allow users to record actual activity and reflect when plans, classifications, or earlier reviews are incomplete. Support partial progress and later correction without creating a backlog of mandatory budgeting chores; required financial validation, authorization, and destructive-action safeguards still apply. |
| PHIL-012 | Evaluate the experience "through the client's eyes": prioritize what the household user can understand, accomplish, and trust over internal technical convenience. The application must feel polished and responsive while remaining financially accurate; visual polish, speed, and correctness are complementary requirements, not substitutes for one another. |

These requirements establish direction, not a complete specification for
prediction models or reflection scheduling. Each implementation should remain a
small, observable feature within these principles.

## Accounts and Transactions

| ID | Requirement |
| --- | --- |
| LEDGER-001 | Accounts must track balances. Every account must have an explicit initial balance, including zero, effective at midnight at the start of its initial-balance date in the configured household time zone, before any transaction on that date is applied. |
| LEDGER-002 | Represent transactions using an amount and debit or credit in standard accounting terminology, not as universal synonyms for money out and money in. Debits increase asset and expense accounts and decrease liability, equity, and income accounts; credits have the opposite effect. Define the supported account types, balance presentation, and bank-source mappings accordingly, and apply them consistently across manual entry, imports, calculations, and reports. |
| LEDGER-003 | Calculate account balances from the initial balance and realized transaction effects under the documented debit/credit convention. Creating, correcting, deleting, or undoing a realized transaction must update the resulting balance consistently without double-counting the initial balance. |
| LEDGER-004 | Allow users to create and apply custom transaction labels to further distinguish activity without changing debit/credit direction or creating implicit balance effects. |
| LEDGER-005 | Do not support splitting one transaction across multiple classifications or accounts in the current scope. Future split-transaction support requires a separate design. Distributing one paycheck as separate account transactions is explicitly supported under Periods and Income Expectations. |
| LEDGER-006 | Normal account removal must soft-delete the account and archive and mark all its transactions as deleted, preserving their stored data and audit history. Exclude deleted accounts and transactions from normal active-ledger views and totals, and identify them clearly in any explicit historical or archived view. This operation is destructive and requires confirmation. |
| LEDGER-007 | Offer explicit hard deletion of an account and its associated transactions only after confirmation and successful reauthentication of the authorized user. Enforce the requirement in the backend using verified fresh identity-provider authentication, not merely the existing application session or a confirmation checkbox. Hard deletion is not undoable. |
| LEDGER-008 | Before hard deletion, explain that live account and transaction records will be permanently removed but their detailed audit history will remain. Previously exported files and user-managed backups are not erased. Preserve audit references without retaining live records solely to satisfy foreign-key relationships. |
| LEDGER-009 | Explicitly identify and label transfers as transfers. Transfers affect the relevant account balances but must not count as household spending or income. Link corresponding entries when both accounts are tracked, including imported entries, so matching or recording the other side does not duplicate the financial effect. A transfer is a distinct supported operation, not general split-transaction support. |
| LEDGER-010 | Record refunds and reversals as new transactions with their own financial dates and balance effects, retaining the original transactions and linking related records where identifiable. Do not implement a refund or reversal by silently editing or deleting the original transaction or rewriting its historical period. Apply the interval-based classification and planning treatment in LEDGER-013. |
| LEDGER-011 | Retain transactions whose authoritative financial date precedes the account's initial-balance date as historical records only; exclude them from account-balance computation because the initial balance is the starting point. Include realized transactions dated on or after the initial-balance date, including transactions at midnight on that date, after applying the initial balance. Show the historical-only distinction clearly and preserve dates and audit history. |
| LEDGER-012 | Allow negative account balances rather than rejecting them as invalid data. Present them accurately with neutral, actionable guidance encouraging healthy finances; any attention notifications must follow the dismissible, non-pestering notification requirements. |
| LEDGER-013 | A refund or reversal adjusts spending classifications and the corresponding plan's actual-spending comparison only when its authoritative financial date falls in the same reflection interval as the original transaction's authoritative financial date. If it falls in a different interval, it must not affect spending classifications or the current plan, nor retrospectively revise the original interval. In either case, apply its account-balance effect subject to the initial-balance date boundary. Use preserved interval boundaries, not a report's selected date range, to determine whether the intervals match. |

Standard debit/credit terminology does not by itself require a full
double-entry general ledger or bank-statement reconciliation workflow.
Transfer matching and refund linkage still require focused specifications;
custom labels alone must not be assumed to prevent misleading financial totals.

## Periods and Income Expectations

| ID | Requirement |
| --- | --- |
| PERIOD-001 | Use a user-configured interval with an explicit start date. Reflection concerns the current interval; planning targets the immediately following interval of the same cadence, offset by one interval. Make both periods and their boundaries visible rather than treating planning and reflection as the same period. |
| PERIOD-002 | Reports must default to the current reflection interval. Calendar display mode and independent income schedules must not silently replace that default. Users may select other reporting ranges explicitly. |
| PERIOD-003 | When interval preferences change, start a new interval schedule with a new explicit start date. Preserve historical boundaries, plans, reflections, and transaction dates; do not retrospectively rebucket or rewrite historical data. |
| PERIOD-004 | Support multiple jobs or income sources with independent pay schedules and pay periods. Income periods need not match each other or the reflection interval. |
| PERIOD-005 | Allow one paycheck to be distributed across multiple accounts as separately recorded account transactions using standard accounting debit/credit treatment for each account type. Link the allocations to their paycheck, reconcile their sum to its realized total, and count the paycheck only once in household income. This is paycheck distribution, not general split-transaction support. |
| PERIOD-006 | Recurring income and expense expectations are planning aids only. They must not affect account balances or recorded actuals until corresponding transactions are explicitly added and realized. A projected paycheck allocation is not a second set of realized transactions. |

## Kakeibo Classification and Correction

| ID | Requirement |
| --- | --- |
| CLASS-001 | Retain Necessities, Optional, Culture, and Unexpected as the top-level Kakeibo spending classifications. Allow user-defined subcategories beneath those classifications without replacing their top-level meaning. |
| CLASS-002 | Make educated classification suggestions for imported transactions and allow user review and correction. When a classification cannot be established confidently, use an explicit Unclassified catchall rather than forcing an unsupported Kakeibo classification. Unclassified is a review state, not a fifth Kakeibo spending principle. |
| CLASS-003 | Provide a dedicated data-correction interface supporting bulk edits of transaction records, including classification and subcategory corrections. Show affected records and intended changes, validate each change, enforce concurrency and audit requirements, and report conflicts and failures explicitly. |
| CLASS-004 | Keep unclassified activity visible in totals and reports so incomplete categorization never hides recorded money. Labels and subcategories organize records; they must not silently change transaction amounts or balance effects. |

## Household Ownership and Self-Hosting

| ID | Requirement |
| --- | --- |
| HOST-001 | The initial application serves one household with one authorized user, normally the head of household managing its ledger. Multiple users, household membership management, and multi-tenant hosting are outside the current scope. |
| HOST-002 | Support user-operated hosting within a personal network or homelab using Docker, with a Raspberry Pi 4 with 2 GB RAM running a supported ARM64 operating system as the minimum hosting hardware. Document persistent storage, configuration, startup requirements, and the deployment topology used to validate that minimum; do not assume cloud hosting or desktop-class hardware. |
| HOST-003 | Keep household records and application persistence under the self-hosting user's control. The user is responsible for operating the deployment and protecting its persistent storage and backups. |
| HOST-004 | Keep user identity explicit in persisted change history so future multi-user support is possible without treating all historical actions as anonymous. This does not require implementing roles, sharing, or multiple active users now. |

## Browser Compatibility

| ID | Requirement |
| --- | --- |
| BROWSER-001 | Support Google Chrome, Microsoft Edge, Mozilla Firefox, Opera, and Apple Safari across their supported desktop and mobile platforms, with the latest stable versions available on October 15, 2026 as the initial compatibility baseline. Include Blink, Gecko, and WebKit; support is not restricted to WebKit. Beta, developer, nightly, and technology-preview releases are not baseline requirements. |
| BROWSER-002 | Maintain a documented browser and operating-system support matrix recording the exact stable versions available on the baseline date for each supported platform, verified against publisher release information. Verify representative complete workflows, keyboard operation, accessibility, and themes across the supported browsers and engines; a pass in one browser alone does not establish compatibility with the others. |

The browser requirement describes the user's frontend environment, not a
requirement to run a browser on the Raspberry Pi host. Existing Firefox browser
journeys provide Gecko evidence, not proof of Blink or WebKit compatibility.
The dated baseline states required support, not completed browser verification;
later baseline revisions should be recorded explicitly.

## API Compatibility and Lifecycle

| ID | Requirement |
| --- | --- |
| API-001 | Expose explicit API major versions and maintain the latest major revision and its immediate predecessor concurrently, once a second major exists. A new release must not silently replace or break a retained version's contract for other clients. |
| API-002 | Minor revisions must remain backward compatible. Changes that break supported clients, including incompatible request, response, validation, or behavioral contracts, require a new major revision. Keep documentation, OpenAPI, and client convenience types aligned with each retained version. |
| API-003 | Publish deprecation and retirement guidance with the replacement version, affected capabilities, and client migration instructions before retiring a major. Retire a version only when it falls outside the two-major retention window; document release timing and any notice period rather than assuming a fixed duration from the version count alone. |
| API-004 | Verify compatibility of retained major versions against the current backend and schema. A database upgrade must not silently invalidate a retained API contract or reinterpret its financial results. |

## Deployment and Database Upgrades

| ID | Requirement |
| --- | --- |
| UPGRADE-001 | Manage application database schema changes through checked-in EF Core migrations. Upgrades must be non-breaking for existing data and retained API versions, preserving financial meaning, relationships, audit history, and concurrency safeguards. Use staged additive changes where necessary rather than destructive schema replacement. |
| UPGRADE-002 | Execute migrations through an explicit documented upgrade step, not implicit normal application startup. Validate supported upgrade paths against real PostgreSQL with representative existing data and report migration failures explicitly. |
| UPGRADE-003 | Follow standard Docker deployment patterns: publish versioned images for supported hardware, retain data outside replaceable containers, document configuration and image replacement, and provide application readiness checks and recovery instructions. Replacing a container must not reset household data or settings. |
| UPGRADE-004 | Document pre-upgrade backup/export steps and compatible image/schema combinations, including limitations on reverting an image after migration. Do not imply that container image rollback automatically rolls back a database or makes an incompatible downgrade safe. |

## Authentication and Access

| ID | Requirement |
| --- | --- |
| AUTH-001 | Support external identity providers using OpenID Connect (OIDC) for sign-in on top of OAuth 2.0. Authentik is the preferred initial provider; keep provider configuration separate from ledger business rules so other compatible providers can be supported. |
| AUTH-002 | Restrict access to household data and operations to the configured authorized user. Successful authentication with the identity provider alone must not grant every identity access to the ledger. Enforce authorization in the backend, including imports, exports, reports, undo, and audit history. |
| AUTH-003 | A private network is not a substitute for authentication or transport security. Document secure HTTPS deployment, including reverse-proxy configuration where used, and use framework-supported authentication mechanisms. Never expose provider secrets or credentials in browser-delivered configuration, datasets, or routine logs. |
| AUTH-004 | Alternative clients must use authenticated and authorized backend capabilities rather than bypass protections. Specific mobile or MCP authorization flows remain feature-level decisions, not additional implementations required now. |

OAuth 2.0 governs delegated access; OIDC supplies the identity protocol needed
for login. Supporting a configurable identity provider does not expand the
initial single-user scope.

## Dataset Portability and Backup Responsibility

| ID | Requirement |
| --- | --- |
| DATA-001 | Allow the authorized user to export and import their complete application dataset, not only transaction CSVs. Include accounts and initial balances, transactions including soft-deleted records, labels and subcategories, interval history, income schedules and paycheck allocations, plans, reflections, preferences including currency and locale settings, reusable import configurations, import provenance and duplicate-prevention history, and audit history needed to preserve the dataset's meaning. Exclude credentials, tokens, provider secrets, and deployment-specific secret configuration. |
| DATA-002 | Use a documented, versioned dataset format and validate compatibility, relationships, and financial invariants before importing. Preserve historical identities and timestamps without attributing restored changes to the importing user; separately audit the import itself. Restoring a dataset must not become a backdoor for relabeling or mixing an existing ledger's currency. |
| DATA-003 | Clearly preview the consequences of a dataset import and require confirmation before modifying existing data. Reject invalid or incompatible input explicitly, and prevent a failed import from leaving a partially restored ledger. Exact merge or replacement behavior and schema-version compatibility rules must be specified before implementation. |
| DATA-004 | The user is responsible for making, securing, retaining, and testing their own backups. Document dataset export and restoration and distinguish a portable application dataset from a full deployment or PostgreSQL backup. The application must not imply that downloading an export proves an off-device backup or recoverable deployment exists. |
| DATA-005 | Provide configurable in-application reminders to export data for backup. Allow users to acknowledge or defer reminders without blocking ordinary ledger work; reminders do not perform backups or claim that a backup has been completed. |

Full-dataset portability is distinct from CSV banking imports. Automated backup
storage, cloud synchronization, and managed backup services are not required by
this scope. Export files contain sensitive household information and must be
treated accordingly.

## Concurrency and Retry Safety

| ID | Requirement |
| --- | --- |
| WRITE-001 | Track a backend-maintained last-modified timestamp and an opaque concurrency key for mutable records. The key must change on successful modification; a display timestamp alone is not a concurrency safeguard. Use UTC instants for change timestamps and apply user preferences only when displaying them. |
| WRITE-002 | Require the expected concurrency key for updates and deletes, and atomically compare it at persistence time. Reject stale operations explicitly rather than silently overwriting intervening changes. Provide enough context to refresh and review a conflict without discarding the user's pending input unnecessarily. |
| WRITE-003 | Automatically retry only operations with established idempotent semantics and only for transient failures, using bounded retries and delay. HTTP method alone is not proof that an application operation is safe to retry. Do not automatically retry validation failures, authorization failures, or concurrency conflicts. |
| WRITE-004 | Retrying an idempotent write must not repeat its financial effects or create duplicate success audit entries. Keep the original request identity and concurrency conditions; never resolve a conflict by silently fetching a new key and replaying the user's write. |
| WRITE-005 | When a non-idempotent operation has an uncertain outcome, show that uncertainty and provide a way to check the authoritative state instead of automatically resubmitting or claiming success. Duplicate and retry protections must remain consistent across manual operations, CSV imports, dataset imports, and undo. |
| WRITE-006 | After connection loss, on reconnect query the backend for the authoritative outcome of the last attempted data-changing action using a stable operation identity. If it completed successfully, do not repeat it or show a failure alert; reconcile the displayed state with the confirmed result. If it did not complete, alert the user and offer remediation specific to the attempted action. |
| WRITE-007 | Distinguish completed, failed or not applied, still running, and unknown outcomes. An absent or timed-out status response is not proof of failure. Preserve pending input and operation identity while status is unresolved; provide a clear status and safe next steps without replaying a non-idempotent action. Correlate status with the exact operation rather than inferring success from similar records. |

## Undo and Destructive Actions

| ID | Requirement |
| --- | --- |
| CHANGE-001 | Provide a simple, single-level undo for the user's last successful reversible data-changing action. Clearly identify what will be undone. Do not implement a multi-step history or redo stack in the initial scope. |
| CHANGE-002 | Define and communicate which actions support undo, how long it remains available, and when a later action invalidates it. Do not imply that bulk imports, dataset restoration, or every operation can be undone without an explicit feature-level design. |
| CHANGE-003 | Apply undo through backend business rules, authorization, and concurrency checks. Do not overwrite intervening changes or erase history; an undo is a new, audited compensating action. |
| CHANGE-004 | Require an explicit confirmation speed bump before destructive actions such as deleting records or replacing existing data. Identify the affected data and consequences, make cancel safe and clear, and keep confirmation keyboard-accessible. Undo availability does not remove the confirmation requirement. |

## Audit History

| ID | Requirement |
| --- | --- |
| AUDIT-001 | Record successful data changes with the affected record, action, changed fields and their previous and new values where applicable, UTC timestamp, and authenticated acting user. Cover creates, updates, deletes, imports, settings changes, and undo, not only transaction edits. |
| AUDIT-002 | Identify authenticated actors using a stable provider identity, including issuer and subject, rather than relying only on a mutable display name or email. Preserve attribution even if the account later changes or future multi-user support is added. |
| AUDIT-003 | Persist each successful mutation and its audit evidence atomically. Failed operations must not appear as completed changes. Preserve history of deleted records and link undo or import activity to the changes it affects. |
| AUDIT-004 | Make audit history available to the authorized user, but do not allow ordinary ledger editing or undo to rewrite or remove existing audit entries. This is application change history, not a claim of tamper-proof storage against the self-hosting database administrator. |
| AUDIT-005 | Treat audit data as sensitive financial information. Never record authentication secrets or tokens, and do not copy entire uploaded files into audit entries. Define retention and deletion rules explicitly before implementing history cleanup; deleting a current record must not silently remove its audit history. |
| AUDIT-006 | Retain existing detailed audit entries, including previous and new field values, after both soft deletion and hard deletion of accounts and transactions. Record the deletion action and authenticated actor separately. Hard deletion removes live ledger records, not their audit history; make that distinction explicit to the user. |

## Financial Accuracy and Currency

| ID | Requirement |
| --- | --- |
| MONEY-001 | Financial calculations must be consistent, deterministic, and highly accurate across transaction entry, imports, planning, predictions, calendar summaries, and reports. The same underlying records and filters must produce reconcilable results regardless of the consuming frontend. |
| MONEY-002 | Use decimal arithmetic and explicit decimal database precision and scale for authoritative monetary values. Do not use binary floating-point arithmetic for financial calculations or allow serialization, persistence, or display formatting to silently alter monetary values. |
| MONEY-003 | Define and consistently apply supported amount precision, currency minor units, rounding mode, and rounding points. Preserve sufficient intermediate precision, avoid repeated rounding, and reject unsupported precision or overflow explicitly rather than silently truncating values. |
| MONEY-004 | The user must select and confirm their preferred currency once before monetary records are created, including through import. That currency applies consistently across accounts, transactions, planning, predictions, and reports; changing it is not supported in the current scope. The backend must enforce this restriction, not just hide an editing control. |
| MONEY-005 | Currency is not merely a changeable display label. Do not silently relabel existing monetary values, mix currencies in totals, or perform implicit exchange conversion. Currency changes and multi-currency behavior are deferred until their financial meaning and data-transition rules are deliberately specified. |
| MONEY-006 | Verify monetary behavior with exact expected results, including fractional amounts, rounding boundaries, repeated aggregation, allocation reconciliation where applicable, and supported precision limits. Report totals and drill-down details must reconcile using the same calculation rules. |
| MONEY-007 | Track account balances and planning balances as separate concepts. Account balances represent initial balances and realized account activity; planning balances represent the plan's allocations and remaining amounts under explicitly defined rules. Reconcile plans against relevant realized activity for the same period and scope, explaining differences without assuming that a planning remainder must equal an account balance. Planning changes must not create money or alter account balances. |

## Date and Time Display

| ID | Requirement |
| --- | --- |
| DISPLAY-001 | Allow the user to configure preferred date and time display formats, including date ordering and 12-hour or 24-hour time presentation. Persist the preferences and apply them consistently wherever dates and times are displayed, including calendars, transaction details, import previews, and reports. |
| DISPLAY-002 | Show representative previews so the user can understand the selected formats. Display preferences must not change stored dates, instants, transaction amounts, or the meaning of reporting intervals. |
| DISPLAY-003 | Keep display formatting separate from CSV source parsing. Ambiguous imported dates or times must be resolved through explicit import configuration rather than guessed from the user's display preference. |
| DISPLAY-004 | Allow the user to configure their preferred start day of the week, time zone, and locale/cultural presentation settings, including date, time, and numeric formatting conventions. Keep these distinct from the explicit interval start date and fixed ledger currency. Locale changes must not relabel monetary values or rewrite historical period boundaries. |
| DISPLAY-005 | Every transaction must retain a backend-assigned creation timestamp, whether created manually or through import. The creation/import date is the day it was created, derived from that timestamp in the configured time zone, not a source transaction or posting date. Preserve the timestamp during edits and full-dataset restoration; audit restoration separately. |
| DISPLAY-006 | Model transaction date and bank posting date explicitly and separately from creation time. Derive them from mapped bank records on import and expose explicit fields for manual entry. Do not fabricate a missing source date; show absent values honestly. |
| DISPLAY-007 | Support a simplified entry interface requiring only the bank posting date rather than both financial dates. Every transaction must have at least one of transaction date or posting date. The authoritative financial date is the transaction date when present, otherwise the posting date; use it consistently for calendar placement, interval filtering, and balance history. Preserve missing values honestly and never substitute creation/import time for either financial date. |
| DISPLAY-008 | Store all dates and times as UTC values, including financial dates, period boundaries, and creation, modification, and audit timestamps. Normalize input to UTC and transform values for user interaction using the user's preferred time zone and date/time formats. APIs and dataset exports must represent stored timestamps unambiguously as UTC; never depend on the server's local time zone. |
| DISPLAY-009 | Interpret user-entered local dates and times in the preferred time zone before UTC conversion; use explicit source offsets or configured source time zones for imported records. For date-only financial records, preserve that the source supplied no time: a UTC storage representation must not imply a known transaction time. Define the date-only conversion convention and daylight-saving ambiguity rules before implementation, preserving financial-date precedence, local opening-day boundaries, and historical interval meaning. Verify conversion round trips and time-zone boundary cases. |

## CSV Banking Imports

| ID | Requirement |
| --- | --- |
| IMPORT-001 | Allow users to import banking transaction records from CSV files into an explicitly selected account. Support varied bank layouts through configurable parsing and field mappings rather than assuming one standard bank format. |
| IMPORT-002 | Accommodate differences such as delimiters, quoting, encodings, header presence and names, column order, date and time formats, decimal and grouping separators, signed amounts, and separate debit and credit columns. Allow mapping of source identifiers and descriptions where present, and reusable configurations for recurring bank formats. |
| IMPORT-003 | Analyze the file to suggest field mappings and parsing settings, but make suggestions reviewable and editable. Show representative parsed rows, flag ambiguity and unmapped required fields, and require the user's final confirmation of mappings, interpretation, and destination before committing records. |
| IMPORT-004 | Validate imported records using the same backend business and monetary rules as manually entered transactions. Surface row-specific parsing and validation errors and report imported, skipped, duplicate, and failed outcomes honestly; do not silently lose rows or claim a complete import when records were rejected. |
| IMPORT-005 | Prevent duplicate transactions within a file, across repeated or overlapping imports, and on retries or concurrent submissions. Use reliable source transaction identifiers when available and a documented account-scoped comparison strategy when they are absent; do not assume that matching dates and amounts alone prove duplication. |
| IMPORT-006 | Identify confirmed duplicates and potential matches before committing. Let the user resolve ambiguous matches without silently discarding legitimate repeated transactions or bypassing confirmed duplicate and retry safeguards. Keep imported records traceable to their source and import outcome so duplicate decisions can be understood. |
| IMPORT-007 | Do not treat a foreign-currency record as if it were in the configured currency. Surface currency mismatches and require resolution consistent with the single-currency scope before creating a transaction. Treat imported bank content as untrusted input and avoid exposing raw banking records in routine logs or error messages. |
| IMPORT-008 | Remove repetitive transcription while preserving meaningful awareness. Focus review on uncertain mappings or classifications, potential duplicates, unusual activity, and items the user wants to examine. After required mapping and import confirmation, do not demand individual acknowledgement of every confidently interpreted record solely to simulate manual entry; keep all records available for inspection and correction. |

The import outcome is reviewed transaction ingestion, not automatic bank
synchronization. Exact duplicate-match rules and supported parsing options must
be specified and tested in the import feature; automatic suggestions do not
replace user decisions or backend validation.

## Reports and Interactive Analysis

Reports should help users understand their recorded activity and Kakeibo
practice without replacing the simple calendar-centered workflow.

| ID | Requirement |
| --- | --- |
| REPORT-001 | Provide a comprehensive reports section covering recorded income and expenses, accounts, Kakeibo classifications, trends over time, and comparison with plans and predictions where applicable. Clearly distinguish actuals, intentions, and estimates, and do not imply that planned savings are achieved savings. |
| REPORT-002 | Default report filtering to the user's current reflection interval, including user-chosen two-week intervals, rather than forcing a calendar month or adopting an independent income period. Show the active interval and its exact date boundaries. |
| REPORT-003 | Allow users to select other intervals, including month to date, year to date, full calendar months and years, and custom date ranges. Define boundary and time-zone semantics where relevant and apply them consistently to summaries, charts, and drill-down results. |
| REPORT-004 | Make reports interactive: selecting a summary, series, category, or data point must allow meaningful drill-down into the contributing data or transactions. Preserve and expose the active filters and provide a clear way to return to the broader report context. |
| REPORT-005 | Link chart legends or keys with their plotted data. Keyboard focus, pointer hover, or selection of a legend item must highlight the corresponding series or segment, and interacting with plotted data must identify its legend item. Do not rely on color alone for those relationships. |
| REPORT-006 | Support a range of visualizations appropriate to the data and question. Desired chart families include line, bar, pie, doughnut, pyramid, funnel, candlestick, and timeline charts; use each only where its data semantics are meaningful, rather than manufacturing values or suggesting unsupported financial interpretations. |
| REPORT-007 | Provide accessible textual summaries and tabular equivalents of chart data, with keyboard-operable filtering, linked highlighting, and drill-down. Tooltips and data details must be available without requiring hover or precise pointer positioning. |
| REPORT-008 | Obtain authoritative financial calculations and aggregations from the backend. Charts may format and visualize results but must not introduce a separate frontend implementation of financial rules. Keep totals, details, currency, and date/time formatting consistent with the rest of the application. |
| REPORT-009 | Organize reports around understandable questions: what was intended, what happened, what explains the difference, and what might inform the next interval. Choose visualizations that answer those questions rather than presenting a chart gallery or requiring users to interpret accounting terminology. |
| REPORT-010 | Keep cross-interval refunds and reversals visible as account activity even when LEDGER-013 excludes them from spending classifications and the current plan. Explain their balance effect in plain language; do not present classification totals as a complete account-movement reconciliation when they exclude such activity. |

The breadth of reporting is product direction, not a requirement to deliver
every report and chart in one feature. Implement focused report slices with
observable outcomes, appropriate visualizations, and proportionate validation;
chart-library selection and prediction models remain separate decisions.

## Workspace and Interaction

| ID | Requirement |
| --- | --- |
| UX-001 | Draw structural inspiration from Visual Studio and Visual Studio Code: navigation on the left, the primary calendar workspace in the center, and contextual properties, menus, and actions on the right. |
| UX-002 | Keep navigation and contextual panes subordinate to the main task. Contextual content must clearly relate to the current selection, and pane visibility must not prevent access to essential actions. |
| UX-003 | Adapt the workspace for narrow screens, zoom, and different input methods without losing content or functionality. The left/right arrangement is the default desktop presentation, not a requirement to squeeze three panes onto every viewport. |
| UX-004 | Use familiar editor-workspace conventions without copying product branding or introducing IDE complexity that does not help household budgeting. |
| UX-005 | Present everyday tasks in household language, such as money in, money out, and transfers, without requiring users to understand debits, credits, concurrency keys, or storage details. The backend must map user intent to correct accounting treatment for the account type; simpler wording must not change financial semantics or conceal consequential choices. |
| UX-006 | Keep routine entry focused on the information necessary to record the activity correctly. Use explicit, editable contextual defaults where appropriate and progressively disclose optional details and advanced tools. Do not fabricate financial values, silently resolve ambiguity, or require repeated setup and navigation for each record. |
| UX-007 | Keep entry, interval tracking, reflection, and next-interval planning easy to reach from the calendar workspace. Evaluate representative complete workflows for unnecessary steps, repeated input, and technical jargon alongside correctness, keyboard access, and clear outcomes. Simplicity must not remove required validation or confirmations. |
| UX-008 | Deliver a cohesive, polished interface with consistent controls, readable information hierarchy, stable layouts, and smooth task transitions. Avoid decorative complexity, disruptive motion, and layout shifts that interfere with entry or comprehension. Polish must extend to loading, empty, error, and recovery states, not only successful workflows. |
| UX-009 | Keep input, navigation, and feedback responsive during data loading and processing. Clearly communicate pending work without presenting an unconfirmed financial change as saved, and prevent accidental duplicate submissions without unnecessarily blocking unrelated tasks. |
| UX-010 | Define and verify measurable responsiveness targets for core workflows using representative dataset sizes, supported browsers, and the minimum hosting configuration. Assess user-observed response times and interaction delays as well as backend timings; record the workload, environment, and results rather than claiming performance from appearance alone. |

## Themes

| ID | Requirement |
| --- | --- |
| THEME-001 | Provide cohesive light and dark themes inspired by Visual Studio themes, consistent with the Visual Studio and Visual Studio Code workspace direction while retaining HouseholdLedger's identity. |
| THEME-002 | Default to the operating system's light or dark preference. When following the system, respond to preference changes; allow an explicit light or dark selection to override that behavior and allow the user to return to system-following mode. |
| THEME-003 | Apply themes consistently across the entire frontend, including loading, error, empty, validation, focus, selection, and disabled states, without changing financial meaning or application behavior. |
| THEME-004 | Keep theme definitions separate from component behavior through a shared semantic design-token contract. Adding a theme must not require duplicating component markup or business logic. |
| THEME-005 | Document the theme contract and the steps for adding a theme, including token meanings, required values and states, registration and selection, and accessibility verification. Future themes must be straightforward to add using that documented process. |
| THEME-006 | Every theme must meet the same accessibility requirements and respect user preferences such as reduced motion and forced colors where applicable. |

## Accessibility

Accessibility is a first-class requirement across the entire frontend, not an
optional finishing step. WCAG 2.2 Level AA is the minimum conformance target.

| ID | Requirement |
| --- | --- |
| A11Y-001 | Meet applicable WCAG 2.2 Level A and AA success criteria across complete user workflows, all frontend surfaces, and all supported themes and responsive layouts. |
| A11Y-002 | Make every action operable using only a keyboard, including calendar navigation and selection, transaction editing, account management, reflection, settings, menus, dialogs, and pane controls. No action may depend solely on a pointer, hover, dragging, or touch gesture. |
| A11Y-003 | Provide a logical keyboard focus order, clearly visible focus that is not obscured, and predictable focus management when content, routes, panes, menus, or dialogs change. Avoid keyboard traps and provide efficient ways to bypass repeated navigation. |
| A11Y-004 | Use semantic HTML and native controls where possible, meaningful landmarks and headings, accessible control names and labels, and appropriate accessible relationships. Calendar dates, selection, and activity must be understandable to assistive technology without relying on visual position alone. |
| A11Y-005 | Meet WCAG contrast, resize, reflow, and target-size requirements. Do not communicate financial meaning, errors, status, or selection by color alone. |
| A11Y-006 | Make validation errors, loading and save outcomes, and dynamic status changes perceivable to assistive technology without unnecessarily interrupting users or moving focus. Provide understandable error messages and a clear path to correction. |
| A11Y-007 | Verify accessibility with focused automated checks plus manual keyboard-only and assistive-technology checks on representative complete workflows. Automated results alone do not establish WCAG conformance; report unverified criteria and known gaps explicitly. |

## Technology

| ID | Requirement |
| --- | --- |
| TECH-001 | The backend must use ASP.NET Core Web API. |
| TECH-002 | The built-in frontend must use Blazor WebAssembly. |
| TECH-003 | The database must be PostgreSQL, accessed through Entity Framework Core using the PostgreSQL provider. EF Core mappings, migrations, and provider-specific implementation belong at the persistence boundary. |
| TECH-004 | The backend must expose application capabilities through a documented HTTP API that can be consumed independently of the Blazor frontend. Alternative clients must not require server implementation assemblies. |

## Frontend Independence

| ID | Requirement |
| --- | --- |
| UI-001 | The frontend must not contain business logic. Business rules, financial calculations, business validation, and use-case decisions must execute in the backend's domain and application layers. |
| UI-002 | The Blazor frontend must be replaceable or bypassable without losing business behavior or requiring that behavior to be reimplemented in another client. A mobile application or Model Context Protocol (MCP) server integration must be able to use the same backend capabilities. |
| UI-003 | Frontend code may handle presentation, formatting, interaction state, API calls, and user-input feedback. Client-side checks must not be authoritative business validation or the only enforcement of a business rule. |
| UI-004 | Every Blazor page and component must use a same-directory, matching `.razor.cs` partial class. Razor files contain markup, directives, composition, and declarative bindings; parameters, injected services, lifecycle methods, event handlers, and presentation logic belong in code-behind. |
| UI-005 | Code-behind must support isolated testing of presentation behavior. Rendered component behavior must also be tested where markup, binding, or interaction is relevant; code-behind alone is not evidence that the UI works. |

Mobile applications and MCP integrations are examples of consumers this boundary
must support, not requirements to implement those clients in the current scope.
Hosting the Blazor assets with the API does not change the separation of their
responsibilities.

## Code Organization and Design

| ID | Requirement |
| --- | --- |
| DESIGN-001 | Code must have logical separation of domain rules, application use cases, HTTP transport, persistence and external integrations, and presentation. Responsibilities and dependency direction must be clear and independently testable. |
| DESIGN-002 | Follow Clean Architecture principles pragmatically. Domain rules must remain independent of UI, HTTP, EF Core, and database-provider details. Application use cases must not depend on frontend or infrastructure implementations. |
| DESIGN-003 | API endpoints must map HTTP requests and responses and invoke application behavior, rather than implement business rules. Persistence and integration adapters must implement the boundaries needed by that behavior. |
| DESIGN-004 | Apply SOLID principles where they improve cohesion, clarity, substitutability, and testability. Do not introduce interfaces, layers, wrappers, or abstractions solely to satisfy a pattern. |
| DESIGN-005 | Prefer the simplest framework-supported design that meets the requirements. Clean Architecture and SOLID are guidance, not mandates for rigid project structures or indirection that adds complexity without value or causes avoidable performance problems. |
| DESIGN-006 | Use engineering judgment for departures from architectural guidance. Explain meaningful maintainability, testability, or performance tradeoffs, and document lasting or surprising exceptions. Such departures must not move business logic into the frontend or remove backend enforcement. |

Logical separation does not require a new project, interface, or service for
every responsibility. Reuse existing boundaries and keep abstractions
proportionate to the actual behavior and dependencies.

## Test-Driven Development

| ID | Requirement |
| --- | --- |
| TDD-001 | Use test-driven development for new behavior and behavior changes: write a focused test, observe it fail for the expected reason, implement the smallest correct change, then refactor while keeping tests passing. |
| TDD-002 | For a defect, first add a regression test that reproduces the incorrect behavior, then fix the cause and verify the test passes. |
| TDD-003 | Choose the lowest test layer capable of proving the behavior. When behavior depends on rendering, HTTP wiring, database semantics, or a running system, use the appropriate higher-layer test rather than claiming a unit test proves that boundary. |
| TDD-004 | Refactoring must preserve observable behavior and keep the relevant tests passing. Extend tests when an uncovered behavior or regression risk is discovered. |

## Testing Pyramid

The test suite must follow the testing pyramid: a broad base of fast unit tests,
a smaller set of component and integration tests, and a focused set of
end-to-end tests against the fully running system.

| ID | Requirement |
| --- | --- |
| TEST-001 | Nearly all handwritten logic must have unit tests, including business rules, calculations, validation, use-case decisions, error paths, and presentation logic. Test observable behavior rather than private implementation details. |
| TEST-002 | Unit tests must be fast, deterministic, and isolated from real databases, networks, browsers, and external processes. |
| TEST-003 | Component tests must verify relevant Blazor rendering, bindings, interaction, and UI states in isolation from a live backend. |
| TEST-004 | Integration tests must verify collaborating components and actual boundaries, including API routing, serialization, validation responses, dependency wiring, and persistence behavior as relevant to the feature. |
| TEST-005 | PostgreSQL-specific behavior, EF Core mappings and migrations, constraints, and transactions must be tested against an isolated real PostgreSQL database. EF InMemory and SQLite are not substitutes for this evidence. Automated tests must not mutate the persistent manual-development database. |
| TEST-006 | End-to-end tests must exercise representative critical user journeys through the running Blazor frontend, API, and real PostgreSQL database. HTTP-only smoke tests supplement but do not replace full-system browser tests. |
| TEST-007 | Cover edge cases and failures at the lowest effective layer. Use higher-layer tests to prove wiring and representative workflows, not to repeat every lower-layer case. |
| TEST-008 | Exceptions to unit coverage must be narrow and explainable, such as generated code, declarative markup, or trivial transport data holders with no behavior. Necessary component, integration, and end-to-end evidence is still required; neither a percentage target nor code-behind structure alone proves sufficient coverage. |
| TEST-009 | Validate changes with the narrowest executable tests that establish the outcome, expanding to affected layers when boundaries change. Report exact results and unavailable checks; skipped database or browser tests are not passing evidence. |

## Delivery and Main-Branch Stability

| ID | Requirement |
| --- | --- |
| DELIVERY-001 | Follow trunk-based development using `main` as the integration trunk and small, short-lived branches. Merge changes into `main` only through pull requests; direct pushes are not the normal delivery path. Avoid long-lived feature or release integration branches. |
| DELIVERY-002 | Establish an explicitly verified first deployable baseline. Once established, every merged change must preserve a buildable, test-passing, deployable application with working existing user journeys and compatible persisted data. Deployable does not mean every global requirement is already implemented. |
| DELIVERY-003 | Require applicable automated checks to pass before merging a PR, including build and test evidence and deployment or migration checks where affected. Configure repository rules to require PRs and checks; documentation alone does not enforce this policy. |
| DELIVERY-004 | Deliver small, observable, testable slices with explicit acceptance criteria and requirement references. Use TDD and preserve the testing pyramid. Larger outcomes may span several independently safe PRs, but must not expose unfinished or misleading financial behavior. |
| DELIVERY-005 | Integrate incomplete work only when it is safely unreachable or explicitly disabled by default, with its enablement and removal conditions documented. Do not use hidden work to bypass required security, data integrity, accessibility, or compatibility checks. Deploying and releasing remain explicit operator actions, not automatic consequences of merging a PR. |

## Decisions to Resolve Within Delivery Slices

The first round is complete without prematurely deciding every implementation
detail. Resolve the following in the relevant feature specification before
enabling affected behavior. These are not permission to weaken the settled
requirements or blockers to unrelated slices.

| Area | Remaining decision | Delivery slice |
| --- | --- | --- |
| Existing data | Transition existing display-currency settings, destination-only accounts, and stored financial dates without silently reinterpreting records. | L01, L02, L03 |
| Money and account types | Supported account types, amount precision and rounding policy, and source debit/credit mappings consistent with standard accounting. | L01, L03, I01 |
| Time semantics | Date-only UTC representation, source-time-zone rules, daylight-saving ambiguity handling, and preserving historical meaning when the preferred time zone changes. | L02 |
| Interval transitions | New start-date handling that avoids unexplained gaps or overlap and preserves historical intervals. | K01 |
| Related records | Transfer matching, unresolved refund originals, and paycheck-allocation behavior when an account is removed. | L07, K07, K08 |
| Deletion and undo | Historical report scope for archived records, exact reversible actions and undo lifetime, and identity-provider evidence required for fresh reauthentication. | L08, L09, L10 |
| Imports and portability | Dataset replacement/merge compatibility, CSV duplicate-match rules, batch atomicity, file limits, and temporary-data retention. | D02, I01, I02, I04 |
| Supported environments | Exact dated browser/platform versions, services hosted on the Pi 4 2 GB, representative dataset sizes, and measurable responsiveness targets. | B02, U02, U03 |
| Operator recovery | Backup/restore instructions, authorized owner recovery if the identity provider is replaced or unavailable, and compatible image/schema recovery paths. | B03, B04, O01 |
| Attention and backup reminders | Timing, dismissal lifetime, and meaningful-change rules that avoid repeated interruption. | K05, D03 |
| Future-facing analysis | The first prediction question, appropriate chart use cases, and documented API retirement notice timing. | R03, R04, O01 |

Broader multi-user support, currency changes and multi-currency accounting,
general split transactions, full double-entry bookkeeping, bank-statement
reconciliation, live bank synchronization, managed backups, and a multi-step
undo/redo history are not commitments for this baseline. Mobile applications
and MCP clients remain possible consumers, not required implementations.

## Related Documentation

- [Architecture Overview](architecture/overview.md)
- [Contribution Guide](development/contributing.md)
- [Testing](development/testing.md)
- [Delivery Plan](development/delivery-plan.md)
- [Feature Index](features/README.md)
