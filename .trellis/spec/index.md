# AioTieba4DotNet Development Specs

These specs describe the maintained v3 .NET client library and its documentation
site. Paths in code spans are repository-relative unless a section says otherwise.
Local `AGENTS.md` files and executable contracts remain the primary references;
these guides organize their rules for implementation and review.

## Choose a Layer

| Work area | Start here | Scope |
| --- | --- | --- |
| Library, protocol, generator, or test support | [Backend](backend/index.md) | `AioTieba4DotNet/`, `ProtoGenerator/`, the three active test projects, and validation scripts |
| Documentation content or site configuration | [Frontend](frontend/index.md) | `docs/` VitePress site and its consumer-facing documentation contracts |
| Reuse or changes across boundaries | [Thinking guides](guides/index.md) | Shared questions to apply alongside the relevant implementation specs |

## Before Development

1. Read the task's requirements and the nearest `AGENTS.md` for the affected code.
2. Follow the selected layer index's **Pre-Development Checklist**.
3. For a user-visible library change, read both layers: examples, parity records,
   module documentation, and the exported `skills/aiotieba4dotnet/` package can
   need updates alongside the C# implementation.
4. Use current source to resolve implementation details. Record any disagreement
   between source behavior and an existing policy instead of silently inventing
   a new contract.

## Review

Follow each affected layer index's **Quality Check** section. Select verification
that matches the changed behavior, and report what actually ran. The online
`safe` lane is distinct from an offline test, and a successful build does not
prove live service behavior or the repository's coverage target.

## Existing Policy Owners

- `AioTieba4DotNet/AGENTS.md`: library boundaries and local conventions.
- `ProtoGenerator/AGENTS.md`: generator ownership and regeneration rules.
- `.junie/guidelines.md`: durable cross-directory policy.
- `.editorconfig`, `Directory.Build.props`, `Directory.Build.targets`: code style,
  compilation defaults, and coverage configuration.
- `docs/related/parity.md`: upstream parity, implementation mappings, and auth notes.
- `docs/related/public-api-coverage-matrix.md`: online API discoverability.

This repository has no application database layer. The backend guide covers
in-memory session/cache state. The frontend is a documentation site; application
component, hook, and state-management frameworks are not assumed.
