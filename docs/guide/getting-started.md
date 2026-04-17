# 快速开始

这页按第一次接入 AioTieba4DotNet 的顺序来写。你会先确认环境并安装包，然后依次跑通访客读取、登录态调用、`TiebaOptions` 配置、依赖注入和 factory 创建路径。

## 1. 运行环境

- SDK: `.NET 10`
- 包: `AioTieba4DotNet`
- 公开入口: `TiebaClient`、`ITiebaClient`、`AddAioTiebaClient(...)`、`ITiebaClientFactory`

如果你刚开始接入，建议先用控制台程序或最小样例跑通只读调用，再把同一套代码接到应用里的 DI 或多账号场景。

## 2. 安装

先把包加到项目里：`dotnet add package AioTieba4DotNet`

安装完成后，先不要急着接登录态。先跑通一个不依赖凭据的读取调用，能更快确认 SDK、网络和最小使用姿势都没问题。

### 示例里的常见输入值 {#example-values}

文档里的示例会直接写成“你的吧名”“你的 BDUSS”“目标用户 ID”这类示意值。下面这张表说明这些常见写法通常分别代表什么。

| 示例写法 | 实际应填写 |
| --- | --- |
| `你的 BDUSS` / `你的 STOKEN` | 当前账号的登录凭据 |
| `账号 A 的 BDUSS` / `账号 A 的 STOKEN` | 多账号示例里的账号 A 凭据 |
| `账号 B 的 BDUSS` / `账号 B 的 STOKEN` | 多账号示例里的账号 B 凭据 |
| `你的吧名` / `123456789UL` | 贴吧名称与 fid |
| `123456789L` / `987654321L` | 主题帖 id 与楼层 / 楼中楼 id |
| `123456789` / `目标用户名` | 用户 id 与用户名 |
| `目标用户 portrait` / `目标用户名或 portrait` | portrait 字符串，或同时接受用户名 / portrait 的公开入口值 |
| `123456789L` / `123456789UL` | 私信分组 id 与吧群聊天室 / 吧 id |
| `关键词` / `消息内容` | 搜索词与消息 / 回复正文 |
| `新的昵称` / `新的个性签名` | 资料修改示例值 |
| `图片哈希` / `分类名` | 图片 hash 与帖子分区 / 分类名 |
| `违规理由` / `123456789L` | 吧务原因与解封申诉 id |
| `http://127.0.0.1:7890` | 自定义代理地址 |

当参数类型本身是数字时，示例会直接写成 `123456789`、`123456789L` 或 `123456789UL` 这类字面量，方便你一眼看出目标参数类型。

## 3. 访客读取

访客模式适合查吧、读取帖子列表、读取公开资料这类只读操作。第一次接入时，先从这里开始最稳妥。

```csharp
using AioTieba4DotNet;

using var client = new TiebaClient();

var forum = await client.Forums.GetForumAsync("你的吧名");
var threads = await client.Threads.GetThreadsAsync("你的吧名");

Console.WriteLine($"吧名: {forum.Fname}");
Console.WriteLine($"当前页主题数: {threads.Count}");
```

如果这里就失败，通常先看运行环境、网络链路或请求超时设置。排查入口见文末的排障链接。

## 4. 登录态调用

签到、私信、吧群消息、吧务操作这类能力需要登录态。推荐显式提供 `BDUSS` 和 `STOKEN`，这样本地就能尽早发现缺失凭据或配置错误。

```csharp
using AioTieba4DotNet;

using var client = new TiebaClient("你的 BDUSS", "你的 STOKEN");

await client.Forums.SignAsync("你的吧名");

var selfInfo = await client.Users.GetSelfInfoAsync();
Console.WriteLine(selfInfo.ShowName);
```

接入登录态时，最常见的异常有三类：

- 缺少必需凭据时抛出 `TiebaAuthenticationException`
- 配置本身非法时抛出 `TiebaConfigurationException`
- 服务端业务拒绝时抛出 `TieBaServerException`

## 5. 用 `TiebaOptions` 管理配置

`TiebaOptions` 是公开配置入口。大多数场景可以直接用默认值，只有当你需要显式带上凭据、收紧超时，或者把传输模式固定为 HTTP 时，才需要自己配置它。

```csharp
using AioTieba4DotNet;
using AioTieba4DotNet.Contracts;

using var client = new TiebaClient(new TiebaOptions
{
    Bduss = "你的 BDUSS",
    Stoken = "你的 STOKEN",
    TransportMode = TiebaTransportMode.Http,
    RequestTimeout = TimeSpan.FromSeconds(20),
    MaxReadRetryAttempts = 1
});

var homepage = await client.Users.GetHomepageAsync(123456789);
Console.WriteLine(homepage.Count);
```

