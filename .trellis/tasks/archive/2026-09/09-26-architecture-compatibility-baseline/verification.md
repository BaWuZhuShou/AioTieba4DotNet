# 兼容性基线验证记录

## 环境与原始产物

- 日期：2026-09-26；分支：`refactor/internal-architecture`。
- 用户已回复“批准”最终规划，子任务 A 已由 `task.py start` 激活。
- 原产品提交：`b9a86af79cf94db6d7f33abe8bd1e04e41f63678`。阶段 A 未修改产品、`global.json`、构建属性或依赖版本。
- 主机默认 SDK 为 `10.0.112`。使用 Microsoft 官方安装脚本将 SDK `10.0.201` 安装至 `/tmp/aiotieba-dotnet-10.0.201`；命令先 `source /tmp/aiotieba-task-env.sh`，CLI 目录和 NuGet 包缓存也隔离在 `/tmp`。
- `dotnet restore AioTieba4DotNet.sln --locked-mode`：5 个项目成功还原。
- 原始完整 Release 构建成功，0 错误、55 个既有 CS1591 XML 文档警告；日志 `/tmp/aiotieba-original-build.log`。后续增量构建的 0 警告不意味着这些旧警告已修复。
- 原始产品输出保存在 `/tmp/aiotieba-original-product/`；DLL SHA-256：`cb074149d844004e1959936d9a9a51e804a59540d7d0aa2bc0d675dd903fd5eb`。该副本用于生成基线。

## 原始行为与夹具修正

- 既有五类离线白名单第一次执行：35 项，34 通过、1 失败、0 跳过。结果 `/tmp/aiotieba-test-results/original-offline.trx`。
- 失败为 `MappingCoverageContractTests.FragLinkMapper_MissingOrRelativeUrlKeepsNonNullFallbackUrl`：`/relative` 在当前 Linux 上解析为 `file:///relative`，与该用例期望的相对 URL 回退不符。
- 仅将夹具与对应原始文本断言改为 `./relative`，保留 `about:blank` 回退断言，未修改产品 URI 处理。
- 新查询表征首次成功结果为 39 项，映射契约 12 项，共 51/51，0 跳过；结果 `/tmp/aiotieba-lookup-test-results/lookup-baseline.trx`。
- 内部查询描述符名缺乏公共可观察接缝，行为测试固定业务异常操作名和请求序列；子任务 B 仍须人工核对 `GetFidAsync`、`GetDetailAsync` 和 `${operationName}ResolveFid` 字面值。

## 文档与证据限制

- `bash scripts/verify-local.sh --validate-only` 最初因系统没有 `python` 命令退出 127；在 `/tmp/aiotieba-task-tools/python` 提供指向现有 Python 3 的别名后重跑。
- 重跑退出 1，实际错误为缺少 `.sisyphus/evidence/local-verification.manifest.json`。规划已确认四个必需证据文件均缺失；未创建虚假证据、未降低必需性、未运行 sync 模式。
- 未运行真实在线通道、未测总体覆盖率、未发布或推送。
- 本文记录实际结果；最终全范围检查由同目录 `check-result.md` 补充，产品变更后的结果属于子任务 B。

## 最终兼容性检查

- Trellis 检查代理核对并执行 7 类白名单，共 79/79 通过、0 跳过：生命周期 8、查询表征 39、映射 12、API 基线 5、用户/消息结构 2、直接 WS 11、状态 2。
- 完整 Release 再构建成功；结果 `/tmp/aiotieba-baseline-check-results/architecture-baseline-check.trx`。核对产品 DLL 与保留的原始 DLL 散列完全相同。
- 公开基线包含 163 类型、1,318 行；包括对类型继承、接口实现及泛型约束可空性编码的保守保护，编译器上下文敏感性已在基线 README 明确。
- 检查代理为取消用例增加等待上限，避免令牌传递回归时测试无限挂起；保持原取消断言。
- 工作提交：`9b1afc1`（`test: 建立公开契约与查询行为兼容基线`）。阶段 B 从此有效基线继续，A 可在 B 回退时保留。
