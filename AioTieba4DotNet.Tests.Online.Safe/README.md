# AioTieba4DotNet.Tests.Online.Safe

这个项目承载默认启用的在线测试场景，覆盖 Safe 档位下的只读能力和可逆写入能力。这里的测试面向真实贴吧环境，但要求使用专用 Safe 账号和专用资产，本地缺少前置条件时会显式 `Assert.Inconclusive(...)`，不会回退到隐藏的公共资产。

## 什么时候用

- 你想验证 `ForumFoundation`、`ForumExtensions`、`ThreadRead`、`UserSocial`、`Messaging`、`ThreadWrite` 这些真实在线行为。
- 你只想重跑单个特性、单个 API、单个类或单个测试方法，而不是跑整个 ordered suite。
- 你要确认某个 Safe 场景在缺少专用论坛、专用线程、专用收件人等资产时是否正确 fail-closed。

## 本地怎么用

单特性执行的主入口是直接跑这个项目，不是包装脚本。

按特性过滤：

```bash
dotnet test AioTieba4DotNet.Tests.Online.Safe/AioTieba4DotNet.Tests.Online.Safe.csproj --configuration Release --nologo --filter "TestCategory=Feature:ForumFoundation"
dotnet test AioTieba4DotNet.Tests.Online.Safe/AioTieba4DotNet.Tests.Online.Safe.csproj --configuration Release --nologo --filter "TestCategory=Feature:ThreadWrite&TestCategory=Tier:Safe"
```

按 API 过滤（首选精确入口）：

```bash
dotnet test AioTieba4DotNet.Tests.Online.Safe/AioTieba4DotNet.Tests.Online.Safe.csproj --configuration Release --nologo --filter "TestCategory=Api:Threads.GetPostsAsync&TestCategory=Tier:Safe"
dotnet test AioTieba4DotNet.Tests.Online.Safe/AioTieba4DotNet.Tests.Online.Safe.csproj --configuration Release --nologo --filter "TestCategory=Api:Messages.SendMessageAsync&TestCategory=Tier:Safe"
dotnet test AioTieba4DotNet.Tests.Online.Safe/AioTieba4DotNet.Tests.Online.Safe.csproj --configuration Release --nologo --filter "TestCategory=Api:Users.GetThreadsAsync&TestCategory=Tier:Safe"
```

按类或方法过滤：

```bash
dotnet test AioTieba4DotNet.Tests.Online.Safe/AioTieba4DotNet.Tests.Online.Safe.csproj --configuration Release --nologo --filter "ClassName~ForumFoundationReadScenarioTests"
dotnet test AioTieba4DotNet.Tests.Online.Safe/AioTieba4DotNet.Tests.Online.Safe.csproj --configuration Release --nologo --filter "Name=GetForumAsync_SafeForumQuery_ReturnsCanonicalForumIdentity"
```

如果你想看整个 Safe ordered suite 的编排结果，再用兼容包装脚本或 Suite 项目入口：

```bash
pwsh ./scripts/test-lane.ps1 safe
./scripts/test-lane.sh safe
```

## 关键说明

- 这里的场景全部属于 `Tier:Safe`，也是默认本地在线测试档位。
- API 级别过滤现在是正式支持的开发者入口，使用 `TestCategory=Api:<Module>.<Method>`；`Name=` 只保留给需要精确命中单个测试方法时的兜底场景。
- API 分类使用模块限定名来避免重名冲突，例如 `Api:Threads.GetPostsAsync` 和 `Api:Users.GetPostsAsync` 是两个不同的稳定过滤面。
- Safe 资产必须显式提供，例如 `forumQuery`、`forumName`、`ownedThreadId`、`messageRecipient`、`chatroomId`。缺少时测试会明确跳过，不会偷偷改用公共论坛或公共用户。
- `ForumExtensions`、`Messaging`、`ThreadWrite` 这类会产生副作用的场景，只允许使用专用 Safe 资产，并且要求补偿审计能把可逆写入收回来。
- 真实在线执行是本地行为，CI 仍然只做构建验证，不跑这里的远程场景。
