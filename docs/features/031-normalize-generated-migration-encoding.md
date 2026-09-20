# Feature 031: Normalize Generated Migration Encoding

## Status

Status: Proposed.

- Planned: 2026-09-14.
- Found by the production-readiness formatting gate.

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

- Retain the formatter output identifying the affected files before
  normalization.
- Run focused formatter verification and the existing Infrastructure
  integration coverage after normalization.
- Review the normalization diff to confirm that it contains no migration-model
  or operation change.

## Definition of Done

The feature is complete when generated migration sources conform to repository
text policy and continue to represent the unchanged database schema.