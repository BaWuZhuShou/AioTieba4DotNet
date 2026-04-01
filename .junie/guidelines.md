# Junie 编码指南 - AioTieba4DotNet

本文件记录当前仓库已经稳定下来的长期维护规则。局部实现细节放在最近的 `AGENTS.md`，这里只保留跨目录、跨任务都成立的规则。

## 1. 产品线与范围
- 当前维护线是 **v3**。
- 当前支持矩阵是 **`net10.0` only**。
- 不要在任何活动 guide、说明或发布规则里继续声称 `net8.0`、`net9.0`、multi-target，或仍在维护 v2 发布线。
- `aiotieba/` 只用于 upstream 行为比对，不属于交付中的 .NET 产品，不属于 coverage scope，也不是 release scope。

## 2. 公开契约基线
- v3 主入口固定为 `TiebaClient`、`ITiebaClient`、`AddAioTiebaClient(...)`、`ITiebaClientFactory`、`TiebaClientFactory`。
- v3 稳定公开模块固定为六个：`Forums`、`Threads`、`Users`、`Admins`、`Messages`、`Client`。
- `Messages` 负责消息读取、消息发送、已读回执和 push 解析。
- `Client` 只负责生命周期辅助能力，例如 `InitWebSocketAsync()`、`InitZIdAsync()`、`SyncAsync()`。
- `Client.cs` 是过时兼容包装层。除非任务明确要求兼容性维护，不要把新能力继续堆到这里。

## 3. Parity truth 与文档契约
- `docs/parity-v3.md` 是 **authoritative parity ledger**。
- `docs/todo.md` 现在只保留为历史 backlog 和旧记录，不能再当成当前 parity truth。
- 当前用户文档 IA 以 `README.md` 为入口，向下连接：
  - `docs/getting-started.md`
  - 四份 task guide
  - `docs/modules.md`
  - `docs/advanced.md`
  - `docs/troubleshooting.md`
  - `docs/migration-v2-to-v3.md`
  - `docs/release-notes-v3.md`
  - `docs/parity-v3.md`
- `scripts/verify-local.ps1` 和 `scripts/verify-local.sh` 中的 `requiredDocs` 列表是本地文档契约检查的真源。新增或移除必需 guide 时，必须同步更新脚本与受影响文档。

## 4. upstream 对齐规则
- 任何 API 的参数语义、请求打包、响应解析、错误处理和可观察行为，都必须优先对齐 Python `aiotieba`。
- 对齐时以 upstream 导出路径和导出符号为准，不要把友好别名或旧 backlog 名称当成权威命名。
- 不要擅自增加 upstream 不存在的请求参数、行为分支或传输语义，除非任务明确要求产品偏离。
- `PythonApi` 标记必须保持可搜索，方便 parity 追踪和实现比对。

## 5. 生成代码与 ProtoGenerator
- `.proto` 是可维护真源，生成出来的 `AioTieba4DotNet/Api/Protobuf/*.cs` 与 `AioTieba4DotNet/Api/*/Protobuf/*.cs` 都是派生产物。
- 严禁手改生成的 protobuf C# 文件。需要变更时，改 `.proto` 后运行 `dotnet run --project ProtoGenerator/ProtoGenerator.csproj`。
- `ProtoGenerator` 负责扫描 `AioTieba4DotNet/Api/**/*.proto`，并把生成结果写回对应目录。
- 生成输出属于狭义排除项，不应被写成 coverage 目标，也不该被当成手写产品代码来维护。

## 6. 测试分层与执行规则
- 当前测试结构固定拆分为：
  - `AioTieba4DotNet.Testing`: 共享基础设施、fixture gate、cleanup orchestration、sequencing manifest
  - `AioTieba4DotNet.Tests.Deterministic`: 离线 deterministic tests，也是 coverage-bearing lane
  - `AioTieba4DotNet.Tests.Integration`: 受控真实链路验证
  - `AioTieba4DotNet.Tests.Live`: 带凭据、带写操作、带回滚意识的 live 验证
- 本地和 agent 环境统一通过 `scripts/test-lane.*` 调度四条 lane：`deterministic`、`integration`、`live`、`sequence-dry-run`。
- `AioTieba4DotNet.Testing/test-sequencing.manifest.json` 是 integration 和 live 阶段顺序的唯一真源，当前顺序固定为：
  `ForumFoundation -> ForumExtensions -> ThreadRead -> ThreadWriteModeration -> UserSocial -> MessagingClient -> Cleanup`
- `Cleanup` 是 synthetic compensation wave，不是可直接运行的 MSTest category filter。
- 需要真实链路或 fixture gate 的测试应复用 `TestBase` 与共享 gate，而不是私自读取 secrets 或绕过 safe fixture。

## 7. CI 与本地验证边界
- GitHub Actions 必须保持 **build-only**。
- GitHub Actions 不运行任何 `dotnet test`，也不运行 secret-backed lane。
- 当前 release gate 只允许以下检查族：`restore`、`build`、`codegen`、`docs-contract`、`packaging`、`evidence-presence`。
- 测试执行、凭据门控、live cleanup 和 lane 证据属于本地或 agent-run 范围，通过 `scripts/test-lane.*` 与 `scripts/verify-local.*` 管理。
- `.sisyphus/evidence/local-verification.manifest.json` 与其 schema 是本地验证契约的一部分，不能把它们降级成可选材料。

## 8. Coverage scope
- Coverage truth 是 repo-level total `100/100`，定义在 `Directory.Build.targets`。
- 当前 coverage scope 只面向维护中的手写代码，重点是 `AioTieba4DotNet/**` 和 `ProtoGenerator/**`。
- 允许的窄排除项包括：
  - 生成的 protobuf 输出
  - 测试支持代码 `AioTieba4DotNet.Testing/**`
  - 编译器生成文件，例如 `*.g.cs`、`*.generated.cs`
  - `obj/**`
- 不要把 docs、`.sisyphus/**`、测试项目、或 `aiotieba/**` 写成 coverage 真源。

## 9. Legacy deletion 规则
- 旧实现、旧入口、旧说明的删除，必须以当前 v3 公开契约、文档承诺和 release 叙事为边界。
- 不要因为发现 legacy 文件就直接删。先确认它是否仍然承担：
  - 兼容入口
  - 文档承诺
  - parity 说明中的显式边界
  - release/migration 中仍然提到的迁移路径
- 遗留清理任务与本次 guide 同步任务要分开处理。guide 可以说明删除规则，但不能把尚未批准的删除动作伪装成既成事实。

## 10. 版本与发布规则
- 版本判断围绕 NuGet 使用者看到的公开契约、支持的 TFM、文档承诺和可观察行为。
- 目标框架支持的变化属于版本策略的一部分。对当前 v3 而言，`net10.0` 是唯一活动支持面。
- 正式发布仍然使用 `vMAJOR.MINOR.PATCH` 标签驱动，发布工作流从标签取得版本号。
- 如果改动触碰公开签名、公开异常契约、文档承诺、默认行为或支持矩阵，先按 SemVer 判断，再决定 release notes 和 migration 文案是否需要更新。

## 11. 维护动作要求
- 当稳定规则发生变化时，先更新实现附近的局部 guide，再把跨目录都成立的部分回写到本文件。
- 避免把同一大段规则复制到 repo root、library、testing、generator 多处。每个 guide 只写它自己该负责的范围。
- 如果活动 guide 出现 stale guidance，例如 v2、多目标框架、旧 tests project、`docs/todo.md` 真源说法，应该优先修正。
