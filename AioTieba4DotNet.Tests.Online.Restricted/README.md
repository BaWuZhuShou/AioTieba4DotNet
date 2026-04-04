# AioTieba4DotNet.Tests.Online.Restricted

这个项目承载显式 opt-in 的受限在线测试，覆盖 `Moderation` 和 `Admin` 两类能力。它只面向本地、专用 Restricted 账号和专用 Restricted 资产，默认状态下必须在真正进入变更逻辑前 fail-closed。

## 什么时候用

- 你要验证删帖恢复、封禁解封这类受限能力。
- 你要确认 Restricted 环境门禁是否生效，例如缺少 `optIn`、缺少受限账号、缺少 capability 标记时应当直接停止。
- 你只想跑单个受限特性、单个 API、契约类或测试方法，而不是跑整个 Restricted ordered suite。

## 本地怎么用

先准备独立的 Restricted 前置条件，再直接跑这个项目。

- `TIEBA_ONLINE_RESTRICTED__OPTIN=true`
- `TIEBA_ONLINE_RESTRICTED__ACCOUNT__BDUSS` / `...__STOKEN`
- `TIEBA_ONLINE_RESTRICTED__CAPABILITIES__MODERATION=true` 或 `...__ADMIN=true`
- 对应的专用 Restricted 资产，例如 `moderationForumName`、`moderationThreadId`、`moderationReplyId`、`adminUserName`、`adminUserId`、`adminPortrait`

按特性过滤：

```bash
dotnet test AioTieba4DotNet.Tests.Online.Restricted/AioTieba4DotNet.Tests.Online.Restricted.csproj --configuration Release --nologo --filter "TestCategory=Feature:Moderation"
dotnet test AioTieba4DotNet.Tests.Online.Restricted/AioTieba4DotNet.Tests.Online.Restricted.csproj --configuration Release --nologo --filter "TestCategory=Feature:Admin&TestCategory=Tier:Restricted"
```

按 API 过滤（首选精确入口）：

```bash
dotnet test AioTieba4DotNet.Tests.Online.Restricted/AioTieba4DotNet.Tests.Online.Restricted.csproj --configuration Release --nologo --filter "TestCategory=Api:Threads.RecoverAsync&TestCategory=Tier:Restricted"
dotnet test AioTieba4DotNet.Tests.Online.Restricted/AioTieba4DotNet.Tests.Online.Restricted.csproj --configuration Release --nologo --filter "TestCategory=Api:Admins.BlockAsync&TestCategory=Tier:Restricted"
dotnet test AioTieba4DotNet.Tests.Online.Restricted/AioTieba4DotNet.Tests.Online.Restricted.csproj --configuration Release --nologo --filter "TestCategory=Api:Admins.GetBlocksAsync&TestCategory=Tier:Restricted"
```

按类或方法过滤：

```bash
dotnet test AioTieba4DotNet.Tests.Online.Restricted/AioTieba4DotNet.Tests.Online.Restricted.csproj --configuration Release --nologo --filter "ClassName~RestrictedModerationEnvironmentContractTests"
dotnet test AioTieba4DotNet.Tests.Online.Restricted/AioTieba4DotNet.Tests.Online.Restricted.csproj --configuration Release --nologo --filter "Name=DefaultLocalEnvironment_StopsBeforeRestrictedAdminMutationAttempt"
```

整个受限 ordered suite 只作为显式选择的辅助入口：

```bash
pwsh ./scripts/test-lane.ps1 restricted
./scripts/test-lane.sh restricted
```

## 关键说明

- 这里的场景全部属于 `Tier:Restricted`，默认路径不会自动带上它们。
- API 级别过滤现在是受限场景的正式支持入口，使用 `TestCategory=Api:<Module>.<Method>`；只有在你明确要点名单个测试方法时才再退回 `Name=`。
- API 分类同样使用模块限定名，避免与 Safe 项目里的同名方法混淆，例如 `Api:Threads.DelPostAsync` 和 `Api:Admins.BlockAsync` 分别对应不同模块的稳定过滤面。
- Restricted 必须使用独立账号和独立资产，不能复用 Safe 账号当作后备路径。
- `Moderation` 和 `Admin` 都要求显式 capability gating。缺少 `optIn`、凭据或 capability 时，契约测试和场景测试都应在真正 mutation 之前停止。
- 真实受限在线测试仍然是本地动作，CI 保持 build-only，不承担吧务或管理能力验证。