你可以先记住两条规则：

- 默认 `TransportMode` 是 `Auto`
- `Auto` 下，当链路或接口不适合 WebSocket 时会回退到 HTTP，但取消、鉴权失败、配置错误和服务端业务错误不属于自动回退信号；如果你要强制支持 WebSocket 的调用只能走 WebSocket，可以改成 `TiebaTransportMode.WebSocketOnly`

## 6. 接到依赖注入

如果你的应用已经在用 `Microsoft.Extensions.DependencyInjection`，推荐直接注册 `AddAioTiebaClient(...)`。这样可以统一管理默认配置，也能直接在业务服务里注入 `ITiebaClient`。

```csharp
using AioTieba4DotNet;

builder.Services.AddAioTiebaClient(options =>
{
    options.Bduss = builder.Configuration["Tieba:Bduss"];
    options.Stoken = builder.Configuration["Tieba:Stoken"];
    options.RequestTimeout = TimeSpan.FromSeconds(20);
});
```

注册后，业务代码里直接依赖 `ITiebaClient`：

```csharp
using AioTieba4DotNet;

public sealed class ForumWorker(ITiebaClient client)
{
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        var detail = await client.Forums.GetDetailAsync("你的吧名", cancellationToken);
        Console.WriteLine(detail.Fname);
    }
}
```

## 7. 需要多个账号时，用 factory

`ITiebaClientFactory` 适合 bot、定时任务、客服号切换这类一个进程里要按需创建多个客户端的场景。它保留和 DI 相同的注册方式，但把实例创建推迟到真正使用的时候。

```csharp
using AioTieba4DotNet;
using AioTieba4DotNet.Contracts;

public sealed class MultiAccountJob(ITiebaClientFactory factory)
{
    public async Task RunAsync()
    {
        using var signer = factory.CreateClient("账号 A 的 BDUSS", "账号 A 的 STOKEN");
        using var reader = factory.CreateClient(new TiebaOptions
        {
            Bduss = "账号 B 的 BDUSS",
            Stoken = "账号 B 的 STOKEN",
            TransportMode = TiebaTransportMode.Http
        });

        await signer.Forums.SignAsync("你的吧名");

        var messages = await reader.Messages.GetAtsAsync();
        Console.WriteLine(messages.Count);
    }
}
```

## 8. 让 AI 也能快速用起来 {#ai-skill}

如果你会配合支持 `skills add` 的 AI 工具一起使用这个库，可以直接从仓库添加导出的 `aiotieba4dotnet` skill。它适合这几类场景：

- 让 AI 帮你选择应该用哪个公开模块
- 让 AI 生成最小可运行的 C# 示例
- 让 AI 解释初始化方式、配置项和常见异常边界

最短添加方式：

```bash
npx skills add BaWuZhuShou/AioTieba4DotNet
```

如果你的工具更适合直接从 GitHub 目录添加，也可以使用：

```bash
npx skills add https://github.com/BaWuZhuShou/AioTieba4DotNet/tree/master/skills/aiotieba4dotnet
```

这个 skill 面向的是**已发布库的使用者**，不是仓库源码维护流程。它会优先围绕公开入口、公开模块和公开模型来回答问题，而不是引导到内部 API。

如果你想查看 skill 的入口和引用资料，可以直接看仓库里的这些文件：

- `skills/aiotieba4dotnet/SKILL.md`
- `skills/aiotieba4dotnet/references/quickstart.md`
- `skills/aiotieba4dotnet/references/method-recipes.md`
- `skills/aiotieba4dotnet/references/scenario-templates.md`
- `skills/aiotieba4dotnet/references/output-format.md`

## 9. 下一步

接下来可以按你要解决的问题继续往下读：

- [贴吧相关](/how-to/forums)，查吧、关注、签到、搜索和统计
- [帖子相关](/how-to/threads)，读帖、回帖、楼中楼和帖子管理
- [用户相关](/how-to/users)，资料、主页、社交关系和黑名单能力
- [消息相关](/how-to/messages)，`@`、回复、私信、吧群消息和推送通知解析
- [吧务相关](/how-to/admins)，吧务团队、权限、封禁、日志和申诉处理
- [API 参考](/reference/modules)，查根客户端、配置、六个模块和公开异常
- [排障](/guide/troubleshooting)，排查凭据、配置和链路问题
