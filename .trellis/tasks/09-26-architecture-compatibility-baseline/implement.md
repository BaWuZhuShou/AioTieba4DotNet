# 兼容性基线执行计划

## 进入条件

- [x] 用户已批准父任务最新最终规划摘要，本任务三份规划文档和两个 JSONL 均已就绪。
- [x] 按 Trellis 阶段 1.4 激活本子任务，然后派发 `trellis-implement`；任何实现代理均只负责本子任务范围。

## 执行顺序

1. 阅读上下文和相关测试，确认产品代码仍对应 `b9a86af` 的行为；记录实际产品基线提交和工作区状态。
2. 先提供符合原 `global.json` 的 SDK；可使用隔离的 SDK 路径，不修改仓库 pin。校验 `dotnet --version` 后锁定还原和 Release 构建。无法取得 SDK/依赖时停止依赖它的基线生成，不伪造程序集。
3. 在未改产品逻辑的前提下加入公开元数据提取/比较、显式初始快照及其有效性验证。
4. 增加吧信息查询行为表征，先在原实现上通过；沿用已有接缝，任何必要测试支持修改仅限 Governance/Platform。
5. 运行下面的验证，记录实际测试数、通过/失败、环境与前置缺失。
6. 派发 `trellis-check` 复核公开基线覆盖、行为断言及无在线副作用；主会话完成规范检查与本地工作提交，再按工作流归档和记录会话。
7. 将工作提交号和通过证据交给子任务 B；没有 A 的有效基线不能开始 B。

## 验证命令

```bash
dotnet --version
dotnet restore AioTieba4DotNet.sln --locked-mode
dotnet build AioTieba4DotNet.sln --configuration Release --no-restore
dotnet test AioTieba4DotNet.Tests.Governance/AioTieba4DotNet.Tests.Governance.csproj --configuration Release --no-build --no-restore --filter 'FullyQualifiedName~AioTieba4DotNet.Tests.Governance.Contracts.PublicApiBaselineContractTests|FullyQualifiedName~AioTieba4DotNet.Tests.Governance.Contracts.ForumLookupBehaviorContractTests|FullyQualifiedName~AioTieba4DotNet.Tests.Governance.Contracts.ClientLifecycleAndCompositionContractTests|FullyQualifiedName~AioTieba4DotNet.Tests.Governance.Contracts.UserAndMessageSurfaceContractTests|FullyQualifiedName~AioTieba4DotNet.Tests.Governance.Contracts.ThreadWebSocketDirectContractTests|FullyQualifiedName~AioTieba4DotNet.Tests.Governance.Contracts.StateParityContractTests|FullyQualifiedName~AioTieba4DotNet.Tests.Governance.Contracts.MappingCoverageContractTests' -p:CollectCoverage=false
bash scripts/verify-local.sh --validate-only
git diff --check
```

两个新增类名为约定目标，实施后确认命名与筛选匹配且每个预期类均被执行，不能以零用例成功作证。现有五类已由研究确认离线；执行前检查新增测试仍使用受控传输。不得扩大到未筛选套件或 `safe` 通道。

## 文件责任与回退点

- 主要范围：Governance 的新契约、文本基线及项目资源配置；必要的既有测试支持。产品、生成代码、CI、包版本不在范围。
- 成功的工作提交是 B 的回退落点。首次生成基线以后不得在 B 中重写预期结果。
- 四个既有证据产物缺失属于文档/证据检查的已知前置限制，单独报告；不降低其他构建/离线要求，也不宣称所有治理检查通过。
