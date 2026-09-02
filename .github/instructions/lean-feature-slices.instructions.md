---
name: "Lean Feature Slices"
description: "Use when creating or reviewing feature specifications, feature planning, MVP or minimum viable product scope, thin vertical slices, scope decisions, acceptance criteria, or feature implementation."
---

# Lean Feature Slices

- Give each feature one user-observable outcome and responsibility; split unrelated concerns into later feature documents.
- Specify and implement the smallest end-to-end vertical slice that proves the outcome, reusing framework capabilities and existing local patterns.
- Keep temporary or sample behavior honest and minimal; defer integrations, abstractions, configuration, tests, documentation, and validation that do not directly support the current slice.
- Keep acceptance criteria and the Definition of Done observable, proportionate to risk, and no broader than the feature outcome.
- Create a feature document when it will clarify a substantial or ambiguous outcome, or when the user asks for one. A clear request, issue, or short implementation plan is enough for routine feature work.
- Treat the user's request as authority to implement the described outcome. Ask again only when new information creates a materially different product decision, destructive migration, privacy/security choice, or significant cost.
- Proceed autonomously with routine local repairs, test fixes, lock-file consistency updates, and validation reruns when they do not materially expand product behavior, scope, dependencies, risk, or file ownership.
- Do not require a final documentation audit by default. Complete a feature when its behavior is implemented and supported by proportionate validation. Use a separate audit only when the user requests one or a release, legal, or unresolved technical decision genuinely benefits from a retained record.
- Request one concise user decision only when it is genuinely needed: an ambiguous material product choice, destructive or difficult-to-reverse operation, data migration, sensitive security/privacy decision, paid external service, or conflict with the Kakeibo and calendar-centered direction.
- When a request expands scope or mixes responsibilities, state the conflict, propose a smaller first slice, and seek user confirmation before proceeding.
- Do not use this guidance to skip necessary accessibility, security, correctness, framework-alignment, or regression validation; follow the existing Framework-Aligned Engineering instruction.
- Follow the repository's risk-based dependency guidance; routine FOSS dependency maintenance is ordinary implementation work.