# 兼容性基线检查结果

日期：2026-09-26。检查角色：`trellis-check`。用户已批准父任务实施；本检查只覆盖子任务 A，未提交、推送或触发在线请求。

## 发现的问题（已修复）

- `Contracts/PublicApiSnapshot.cs` 最初仅以 CLR 类型名表达继承与约束，遗漏泛型基类／接口／约束中的可空性。已将问题交由原 API 实施代理修复：类型／泛型方法上下文、`InterfaceImpl` 和 `GenericParamConstraint` 行的可空性编码进入快照，使用真实 C# 成对夹具验证差异；初始预期重新从原始产品 DLL 显式提取，没有使用重构后的产品。
- `Contracts/ForumLookupBehaviorContractTests.cs` 的进行中取消测试原本直接等待 `pending`。如果未来取消令牌透传回归，会无限挂起。检查代理将断言等待改为 `pending.WaitAsync(BarrierTimeout)`，仍要求取消异常，超时会失败。
- 新增 `ForumLookupBehaviorContractTests.cs` 和 `ForumLookupTestTransport.cs` 的 CRLF 被当前 Git 配置判为行尾空白。检查代理统一为相邻 Governance 文件使用的 LF，未调整现有仓库配置或重排既有文件。根 `.editorconfig` 的 CRLF 建议与当前 Git／文件实践不一致，该仓库级策略不属于本次变更。
- 既有 `MappingCoverageContractTests` 将 `/relative` 假定为跨平台相对 URI，在 Linux 上实际得到 `file:///relative`。原行为实施代理仅把夹具与文本断言改成 `./relative`，保留 `about:blank` 回退断言；已核对没有修改产品 URI 行为。

## 发现的问题（未修复）

- **缺少验证前置材料**：四个必需的 `.sisyphus/evidence/` 产物仍缺失。主会话已实际运行 `bash scripts/verify-local.sh --validate-only`，因缺少 `local-verification.manifest.json` 退出 1；详见 [验证记录](verification.md)。未伪造材料或降低门槛，不能称全部治理验证通过。
- **已明确的格式边界，不阻止本次内部迁移**：类型／泛型方法的原始可空上下文可能因编译器或内部成员改变产生保守差异；维护说明已要求调查并归一化编码，不能直接接受新产品快照。本检查没有把此有限快照扩展为通用 ABI 工具，也不声称穷尽全部二进制兼容性。
- **内部描述符的验证边界**：当前公开路径不能直接观察 `${operationName}ResolveFid` 等内部名称。现有用例固定业务异常操作名、认证顺序和请求序列；子任务 B 仍需人工核对这些内部描述符字面值。没有为测试新增产品公共接缝。

## 验证

所有 .NET 命令先执行 `source /tmp/aiotieba-task-env.sh`。

- SDK：`10.0.201`，符合未修改的 `global.json`。
- 锁定还原：`dotnet restore AioTieba4DotNet.sln --locked-mode` 通过。
- 类型检查／编译：完整解决方案 `dotnet build AioTieba4DotNet.sln --configuration Release --no-restore` 通过，本次增量构建 0 警告、0 错误。原始干净构建中的 55 个既有警告未在本任务修复；经主会话核对完整原日志，分别为 CS1591 47 项、CS1573 2 项、CS9107 6 项。
- Lint：针对本轮四个新增 C# 文件及 Mapping 夹具文件的 `dotnet format style ... --no-restore --verify-no-changes --severity warn --include <绝对路径>` 通过；`dotnet format whitespace ... --verify-no-changes` 通过。此结论限于所列文件和检查配置，不代表全仓库分析器／所有建议均通过。
- 空白检查：`git diff --check` 通过；额外对新文件使用 `git diff --no-index --check /dev/null <文件>`，避免普通 diff 漏掉未跟踪文件。
- 离线测试：按 [执行计划](implement.md) 的七类完整名称过滤，使用 `--configuration Release --no-build --no-restore -p:CollectCoverage=false`，**79 通过、0 失败、0 跳过**。修复取消等待和换行后重建并重跑相同白名单；没有运行整个 Governance 套件或 `safe`／`restricted`。

| 类 | 实际通过数 |
| --- | ---: |
| `PublicApiBaselineContractTests` | 5 |
| `ForumLookupBehaviorContractTests` | 39 |
| `ClientLifecycleAndCompositionContractTests` | 8 |
| `UserAndMessageSurfaceContractTests` | 2 |
| `ThreadWebSocketDirectContractTests` | 11 |
| `StateParityContractTests` | 2 |
| `MappingCoverageContractTests` | 12 |

证据文件：

- `/tmp/aiotieba-baseline-check-build.log`
- `/tmp/aiotieba-baseline-check-tests.log`
- `/tmp/aiotieba-baseline-check-results/architecture-baseline-check.trx`
- `/tmp/aiotieba-baseline-style-check.log`
- `/tmp/aiotieba-baseline-whitespace-check.log`

TRX 已逐类核对，全部七类均执行，不存在零用例误报。产品目录、SDK pin、依赖和公共构建配置无修改。当前 Release 产品 DLL 与保存的原始 DLL SHA-256 同为 `cb074149d844004e1959936d9a9a51e804a59540d7d0aa2bc0d675dd903fd5eb`；API 快照 1,318 行仍来自 `b9a86af79cf94db6d7f33abe8bd1e04e41f63678` 的产品。

新 handler 只执行预先声明的内存响应，不创建网络 handler；发现意外请求会失败。测试经实际工厂／模块路径覆盖双向缓存、响应 ID 与请求 ID 差异、空值边界、失败／取消、管理认证和 TBS 顺序、认证期间并发填缓存、Thread 分类及跨客户端隔离。既有直接构造／DI／工厂对象图测试继续通过。

规范同步由主会话维护 `.trellis/spec/backend/quality-guidelines.md`，记录显式基线维护、离线白名单和证据边界。阶段 A 的代码与适用离线检查通过，可由主会话按授权创建独立本地工作提交，再交付子任务 B；此结论不是在线、全覆盖率或所有治理材料就绪证明。
