# 文档目录结构

## 按用途选择页面

当前信息架构由[仓库规则](../../../.junie/guidelines.md)、[README.md](../../../README.md) 和[站点配置](../../../docs/.vitepress/config.mts)定义。

| 位置 | 职责 |
| --- | --- |
| `README.md` | 包介绍、最小示例和文档入口链接 |
| `docs/index.md` | VitePress 首页 frontmatter，包含主视觉操作入口和功能链接 |
| `docs/guide/getting-started.md` | 安装、访客／认证用法、选项、DI 和工厂配置 |
| `docs/how-to/` | 面向任务的吧、主题、用户、消息和管理操作指南 |
| `docs/reference/modules.md` | 面向使用者的 API 签名、模块职责、模型和异常 |
| `docs/guide/advanced.md` | 传输、生命周期和多账号说明 |
| `docs/guide/troubleshooting.md` | 症状、原因和修复方法 |
| `docs/related/` | 当前迁移／发布说明、对齐台账和公开覆盖矩阵 |
| `docs/archive/todo.md` | 历史待办，与当前对齐记录职责分离 |
| `docs/.vitepress/config.mts` | 站点元数据、默认主题导航、搜索和链接处理 |
| `docs/package.json`、`docs/pnpm-lock.yaml` | 本地文档脚本和依赖版本 |

使用现有的小写、连字符文件名，将新内容放在最接近的读者主题下。例如，消息操作指南属于 [how-to/messages.md](../../../docs/how-to/messages.md)，其公开签名属于 [reference/modules.md](../../../docs/reference/modules.md)。

## 路由变化有多个引用方

移动页面前，在整个仓库搜索其路径和锚点。检查首页 frontmatter、`nav` 与 `sidebar`、同级页面链接、README 链接及 [skills/aiotieba4dotnet](../../../skills/aiotieba4dotnet) 下的使用者技能引用。

新增或删除必需指南也会改变 [verify-local.sh](../../../scripts/verify-local.sh) 和 [verify-local.ps1](../../../scripts/verify-local.ps1) 管理的文档契约。在同一项获授权变更中同步两个清单、本地验证清单和受影响的治理契约。只改侧栏不会更新该契约。

## 历史文件与生成输出

[release-notes-v2.md](../../../docs/release-notes-v2.md)、[migration-v1-to-v2.md](../../../docs/migration-v1-to-v2.md) 和 [adr-v3-contract.md](../../../docs/adr-v3-contract.md) 等旧文件仍然存在。当前导航聚焦于结构化的 v3 页面。不要把历史支持声明当作现状，也不要仅因这些文件不在主导航中就删除它们；先检查链接和迁移承诺。

[.gitignore](../../../.gitignore) 排除了 VitePress 的 `cache` 与 `dist` 目录。它们是生成输出，不是可编辑的文档源文件。

## 避免

- 为普通文档变更创建应用式组件、hook 或全局状态仓库；当前站点不存在这些结构。
- 在历史待办中维护当前 API 对齐记录。
- 将当前指南移回旧的平铺文档布局，或未检查引用方就更改路由。
