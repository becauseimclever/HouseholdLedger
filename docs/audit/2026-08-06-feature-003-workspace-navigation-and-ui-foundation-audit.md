# Feature 003 Workspace Navigation and UI Foundation Audit

## Audit Scope

**Date:** 2026-08-06

**Source of truth:**
[Feature 003: Workspace Navigation and UI Foundation](../features/003-workspace-navigation-and-ui-foundation.md)

**Verdict:** Not audit-passed. AC-01 through AC-05 are `Met`; AC-06 is `Not
Met`. Definition of Done items 1, 5, and 6 are `Not Met`; items 2, 3, 4, and 7
are `Met`. The blocking condition is the required dependency-governance review
and approval for the newly added Lucide package closure, not the desktop shell
behavior.

The user approved implementation through completion on 2026-08-04 and approved
the desktop-only scope refinement on 2026-08-06. This read-only audit inspected
current source, tests, package records, retained validation claims, and the
current workspace diff. It did not start or interact with a host, browser,
database, container, port, external service, or another agent's terminal.

## Evidence Basis

### Current Implementation and Test Inspection

- `MainLayout.razor` renders a `nav`, a central calendar workspace section, and
  a complementary `aside`. Both panes initially render expanded and each has a
  distinct native button with `aria-controls`, a state-dependent accessible
  label, `aria-expanded`, a tooltip, and a decorative Lucide icon hidden from
  assistive technology. The panes use `hidden` when collapsed.
- `MainLayout.razor.cs` maintains independent transient Boolean state for the
  navigation and inspector. `MainLayout.razor.css` defines stable controls,
  focus-visible tooltip behavior, and pane styling; global `app.css` uses a
  normal-flow grid and changes its columns as either `hidden` pane changes.
- `CalendarPage.razor` is one `main.calendar-page` headed `Calendar` and adds
  no controls, data, selection, financial, or navigation behavior. The
  inspector's only content is the neutral `No calendar item selected` state,
  and the navigation pane has no destination items.
- `WorkspaceShellTests` asserts the semantic regions, neutral pane content,
  native controls, accessible labels and state, icon hiding, tooltips, and
  independent collapse behavior. `CalendarPageTests` asserts one main and h1
  and no interactive calendar content.
- `BrowserCalendarJourneyTests.ApiHostRendersAccessibleDesktopCalendarWorkspace`
  sets a 1440 by 900 inner viewport, asserts the root heading and central main,
  navigation and inspector state, zero navigation destinations, neutral
  inspector text, focus on the pressed toggle, no page-level horizontal
  overflow, and normal-flow rectangle ordering. It executes the expanded,
  navigation-collapsed, both-collapsed, inspector-collapsed, and re-expanded
  states.

### Retained Validation Evidence

| Check | Exact retained result | Audit use |
| --- | --- | --- |
| Client Release build | Final focused Release build passed with no warnings. Earlier Client builds after the event-callback and explicit-string `aria-expanded` repairs also passed with no warnings. | Supports the Client shell source and package compilation. |
| Focused component tests | `CalendarPageTests\|WorkspaceShellTests` passed 2 of 2 in 0.9 seconds after the final focused Release build. | Supports AC-01 through AC-03 and shell markup/state assertions. |
| Full component-suite claim | A later UI-owner report says 22 of 22 Client component tests passed with `--no-build --no-restore`. Current testing documentation identifies 22 Client cases, but no retained command transcript was supplied to this audit. | Not used as a passed command result. It does not weaken the focused 2 of 2 evidence. |
| Hosted browser proof | Fresh isolated API and Client Release publishes passed with no warnings. `ApiHostRendersAccessibleDesktopCalendarWorkspace` passed 1 of 1 in 4.7 seconds at 1440 by 900 using test-owned API port 51371; the unused Client port was 51372. Resources were cleaned up. | Authoritative desktop behavior and geometry proof for AC-01 through AC-05. |

The browser test has no pixel comparison or narrow-viewport case. That is not
a missing Feature 003 acceptance item: the approved 2026-08-06 refinement
explicitly defers narrow viewport behavior and proof.

## Acceptance Criteria Matrix

| Criterion | Verdict | Evidence and limits |
| --- | --- | --- |
| AC-01: Calendar-centered workspace | Met | Current layout contains the left navigation, central calendar workspace, and right inspector regions. Current calendar markup is a single `main` with one `Calendar` h1 and no interactive or financial content. The focused component checks and passed authoritative browser proof confirm the regions and central main at the required desktop viewport. |
| AC-02: Independent pane controls | Met | Separate state fields and toggle methods control the navigation and inspector. The component test proves each click changes only the intended pane state; the browser proof cycles every independent state combination, checks `aria-expanded`, accessible labels, and focus on the pressed toggle. |
| AC-03: Honest neutral inspector | Met | The current inspector has only `Inspector` and `No calendar item selected`; navigation has no destinations and the calendar has no data or selection surface. The component test and browser proof respectively assert the exact empty-state text and visible inspector text. |
| AC-04: Calendar remains unobscured | Met | The grid uses normal-flow columns rather than drawer positioning. The authoritative browser proof verifies nonzero calendar and main rectangles, calendar-to-pane ordering whenever a pane is present, and no page-level horizontal overflow through every expanded/collapsed cycle at 1440 by 900. |
| AC-05: Accessible desktop shell | Met | Current source provides navigation, main, and complementary landmarks; one main h1; logical pane h2 headings; native controls; accessible names; `aria-controls`; `aria-expanded`; `hidden` collapsed panes; and a visible `:focus-visible` outline. The browser proof verifies the default expanded state, desktop dimensions, toggle focus/state, text, normal-flow geometry, and no horizontal overflow. The scope deliberately does not claim narrow layout behavior. |
| AC-06: Bounded implementation | Not Met | The visible implementation is bounded: it uses local Blazor/CSS primitives, native buttons, and Lucide only for the two pane icons; current diff review found no UI framework, product data behavior, or runtime code changes outside Client. The central version, Client-only direct reference, and resolved Client closure are present. However, the required dependency-governance review and approval before manifest change is not recorded. `dependency-governance.md` explicitly says its existing direct-family snapshot does not prove complete closure, notices, or published-runtime review and says not to describe overall supply-chain review as complete. No Feature 003 record closes those requirements for `Blazicons.Lucide` 3.0.8, `Blazicons` 4.0.21, and `BlazorComponentUtilities` 1.8.0. Therefore this aggregate criterion cannot be marked `Met`. |

