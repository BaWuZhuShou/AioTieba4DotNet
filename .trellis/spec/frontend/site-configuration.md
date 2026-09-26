# VitePress Site Configuration

## Existing Theme and Configuration Boundary

[docs/package.json](../../../docs/package.json) pins VitePress `1.6.4` and pnpm `10.14.0`. [docs/.vitepress/config.mts](../../../docs/.vitepress/config.mts) is the only current site TypeScript source; the checkout has no custom Vue theme, components, composables, stores, or runtime data-fetching layer.

Use the default theme's configuration and Markdown/frontmatter capabilities for ordinary page work. A custom theme or interactive application feature would introduce a new maintenance boundary and needs its own requirements; do not imply one already exists.

Keep `defineConfig` from `vitepress` as the typed configuration entry point. The existing config chooses local search and a two-level heading outline:

```ts
search: {
  provider: 'local'
},
outline: {
  level: [2, 3],
  label: '本页目录'
}
```

The package exposes `dev`, `build`, and `preview`; it has no dedicated `lint` or `typecheck` script and no project-specific runtime schema validator. Do not describe a VitePress build as a standalone TypeScript check or C# example compilation. Preserve config inference rather than adding casts to force unsupported fields through it.

## Presentation and Navigation

- Keep `zh-CN` and Chinese navigation labels unless the task changes localization.
- Configure global links in both `themeConfig.nav` and `themeConfig.sidebar`. They are maintained separately; update both when a page belongs in both surfaces.
- Keep home-page hero actions and feature cards in [docs/index.md](../../../docs/index.md) frontmatter. It uses `layout: home`, not a custom component.
- Keep the existing `repoUrl` shared by the GitHub link and edit-link pattern. The current edit target is `master/docs/:path`; a branch/repository change needs both consumers reviewed.
- Use meaningful headings. The outline displays levels 2 and 3; [troubleshooting.md](../../../docs/guide/troubleshooting.md) demonstrates symptom groups with explanatory subsections.

## Routes, Anchors, and Links

Navigation uses extensionless site routes such as `/guide/getting-started` and `/reference/modules`. Markdown also contains relative `.md` links, particularly between related/reference documents. Follow adjacent pages and verify both destinations and fragments.

[getting-started.md](../../../docs/guide/getting-started.md) explicitly defines `#example-values` and `#ai-skill`; [the home page](../../../docs/index.md) and [README](../../../README.md) link to the latter. Preserve referenced anchors when renaming headings.

`ignoreDeadLinks` is an explicit allowlist of README-relative link spellings. Keep it narrow. Fix a missing page or incorrect anchor rather than setting it to `true` or adding broad suppression patterns. README-relative links are an intentional exception because the repository README is outside the VitePress content root.

## Formatting and Output

Follow [.editorconfig](../../../.editorconfig) for UTF-8, final newlines, and trailing whitespace. It declares four-space indentation and CRLF globally, while existing config/frontmatter uses two-space indentation in places. Keep edits focused and preserve nearby structure rather than reformatting entire documents during content work.

VitePress output and cache directories are ignored by [.gitignore](../../../.gitignore). Change Markdown/config sources and rebuild; do not patch generated HTML or commit cache files.
