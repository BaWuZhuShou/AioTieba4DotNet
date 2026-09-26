# 初始化依据与审查说明

## 初始发现

- 产品是 .NET 10 客户端库，`Directory.Build.props` 默认使用 C# 14；它不是 ASP.NET 应用。
- 现有政策来源是 `AioTieba4DotNet/AGENTS.md`、`ProtoGenerator/AGENTS.md`、`.junie/guidelines.md` 和 `.editorconfig`。
- `docs/package.json` 与 `docs/.vitepress/config.mts` 表明存在实际文档站前端。选择适用前端模板主题前先检查它们。
- backend/frontend 规范文件当时是初始模板。`.trellis/spec/guides/` 含已有通用思考指南，除非证明不兼容，否则不属于本次规范编写范围。
- 三个活动测试项目分别为 Platform（共享支持）、Online（可执行场景）和 Governance（有序套件宿主与保留离线契约）。通过包装脚本和属性区分离线验证与在线执行；`safe` 是在线通道，不能保证离线。
- `docs/related/parity.md` 负责上游对齐、实现映射和认证说明。`docs/related/public-api-coverage-matrix.md` 负责直接在线 API 的可发现性。
- checkout 初始已有 `AGENTS.md` 改动，以及未跟踪的 `.codex/`、`.gitattributes`、`.trellis/`。保留无关初始化文件，不将其卷入任务提交。
- 当时会话没有 GitNexus/ABCoder 集成；可使用直接源码检查。

## 编写与验证约定

- 遵循 `.agents/skills/trellis-spec-bootstrap/references/spec-writing.md` 和 PRD。
- 当时要求新规范使用英文正文、具体源码路径、简明示例和明确的本地反例；这是历史约定，当前语言要求以新的中文规范为准。
- 删除或调整不适用模板；不虚构 ORM、迁移、React 或应用状态约定。
- 对照源码检查新编写的本地链接/路径、最终索引、模板标记、空白和代表性陈述。
- 说明命令意图与前置条件。未实际运行，不得声称测试、构建或覆盖率通过。本任务仅修改 Trellis 文档，无需在线测试或代码生成。
