# Bootstrap Evidence and Review Notes

## Initial Findings

- The product is a .NET 10 client library, with C# 14 defaults in `Directory.Build.props`; it is not an ASP.NET application.
- Existing policy sources are `AioTieba4DotNet/AGENTS.md`, `ProtoGenerator/AGENTS.md`, `.junie/guidelines.md`, and `.editorconfig`.
- `docs/package.json` and `docs/.vitepress/config.mts` identify a real documentation frontend. Inspect these before deciding which frontend scaffold topics apply.
- Backend and frontend spec files are initial templates. `.trellis/spec/guides/` contains existing generic thinking guides, which are outside the authored spec scope unless demonstrably incompatible.
- The three active test projects are Platform (shared support), Online (runnable scenarios), and Governance (ordered suite host and retained offline contracts). Read wrappers and attributes to distinguish offline verification from live execution; `safe` is an online lane, not an offline test guarantee.
- `docs/related/parity.md` owns upstream parity, implementation mappings, and auth notes. `docs/related/public-api-coverage-matrix.md` owns direct online API discoverability.
- The checkout initially has pre-existing changes to `AGENTS.md`, plus untracked `.codex/`, `.gitattributes`, and `.trellis/`. Preserve unrelated initialization files and do not sweep them into task commits.
- No GitNexus/ABCoder integration is exposed in this session. Direct source inspection is the available analysis method.

## Writing and Verification Contract

- Follow `.agents/skills/trellis-spec-bootstrap/references/spec-writing.md` and the PRD.
- All authored specs must use English prose, concrete source paths, concise examples, and explicit local anti-patterns.
- Remove or reshape non-applicable template files; do not fabricate ORM, migration, React, or application-state conventions.
- Check each authored local link/path, final indexes, template markers, whitespace, and representative claims against source.
- Document command intent and its prerequisites. Do not claim tests, builds, or coverage passed unless actually run. This task changes only Trellis documents and does not require live tests or code generation.
