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
- **语言**: 鼓励使用中文注释描述核心逻辑、算法背景（尤其是从 `aiotieba` 移植的部分）以及 API 字段含义。

## 4. 架构与目录结构

### 4.1 API 实现 (`AioTieba4DotNet/Api/`)
每个 API 功能应有独立的文件夹，包含实现类、Protobuf 定义（如有）和实体类映射。
- **基类使用**:
    - 所有 API 类必须继承自 `ApiBase`。
    - **JSON API**: 继承 `JsonApiBase`，使用其内置的 `ParseBody` 静态方法自动处理 `error_code` 检查。
    - **Protobuf API**: 继承 `ProtoApiBase`，使用 `CheckError` 静态方法验证响应。
    - **双模 API**: 若需支持 WebSocket，应继承 `ApiWsBase<TResult>` 或 `ProtoApiWsBase<TResult>`。
- **实现模式**:
    - **Cmd 常量**: 定义 API 对应的指令号（主要用于 WebSocket 和 Protobuf API）。
    - **PackProto / PackForm**: 私有方法，负责将输入参数打包为字节流或表单数据。
    - **ParseBody**: 负责将响应解析为实体类。对于 JSON，调用基类的 `ParseBody` 获取 `JObject`；对于 Protobuf，解析后调用 `CheckError`。
    - **双模调度**: 在 `RequestAsync` 中调用 `ExecuteAsync` 方法。该方法会根据 `TiebaRequestMode` 自动分发到 `RequestHttpAsync` 或 `RequestWsAsync`，并在 WS 不可用时自动回退。
- **依赖**: 统一通过注入的 `HttpCore` 或 `WsCore` (来自基类) 发起网络请求。

### 4.2 模块化入口 (`AioTieba4DotNet/Modules/`)
- 按业务领域划分（如 `ForumModule`, `ThreadModule`, `UserModule`）。
- **职责**: 提供高层次的业务接口，内部封装 API 类的实例化与调用逻辑。
- **注入**: 模块类应通过构造函数注入所需的 Core 组件或其它模块。

### 4.3 实体类与映射 (`Entities/`)
- **位置**: 优先在 API 目录下的 `Entities` 文件夹中定义。通用实体放在 `AioTieba4DotNet/Entities`。
- **FromTbData**: 实体类应包含静态方法 `FromTbData`，负责从 Protobuf 生成类或 JSON 对象转换。
- **内容碎片**: 帖子内容应映射为 `Content` 类，包含多种 `IFrag`（如 `FragText`, `FragImage`, `FragAt`）的集合。

### 4.4 核心层与工具 (`AioTieba4DotNet/Core/`)
- **Utils**: 包含常用的业务工具（如 `TbNumToInt` 转换贴吧热度数字）。
- **Signer / TbCrypto**: 负责请求签名与加密逻辑。
- **HttpCore / WebsocketCore**: 封装底层的 HTTP 和 WebSocket 通信逻辑。

## 5. 异常处理
- **TieBaServerException**: 业务级错误（如 `error_code != 0`）必须抛出此异常。
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

## 8. 协作准则
- **一致性**: 严格遵循现有代码的实现模式。
- **基类优先**: 在实现新的 API 时，必须优先继承 `ApiBase` 及其衍生类，严禁手动编写重复的注入、分发和错误检查代码。
- **DRY 原则**: 优先复用 `Abstractions` 中的接口和 `Core` 中的工具，避免重复造轮子。
- **高性能**: 关注异步调用的开销，避免不必要的内存分配（如在大循环中使用集合表达式时需权衡）。
