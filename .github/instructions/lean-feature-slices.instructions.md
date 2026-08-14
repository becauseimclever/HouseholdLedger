---
name: "Lean Feature Slices"
description: "Use when creating or reviewing feature specifications, feature planning, MVP or minimum viable product scope, thin vertical slices, scope decisions, acceptance criteria, or feature implementation."
---

# Lean Feature Slices

- Give each feature one user-observable outcome and responsibility; split unrelated concerns into later feature documents.
- Specify and implement the smallest end-to-end vertical slice that proves the outcome, reusing framework capabilities and existing local patterns.
- Keep temporary or sample behavior honest and minimal; defer integrations, abstractions, configuration, tests, documentation, and validation that do not directly support the current slice.
- Keep acceptance criteria and the Definition of Done observable, proportionate to risk, and no broader than the feature outcome.
- Require a feature document for a meaningful new product outcome or material product-behavior change. Do not require one for a small, localized UI change, bug fix, maintenance repair, or validation-only work when the request states clear, observable acceptance criteria.
- A user's approval of a feature scope authorizes all documented implementation waves through completion. Do not seek approval again between waves unless the work materially expands product behavior, scope, dependencies, risk, or file ownership.
- Proceed autonomously with routine local repairs, test fixes, lock-file consistency updates, and validation reruns when they do not materially expand product behavior, scope, dependencies, risk, or file ownership.
- Do not require a final documentation audit by default. Complete a feature when its applicable acceptance criteria are implemented and supported by proportionate validation evidence. Use a separate audit only when the user requests one, a release/compliance gate requires an independently retained review, or unresolved evidence needs a formal decision record.
- Request one concise user decision or clarification only when it is genuinely needed: a material scope or product-behavior change, destructive or risky operation, new dependency, data migration, security or privacy decision, external service, or conflict with the Kakeibo and calendar-centered product direction.
- When a request expands scope or mixes responsibilities, state the conflict, propose a smaller first slice, and seek user confirmation before proceeding.
- Do not use this guidance to skip necessary accessibility, security, correctness, framework-alignment, or regression validation; follow the existing Framework-Aligned Engineering instruction.
- This repository policy does not relax dependency-governance review or approval requirements for dependency changes.