# 在线测试环境变量配置示例

这个文件只演示本地在线测试环境现在的配置方式，不提供任何真实凭据。

## 加载顺序

`OnlineTestEnvironment.LoadFromRepository(...)` 的加载模型是固定的：

1. 先读取仓库里跟踪的空白模板文件。
2. 再用环境变量逐项覆盖模板中的值。

当前会先读取这两个模板文件：

- `online-test.safe.template.json`
- `online-test.restricted.template.json`

这两个 JSON 模板必须保持空白并持续跟踪在仓库里。真实本地配置不要写回模板，而是通过 `TIEBA_ONLINE_*` 环境变量覆盖。这样可以保留字段形状，同时避免把凭据、用户标识或专用资产提交进仓库。

补充一点，代码里把正整数资产解析为可选 `long`。如果值为空、空白、`0` 或非正数，会按未设置处理。Restricted 也不是公共兜底模式，必须依赖显式 `optIn`、能力开关和专用资产，缺任何一项都应失败关闭。

## Safe 配置

Safe 用于默认本地在线测试路径。它可以覆盖账号信息和专用 Safe 资产，但这些值都应该是本地专用占位或真实本地环境变量，不要写回仓库。

### Safe 配置项是什么意思

可以先把 Safe 项分成两类理解：

- `ACCOUNT`：本地测试账号登录凭据。
- `ASSETS`：专门为在线测试准备的独立资产，缺失时应直接跳过对应场景，而不是退回公共数据。

具体含义如下：

- `BDUSS` / `STOKEN`：本地 Safe 测试账号的登录凭据。需要登录态的 Safe 读取、可逆写入、私信和吧群消息场景都会用到它们。
- `FORUMQUERY`：用于查找专用 Safe 吧的查询选择器。适合那些可以先按查询词解析到目标吧的场景，比如 forum foundation、forum extensions、thread read，以及部分 forum-scoped user/chatroom 相关路径。
- `FORUMNAME`：专用 Safe 吧名。在 API 或场景本身需要明确吧名时直接使用；也可以作为 `FORUMQUERY` 找到后的稳定吧名上下文。
- `OWNEDTHREADID`：由 Safe 账号控制的专用根主题帖 ID，主要用于 thread-write 场景。它应该指向一个可安全写入、可验证、可清理的根帖。
- `OWNEDREPLYID`：专用回复/楼层资产 ID。用于需要定位既有回复、执行写入后核对或补偿清理的场景；如果本地没有这类覆盖，可以保持未设置。
- `TARGETUSERNAME` / `TARGETUSERID` / `TARGETPORTRAIT`：专用用户身份夹具。用于 user-social 读取、吧内用户资料查询和依赖用户身份定位的 forum-scoped 查询。三者分别表示目标用户名、目标用户 ID、目标 portrait 标识。
- `MESSAGERECIPIENT`：专用私信接收方。用于 Safe 私信发送场景和对应的补偿验证，不应指向随意的公共目标。
- `CHATROOMID`：专用吧群/聊天室目标 ID。用于 chatroom messaging 覆盖，应该对应本地明确允许测试的目标会话。

### Safe 环境变量

- `TIEBA_ONLINE_SAFE__ACCOUNT__BDUSS`
- `TIEBA_ONLINE_SAFE__ACCOUNT__STOKEN`
- `TIEBA_ONLINE_SAFE__ASSETS__FORUMQUERY`
- `TIEBA_ONLINE_SAFE__ASSETS__FORUMNAME`
- `TIEBA_ONLINE_SAFE__ASSETS__OWNEDTHREADID`
- `TIEBA_ONLINE_SAFE__ASSETS__OWNEDREPLYID`
- `TIEBA_ONLINE_SAFE__ASSETS__TARGETUSERNAME`
- `TIEBA_ONLINE_SAFE__ASSETS__TARGETUSERID`
- `TIEBA_ONLINE_SAFE__ASSETS__TARGETPORTRAIT`
- `TIEBA_ONLINE_SAFE__ASSETS__MESSAGERECIPIENT`
- `TIEBA_ONLINE_SAFE__ASSETS__CHATROOMID`

### Safe 占位示例

