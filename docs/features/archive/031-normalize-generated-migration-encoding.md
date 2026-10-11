# Feature 031: Normalize Generated Migration Encoding

## Status

Status: Complete.

- Planned: 2026-09-14.
- Implemented: 2026-10-11.
- Found by the production-readiness formatting gate.
- Removed UTF-8 BOMs from the five formatter-identified migration sources:
  `20260906132640_AddGlobalSettings.cs`,
  `20260906142223_AddGenericMoneyDisplay.cs`,
  `20260906151001_AddWorkbenchTheme.cs`,
  `20260907191945_EnforceExpenseTransactionIntegrity.cs`, and
  `20260913042440_AddIncomeSchedules.cs`.
- The formatter's whitespace-only diagnostics in
  `20260913042440_AddIncomeSchedules.cs` were corrected; focused formatting
  verification passed for all five files and the Feature 030 source.
- Infrastructure Release build passed with zero warnings and errors.
- Isolated PostgreSQL integration tests passed (9 total, 0 skipped).

## Outcome

Maintainers can run repository formatting verification without encoding
diagnostics in the formatter-identified generated migration files.

## Acceptance Criteria

| Criterion | Observable outcome |
| --- | --- |
| AC-01: Repository text policy | Each migration file identified by the formatter uses the repository-required UTF-8 encoding, CRLF line endings, final newline, and no trailing whitespace. |
| AC-02: Migration preservation | Generated migration and model-snapshot semantics remain unchanged after normalization. |
| AC-03: Focused format proof | Focused formatter verification reports no diagnostics for each normalized migration file. |
| AC-04: Persistence regression proof | The existing Infrastructure build and migration-oriented integration coverage remain successful. |
| AC-05: Bounded slice | No schema, migration operation, persistence behavior, dependency, or production data change is introduced. |

## Responsibility Boundaries

| Layer | Responsibility |
| --- | --- |
| Infrastructure | Preserve generated migration source in the repository text format without changing persistence semantics. |

## Validation

- The initial formatter output identified five `CHARSET` diagnostics and
  whitespace diagnostics in `20260913042440_AddIncomeSchedules.cs`.
- Focused formatting verification, Infrastructure Release build, and all
  isolated PostgreSQL integration tests passed after normalization.
- The migration diff was reviewed ignoring whitespace; no migration operation,
  model, or schema change remains.

## Definition of Done

The feature is complete when generated migration sources conform to repository
text policy and continue to represent the unchanged database schema.
