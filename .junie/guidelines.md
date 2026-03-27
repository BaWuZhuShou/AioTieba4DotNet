# Junie 编码指南 - AioTieba4DotNet

本指南旨在为 AI 助手（Junie）在维护和扩展 `AioTieba4DotNet` 项目时提供一致的编码标准、架构规范和最佳实践。

## 1. 项目概览
`AioTieba4DotNet` 是一个基于 .NET 现代特性构建的高性能百度贴吧异步操作库。它参考了 Python 版 `aiotieba` 的设计理念，旨在提供模块化、可扩展且易于使用的 C# 实现。

## 2. 核心技术栈
- **运行时**: .NET 8/9/10
- **语言版本**: C# 12+ (深度使用 Primary Constructors, Collection Expressions, Nullable Reference Types, File-scoped Namespaces 等)
- **通信协议**: HTTP/1.1 (App 端), WebSocket
- **序列化**: Protobuf (主要), JSON (部分旧 API 或简单接口)
- **依赖注入**: 遵循 `Microsoft.Extensions.DependencyInjection` 标准。

## 3. 代码风格与规范

### 3.1 命名规范
- **类/接口/方法**: `PascalCase`
- **私有字段**: `_camelCase`
- **局部变量**: `camelCase`
- **常量**: `PascalCase` (如 API 中的 `Cmd`, `Const.MainVersion`)
- **接口**: 以前缀 `I` 开头 (如 `ITiebaClient`)

### 3.2 现代 C# 特性
- **Primary Constructors**: 在类定义中直接声明构造函数参数（例如 `public class MyService(IHttpClientFactory factory) { ... }`）。
- **文件范围命名空间**: 始终使用 `namespace AioTieba4DotNet.Api;` 格式。
- **异步编程**: 始终使用 `Task`。后缀 `Async` 必须添加到所有异步方法名。
- **目标类型推导**: 使用 `var` 提高代码整洁度，除非类型不明确或为了增强可读性。
- **集合表达式**: 优先使用 `[]` 初始化集合。

### 3.3 注释与文档
- 使用 XML 文档注释（`<summary>`, `<param>`, `<returns>`）描述公共 API。
- **类型引用**: 对于公共 API 的参数、属性或返回值中的复杂类型（如实体类、接口、核心组件），**必须**在 XML 注释中使用 `<see cref="..."/>` 标签，以便在 IDE 中实现类型跳转。
- **返回值标注**: 在 `<returns>` 标签中应明确指出返回值的类型。对于复杂类型使用 `<see>`，对于简单类型（如 `string`, `bool`, `ulong`）建议在描述后用括号标注，如 `操作是否成功 (bool)` 或 `吧 ID (ulong)`。
- **语言**: 鼓励使用中文注释描述核心逻辑、算法背景（尤其是从 `aiotieba` 移植的部分）以及 API 字段含义。

## 4. 架构与目录结构

### 4.1 原版 (Python aiotieba) 映射指南
在实现或维护功能时，必须参考 `aiotieba/` 目录下的原版 Python 代码。

