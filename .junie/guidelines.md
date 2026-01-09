# Junie 编码指南 - AioTieba4DotNet

本指南旨在为 AI 助手（Junie）在维护和扩展 `AioTieba4DotNet` 项目时提供一致的编码标准、架构规范和最佳实践。

## 1. 项目概览
`AioTieba4DotNet` 是一个基于 .NET 现代特性构建的高性能百度贴吧异步操作库。它参考了 Python 版 `aiotieba` 的设计理念，旨在提供模块化、可扩展且易于使用的 C# 实现。

## 2. 核心技术栈
- **运行时**: .NET 8/9/10
- **语言版本**: C# 12+ (深度使用 Primary Constructors, Collection Expressions, Nullable Reference Types 等)
- **通信协议**: HTTP/1.1 (App 端), WebSocket
- **序列化**: Protobuf (主要), JSON (部分旧 API 或简单接口)
- **依赖注入**: 遵循 `Microsoft.Extensions.DependencyInjection` 标准

## 3. 代码风格与规范

### 3.1 命名规范
- **类/接口/方法**: `PascalCase`
- **私有字段**: `_camelCase`
- **局部变量**: `camelCase`
- **常量**: `PascalCase` (如 API 中的 `Cmd`, `Const.MainVersion`)
- **接口**: 以前缀 `I` 开头 (如 `ITiebaClient`)

### 3.2 现代 C# 特性
- **Primary Constructors**: 在类定义中直接声明构造函数参数。
- **文件范围命名空间**: `namespace AioTieba4DotNet.Api.GetThreads;`
- **异步编程**: 始终使用 `Task` 和 `async/await`。后缀 `Async` 必须添加到所有异步方法名。
- **目标类型推导**: 使用 `var` 提高代码整洁度，除非类型不明确或为了增强可读性。

### 3.3 注释
- 使用 XML 文档注释（`<summary>`, `<param>`, `<returns>`）描述公共 API。
- 对于复杂的逻辑，尤其是从 `aiotieba` 移植过来的算法，可以使用中文注释以提高可理解性。

## 4. 架构与目录结构

### 4.1 API 实现 (`AioTieba4DotNet/Api/`)
每个 API 功能应有独立的文件夹，包含：
- **API 实现类**: 负责打包请求、发起请求、解析响应。
- **实现模式**:
    - `PackProto` / `PackForm`: 内部私有方法，将参数打包。
    - `ParseBody`: 内部静态私有方法，解析响应并检查 `error_code`。
    - 统一通过 `ITiebaHttpCore` 或 `ITiebaWsCore` 发起请求。
    - 若支持多种请求模式，应提供 `RequestAsync` 调度方法。

### 4.2 模块化入口 (`AioTieba4DotNet/Modules/`)
- 按业务领域划分（如 `ForumModule`, `ThreadModule`, `UserModule`）。
- 模块类应包含多个相关的 API 调用封装，通过构造函数注入具体的 API 实现类或 `ITiebaHttpCore`。

### 4.3 实体类
- 优先在 API 文件夹下的 `Entities` 目录中定义专用的实体类（如 `AioTieba4DotNet/Api/GetThreads/Entities/Threads.cs`）。
- 实体类应包含静态方法 `FromTbData`（接受 Protobuf 对象或 JSON 对象），负责数据映射。

## 5. 异常处理
- 业务错误必须抛出 `TieBaServerException`（包含错误码和消息）。
- 底层连接或协议错误应由相应的 Core 层处理并封装。

## 6. 测试要求
- 测试项目位于 `AioTieba4DotNet.Tests`。
- 新增功能必须包含相应的单元测试。
- 模拟网络响应时，应尽量还原真实 API 的返回结构（JSON 或 Protobuf 字节流）。

## 7. 协作准则
- **一致性**: 严格遵循现有代码的实现模式。例如，如果现有 API 使用 Protobuf，新增类似 API 时也应优先考虑 Protobuf 实现。
- **复用性**: 优先使用 `AioTieba4DotNet.Core` 和 `AioTieba4DotNet.Abstractions` 中的现有工具和定义。
- **简洁性**: 保持代码模块化，避免在 `Module` 中直接编写复杂的解析逻辑。
