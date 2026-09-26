# 库与工具开发指南

本目录覆盖当前维护的 v3 .NET 10 客户端库、protobuf 生成器及测试支持。`backend` 是 Trellis 的路由约定；本项目不是 ASP.NET 服务，也没有数据库层。文档站工作参见[前端指南](../frontend/index.md)。库的核心定位与上游功能语义对齐要求见[项目定位与上游对齐](../project-positioning.md)。

## 规范索引

| 指南 | 修改以下内容时阅读 |
| --- | --- |
| [目录结构](./directory-structure.md) | 包边界、组合、公开契约或功能归属 |
| [传输与请求](./transport-guidelines.md) | 打包、HTTP/WebSocket 选择、签名或重试 |
| [模型与代码生成](./mapping-and-codegen.md) | DTO、响应映射、`.proto` 文件或 ProtoGenerator |
| [会话与缓存所有权](./session-and-cache.md) | 认证、生命周期状态、资源或吧信息查询 |
| [错误处理](./error-handling.md) | 校验、服务端错误、取消或回退 |
| [日志规范](./logging-guidelines.md) | 可选的文件日志或诊断输出 |
| [质量规范](./quality-guidelines.md) | 代码风格、测试、本地验证、CI 或文档更新 |

## 开发前检查清单

- 阅读[库规则](../../../AioTieba4DotNet/AGENTS.md)和上述相关指南；修改生成器时还应阅读 [ProtoGenerator 规则](../../../ProtoGenerator/AGENTS.md)。
- 核对[跨目录规则](../../../.junie/guidelines.md)、公开契约和[对齐台账](../../../docs/related/parity.md)中的上游族系。台账负责实现映射和认证说明，历史待办不承担此职责。涉及接口、协议、模型或错误时，按[项目定位与上游对齐](../project-positioning.md)记录固定上游来源及语义差异。
- 添加辅助函数前，追踪最接近的契约／模块／协议／请求／映射路径。需要时使用[复用](../guides/code-reuse-thinking-guide.md)与[跨层](../guides/cross-layer-thinking-guide.md)指南。
- 区分手写代码和生成代码。需要生成时，保持 `.proto` 与生成输出同步更新。
- 选择与变更相符的验证。`safe` 是有真实副作用的在线通道；文档修改不足以成为执行该通道的理由。

## 质量检查

- 保留六个模块、仅支持 .NET 10 的边界、协议无关的 DTO，以及直接构造／DI／工厂共用的组合逻辑。
- 涉及认证、取消、回退和状态修改顺序时验证这些行为，避免意外扩大重试或错误归一化范围。
- 执行适用的[质量检查](./quality-guidelines.md)，记录实际结果和前置条件。构建不能证明真实在线对齐或覆盖率。
- 契约变化时同步公开用法文档、对齐／认证说明、使用者技能及发布／迁移说明。
- 仅修改规范时，检查引用、索引、模板占位文字和空白格式；不要为验证文字而生成协议代码或执行真实在线测试夹具。
