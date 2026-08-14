# Contribution Guide

HouseholdLedger is a from-scratch, calendar-centered household ledger informed
by Kakeibo. Kakeibo combines recording money with planning and reflection. New
work should keep calendar meaning and household decisions explicit without
assuming a user's income, family structure, goals, or financial experience.

## Before Changing Code

1. For a meaningful new product outcome or material product-behavior change,
   read the approved feature document and its current implementation status.
   A small, localized UI change, bug fix, maintenance repair, or validation-only
   task may proceed from clear, observable acceptance criteria in the request.
2. Confirm the owning layer in the [Architecture Overview](../architecture/overview.md).
3. Complete dependency review before adding any package, tool, image, runtime,
   or downloaded asset.
4. Keep work inside `HouseholdLedger`; `BudgetExperiment` is read-only reference
   material.
5. Record exclusive ownership of shared files and external resources when work
   is coordinated across specialists.

Do not introduce ledger behavior that is not in an approved feature. Preserve
validated product lessons from the reference repository, not its implementation
or accumulated architecture.

## Approval and Completion

User approval of a feature scope authorizes every documented implementation
wave through completion. Do not request a separate wave or phase approval
unless the work materially expands product behavior, scope, dependencies, risk,
or file ownership.

Proceed autonomously with routine local repairs, test fixes, lock-file
consistency updates, and validation reruns that do not materially expand product
behavior, scope, dependencies, risk, or file ownership. Request one concise
user decision or clarification only when it is needed for a material scope or
product-behavior change, destructive or risky operation, new dependency, data
migration, security or privacy decision, external service, or a conflict with
the Kakeibo and calendar-centered product direction.

Do not require a final documentation audit by default. A feature is complete
when its applicable acceptance criteria are implemented and supported by
proportionate validation evidence. Create a separate audit only when the user
requests one, a release or compliance gate requires an independently retained
review, or unresolved evidence needs a formal decision record. The dependency
review and approval requirements in
[Dependency Governance](dependency-governance.md) remain mandatory for every
dependency change.

## Code and Boundary Rules

- Domain stays framework-independent.
- Application owns use cases and ports and does not expose provider or HTTP
  types.
- Infrastructure owns EF Core, Npgsql, mappings, and migrations.
- API controllers own HTTP mapping and call application behavior; they do not
  implement business rules or query a `DbContext`.
- API Contracts remains dependency-free and transport-only. Checked OpenAPI is
  the canonical language-neutral contract.
- Client references only API Contracts and calls the API over HTTP.
- Use explicit mapping, direct interfaces, built-in dependency injection, and
  readable test builders or factories.

Warnings, analyzers, code style, deterministic builds, locked restore, and
NuGet audit are centrally enforced. Do not suppress a rule or weaken shared
settings to make a local change pass without an approved reason.

## Razor Convention

Every `.razor` file has a same-directory `.razor.cs` partial class with the same
component name. This includes application, routing, layout, navigation, page,
not-found, and error components.

Razor files contain markup, directives, component composition, and declarative
binding. They contain no `@code` block. Parameters, injected services, lifecycle
methods, handlers, and presentation logic belong in code-behind. Use a matching
`.razor.css` file for component-scoped styles when needed.

Keep UI states honest and accessible. Do not invent financial entries, totals,
savings, or reflections for an empty state. Use landmarks, meaningful headings,
keyboard-operable controls, visible focus, sufficient contrast, and text that
does not rely on color or calendar position alone.

## Validate a Change

Start with the narrowest relevant build or test, then follow
[Testing](testing.md). Before handoff, verify formatting and inspect the diff:

```powershell
dotnet format HouseholdLedger.slnx --verify-no-changes --no-restore
git diff --check -- .
```

Report exact commands and results. Do not claim blocked PostgreSQL or browser
evidence from project presence, test names, or another contributor's report.
