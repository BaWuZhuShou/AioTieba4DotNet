# 文档内容契约

## 保留 v3 公开接口范围

[仓库规则](../../../.junie/guidelines.md) 将维护范围固定为 `net10.0` 和六个公开模块：`Forums`、`Threads`、`Users`、`Admins`、`Messages`、`Client`。对照 [ITiebaClient.cs](../../../AioTieba4DotNet/Clients/ITiebaClient.cs) 核验根访问器，对照 [Contracts](../../../AioTieba4DotNet/Contracts) 核验方法，并对照实现核验示例。以 [Python aiotieba → C# 的项目定位](../project-positioning.md)组织接入场景，明确目标与已验证能力的区别。

- 使用者示例采用 `TiebaClient`、`ITiebaClient`、`AddAioTiebaClient(...)` 或公开工厂，参见 [getting-started.md](../../../docs/guide/getting-started.md)。
- `Messages` 负责消息读取、发送、已读回执和推送解析。`Client` 负责 `InitWebSocketAsync`、`InitZIdAsync` 和 `SyncAsync` 等生命周期辅助操作。[messages.md](../../../docs/how-to/messages.md)、[IMessagesModule.cs](../../../AioTieba4DotNet/Contracts/IMessagesModule.cs) 和 [IClientModule.cs](../../../AioTieba4DotNet/Contracts/IClientModule.cs) 展示了这一边界。
- 当前公开声明优先于陈旧的兼容性指南。既有规则提到 `Client.cs` 兼容包装，但当前库中不存在此文件；[Clients/TiebaClient.cs](../../../AioTieba4DotNet/Clients/TiebaClient.cs) 才是具体入口。不要在新示例中虚构旧入口。
- 受支持框架、公开异常、默认值或文档行为变化时，须按仓库版本规则进行发布／迁移审查。

## 区分各文档的权威职责

| 问题 | 权威来源 |
| --- | --- |
| 当前 C# API 声明了什么？ | `AioTieba4DotNet/Clients/`、`AioTieba4DotNet/Contracts/` 和 `AioTieba4DotNet/DependencyInjection.cs` 中的公开声明 |
| 使用者应如何调用？ | [reference/modules.md](../../../docs/reference/modules.md)、入门文档和场景指南 |
| 实现遵循哪个上游族系与认证要求？ | [related/parity.md](../../../docs/related/parity.md) |
| 哪些公开方法有直接在线覆盖和可筛选的 `Api:*` 分类？ | [related/public-api-coverage-matrix.md](../../../docs/related/public-api-coverage-matrix.md) |
| 本地需要哪些文档与证据文件？ | [verify-local.sh](../../../scripts/verify-local.sh) 和 [verify-local.ps1](../../../scripts/verify-local.ps1) |

API 参考供使用者交叉核对，不能替代上述两份台账。延期、间接或仅离线覆盖的行，不得宣传为可直接运行的在线 API 分类。当前测试拓扑中，Platform 提供共用支持，Online 负责可运行场景发现，Governance 负责有序宿主和契约；参见 [Governance/README.md](../../../AioTieba4DotNet.Tests.Governance/README.md)。

## 编写与类型匹配的示例

[入门示例](../../../docs/guide/getting-started.md) 从访客读取逐步介绍认证调用、选项、DI 和工厂用法。遵循其中的具体模式：

- 包含相关 `using` 指令，对直接创建的客户端使用 `using var` 释放。
- 凭据使用 `"你的 BDUSS"`、`"你的 STOKEN"` 等示意文字；不得使用真实凭据或生产环境修改目标。
- 保留数字类型：消息 ID 使用 `123456789L` 等 `long` 字面量，吧 ID 可能需要 `ulong`，如 `123456789UL`。逐一核对重载，不要为所有 ID 套用同一类型。
- 引用完整签名时，保留可空／默认参数、参数顺序和取消支持。API 参考规定了常见的末尾参数约定 `CancellationToken cancellationToken = default`。
- 如实标明不完整上下文。DI 示例假定存在宿主 `builder`；推送解析假定调用者提供字节数据。代码块不等于示例已编译或执行的证据。

例如，[IMessagesModule.cs](../../../AioTieba4DotNet/Contracts/IMessagesModule.cs) 解释了[聊天室操作指南](../../../docs/how-to/messages.md)中不同的字面量：

```csharp
await client.Messages.SendChatroomMessageAsync(
    chatroomId: 123456789L,
    forumId: 123456789UL,
    text: "消息内容");
```

## 机器读取的 Markdown 需要契约审查

[PublicApiCoverageMatrixContract.cs](../../../AioTieba4DotNet.Tests.Governance/Contracts/PublicApiCoverageMatrixContract.cs) 解析矩阵的精确表头和分隔行，要求每行九个非空列。保留格式、允许的处置值和分类语法；装饰性的表格修改或单元格内的字面管道符都可能破坏解析。

[OnlineArchitectureContractTests.cs](../../../AioTieba4DotNet.Tests.Governance/Contracts/OnlineArchitectureContractTests.cs) 对照已声明且可发现的 API 分类检查矩阵资格。即使渲染后的文字看起来合理，修改验证命令单元格也可能改变受测试约束的契约。

## 避免

- 因旧文档提到而在当前指南中宣称支持 .NET 8／9 或仍维护 v2 发布线。
- 承诺认证、配置、取消或业务错误会自动触发传输回退；更改说明前应交叉核对 [advanced.md](../../../docs/guide/advanced.md) 与实现。
- 将推送解析描述为内置后台事件总线；载荷获取和消费由调用者负责。
- 将 `safe` 当作离线通道，或根据站点构建宣称真实在线覆盖。