| 类别 | 原版位置 (Python) | 对应位置 (C#) |
| :--- | :--- | :--- |
| **API 实现** | `aiotieba/aiotieba/api/{api_dir}/_api.py` | `AioTieba4DotNet/Api/{ApiDir}/{ApiName}.cs` |
| **Protobuf 定义** | `aiotieba/aiotieba/api/{api_dir}/protobuf/*.proto` | `AioTieba4DotNet/Api/{ApiDir}/Protobuf/*.proto` |
| **业务模块** | `aiotieba/aiotieba/client.py` (类方法) | `AioTieba4DotNet/Modules/{Category}Module.cs` |
| **核心层** | `aiotieba/aiotieba/core/` | `AioTieba4DotNet/Core/` |
| **常量** | `aiotieba/aiotieba/const.py` | `AioTieba4DotNet/Core/Const.cs` |

**快速查找技巧**:
1. **通过路径查找**: 若已知 API 的 HTTP 路径（如 `/c/c/thread/add`），在 `aiotieba/aiotieba/api` 目录下搜索该字符串即可定位原版逻辑。
2. **通过 CMD 查找**: 对于 Protobuf/WS API，可以通过其 `CMD` 数字（如 `309731`）在原版中全局搜索。
3. **参数对齐**: Python 中的 `pack_proto` 或 `pack_form` 包含了最准确的请求参数构造逻辑，C# 实现必须逐字段对照。

### 4.2 API 实现 (`AioTieba4DotNet/Api/`)
每个 API 功能应有独立的文件夹，包含实现类、Protobuf 定义（如有）和实体类映射。
- **基类使用**:
    - 所有 API 实现类（包括基类 `ApiBase` 及其派生类）必须声明为 `internal`。它们不应直接暴露给库的用户，而应通过 `Modules` 层的公开接口进行访问。
    - 所有 API 类必须继承自 `ApiBase`。`ApiBase` 提供了统一的 `CheckError` (错误码检查) 和 `ParseBody` (JSON 解析) 静态方法，子类应通过静态调用复用这些逻辑。
    - **JSON API**: 继承 `JsonApiBase`。
    - **Protobuf API**: 继承 `ProtoApiBase`。对于响应，调用 `ApiBase.CheckError` 验证 Protobuf 错误。
    - **双模 API**: 若需支持 WebSocket，应继承 `ApiWsBase<TResult>` (JSON) 或 `ProtoApiWsBase<TResult>` (Protobuf)。
- **实现模式**:
    - **协议优先**: 参照原版 `aiotieba`，对于支持 Protobuf 的 API，无论在 HTTP 还是 WebSocket 模式下，都应优先使用 Protobuf 协议以提升性能并保持一致性。
    - **Cmd 常量**: 定义 API 对应的指令号（主要用于 WebSocket 和 Protobuf API）。
    - **参数打包 (PackProto / PackForm)**:
        - 必须严格遵循原版 `aiotieba` 的逻辑。
        - **CommonReq 补全**: Protobuf API 必须包含 `CommonReq`。C# 中通常通过 `account` 获取字段（如 `Bduss`, `ClientId`, `CuidGalaxy2`, `Tbs`, `Stoken` 等）。
        - **签名**: HTTP 表单请求必须进行签名。`HttpCore.SendAppFormAsync` 会自动处理签名逻辑，只需传入 `List<KeyValuePair<string, string>>` 即可。
    - **请求发起**: 统一调用 `HttpCore.Send*Async` 方法。这些方法会自动处理资源的自动释放（Disposal），**严禁**在 API 层手动处置 `HttpResponseMessage`。
    - **双模调度**: 在 `RequestAsync` 中调用 `ExecuteAsync` 方法。该方法会根据 `TiebaRequestMode` 自动分发到 `RequestHttpAsync` 或 `RequestWsAsync`。若 WS 模式未实现或不可用，应妥善处理回退逻辑。
    - **API 标记 (PythonApi)**: 所有 API 类必须添加 `[PythonApi("aiotieba.api.{path}")]` 特性，以指明其在原版 Python `aiotieba` 中对应的接口路径，方便开发者和 AI 进行查找对比与逻辑对齐。
    - **鉴权**: 若 API 需要用户登录，**必须**在 API 类上添加 `[RequireBduss]` 特性。
- **依赖**: 统一通过注入的 `HttpCore` 或 `WsCore` (来自基类) 发起网络请求。

### 4.2 Protobuf 工具链 (`ProtoGenerator/`)
项目包含自动化的 Protobuf 代码生成工具。
- **定义位置**: `.proto` 文件应放在对应 API 目录下的 `Protobuf` 文件夹中。公共定义放在 `Api/Protobuf`。
- **生成代码**: 使用 `ProtoGenerator` 项目生成 C# 代码。运行命令：`dotnet run --project ProtoGenerator\ProtoGenerator.csproj --framework <net_version>`。
- **规范**: 严禁手动修改生成的 `.cs` 代码，所有变更应通过修改 `.proto` 文件并重新生成来实现。

### 4.3 模块化入口 (`AioTieba4DotNet/Modules/`)
- 按业务领域划分（如 `ForumModule`, `ThreadModule`, `UserModule`）。
- **职责**: 提供高层次的业务接口，内部封装 API 类的实例化与调用逻辑。
- **注入**: 模块类应通过构造函数注入所需的 Core 组件或其它模块。

### 4.4 实体类与映射 (`Entities/`)
- **位置**: 优先在 API 目录下的 `Entities` 文件夹中定义。通用实体放在 `AioTieba4DotNet/Entities`。
- **FromTbData**: 实体类应包含 **`internal`** 静态方法 `FromTbData`，负责从 Protobuf 生成类或 JSON 对象转换。隐藏此方法可以防止将底层的协议模型（如 Protobuf 类）暴露给用户。
- **内容碎片**: 帖子内容应映射为 `Content` 类，包含多种 `IFrag`（如 `FragText`, `FragImage`, `FragAt`）的集合。

### 4.5 核心层与工具 (`AioTieba4DotNet/Core/`)
- **Utils**: 包含常用的业务工具（如 `TbNumToInt` 转换贴吧热度数字）。
- **Signer / TbCrypto**: 负责请求签名与加密逻辑。
- **HttpCore / WebsocketCore**: 封装底层的 HTTP 和 WebSocket 通信逻辑。`HttpCore` 特别提供了高层 API 以简化资源管理，确保所有 `IDisposable` 对象在请求结束后被正确处置。

## 5. 异常处理
- **TieBaServerException**: 服务端返回的业务错误（如 `error_code != 0`）必须抛出此异常。
- **TiebaException**: 客户端检查错误（如缺少必要的 BDUSS）应抛出此异常。
- **底层错误**: 网络异常或协议解析失败应在 Core 层捕获并视情况封装或直接抛出。

## 6. 依赖注入 (DI)
- 项目在 `DependencyInjection.cs` 中提供 `AddAioTiebaClient` 扩展方法。
- 新增模块或全局服务时，应在此方法中进行注册。
- 模块通常注册为 `Scoped`。

## 7. 测试要求
- **位置**: `AioTieba4DotNet.Tests` 镜像源代码结构。
- **基类**: 测试类应继承 `TestBase` 以获取预配置的 `HttpCore` 和 `WebsocketCore`。
- **集成测试**: 默认支持基于 `appsettings.test.json` 或环境变量的集成测试。
- **单元测试**: 新增复杂逻辑（如解析器、工具类）必须包含单元测试。模拟网络响应时，应尽量还原真实 API 的数据结构。

## 8. 协作与任务指南
- **任务来源**: 优先参考 `docs/todo.md` 中列出的未实现功能。
- **一致性**: 严格遵循现有代码的实现模式。
- **对齐原版**:
    - 任何 API 的参数逻辑、打包规范、响应解析及错误处理，必须严格参照 Python 版 `aiotieba`。
    - **严禁**私自增加原版不存在的参数或逻辑（除非有明确的跨平台适配需求）。
    - 若原版逻辑使用了特殊的位运算或加密（如 `cuid` 生成），请在 `Core/TbCrypto` 中寻找对应实现。
- **基类优先**: 在实现新的 API 时，必须优先继承 `ApiBase` 及其衍生类，严禁手动编写重复的注入、分发和错误检查代码。
- **资源管理**: 严格遵守 `IDisposable` 处置规范。对于 HTTP 请求，应始终利用 `HttpCore` 封装好的高层方法，避免在业务代码中暴露底层的资源管理细节。
- **DRY 原则**: 优先复用 `Abstractions` 中的接口和 `Core` 中的工具，避免重复造轮子。
- **高性能**: 关注异步调用的开销，避免不必要的内存分配。在 Protobuf 和 JSON 之间权衡时，优先选择 Protobuf。
- **准则维护**: **必须**将新发现的需求、复杂的业务逻辑、核心知识点或重要的架构决策回写到本 `guidelines.md` 文件中。这确保了知识的沉淀，并能指导未来的开发任务。

## 9. 并发控制与资源缓存规范
- **适用场景**: 当多个并发请求可能同时触发同一个昂贵的初始化操作（如刷新 Token、获取 TBS、懒加载配置）时。
- **解决方案**: 统一使用 `AsyncInit<T>` 工具类。
    - **位置**: `AioTieba4DotNet.Core.AsyncInit<T>`
    - **原理**: 封装了标准的“异步双重检查锁定 (Async Double-Check Locking)”模式，内部使用 `SemaphoreSlim(1, 1)` 确保线程安全。
- **使用指南**:
    - 在构造函数或初始化方法中实例化 `AsyncInit<T>`，传入工厂方法 `Func<Task<T>>`。
    - 调用 `GetAsync()` 获取资源，该方法会自动处理加锁、双重检查和初始化。
    - 若需预设值，可调用 `SetValue(T value)`。
- **代码示例**:
    ```csharp
    private readonly AsyncInit<string> _resourceInit;

    public MyService()
    {
        // 定义初始化逻辑
        _resourceInit = new AsyncInit<string>(async () =>
        {
            return await FetchResourceAsync();
        });
    }

    public async Task<string> GetResourceAsync()
    {
        // 线程安全的获取
        return await _resourceInit.GetAsync();
    }
    ```
- **后台自动初始化**:
    - 若需 Fire-and-forget 初始化，可直接调用 `_ = Task.Run(() => _resourceInit.GetAsync());`。
    - 前台请求与后台任务共享同一个 `AsyncInit` 实例，天然保证了竞态控制和去重。
- **吧信息缓存 (ForumInfoCache)**:
    - **位置**: `AioTieba4DotNet.Core.ForumInfoCache`
    - **原理**: 封装了 `MemoryCache`，用于存储吧名 (ForumName) 与吧 ID (Fid) 的双向映射。
    - **使用规范**: 在涉及需要频繁转换吧名和 Fid 的场景中，应优先通过 `ForumInfoCache` 获取。若缓存未命中，再调用相应的 API 并通过 `SetForumName` 更新缓存。

## 10. 可见性与 API 暴露规范
为了保持公开 API 的简洁性并隐藏底层实现细节，必须严格遵守以下可见性规则：
- **API 实现类**: `AioTieba4DotNet/Api/` 下的所有类（包括基类和具体 API 实现）均设为 `internal`。它们应通过 `Modules` 层的公开接口暴露功能。
- **实体类 (Entities)**: 保持 `public`。它们是用户获取数据的唯一合法途径。
- **数据转换方法**: 实体类中的 `FromTbData` 静态方法必须设为 `internal`。
- **Protobuf 类**: 保持 `public`。因为它们是由工具生成的，手动修改可见性会在重新生成时被覆盖。
- **测试支持**: 项目配置了 `InternalsVisibleTo`，因此 `internal` 成员对测试项目依然可见。

## 11. Semantic Versioning 策略
此项目是面向 NuGet 使用者的 C# 类库。版本号必须围绕“使用者会编译依赖、运行依赖、或从文档中形成预期”的内容来判断，而不是围绕内部 `Api/*` 实现细节来判断。

### 11.1 兼容性范围
- SemVer 的判断对象是公开类库契约，包括 `TiebaClient`、`DependencyInjection.AddAioTiebaClient`、`Abstractions/*` 中的公开接口、公开实体类型、公开异常类型、`TiebaOptions`、`TiebaRequestMode`，以及保留兼容性的 `Client.cs` 包装层。
- `AioTieba4DotNet/Api/*` 下的内部实现类、请求打包细节、解析流程、生成后的 protobuf 实现细节，不单独触发版本升级；只有当它们改变了公开签名、公开行为或文档承诺时，才按 SemVer 处理。

### 11.2 文档行为也属于契约
- `README.md`、`docs/modules.md`、`docs/advanced.md`、公共 XML 文档注释共同定义了使用者对类库的行为预期。
- 即使公开方法签名未变化，只要修改了下列任一项，仍应视为契约变更：参数语义、默认行为、返回语义、异常抛出时机、请求模式回退行为、鉴权前置检查、可观察的资源生命周期行为。

### 11.3 目标框架支持承诺
- 当前稳定支持的 Target Frameworks 为 `net8.0`、`net9.0`、`net10.0`。
- 删除任一已支持 TFM、提高最低支持版本，属于 **MAJOR**。
- 在保留现有 TFM 的前提下新增支持的 TFM，属于 **MINOR**。

### 11.4 发布标签与版本来源
- 正式发布采用标签驱动，格式为 `vMAJOR.MINOR.PATCH`。
- `.github/workflows/publish.yml` 会去掉 `v` 前缀，并将标签值作为 NuGet 包版本与 GitHub Release 版本来源。
- 当前规则只覆盖稳定版本；若将来引入 `-preview`、`-rc` 等预发布标签，应单独补充对应策略。

### 11.5 MAJOR / MINOR / PATCH 判定规则
- **MAJOR**:
  - 删除、重命名或修改任何公开类型、接口、方法、属性、构造函数、枚举值、参数列表、返回类型或空值性约束。
  - 改变 `TiebaClient`、`ITiebaClientFactory`、`AddAioTiebaClient`、`TiebaOptions` 等公开入口的既有行为，导致旧调用方式失效或结果语义变化。
  - 改变已文档化的异常契约、鉴权行为、请求模式传播与回退逻辑、资源释放语义，导致现有使用者需要修改代码或运行时假设。
  - 移除或破坏 `Client.cs` 兼容层，或删除已支持的 `net8.0` / `net9.0` / `net10.0`。
- **MINOR**:
  - 以向后兼容方式新增公开方法、公开重载、公开实体字段/属性、可选配置项或新的模块能力。
  - 在不破坏现有调用的前提下，新增可选行为、新支持的贴吧 API 封装能力或新的目标框架支持。
  - 扩展文档化能力边界，但不改变既有调用结果与默认语义。
- **PATCH**:
  - 不改变公开契约的 bug 修复、性能优化、内部重构、测试补充、生成链路修正、文档澄清。
  - 修复内部解析、签名、传输或缓存问题，只要对现有公开 API 的签名、异常契约和已承诺行为没有破坏性变化。

### 11.6 执行准则
- 判断版本升级时，优先检查“公开签名是否变化”“文档承诺是否变化”“支持的目标框架是否变化”。
- 当改动同时包含新增能力与破坏性调整时，以最高影响级别为准。
- 不要因为项目文件中的默认开发版本号而放宽破坏性变更标准；只要进入稳定标签发布流，就按正式类库的 SemVer 规则执行。

### 11.7 PR / Release 快速检查清单
在合并 PR 或创建发布标签前，快速确认以下问题：
- **版本等级**: 这次改动是否触碰公开签名、公开入口、公开异常契约、`Client.cs` 兼容层或 `TiebaRequestMode` 等公开契约？若是，按 `11.5` 判定，并以最高影响级别作为最终版本等级。
- **文档行为**: 这次改动是否改变了 `README.md`、`docs/*` 或公共 XML 注释中已经承诺的参数语义、默认行为、异常时机、鉴权检查、请求模式回退或资源生命周期？若是，按契约变更处理。
- **目标框架**: 这次改动是否删除、提高或新增受支持的 TFM？删除/提高最低支持版本是 **MAJOR**；仅新增支持是 **MINOR**。
- **文档更新**: 如果用户可见行为、调用方式、默认值或能力边界发生变化，只更新直接受影响的文档与 XML 注释，不在多处重复维护规则说明。
- **兼容性说明**: 如果现有调用方需要改代码、调整运行时假设，或需要从旧入口迁移到新入口，必须在 PR 描述或 Release Notes 中写清迁移/兼容性说明。
- **PATCH 自检**: 如果改动只是内部 bug 修复、性能优化、内部重构、测试补充、生成链路维护或文档澄清，且没有公开契约与文档行为变化，则应保持为 **PATCH**。
- **记录依据**: 在 PR 描述或发版说明中明确写出本次选择 `MAJOR` / `MINOR` / `PATCH` 的依据，避免后续重新解读同一批变更。

### 11.8 PR 标签参考
- `.github/PULL_REQUEST_TEMPLATE.md` 是 PR 作者提交分类建议的入口；`.github/release.yml` 是 release notes 分类所依赖的标签真源。
- Maintainer 在 triage 时应对照 PR 模板中的勾选结果，补齐或修正实际 PR labels，而不是只依赖正文描述。
- Release-note 主分类优先只保留一个，使用下列标签族之一：
  - `feat` / `feature` / `enhancement`
  - `fix` / `bug`
  - `refactor`
  - `perf`
  - `docs` / `documentation`
  - `test` / `tests`
  - `chore` / `dependencies`
  - `build` / `ci`
  - `skip-changelog`
- `skip-changelog` 仅用于确实不需要进入用户可见 release notes 的改动，例如纯内部维护、无用户影响的仓库整理，或仅与机器人/自动化相关的变更。
- 如需额外补充便于检索的 workflow labels，可使用维护约定标签：`semver:major`、`semver:minor`、`semver:patch`、`semver:unsure`、`breaking-change`、`migration-needed`。这些标签仅用于协作检索，不参与 `.github/release.yml` 的分类。
- 如果 PR 被标记为 `breaking-change`、`migration-needed` 或其 SemVer 判定为 `MAJOR`，则 PR 描述或 release notes 中必须包含迁移说明。
- 在修改 `.github/release.yml` 的标签集合时，必须同步检查 `.github/PULL_REQUEST_TEMPLATE.md` 与本节内容是否仍然一致。
