# AioTieba4DotNet

面向 .NET 10 的异步贴吧客户端库，适合在 .NET 应用中完成贴吧读取、用户资料查询、消息处理，以及常见的吧务管理任务。

[![NuGet version (AioTieba4DotNet)](https://img.shields.io/nuget/v/AioTieba4DotNet.svg?style=flat-square)](https://www.nuget.org/packages/AioTieba4DotNet/)
[![CodeQL](https://github.com/BaWuZhuShou/AioTieba4DotNet/actions/workflows/codeql-analysis.yml/badge.svg)](https://github.com/BaWuZhuShou/AioTieba4DotNet/actions/workflows/codeql-analysis.yml)
[![QQ Group](https://img.shields.io/badge/QQ%E7%BE%A4-278662447-blue)](https://qm.qq.com/q/a0I1RepoA2)

## 这个库能做什么

- 查吧、搜索、签到、统计和关注管理
- 读取主题帖、楼层、楼中楼，以及常见的帖子管理操作
- 查询用户资料、主页、社交关系和黑名单
- 处理 `@`、回复、私信、吧群消息和推送通知解析
- 完成吧务团队、权限、封禁、日志和申诉处理

## 安装

```shell
dotnet add package AioTieba4DotNet
```

## 最小示例

先用一个不依赖登录态的读取调用确认环境可用：

```csharp
using AioTieba4DotNet;

using var client = new TiebaClient();

var forum = await client.Forums.GetForumAsync("csharp");
var threads = await client.Threads.GetThreadsAsync("csharp");

Console.WriteLine($"吧名: {forum.Fname}");
Console.WriteLine($"当前页主题数: {threads.Objs.Count}");
```

如果你接下来要做签到、私信、消息读取或吧务操作，再继续看文档里的登录态示例和对应场景页。

## 从哪里开始

如果你是第一次接触这个库，推荐直接从在线文档开始。文档站会把快速开始、API 参考和使用场景串在一起，阅读路径更顺。

- [在线文档](https://docs.tieba.bakasnow.com/): 从文档站首页开始，继续查看快速开始、使用场景和完整说明。
- [API 参考](https://docs.tieba.bakasnow.com/reference/modules): 直接查根客户端、配置、六个模块和公开异常。

## 让 AI 快速用起来

这个仓库额外导出了一个面向 `AioTieba4DotNet` 使用者的 agent skill。对支持 `skills add` 的 AI
工具，可以直接从这个仓库添加，让它更快生成正确的初始化、模块选择和示例代码。

```bash
npx skills add BaWuZhuShou/AioTieba4DotNet
```

如果你更想直接指向这个 skill 所在目录，也可以使用：

```bash
npx skills add https://github.com/BaWuZhuShou/AioTieba4DotNet/tree/master/skills/aiotieba4dotnet
```

更多说明见文档里的[AI Skill 使用](https://docs.tieba.bakasnow.com/guide/getting-started#ai-skill)。

## 相关项目

- [aiotieba](https://github.com/lumina37/aiotieba): 上游 Python 实现，也是接口行为和能力对齐的重要参考。
- [TiebaManager](https://github.com/dog194/TiebaManager): 吧务管理器项目，可参考实际的管理场景组织方式。
- [tbclient.protobuf](https://github.com/n0099/tbclient.protobuf): 贴吧相关 Protobuf 定义参考。

## 开发与贡献

- 任何 API 的参数语义、请求打包、响应解析和错误处理，都应优先对齐 Python
  版 [aiotieba](https://github.com/lumina37/aiotieba)。
- 修改 `.proto` 后，请运行 `dotnet run --project ProtoGenerator/ProtoGenerator.csproj`，不要手改生成的 `.cs` 文件。
- 文档站源码位于 `docs/`，本地构建使用 `pnpm --dir docs install` 和 `pnpm --dir docs run build`。
- 本地验证入口包括 `scripts/verify-local.*` 与 `scripts/test-lane.*`，其中 `test-lane` 现在路由到 `AioTieba4DotNet.Tests.Online.Suite` 的 `safe` / `restricted` ordered suite reality。

## 开源协议

[Unlicense](LICENSE)
