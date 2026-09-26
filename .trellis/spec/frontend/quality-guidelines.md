# 文档质量规范

## 按变更选择检查

[docs/package.json](../../../docs/package.json) 提供站点构建／服务脚本。[verify-local.sh](../../../scripts/verify-local.sh) 和 [verify-local.ps1](../../../scripts/verify-local.ps1) 验证仓库文档／证据契约。这些是独立检查，都不能证明真实 API 行为。

从仓库根目录运行命令：

| 目的 | 命令 | 前置条件与范围 |
| --- | --- | --- |
| 安装站点依赖 | `pnpm --dir docs install` | Node 和声明的 pnpm 版本，以及包访问权限或缓存。会写入依赖，并可能更新锁文件。 |
| 构建站点 | `pnpm --dir docs run build` | 已安装文档依赖；检查站点生成和已配置的链接检查。 |
| 本地查看修改 | `pnpm --dir docs run dev` | 已安装依赖；启动开发服务器。 |
| 查看构建输出 | `pnpm --dir docs run preview` | 站点构建成功；启动预览服务器。 |
| 使用 Bash 验证既有契约 | `bash ./scripts/verify-local.sh --validate-only` | Bash、PATH 中的 `python` 和必需的本地证据文件。 |
| 使用 PowerShell 验证既有契约 | `pwsh ./scripts/verify-local.ps1 -ValidateOnly` | PowerShell 和必需的本地证据文件。 |

验证器检查契约后会打印文档安装／构建命令，但**不会**执行这些命令。不带仅验证选项时，它会先写入清单再验证。审查既有产物时选择仅验证模式，不得将此检查描述为站点构建。

仅修改 Trellis 规范时，检查源码引用、本地链接、索引完整性、模板残留和空白格式。这些文件位于站点内容根目录之外，不因修改它们而要求安装依赖或运行在线测试。

## 本地证据前置条件

两个验证器和 [ParityArtifactRetentionContract.cs](../../../AioTieba4DotNet.Tests.Governance/Contracts/ParityArtifactRetentionContract.cs) 都要求 `.sisyphus/evidence/` 下存在以下文件组合：

- `parity-truth-freeze.json`
- `parity-gap-ledger.json`
- `local-verification.manifest.json`
- `local-verification.manifest.schema.json`

该目录被 [.gitignore](../../../.gitignore) 忽略，因此检出后可能缺少所需产物。本次规范初始化时四个文件均不存在。缺失时应报告前置条件缺口，不得编造证据或删除契约以获得通过。仅同步清单无法重建上游证据或 schema。

新增／删除必需指南时，必须同步 Bash 验证器中的 `required_docs`、PowerShell 验证器中的 `$requiredDocs`、清单及受影响的本地契约测试。[ParityArtifactRetentionContractTests.cs](../../../AioTieba4DotNet.Tests.Governance/Contracts/ParityArtifactRetentionContractTests.cs) 展示了保留产物检查。

## 审查内容与渲染行为

- 对照公开声明检查示例和签名，包括数字 ID 类型、可选参数、模块归属和释放行为。
- 检查导航、首页目的地和引用锚点。README 死链接例外应保持有限范围。
- 渲染站点变化时，使用开发或预览模式查看修改的页面、导航、代码块和表格。手工查看与构建成功分别报告。
- 矩阵变化时，审查[内容契约](./content-guidelines.md)中的解析器／分类契约；站点构建不强制这些语义规则。
- 示例保持示意性质，不包含凭据。不要为验证文档修改执行写入示例或在线通道。

## 验证边界

[仓库规则](../../../.junie/guidelines.md) 要求测试和文档／证据检查在本地或由代理执行。当前 [CodeQL 工作流](../../../.github/workflows/codeql-analysis.yml) 执行 .NET 构建／代码生成／打包检查及分析，不运行文档构建或 `dotnet test`。Actions 通过不代表站点或其真实调用示例已验证。

前端包没有 lint、独立 typecheck 或浏览器测试脚本。除非实际运行了明确工具并说明其范围，否则不得报告这些检查通过。同样，`safe` 是在线通道，`sequence-dry-run` 只打印有序计划；两者都不是离线文档测试。
