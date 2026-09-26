# Validation Evidence for Spec Review

Read-only source inspection on 2026-09-26; no builds or tests were executed during
this research. Recheck these observations when the test topology changes.

## Commands and Boundaries

- `scripts/verify-local.sh --validate-only` and
  `scripts/verify-local.ps1 -ValidateOnly` validate existing retained artifacts and
  documentation contracts. Their default modes rewrite the local verification
  manifest. They print docs commands but do not run those commands or any tests.
- `pnpm --dir docs install` and `pnpm --dir docs run build` are the separate site
  install/build commands in `docs/package.json` and the verification scripts.
- `global.json` requests SDK `10.0.201` with `latestFeature` roll-forward;
  `Directory.Build.props` sets `net10.0` and C# 14. Build-time analyzer execution
  and code-style enforcement default to false.
- The solution restore/build commands are recorded in
  `.github/workflows/codeql-analysis.yml`. Discoverability contracts load the
  separately built Online assembly, so building only Governance is insufficient
  for those contracts. See
  `AioTieba4DotNet.Tests.Governance/Contracts/DiscoverableOnlineTestApiCategoryContract.cs`
  and `AioTieba4DotNet.Tests.Governance/AioTieba4DotNet.Tests.Governance.csproj`.

## Offline and Live Tests

- `AioTieba4DotNet.Tests.Governance/Scenarios/OrderedSuiteHostTests.cs` exposes both
  ordered suites as discoverable tests.
- `AioTieba4DotNet.Tests.Governance/Contracts/ThreadWebSocketOnlineContractTests.cs`
  makes live calls while carrying `Contract:Architecture`. Neither all Governance
  tests nor that category is an offline selection.
- `scripts/test-lane.sh` and `.ps1` require an explicit lane; `safe` is policy for
  the default choice, not a global runner filter. `sequence-dry-run` prints the
  plan, while `safe` and `restricted` execute live suites.
- The Safe ThreadWrite scenarios create/delete replies and change agrees.
  Messaging scenarios send private messages; compensation sends a further notice.
  Source: `AioTieba4DotNet.Tests.Online/Tiers/Safe/Features/ThreadWrite/Scenarios/ThreadWriteScenarioTests.cs`
  and `AioTieba4DotNet.Tests.Online/Tiers/Safe/Features/Messaging/Scenarios/MessagingScenarioTests.cs`.
- Missing credentials do not make an arbitrary test command network-free:
  `AioTieba4DotNet.Tests.Platform/Execution/OnlineExecutionGate.cs` permits
  capability `None` without account gating.
- Prefer a specifically inspected offline test class over a broad category or
  a fragile exclusion list in persistent development guidance.

## Coverage and CI

- `Directory.Build.targets` declares a total line/branch threshold of 100.
  `Directory.Packages.props` declares the `coverlet.msbuild` version, but current
  active project/targets files do not reference or import that collector.
- Test wrappers and `AioTieba4DotNet.Tests.Governance/Execution/OrderedSuiteHost.cs`
  disable collection. A successful ordinary test run is not evidence of achieved
  or enforced repository-wide 100% coverage.
- CI restores, builds, regenerates protobuf outputs, and packs. It does not run
  `dotnet test` or build the docs site. CodeQL analysis and release publishing are
  separate workflow responsibilities; the existing "build-only" policy is about
  the validation boundary, not the absence of those responsibilities.
- The CI regeneration check compares all of `AioTieba4DotNet/` to a clean Git
  checkout, not just generated files. A local dirty checkout needs a baseline-aware
  comparison. This bootstrap does not run the generator.
- `ProtoGenerator/ProtoGeneratorApp.cs` prefers bundled protoc and can fall back
  to PATH. `.github/scripts/validate-package-artifacts.sh` checks the expected
  package and symbol-package filenames/counts, not their full contents.
