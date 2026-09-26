# VitePress 站点配置

## 现有主题与配置边界

[docs/package.json](../../../docs/package.json) 固定 VitePress `1.6.4` 和 pnpm `10.14.0`。[docs/.vitepress/config.mts](../../../docs/.vitepress/config.mts) 是当前唯一的站点 TypeScript 源文件；检出中没有自定义 Vue 主题、组件、组合式函数、状态仓库或运行时数据获取层。

普通页面工作使用默认主题配置及 Markdown／frontmatter 能力。自定义主题或交互应用功能会引入新的维护边界，需要单独明确需求；不要暗示它们已经存在。

保持从 `vitepress` 导入的 `defineConfig` 作为带类型的配置入口。既有配置选择本地搜索和两级标题大纲：

```ts
search: {
  provider: 'local'
},
outline: {
  level: [2, 3],
  label: '本页目录'
}
```

该包提供 `dev`、`build` 和 `preview`，没有专用 `lint`／`typecheck` 脚本，也没有项目专用的运行时 schema 校验器。不要把 VitePress 构建描述为独立 TypeScript 检查或 C# 示例编译。保留配置推断，不要通过类型断言强行放入不支持的字段。

## 呈现与导航

- 除非任务修改本地化，否则保留 `zh-CN` 和中文导航标签。
- 在 `themeConfig.nav` 与 `themeConfig.sidebar` 中配置全局链接。两者分别维护；页面应出现在两处时须同时更新。
- 首页主视觉操作入口和功能卡片保留在 [docs/index.md](../../../docs/index.md) 的 frontmatter 中。它使用 `layout: home`，不是自定义组件。
- 保留 GitHub 链接与编辑链接模式共用的 `repoUrl`。当前编辑目标为 `master/docs/:path`；分支／仓库变化需要审查两个引用方。
- 使用有意义的标题。大纲显示二级和三级标题；[troubleshooting.md](../../../docs/guide/troubleshooting.md) 展示了按症状分组并附解释子节的结构。

## 路由、锚点与链接

导航使用 `/guide/getting-started`、`/reference/modules` 等不含扩展名的站点路由。Markdown 也包含相对 `.md` 链接，尤其在 related／reference 文档之间。遵循相邻页面的方式，同时验证目标和片段锚点。

[getting-started.md](../../../docs/guide/getting-started.md) 显式定义了 `#example-values` 与 `#ai-skill`；[首页](../../../docs/index.md) 和 [README](../../../README.md) 链接后者。重命名标题时保留被引用的锚点。

`ignoreDeadLinks` 是针对 README 相对链接写法的显式允许列表。保持其范围有限。修复缺失页面或错误锚点，不要将其设为 `true` 或增加宽泛抑制规则。README 相对链接是有意保留的例外，因为仓库 README 位于 VitePress 内容根目录之外。

## 格式与输出

遵循 [.editorconfig](../../../.editorconfig) 的 UTF-8、文件末尾换行和行尾空白要求。它全局声明四空格缩进和 CRLF，但既有配置／frontmatter 的部分位置使用两空格缩进。保持修改聚焦并保留附近结构，不要在内容工作中重排整份文档。

VitePress 输出和缓存目录被 [.gitignore](../../../.gitignore) 忽略。修改 Markdown／配置源文件后重新构建；不要修补生成的 HTML 或提交缓存文件。
