# 规范审查的验证依据

2026-09-26 进行只读源码检查；该研究期间没有运行构建或测试。测试拓扑变化后应重新核对这些观察。

## 命令与边界

- `scripts/verify-local.sh --validate-only` 和 `scripts/verify-local.ps1 -ValidateOnly` 校验现有保留产物与文档契约。默认模式会改写本地验证清单。脚本会打印文档命令，但不会执行它们或任何测试。
- `pnpm --dir docs install` 与 `pnpm --dir docs run build` 是 `docs/package.json` 和验证脚本中的独立站点安装/构建命令。
- `global.json` 要求 SDK `10.0.201`，允许 `latestFeature` 前滚；`Directory.Build.props` 设置 `net10.0` 和 C# 14。构建时分析器执行和代码风格强制检查默认关闭。
- 解决方案还原/构建命令记录在 `.github/workflows/codeql-analysis.yml`。可发现性契约加载单独构建的 Online 程序集，因此仅构建 Governance 不足以支持这些契约。参见 `AioTieba4DotNet.Tests.Governance/Contracts/DiscoverableOnlineTestApiCategoryContract.cs` 和 `AioTieba4DotNet.Tests.Governance/AioTieba4DotNet.Tests.Governance.csproj`。

## 离线与在线测试

- `AioTieba4DotNet.Tests.Governance/Scenarios/OrderedSuiteHostTests.cs` 将两个有序套件暴露为可发现测试。
- `AioTieba4DotNet.Tests.Governance/Contracts/ThreadWebSocketOnlineContractTests.cs` 带有 `Contract:Architecture`，同时发起真实调用。全部 Governance 测试或该分类都不能视为离线选择。
- `scripts/test-lane.sh` 和 `.ps1` 要求显式通道；`safe` 是默认选择政策，不是全局运行器过滤器。`sequence-dry-run` 打印计划，`safe` 和 `restricted` 执行在线套件。
- Safe ThreadWrite 场景创建/删除回复并修改赞。Messaging 场景发送私信，补偿还会发送额外通知。源码为 `AioTieba4DotNet.Tests.Online/Tiers/Safe/Features/ThreadWrite/Scenarios/ThreadWriteScenarioTests.cs` 和 `AioTieba4DotNet.Tests.Online/Tiers/Safe/Features/Messaging/Scenarios/MessagingScenarioTests.cs`。
- 缺少凭据并不意味着任意测试命令无网络行为：`AioTieba4DotNet.Tests.Platform/Execution/OnlineExecutionGate.cs` 允许能力 `None` 绕过账号门禁。
- 长期开发指引应选用明确检查过的离线测试类，而不是宽泛分类或脆弱的排除清单。

## 覆盖率与 CI

- `Directory.Build.targets` 声明总行/分支覆盖阈值为 100。`Directory.Packages.props` 声明 `coverlet.msbuild` 版本，但当前活动项目/targets 文件未引用或导入该收集器。
- 测试包装脚本与 `AioTieba4DotNet.Tests.Governance/Execution/OrderedSuiteHost.cs` 关闭覆盖采集。普通测试成功不能证明达成或强制执行全仓库 100% 覆盖率。
- CI 执行还原、构建、protobuf 重新生成和打包，不运行 `dotnet test` 或文档站构建。CodeQL 分析与发布是独立工作流职责；既有“仅构建”政策限定的是验证边界，并不否认其他职责。
- CI 生成检查对比整个 `AioTieba4DotNet/` 与干净 Git checkout，而不只比较生成文件。已有改动的本地 checkout 需要考虑基线。该初始化任务没有运行生成器。
- `ProtoGenerator/ProtoGeneratorApp.cs` 优先使用捆绑 protoc，也可回退到 PATH。`.github/scripts/validate-package-artifacts.sh` 检查预期包/符号包文件名和数量，不检查完整内容。
