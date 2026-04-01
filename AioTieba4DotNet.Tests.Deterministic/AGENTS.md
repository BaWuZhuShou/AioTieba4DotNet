# Deterministic Test Lane Guide

## OVERVIEW

This project owns offline deterministic verification for the v3 line. It is the coverage-bearing lane and should stay
runnable without live secrets.

## RULES

- Keep tests offline, repeatable, and safe for local or agent execution.
- Prefer these tests for mappers, protocol orchestration, option validation, generator helpers, and regression coverage
  that does not need real network state.
- Do not add secret-backed or mutation-dependent tests here.
- When feature work adds meaningful handwritten logic, start with deterministic coverage before leaning on integration
  or live lanes.
