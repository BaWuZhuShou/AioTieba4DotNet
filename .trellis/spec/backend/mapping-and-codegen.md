# 模型、映射与 Protobuf 生成

## 公开模型边界

[库规则](../../../AioTieba4DotNet/AGENTS.md) 要求公开 DTO 与协议无关，并使用属性而非公开数据字段。同一使用者概念应复用模型；仅在语义有实质差异时拆分。[UserInfoT](../../../AioTieba4DotNet/Models/Threads/UserInfoT.cs) 在共用用户数据上扩展了主题作者字段。

转换放在 `AioTieba4DotNet/Internal/Mapping/` 下；即使目录更深，mapper 命名空间仍为 `AioTieba4DotNet.Internal.Mapping`。公开 DTO 的构造函数／签名中避免出现 protobuf 类型、`JObject` 和端点解码逻辑。

[ThreadsMapper](../../../AioTieba4DotNet/Internal/Mapping/Models/Threads/ThreadsMapper.cs) 关联用户、附加吧标识，并初始化 [Threads](../../../AioTieba4DotNet/Models/Threads/Threads.cs) 的必需属性。保留文档规定的空值／默认值行为；各端点的字段可能不同。按[项目定位与上游对齐](../project-positioning.md)保留数据语义并记录强类型／属性适配及缺省值的证据。

## 共用转换规则

[UserProtoMapping](../../../AioTieba4DotNet/Internal/Mapping/Models/Shared/UserProtoMapping.cs) 集中处理头像标识归一化、不受区域性影响的数字解析、图标和标志。其数字回退行为是有意设计：

```csharp
return long.TryParse(tiebaUid, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
    ? parsed
    : 0;
```

复用这些转换，不要改用受区域性影响的解析或新增基于子串的猜测规则。不要将宽容解析扩展到本应报错的字段。[MappingCoverageContractTests](../../../AioTieba4DotNet.Tests.Governance/Contracts/MappingCoverageContractTests.cs) 断言用户字段、无效数字默认值、头像标识归一化和缺失链接的回退。应断言结果，不能只检查未抛异常。

## 生成输出契约

- API `.proto` 文件是可编辑源码。共用导入位于 `AioTieba4DotNet/Api/Protobuf/`；族系文件放在端点旁，例如 [FrsPageReqIdl.proto](../../../AioTieba4DotNet/Api/GetThreads/Protobuf/FrsPageReqIdl.proto)。
- 同目录生成的 C# 是派生产物。应修改 schema／生成器并重新生成；不得手工修补生成类或将其公开。
- [ProtoGenerationPlanner](../../../ProtoGenerator/ProtoGenerationPlanner.cs) 查找精确的根标记 `AioTieba4DotNet.sln`，递归扫描 API `.proto` 文件，并按序号比较规则排序归一化后的相对路径。输出放在源 schema 旁。
- [ProtocExecutor](../../../ProtoGenerator/ProtocExecutor.cs) 使用 `ProcessStartInfo.ArgumentList`、本地／共用导入路径和 `--csharp_opt=serializable,internal_access`。保留参数边界和可见性。
- [ProtoGenerator.csproj](../../../ProtoGenerator/ProtoGenerator.csproj) 随包提供 `Google.Protobuf.Tools` 编译器；当前路径为 Windows／Linux／macOS 选择 x64 工具。不要承诺超出该配置的架构支持。[ProtoGeneratorApp](../../../ProtoGenerator/ProtoGeneratorApp.cs) 优先使用随包可执行文件，缺失时才回退至 PATH。

完成获授权的 schema／生成器修改后，从仓库根目录运行：

```bash
dotnet run --project ProtoGenerator/ProtoGenerator.csproj
```

此命令会写入生成源码，需要仓库规定的 SDK 和已还原的依赖。审查差异并重新构建受影响项目。程序对失败目标返回非零退出码，并传播取消。仅修改文字时不要执行生成。

[生成器 README](../../../ProtoGenerator/README.md) 只概述了 `serializable` 和通用解决方案发现；代码还要求 `internal_access` 及精确的解决方案标记。长期行为变化时，同步[生成器规则](../../../ProtoGenerator/AGENTS.md)和跨目录指南。
