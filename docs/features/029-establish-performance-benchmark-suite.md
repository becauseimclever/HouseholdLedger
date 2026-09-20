# Feature 029: Establish Performance Benchmark Suite

## Status

Status: Proposed.

- Planned: 2026-09-14.
- Depends on the production API host, PostgreSQL persistence boundary, and
  calendar and account user workflows.

## Outcome

Maintainers can measure representative production operations with repeatable
benchmarks and memory profiles, establish reviewed baselines, and detect
meaningful performance or allocation regressions before release.

## Performance Contract

The suite measures the API-hosted Client, API request path, and PostgreSQL
backed operations separately so a result identifies the responsible boundary.
It uses deterministic representative fixtures, explicit warm-up, recorded
runtime and machine characteristics, and a fresh result directory per run.

- Benchmarks cover the calendar range read, selected-day inspector read,
  account detail/history read, expense creation, pay-schedule receipt
  materialization, and hosted Client startup.
- Results record latency percentiles, throughput where applicable, managed
  allocations per operation, process working set, garbage-collection activity,
  and published artifact size.
- Memory analysis records retained managed memory after warm-up and repeated
  representative operations. It distinguishes normal runtime initialization
  from growth that remains after collection and request completion.
- Each benchmark has a documented fixture scale and a reviewed baseline and
  regression threshold. Thresholds are not invented from one developer
  workstation result.
- The suite runs against an isolated PostgreSQL database for persistence
  measures. It does not modify the persistent manual-development database or
  require production credentials.

## Acceptance Criteria

| Criterion | Observable outcome |
| --- | --- |
| AC-01: Repeatable representative suite | A documented command runs deterministic benchmarks for the defined calendar, account, expense, income, and hosted-client paths and writes an inspectable result set. |
| AC-02: Allocation and memory evidence | Each API and persistence benchmark reports allocations and GC activity; a memory-profile workflow reports retained memory after warm-up and repeated operations. |
| AC-03: Reviewed baselines | Versioned baseline metadata records the fixture scale, runtime, machine class, result date, and approved latency, throughput, allocation, and retained-memory thresholds. |
| AC-04: Regression detection | The suite fails or clearly reports when a result exceeds an approved threshold, while retaining raw measurements for diagnosis. |
| AC-05: Isolation and privacy | Benchmark fixtures use generated non-personal data, isolate PostgreSQL resources, redact connection information, and clean up owned processes and data. |
| AC-06: Bounded scope | The suite does not add application features, alter user data, introduce cloud services, or treat one machine's baseline as a universal production service-level objective. |

## Responsibility Boundaries

| Layer | Responsibility |
| --- | --- |
| Application and Infrastructure | Expose representative operations through existing production paths and provide deterministic test fixtures without special production behavior. |
| API and Client | Support hosted startup and request-path measurement without diagnostic data leaking to users. |
| Test infrastructure | Own benchmark execution, isolated resources, result capture, baseline comparison, and cleanup. |
| Documentation | Define the command, fixtures, environment record, baselines, thresholds, and interpretation limits. |

## Validation

- Benchmark tests prove result schema, fixture isolation, cleanup, and threshold
  comparison independently from performance measurements.
- A controlled baseline run records results for each representative operation.
- A deliberate threshold breach proves regression reporting without relying on
  an unstable timing assertion.
- Existing functional and PostgreSQL integration tests continue to exercise the
  same production paths for correctness.

## Definition of Done

The feature is complete when maintainers can run a repeatable, isolated suite
that measures representative production behavior, captures memory efficiency
evidence, compares results with reviewed baselines, and reports actionable
regressions.