---
name: "Framework-Aligned Engineering"
description: "Use when making .NET/framework pattern choices, UX/usability/accessibility decisions, infrastructure/operations changes, architecture decisions, or responding to requests that might conflict with best practices."
applyTo: "**"
---

# Framework-Aligned Engineering

- Default to platform- and framework-supported mechanisms and established .NET practices over custom reimplementations.
- Apply the same standard to usability, accessibility, infrastructure, operations, and architecture decisions.
- Use engineering judgment rather than treating every departure from guidance as an approval gate. Explain a meaningful tradeoff when it affects maintainability, accessibility, security, data integrity, or operating cost, then proceed with the user's stated direction unless the action is destructive, irreversible, or still materially ambiguous.
- Document architectural exceptions only when the decision is likely to surprise a future maintainer or has a lasting consequence. User direction remains authoritative.