# Documentation Directory Structure

## Choose the Page by Its Purpose

The current information architecture is defined by [repository policy](../../../.junie/guidelines.md), [README.md](../../../README.md), and [the site configuration](../../../docs/.vitepress/config.mts).

| Location | Responsibility |
| --- | --- |
| `README.md` | Package introduction, minimal example, and documentation entry links |
| `docs/index.md` | VitePress home-page frontmatter with hero actions and feature links |
| `docs/guide/getting-started.md` | Installation, visitor/authenticated usage, options, DI, and factory setup |
| `docs/how-to/` | Task-oriented forum, thread, user, message, and admin recipes |
| `docs/reference/modules.md` | Consumer-facing API signatures, module responsibilities, models, and exceptions |
| `docs/guide/advanced.md` | Transport, lifetime, and multi-account explanations |
| `docs/guide/troubleshooting.md` | Symptoms, causes, and corrective actions |
| `docs/related/` | Current migration/release notes, parity ledger, and public coverage matrix |
| `docs/archive/todo.md` | Historical backlog, separate from current parity ownership |
| `docs/.vitepress/config.mts` | Site metadata, default-theme navigation, search, and link handling |
| `docs/package.json`, `docs/pnpm-lock.yaml` | Local documentation scripts and dependency versions |

Use the existing lowercase, hyphenated file names and put new material with its nearest reader-facing topic. For example, a message recipe belongs in [how-to/messages.md](../../../docs/how-to/messages.md); its public signature belongs in [reference/modules.md](../../../docs/reference/modules.md).

## Route Changes Have Multiple Callers

Before moving a page, search for its path and anchors across the repository. Check home-page frontmatter, both `nav` and `sidebar`, sibling-page links, README links, and consumer skill references under [skills/aiotieba4dotnet](../../../skills/aiotieba4dotnet).

Adding or removing a required guide also changes the documentation contract owned by [verify-local.sh](../../../scripts/verify-local.sh) and [verify-local.ps1](../../../scripts/verify-local.ps1). Keep both inventories, the local verification manifest, and affected governance contracts aligned in the same authorized change. A sidebar edit alone does not update that contract.

## Historical Files and Generated Output

Older files such as [release-notes-v2.md](../../../docs/release-notes-v2.md), [migration-v1-to-v2.md](../../../docs/migration-v1-to-v2.md), and [adr-v3-contract.md](../../../docs/adr-v3-contract.md) still exist. Current navigation focuses on structured v3 pages. Do not present historical support statements as current, or delete those files merely because they are outside the main navigation; check links and migration commitments first.

[.gitignore](../../../.gitignore) excludes VitePress `cache` and `dist` directories. Treat those as generated output, not editable documentation source.

## Avoid

- Creating application-style components, hooks, or global stores for ordinary documentation changes; none is present in the current site.
- Making the historical backlog the place to maintain current API parity.
- Moving current guides into the old flat document layout or changing routes without checking callers.
