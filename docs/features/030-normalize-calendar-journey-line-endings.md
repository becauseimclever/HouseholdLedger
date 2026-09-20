# Feature 030: Normalize Calendar Journey Line Endings

## Status

Status: Proposed.

- Planned: 2026-09-14.
- Found by the production-readiness formatting gate.

## Outcome

Maintainers can run repository formatting verification without line-ending
diagnostics in the calendar browser-journey source.

## Acceptance Criteria

| Criterion | Observable outcome |
| --- | --- |
| AC-01: Repository text policy | `BrowserCalendarJourneyTests.cs` uses the repository-required CRLF line endings, UTF-8 encoding, final newline, and no trailing whitespace. |
| AC-02: Focused format proof | Focused formatter verification reports no diagnostics for the browser-journey source. |
| AC-03: Preserved test behavior | The normalization has no semantic or executable change to the calendar browser journey. |
| AC-04: Bounded slice | No browser test behavior, production source, package, or test runtime change is introduced. |

## Responsibility Boundaries

| Layer | Responsibility |
| --- | --- |
| End-to-end test infrastructure | Maintain the browser-journey source in the repository text format. |

## Validation

- Run focused formatting verification for `BrowserCalendarJourneyTests.cs`.
- Review the normalization diff to confirm it contains no semantic source
  change.

## Definition of Done

The feature is complete when calendar browser-journey formatting conforms to
the repository policy without changing its test behavior.