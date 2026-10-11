# Feature 032: Complete Selected-Date Inspector Test Fixture

## Status

Status: Complete.

- Planned: 2026-09-14.
- Implemented: 2026-10-11.
- Found by the production-readiness Client component-test gate.
- The selected-date test now supplies a fixed October 11, 2026 clock, asserts
  the initial month link deterministically, then proves the selected date
  expands the inspector without expanding collapsed navigation.
- The isolated test passed (1 total), and the full Client component suite
  passed (148 total).

## Outcome

Maintainers have a deterministic component proof that selecting a date expands
a collapsed inspector without reopening collapsed workspace navigation.

## Acceptance Criteria

| Criterion | Observable outcome |
| --- | --- |
| AC-01: Complete fixture | The component-test fixture supplies every dependency needed by the rendered inspector, including its clock dependency. |
| AC-02: Inspector reveal proof | Selecting a date after collapsing both panes expands the inspector. |
| AC-03: Navigation remains collapsed | The same selected-date action leaves workspace navigation collapsed. |
| AC-04: Isolated deterministic test | The test uses no real HTTP service, system-time dependency, or external resource. |
| AC-05: Bounded slice | No production inspector, navigation, API, persistence, or feature behavior changes are introduced. |

## Responsibility Boundaries

| Layer | Responsibility |
| --- | --- |
| Client component tests | Configure the test host with production-equivalent required dependencies and assert workspace shell behavior. |

## Validation

- `SelectedDateAutomaticallyRevealsCollapsedInspector` passed in isolation.
- The full Client component-test project passed (148 tests).

## Definition of Done

The feature is complete when the selected-date inspector reveal behavior is
deterministically covered by a passing isolated and project-level component test.
