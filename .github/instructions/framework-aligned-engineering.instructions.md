---
name: "Framework-Aligned Engineering"
description: "Use when making .NET/framework pattern choices, UX/usability/accessibility decisions, infrastructure/operations changes, architecture decisions, or responding to requests that might conflict with best practices."
applyTo: "**"
---

# Framework-Aligned Engineering

- Default to platform- and framework-supported mechanisms and established .NET practices over custom reimplementations.
- Apply the same standard to usability, accessibility, infrastructure, operations, and architecture decisions.
- Before implementing a concrete conflict with an established practice, alert the user to the specific practice and consequence, offer at least one framework-aligned alternative, and request explicit confirmation for the exception.
- Do not claim a conflict without a concrete concern. User direction remains authoritative after informed confirmation; document deliberate exceptions near the relevant decision or specification.