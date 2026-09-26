# Bootstrap Project Development Guidelines

## Goal

Replace the initial Trellis spec scaffolding with practical, source-backed guidance for the maintained AioTieba4DotNet v3 repository.

## Scope

- Write English guidelines under `.trellis/spec/` for the .NET library, its protobuf generator and test support, and the VitePress documentation site.
- Import existing conventions from `AioTieba4DotNet/AGENTS.md`, `ProtoGenerator/AGENTS.md`, `.junie/guidelines.md`, `.editorconfig`, project manifests, and maintained documentation; verify claims against representative source and tests.
- Adapt or remove template topics that do not describe this repository. Document session/cache ownership instead of inventing database conventions; describe the documentation frontend instead of inventing a React application.
- Preserve the existing shared thinking guides unless a concrete incompatibility requires a change.
- Maintain task artifacts and verification evidence for this bootstrap.

## Constraints

- This is a documentation-only task. Do not change product code, generated code, test behavior, build configuration, existing guides outside Trellis, or unrelated initialization changes.
- Preserve the .NET 10-only public contract and the six public modules, upstream parity ownership, and current test topology.
- Cite real repository paths and concise examples. Distinguish observed behavior from desired future architecture and note material discrepancies in existing guides.
- Use direct source inspection when optional repository-analysis integrations are unavailable.
- Do not execute live online lanes, mutate service fixtures, or regenerate protobuf outputs for this documentation task.

## Acceptance Criteria

- [x] Backend/library guidelines explain architecture, public contracts, request/transport behavior, mapping/code generation, state ownership, exceptions, logging, and verification.
- [x] Frontend guidelines describe the actual VitePress documentation site; irrelevant component/hook/state boilerplate is removed or replaced.
- [x] Important rules and examples are backed by current source, tests, or existing policy documents.
- [x] Spec indexes match their files and provide pre-development and quality-check entry points.
- [x] No unfilled template sections or broken local references remain in the authored specs.
- [x] Review and documentation checks are recorded; product source and unrelated initial work remain unchanged.

## Execution Boundary

The user selected this existing bootstrap task on 2026-09-26. It was already marked `in_progress`, but was not bound to this session. This is a bounded documentation bootstrap with no runtime design change; the PRD and research/context manifests are sufficient planning artifacts.
