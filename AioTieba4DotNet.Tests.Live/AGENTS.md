# Live Test Lane Guide

## OVERVIEW

This project owns credentialed and mutation-capable verification for the v3 line. Tests here must assume safe-fixture
gating and cleanup awareness.

## RULES

- Reuse shared fixture gates and cleanup helpers from `AioTieba4DotNet.Testing/`.
- Keep destructive operations behind explicit safe fixtures and manual gates when required.
- Respect the sequencing manifest order. Live write and moderation work should not run ahead of its prerequisites.
- Record or compensate for live mutations through the supported cleanup flow rather than leaving state behind.
- `Cleanup` remains a synthetic compensation stage, not a runnable MSTest category filter.
