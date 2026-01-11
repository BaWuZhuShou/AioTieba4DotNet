# ProtoGenerator

这是一个为 `AioTieba4DotNet` 量身定制的、现代化且彻底独立运行的 Protobuf 到 C# 代码生成工具。

## ✨ 特点

- **彻底独立运行**：深度集成 `Google.Protobuf.Tools`。工具在构建时会自动根据当前操作系统（Windows/Linux/macOS）提取对应的
  `protoc` 二进制文件，**无需**在系统中预装 Protobuf 环境。
- **现代化 C#**：基于 **.NET 10** 和顶级语句 (Top-level statements) 构建。
- **智能路径探测**：运行时自动向上递归探测项目根目录（通过寻找 `.sln` 文件），确保在任何环境下启动都能正确定位。
- **全自动递归扫描**：自动扫描 `AioTieba4DotNet/Api` 目录及其所有子目录下的 `.proto` 文件，并将其转换为对应的 C# 类文件。
- **完善的依赖支持**：自动处理对 `AioTieba4DotNet/Api/Protobuf` 目录下公共定义文件的引用（`-I` 参数支持）。

## 🚀 如何运行

在项目根目录下执行：

```bash
dotnet run --project ProtoGenerator/ProtoGenerator.csproj
```

生成的 C# 文件将保存在与其对应的 `.proto` 文件相同的目录下，方便立即使用。

## 🛠️ 开发说明

如果你需要添加新的 API 接口：

1. 在 `AioTieba4DotNet/Api` 的相应模块目录下创建 `.proto` 文件。
2. 如果需要引用通用的 Protobuf 定义，请确保这些通用定义位于 `AioTieba4DotNet/Api/Protobuf` 目录下。
3. 运行此工具，它将自动扫描并生成对应的 `.cs` 代码。
4. 生成的代码默认包含 `serializable` 特性（通过 `--csharp_opt=serializable` 参数）。