```powershell
$env:TIEBA_ONLINE_SAFE__ACCOUNT__BDUSS = "<safe-bduss>"
$env:TIEBA_ONLINE_SAFE__ACCOUNT__STOKEN = "<safe-stoken>"

$env:TIEBA_ONLINE_SAFE__ASSETS__FORUMQUERY = "<safe-forum-query>"
$env:TIEBA_ONLINE_SAFE__ASSETS__FORUMNAME = "<safe-forum-name>"
$env:TIEBA_ONLINE_SAFE__ASSETS__OWNEDTHREADID = "<safe-owned-thread-id>"
$env:TIEBA_ONLINE_SAFE__ASSETS__OWNEDREPLYID = "<safe-owned-reply-id>"
$env:TIEBA_ONLINE_SAFE__ASSETS__TARGETUSERNAME = "<safe-target-user-name>"
$env:TIEBA_ONLINE_SAFE__ASSETS__TARGETUSERID = "<safe-target-user-id>"
$env:TIEBA_ONLINE_SAFE__ASSETS__TARGETPORTRAIT = "<safe-target-portrait>"
$env:TIEBA_ONLINE_SAFE__ASSETS__MESSAGERECIPIENT = "<safe-message-recipient>"
$env:TIEBA_ONLINE_SAFE__ASSETS__CHATROOMID = "<safe-chatroom-id>"
```

如果你想对照模板理解字段位置，`online-test.safe.template.json` 的结构等价于下面这种空白形状：

```json
{
  "safe": {
    "account": {
      "bduss": "",
      "stoken": ""
    },
    "assets": {
      "forumQuery": "",
      "forumName": "",
      "ownedThreadId": 0,
      "ownedReplyId": 0,
      "targetUserName": "",
      "targetUserId": 0,
      "targetPortrait": "",
      "messageRecipient": "",
      "chatroomId": 0
    }
  }
}
```

## Restricted 配置

Restricted 用于受限场景，不应该复用模糊的公共数据。它和 Safe 明确分开，要求单独的受限账号、显式 `optIn`、能力开关，以及受限专用资产。

### Restricted 配置项是什么意思

Restricted 项可以分成三类理解：

- `OPTIN`：总开关，不显式允许就不应该运行。
- `CAPABILITIES`：能力门禁，只开放你明确准备验证的受限能力。
- `ASSETS`：专门为 restricted moderation / admin 场景准备的独立资产。

具体含义如下：

- `OPTIN`：Restricted 顶层总开关。只有显式设为 `true`，受限场景才允许继续进入后续门禁判断。
- `BDUSS` / `STOKEN`：本地 Restricted 测试账号的登录凭据。它应当和 Safe 凭据分开管理，不要假定 Safe 账号可以承担 restricted mutation。
- `CAPABILITIES__MODERATION`：显式声明当前本地环境允许执行受限吧务能力验证。没有这个开关时，即使 restricted 账号和资产已配置，也不应进入 moderation mutation。
- `CAPABILITIES__ADMIN`：显式声明当前本地环境允许执行受限管理能力验证。没有这个开关时，不应进入 admin mutation。
- `MODERATIONFORUMNAME` / `MODERATIONTHREADID` / `MODERATIONREPLYID`：专用 restricted moderation 夹具。分别表示受限吧务场景使用的吧名、主题帖 ID、回复 ID，用来覆盖删帖、删回帖、恢复或类似 moderation 路径；这些值必须彼此对应，不能随意拼接。
- `ADMINUSERNAME` / `ADMINUSERID` / `ADMINPORTRAIT`：专用 restricted admin 目标身份夹具。分别表示受限管理目标的用户名、用户 ID、portrait 标识，用于需要精确定位目标用户的 admin 场景。

### Restricted 环境变量

