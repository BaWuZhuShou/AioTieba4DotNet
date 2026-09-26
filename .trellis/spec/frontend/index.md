# Documentation Frontend Guidelines

The frontend is the VitePress documentation site in `docs/`. It documents the maintained v3, .NET 10-only library. It uses the default theme, Markdown pages, and one TypeScript configuration file; there is no custom component, hook, or application-state layer in this checkout.

These maintainer specs use English. The existing site is primarily Chinese (`lang: 'zh-CN'`), with some English reference material; preserve the language and purpose of the page being edited.

## Guidelines Index

| Guide | Read when |
| --- | --- |
| [Directory Structure](./directory-structure.md) | Adding, moving, or choosing the owner of a page |
| [Content Contracts](./content-guidelines.md) | Editing examples, public API reference, parity, or coverage claims |
| [Site Configuration](./site-configuration.md) | Changing navigation, home-page presentation, routes, or config types |
| [Quality Guidelines](./quality-guidelines.md) | Choosing local checks and reporting their actual scope |

## Pre-Development Checklist

- Read [Directory Structure](./directory-structure.md) and [Content Contracts](./content-guidelines.md), then the affected page and its linked source declarations.
- Read [Site Configuration](./site-configuration.md) for route or presentation changes, and inspect [config.mts](../../../docs/.vitepress/config.mts) for both navigation lists.
- Read [Quality Guidelines](./quality-guidelines.md) before selecting commands; documentation contract validation and site generation are separate checks.
- Consult [the shared thinking guides](../guides/index.md) for cross-layer claims or duplicated documentation rules.
- For library behavior or C# examples, also consult [the library guidelines](../backend/index.md), [library policy](../../../AioTieba4DotNet/AGENTS.md), and the applicable public contracts. Documentation must describe implemented behavior.

## Quality Check

- Check the v3/.NET 10 support statement, six module boundaries, signatures, and example parameter types against source.
- Check page links, stable anchors, home-page links, navigation, and required-document inventories when paths change.
- Keep parity ownership and direct online coverage ownership separate; preserve machine-read table formats.
- Run relevant checks from [Quality Guidelines](./quality-guidelines.md), recording missing prerequisites and skipped checks explicitly.

Primary policy sources are [.junie/guidelines.md](../../../.junie/guidelines.md), [docs/package.json](../../../docs/package.json), and [docs/.vitepress/config.mts](../../../docs/.vitepress/config.mts).
