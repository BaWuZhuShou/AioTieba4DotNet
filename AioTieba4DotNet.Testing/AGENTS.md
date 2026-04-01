# Shared Testing Guide

## OVERVIEW
This directory holds shared testing infrastructure for the split v3 test layout. Put cross-lane helpers here, not new product behavior tests.

## RESPONSIBILITIES
- `TestBase` and environment bootstrap
- fixture gates and safe-fixture enforcement
- cleanup orchestration and recorded-object rollback helpers
- repository path helpers and manifest loading
- sequencing truth in `test-sequencing.manifest.json`

## RULES
- Keep this project non-product and support-only. Feature assertions belong in the lane projects.
- `test-sequencing.manifest.json` is the only truth source for integration and live stage order.
- Treat `Cleanup` as a synthetic compensation wave. Do not model it as a runnable MSTest category.
- Shared helpers should enforce safe-fixture and credential gating, not bypass them.
- If a change alters stable lane sequencing or shared fixture policy, also sync `.junie/guidelines.md`.
