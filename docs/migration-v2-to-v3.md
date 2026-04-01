# AioTieba4DotNet v2 -> v3 迁移指南

这份文档面向从 v2 升级到 v3 的调用方。它不重复讲 every method 的细节，而是先告诉你升级时最容易踩的变化，再给出新的主路径。

## 先看结论

- v3 只支持 `net10.0`
- 根客户端故事没变，仍然是 `TiebaClient`、`ITiebaClient`、`AddAioTiebaClient(...)`、`ITiebaClientFactory`
- 公开模块从旧文档里的四块叙事，收敛为六个明确边界: `Forums`、`Threads`、`Users`、`Admins`、`Messages`、`Client`
- 消息业务的 canonical 入口改为 `client.Messages`
- 吧务和后台管理的 canonical 入口改为 `client.Admins`
- `client.Client` 只保留生命周期与初始化职责

## 1. 支持矩阵变化

### v2

- 同时承载旧版兼容叙事和过渡期文档
- 文档里经常把 v1、v2、v3 的历史层叠在一起

### v3

- **只支持 `net10.0`**
- 仓库语言版本策略固定为 **C# 14**
- 不再为 `net8.0` 或 `net9.0` 保留兼容策略和文档承诺

### 迁移动作

1. 先把消费方项目升级到 .NET 10 SDK 和目标框架
2. 清理任何为了旧目标框架保留的条件编译和兼容分支
3. 再处理 API 调整

## 2. 根入口不需要重学

以下入口在 v3 继续成立:

- `TiebaClient`
- `ITiebaClient`
- `DependencyInjection.AddAioTiebaClient(...)`
- `ITiebaClientFactory`

也就是说，升级时你通常不用把“如何创建客户端”整套推翻，只需要把“创建出来之后访问哪个模块”改对。

## 3. 公开模块边界变化

## v2 文档叙事的问题

旧文档长期把很多能力压在 `Users` 和 `Client` 这两条线上，调用方很难从入口名判断真正的业务边界。

## v3 的边界

| v3 模块 | 主职责 |
| --- | --- |
| `Forums` | 贴吧发现、关注、签到、搜索、统计 |
| `Threads` | 读帖、回帖、互动、主题管理 |
| `Users` | 资料、主页、社交关系、黑名单、资料修改 |
| `Admins` | 吧务团队、权限、日志、封禁、申诉 |
| `Messages` | @、回复、私信、吧群消息、push 解析 |
| `Client` | WebSocket、ZId、ClientId / SampleId 初始化 |

## 4. 最重要的代码调整

### 4.1 消息业务从 `Users` / `Client` 迁到 `Messages`

旧代码可能长这样:

```csharp
var ats = await client.Users.GetAtsAsync();
var replies = await client.Users.GetRepliesAsync();
```

v3 推荐改成:

```csharp
var ats = await client.Messages.GetAtsAsync();
var replies = await client.Messages.GetRepliesAsync();
```

私信、吧群消息和 `push_notify` 解析也统一走 `client.Messages`。

### 4.2 吧务管理从分散入口迁到 `Admins`

v3 的 canonical 路径是:

```csharp
var info = await client.Admins.GetBawuInfoAsync("csharp");
await client.Admins.BlockAsync("csharp", "目标 portrait", day: 1, reason: "示例");
```

兼容入口仍可能存在，但新代码应优先依赖 `Admins`。

### 4.3 `Client` 不再承担消息职责

保留下来的只有:

- `InitWebSocketAsync()`
- `InitZIdAsync()`
- `SyncAsync()`

如果你在 v2 的理解里把 `Client` 当成“连接相关能力 + 消息能力”的总入口，v3 需要把消息部分迁走。

## 5. 配置和异常模型

### 保持不变的地方

- 还是用 `TiebaOptions`
- 还是用 `TiebaTransportMode`
- 还是支持直接构造、DI 和 factory

### 更需要注意的地方

- 缺少本地必需凭据时，明确抛出 `TiebaAuthenticationException`
- 配置非法时，明确抛出 `TiebaConfigurationException`
- 服务端业务拒绝时，抛出 `TieBaServerException`

这意味着你在迁移异常处理时，最好按“本地配置 / 本地鉴权 / 服务端业务”三类来分，而不是继续把它们混在一起。

## 6. 推荐升级顺序

1. 升级目标框架到 `net10.0`
2. 保持原有客户端创建方式不变
3. 把消息相关调用迁到 `client.Messages`
4. 把吧务和后台管理调用迁到 `client.Admins`
5. 把依赖 `client.Client` 的代码压缩回生命周期初始化用途
6. 复查 README 和 how-to 示例里是否还有旧模块叙事

## 7. 升级后的最小验证

至少做下面这些检查:

1. `dotnet build AioTieba4DotNet.sln --configuration Release --no-restore --nologo`
2. `pwsh ./scripts/verify-local.ps1 -ValidateOnly`
3. 打开新的导航链路，确认 README -> Getting Started -> How-to -> Advanced / Troubleshooting 可以走通

## 8. 相关阅读

- [README](../README.md)
- [Getting Started](./getting-started.md)
- [Modules Reference](./modules.md)
- [Release Notes v3](./release-notes-v3.md)
- [Parity v3](./parity-v3.md)