- `TIEBA_ONLINE_RESTRICTED__OPTIN`
- `TIEBA_ONLINE_RESTRICTED__ACCOUNT__BDUSS`
- `TIEBA_ONLINE_RESTRICTED__ACCOUNT__STOKEN`
- `TIEBA_ONLINE_RESTRICTED__CAPABILITIES__MODERATION`
- `TIEBA_ONLINE_RESTRICTED__CAPABILITIES__ADMIN`
- `TIEBA_ONLINE_RESTRICTED__ASSETS__MODERATIONFORUMNAME`
- `TIEBA_ONLINE_RESTRICTED__ASSETS__MODERATIONTHREADID`
- `TIEBA_ONLINE_RESTRICTED__ASSETS__MODERATIONREPLYID`
- `TIEBA_ONLINE_RESTRICTED__ASSETS__ADMINUSERNAME`
- `TIEBA_ONLINE_RESTRICTED__ASSETS__ADMINUSERID`
- `TIEBA_ONLINE_RESTRICTED__ASSETS__ADMINPORTRAIT`

### Restricted 占位示例

```powershell
$env:TIEBA_ONLINE_RESTRICTED__OPTIN = "true"
$env:TIEBA_ONLINE_RESTRICTED__ACCOUNT__BDUSS = "<restricted-bduss>"
$env:TIEBA_ONLINE_RESTRICTED__ACCOUNT__STOKEN = "<restricted-stoken>"

$env:TIEBA_ONLINE_RESTRICTED__CAPABILITIES__MODERATION = "true"
$env:TIEBA_ONLINE_RESTRICTED__CAPABILITIES__ADMIN = "false"

$env:TIEBA_ONLINE_RESTRICTED__ASSETS__MODERATIONFORUMNAME = "<restricted-moderation-forum-name>"
$env:TIEBA_ONLINE_RESTRICTED__ASSETS__MODERATIONTHREADID = "<restricted-moderation-thread-id>"
$env:TIEBA_ONLINE_RESTRICTED__ASSETS__MODERATIONREPLYID = "<restricted-moderation-reply-id>"
$env:TIEBA_ONLINE_RESTRICTED__ASSETS__ADMINUSERNAME = "<restricted-admin-user-name>"
$env:TIEBA_ONLINE_RESTRICTED__ASSETS__ADMINUSERID = "<restricted-admin-user-id>"
$env:TIEBA_ONLINE_RESTRICTED__ASSETS__ADMINPORTRAIT = "<restricted-admin-portrait>"
```

如果你想对照模板理解字段位置，`online-test.restricted.template.json` 的结构等价于下面这种空白形状：

```json
{
  "restricted": {
    "optIn": false,
    "account": {
      "bduss": "",
      "stoken": ""
    },
    "capabilities": {
      "moderation": false,
      "admin": false
    },
    "assets": {
      "moderationForumName": "",
      "moderationThreadId": 0,
      "moderationReplyId": 0,
      "adminUserName": "",
      "adminUserId": 0,
      "adminPortrait": ""
    }
  }
}
```

实践上可以这样理解：

- `optIn` 是总开关，没有显式开启就不该跑 Restricted。
- `capabilities.moderation` 和 `capabilities.admin` 是能力声明，只开需要的那部分。
- `assets` 必须是受限场景自己的专用资产，不能把 Safe 资产当作默认后备。

## PowerShell 当前会话示例

如果你只想在当前 PowerShell 会话里临时配置，可以直接设置 `$env:` 变量，然后在同一个会话里运行测试。

```powershell
$env:TIEBA_ONLINE_SAFE__ACCOUNT__BDUSS = "<safe-bduss>"
$env:TIEBA_ONLINE_SAFE__ACCOUNT__STOKEN = "<safe-stoken>"

dotnet test AioTieba4DotNet.Tests.Online.Safe/AioTieba4DotNet.Tests.Online.Safe.csproj --configuration Release --nologo
```

## 把用户级 `TIEBA_ONLINE_*` 变量重新载入当前 shell

有些本地终端进程启动得比较早，不会自动拿到你后来写入的用户级 `TIEBA_ONLINE_*` 变量。这种情况下，可以在当前 PowerShell 会话里手动刷新一遍：

```powershell
[Environment]::GetEnvironmentVariables("User").GetEnumerator() |
    Where-Object { $_.Key -like "TIEBA_ONLINE_*" } |
    ForEach-Object {
        Set-Item -Path ("Env:" + $_.Key) -Value ([string]$_.Value)
    }
```

刷新后，当前 shell 会拿到用户级 `TIEBA_ONLINE_*` 配置。之后再运行本地在线测试时，代码仍然会先读跟踪模板，再用这些环境变量覆盖对应字段。