## Definition of Done Matrix

| Definition of Done item | Verdict | Evidence and limits |
| --- | --- | --- |
| 1. Read-only audit marks AC-01 through AC-06 `Met` with current code and retained evidence. | Not Met | This report is read-only and records concrete evidence, but AC-06 is `Not Met` because the required dependency-governance review is absent. |
| 2. Audit confirms calendar-only central surface and independent non-overlay panes at desktop viewport. | Met | Current markup and CSS keep the calendar as the sole central temporary surface. The passed browser proof measures normal-flow pane ordering and calendar visibility at 1440 by 900 through all pane states. |
| 3. Audit confirms neutral inspector and no invented finance, selection, persistence, API, or navigation-destination behavior. | Met | Current Calendar page, layout, component tests, and browser proof demonstrate only the empty inspector statement, no navigation destinations, and no interactive calendar content. Current diff review found no Application, Domain, Infrastructure, Contracts, API runtime, migration, or hosting code change. |
| 4. Focused Client build, component tests, and authoritative desktop browser proof pass under documented test-owned resources. | Met | Retained evidence reports a no-warning final focused Client Release build, focused component filter passing 2 of 2 in 0.9 seconds, and the fresh-publish browser proof passing 1 of 1 in 4.7 seconds with test-owned resources and cleanup. The untranscribed 22 of 22 owner report is not asserted as independently verified evidence. |
| 5. Audit confirms constrained approved Lucide icon use, accessible native buttons, and no UI framework, unapproved dependency, hosting change, or undocumented scope expansion. | Not Met | Current code confirms icon-only Lucide use, native labeled tooltip-bearing buttons, and no UI framework or hosting change. The Lucide package cannot be treated as approved under repository policy because the prerequisite review and approval record is absent; DoD 6 identifies the missing evidence. |
| 6. Audit confirms completed pre-manifest dependency review, central version, Client-only reference, matching Client lock closure, and retained supply-chain/license evidence. | Not Met | Central version, Client-only direct reference, and the locked closure are observable. The governing document requires review and approval before manifest change plus complete closure, license/notice, vulnerability, integrity, commercial-model, inventory, and published-output evidence. It expressly states these overall closure reviews remain unrecorded. No Feature 003-specific record supplies the required review or approval. |
| 7. User-facing documentation is updated only when a later approved task shows the shell needs it. | Met | The specification states this shell alone creates no end-user documentation requirement. Current Feature 003 changes add no user-facing behavior that requires task guidance, and this audit makes no unsupported claim that such documentation exists. |

## Scope and Workspace Review

The implementation honors the approved desktop-only refinement. It makes no
narrow viewport claim and does not add JavaScript viewport detection or
interoperability. It retains a bespoke local design: no third-party UI library
or visual component framework is present. Lucide is limited in markup to the
two pane-control icons and is not the controls' accessible name or only
explanation.

The current workspace is dirty relative to Feature 002. The Feature 003 code
and tests are uncommitted, as are Feature 003 and the distinct follow-on
Feature 004 through 007 proposals. Those follow-on documents are outside this
audit and are neither evidence of Feature 003 expansion nor modified here.

Production runtime changes are confined to the Client. API, Application,
Domain, Infrastructure, and Contracts source projects have no changed source
files. The API lock-file additions and Client ComponentTests lock-file additions
are consumer-closure updates caused by the Client's new package reference; this
matches the retained dependency-repair evidence. Application and Domain lock
file changes are newline-only unrelated modifications, not Feature 003 work.
No change was reverted.

## Required Next Action

The smallest responsible action is for the dependency-governance owner to
create and obtain approval for a Feature 003 Lucide closure review before the
manifest changes: exact package provenance and publisher; full direct,
transitive, build, runtime, and published-output inventory; canonical licenses
and notices; FOSS and commercial-model assessment; vulnerability-scanner scope
and findings; package integrity evidence; the built-in-alternative rationale;
and the user's recorded exception decision. It must retain the prescribed
locked restore, inventory, vulnerability, license, and published-output checks.
After that evidence exists, re-audit AC-06 and DoD items 1, 5, and 6. No
production or test implementation change is indicated by this audit.

## Documentation Validation Record

- Before this report was created, VS Code Markdown diagnostics found no errors
  in the Feature 003 source specification.
- The report's relative link targets the existing Feature 003 specification.
- This audit did not run build, test, restore, browser, or package commands;
  all command and browser outcomes above are retained evidence supplied to the
  audit.
- Markdown diagnostics and a CRLF-aware `git diff --check` for this report are
  recorded after creation below.

## Sources

- Current local inspection on 2026-08-06: Feature 003 specification; Client
  layout, page, and CSS files; component and browser tests; central package,
  Client project, and relevant lock files; dependency-governance and testing
  documentation; and the current git status and diff.
- Retained implementation and validation evidence supplied for this final audit
  on 2026-08-06: Client builds, focused component result, fresh-publish hosted
  browser proof, and resource cleanup result.
