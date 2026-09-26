# 文档前端指南

前端是 `docs/` 下的 VitePress 文档站，描述当前维护的 v3、仅支持 .NET 10 的库。它使用默认主题、Markdown 页面和一个 TypeScript 配置文件；当前检出没有自定义组件、hook 或应用状态层。库以 Python aiotieba 为功能与接口语义参照，见[项目定位与上游对齐](../project-positioning.md)。

这些维护者规范使用中文。现有站点主要为中文（`lang: 'zh-CN'`），也有部分英文参考资料；编辑产品文档页面时，应保留该页的语言和用途。Trellis 内容的持续中文维护规则见 [Trellis 中文维护与自动提交](../trellis-localization.md)。

## 规范索引

| 指南 | 阅读时机 |
| --- | --- |
| [目录结构](./directory-structure.md) | 新增、移动页面或确定页面归属 |
| [内容契约](./content-guidelines.md) | 修改示例、公开 API 参考、对齐或覆盖率声明 |
| [站点配置](./site-configuration.md) | 修改导航、首页呈现、路由或配置类型 |
| [质量规范](./quality-guidelines.md) | 选择本地检查并报告其实际范围 |

## 开发前检查清单

- 阅读[目录结构](./directory-structure.md)和[内容契约](./content-guidelines.md)，再阅读受影响页面及其链接的源码声明。
- 路由或呈现变化需阅读[站点配置](./site-configuration.md)，并检查 [config.mts](../../../docs/.vitepress/config.mts) 中的两个导航列表。
- 选择命令前阅读[质量规范](./quality-guidelines.md)；文档契约验证和站点生成是独立检查。
- 涉及跨层声明或重复文档规则时，参阅[共用思考指南](../guides/index.md)。
- 涉及库行为或 C# 示例时，同时参阅[库指南](../backend/index.md)、[库规则](../../../AioTieba4DotNet/AGENTS.md)及适用的公开契约。文档必须描述已实现的行为。

## 质量检查

- 对照源码核对 v3／.NET 10 支持声明、六模块边界、签名和示例参数类型。
- 路径变化时检查页面链接、稳定锚点、首页链接、导航和必需文档清单。
- 区分对齐台账与直接在线覆盖记录的职责，保留机器读取的表格格式。
- 执行[质量规范](./quality-guidelines.md)中的相关检查，明确记录缺失的前置条件和跳过的检查。

主要规则来源为 [.junie/guidelines.md](../../../.junie/guidelines.md)、[docs/package.json](../../../docs/package.json) 和 [docs/.vitepress/config.mts](../../../docs/.vitepress/config.mts)。
