# Integration Test Lane Guide

## OVERVIEW
This project owns staged real-link verification that depends on the sequencing manifest and shared safe-fixture gates, but avoids destructive live cleanup stories when possible.

## RULES
- Keep stage ownership aligned with `AioTieba4DotNet.Testing/test-sequencing.manifest.json`.
- Reuse shared gates and `TestBase` for environment checks and safe-fixture discovery.
- Prefer read and controlled verification here. Destructive or rollback-heavy scenarios belong in the live lane.
- Do not invent a second stage-order system in code or docs. The manifest is the truth source.
