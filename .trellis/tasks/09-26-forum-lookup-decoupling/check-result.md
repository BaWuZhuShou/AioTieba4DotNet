# 吧信息查询解耦检查结果

日期：2026-09-26。检查角色：`trellis-check`。范围：子任务 B 相对 A 工作提交 `9b1afc1` 的变化，以及父任务相对原产品提交 `b9a86af79cf94db6d7f33abe8bd1e04e41f63678` 的累计产品边界。

## 发现的问题（已修复）

本阶段未发现需要修复的代码问题。检查代理没有修改产品、测试或 A 基线，只新增本检查记录。主会话另已纠正 A 验证文档中既有警告的分类描述。

## 发现的问题（未修复）

- **缺少治理前置材料**：四个既有 `.sisyphus/evidence/` 产物仍缺失。文档验证因缺少 `local-verification.manifest.json` 退出 1，详见 [验证记录](verification.md)。不属于本次内部重构的产品缺陷，未伪造证据或修改验证门槛。
- **沿用 A 的静态快照边界**：原始可空性编码是有限的保守保护，可能受编译器上下文变化影响；不等同完整 ABI、运行时序列化或在线兼容性证明。本次原始快照未改变，实际比较无差异。
- **已有构建警告**：首次完整构建的 55 项警告为 CS1591 47 项、CS1573 2 项、CS9107 6 项；主会话核对与原始构建去重警告集合相同。本检查不扩大范围修复既有文档／构造警告。

没有未解决的本次代码缺陷、公共接口决策或模块边界问题。任务间相对链接的归档收尾由主会话统一处理。

## 真实路径核对

- `ForumIdentityResolver.GetFidAsync` 保留取消检查 → 缓存 → `GetFidAsync` 请求 → 原过滤回填。
- `GetDetailAsync` 保留取消检查、`GetDetailAsync` 描述符、`HttpOnly`、原 `(long)fid` 转换及按响应身份回填。
- `GetFnameAsync` 仍用 `IsNullOrEmpty` 判断命中；先通过详情请求按响应 ID 条件回填，再以请求 ID 无条件 `SetForumName`。空白名称、零 ID 和两个 ID 不同的行为均保持。
- `ResolveFidForOperationAsync` 保留缓存命中短路；未命中执行传入业务操作的认证／TBS 准备，然后直接调用 `RequestFidAsync`。准备后没有再次查询缓存；描述符仍为 `${operationName}ResolveFid`，只按非 0 ID 回填。
- Admin 的 10 个解析调用保留原操作名、能力和参数／取消顺序。普通查询与管理查询只共享负责请求组装的私有方法，未混合两条路径的认证／缓存策略。
- Thread 的 15 处 ID、2 处名称和 1 处分类调用只替换依赖目标；User 的 1 处 ID 调用同样保留顺序。`GoodAsync` 仍先认证／TBS，再查 ID、分类并加精；分类重载实现及空分类短路未修改。
- 所有 `new GetFid(...)` 请求创建已在协议层收敛为 1 处；协议层缓存读写全部位于身份服务。组合根每客户端创建一份原缓存和一份身份服务；分类接口由同一 ForumProtocol 提供。resolver 不依赖业务协议，没有反向循环。
- 新结构测试只检查类型、构造参数和字段类型，没有读取在线环境或发出请求；既有在线 WebSocket 测试只调整两处内部对象构造，未改变断言，且本次仅编译未执行。

## 变更范围与基线完整性

产品代码只有 8 个文件：`TiebaClientComposition`、Forum/Admin/Thread/User 四个协议，以及新增的 `ForumIdentityResolver`、`IForumIdentityResolver`、`IForumCategoryResolver`。`AioTieba4DotNet/AGENTS.md` 的变化是主会话维护的内部协作规则。

累计比较确认：Session、Transport、Api／生成代码、Models、Modules、公开 Contracts、Internal／ForumInfoCache、SDK pin、公共构建配置、依赖及 CI 均没有变化。

A 的 `public-api.txt`、维护说明、提取器、API 有效性测试、39 项查询行为测试及受控 handler 相对 `9b1afc1` 无差异；不是通过改写预期取得通过。B 的测试变化仅新增结构契约及适配两处旧内部构造。

规范已由主会话同步目录结构、会话／缓存协作契约及跨目录指南。没有平台配置或生成模板变化，不需要补模板／检测入口。

## 验证

先执行 `source /tmp/aiotieba-task-env.sh`，使用 SDK `10.0.201`。

- Lint：仅对本轮 8 个产品与 2 个测试 C# 文件执行 `dotnet format style <项目> --no-restore --verify-no-changes --severity warn --include <绝对路径>`，两个项目均退出 0。结论限于所列文件及警告级风格检查，不代表全仓库分析器或全部建议均通过。
- 空白：`git diff --check` 通过；4 个新增 C# 文件另用 `git diff --no-index --check /dev/null <文件>` 检查，防止遗漏未跟踪文件。
- 锁定还原：`dotnet restore AioTieba4DotNet.sln --locked-mode` 通过。
- 类型检查：`dotnet build AioTieba4DotNet.sln --configuration Release --no-restore` 通过；本次增量构建 0 警告、0 错误，不表示前述 55 项既有警告已修复。
- 测试：A 的七类完整名称过滤追加 `ForumLookupArchitectureContractTests`，使用 Release、`--no-build --no-restore -p:CollectCoverage=false`；**80/80 通过，0 失败、0 跳过**。

| 实际执行类 | 通过数 |
| --- | ---: |
| `PublicApiBaselineContractTests` | 5 |
| `ForumLookupBehaviorContractTests` | 39 |
| `ForumLookupArchitectureContractTests` | 1 |
| `ClientLifecycleAndCompositionContractTests` | 8 |
| `UserAndMessageSurfaceContractTests` | 2 |
| `ThreadWebSocketDirectContractTests` | 11 |
| `StateParityContractTests` | 2 |
| `MappingCoverageContractTests` | 12 |

已解析 TRX 核对八类均实际执行，`notExecuted`、`inconclusive`、`aborted` 均为 0。

完整构建后，产品、Governance、Online 三个 Release 输出目录中的 `AioTieba4DotNet.dll` SHA-256 均为 `b0b4362d1af8c7ca66e8771b0791f9639bcf231c18ff7e28d5053e11a5e790d9`，防止反射契约加载旧 Online 副本。重构后 DLL 与 A 的原 DLL 字节不同符合预期；公开元数据比较仍与 A 的固定文本完全一致。

检查证据：

- `/tmp/aiotieba-decoupling-check-restore.log`
- `/tmp/aiotieba-decoupling-check-build.log`
- `/tmp/aiotieba-decoupling-check-tests.log`
- `/tmp/aiotieba-decoupling-check-results/forum-lookup-decoupling-check.trx`
- `/tmp/aiotieba-decoupling-style-AioTieba4DotNet.log`
- `/tmp/aiotieba-decoupling-style-AioTieba4DotNet.Tests.Governance.log`

子任务 B 及父任务产品变更的适用离线检查通过，可由主会话按授权创建独立工作提交并收尾。未运行真实在线通道、未提供总体覆盖率或发布证明；检查代理未执行提交、推送或合并。
