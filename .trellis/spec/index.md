# AioTieba4DotNet 开发规范

AioTieba4DotNet 是以 Python aiotieba 为功能与接口语义参照的 C# 实现，面向 C#/.NET 使用者提供异步贴吧客户端库。这些规范覆盖当前维护的 v3、仅支持 .NET 10 的客户端库及其文档站。项目持续对齐上游能力；具体实现、差异及验证情况以现有台账和证据为准，不能把目标表述为已全面对齐。

除非另有说明，代码片段中的路径均相对于仓库根目录。各目录的 `AGENTS.md` 和可执行契约仍是主要依据；这些指南将相关规则组织为便于实施与审查的入口。

## 项目级约定

- [项目定位与上游对齐](project-positioning.md)：固定 aiotieba 基线、Python → C# 语义对齐、.NET 接入习惯及证据边界。
- [Trellis 中文维护与自动提交](trellis-localization.md)：中文内容范围、机器契约保护、主会话自动提交和升级复核。

## 选择规范层

| 工作范围 | 起始入口 | 覆盖内容 |
| --- | --- | --- |
| 客户端库、协议、生成器或测试支持 | [库与工具](backend/index.md) | `AioTieba4DotNet/`、`ProtoGenerator/`、三个有效测试项目及验证脚本 |
| 文档内容或站点配置 | [文档前端](frontend/index.md) | `docs/` VitePress 站点及面向使用者的文档契约 |
| 代码复用或跨边界变更 | [思考指南](guides/index.md) | 与相关实施规范一起使用的通用问题清单 |

## 开发前

1. 阅读任务需求和受影响代码最近的 `AGENTS.md`。
2. 遵循所选规范层索引中的**开发前检查清单**。
3. 对使用者可见的库变更，同时阅读两个规范层：示例、对齐记录、模块文档和对外提供的 `skills/aiotieba4dotnet/` 包可能需要随 C# 实现同步更新。
4. 用当前源码确定实施细节。如果源码行为与既有规则冲突，应记录差异，不得静默编造新契约。

## 审查

遵循各受影响规范层索引中的**质量检查**。选择与变更行为相符的验证，并报告实际执行结果。在线 `safe` 通道与离线测试不同；构建成功不能证明真实服务行为或仓库覆盖率目标已达成。

## 既有规则的权威来源

- `AioTieba4DotNet/AGENTS.md`：库边界和局部约定。
- `ProtoGenerator/AGENTS.md`：生成器职责和重新生成规则。
- `.junie/guidelines.md`：长期跨目录规则。
- `.editorconfig`、`Directory.Build.props`、`Directory.Build.targets`：代码风格、编译默认值和覆盖率配置。
- `docs/related/parity.md`：上游对齐、实现映射和认证说明。
- `docs/related/public-api-coverage-matrix.md`：在线 API 可发现性。

本仓库没有应用数据库层，后端指南覆盖的是内存中的会话与缓存状态。前端是文档站，不预设应用组件、hook 或状态管理框架。
