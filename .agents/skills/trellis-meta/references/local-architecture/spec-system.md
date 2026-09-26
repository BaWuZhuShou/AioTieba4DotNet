# 本地规范系统

`.trellis/spec/` 是用户项目专用的工程规范库。Trellis 不要求 AI 记住约定，而是在合适的时机注入相关规范或要求 AI 主动读取。

## 目录模型

常见的单仓库结构：

```text
.trellis/spec/
├── backend/
│   ├── index.md
│   └── ...
├── frontend/
│   ├── index.md
│   └── ...
└── guides/
    ├── index.md
    └── ...
```

常见的 monorepo 结构：

```text
.trellis/spec/
├── cli/
│   ├── backend/
│   │   ├── index.md
│   │   └── ...
│   └── unit-test/
│       ├── index.md
│       └── ...
├── docs-site/
│   └── docs/
│       ├── index.md
│       └── ...
└── guides/
    ├── index.md
    └── ...
```

`index.md` 是各层入口，应列出开发前检查清单和质量检查。具体指南放在同一目录中的其他 Markdown 文件内。

## 包配置

`.trellis/config.yaml` 可以声明包：

```yaml
packages:
  cli:
    path: packages/cli
  docs-site:
    path: docs-site
    type: submodule
default_package: cli
```

AI 可以运行：

```bash
python3 ./.trellis/scripts/get_context.py --mode packages
```

该命令列出当前项目的包与规范层。配置上下文 JSONL 时应参考此输出。

## 规范如何进入任务

任务进入实施前，如果需要任务产物之外的规范或研究上下文，规划阶段可将相关规范写入 `implement.jsonl` / `check.jsonl`：

```jsonl
{"file": ".trellis/spec/cli/backend/index.md", "reason": "CLI 后端约定"}
{"file": ".trellis/spec/cli/unit-test/conventions.md", "reason": "测试要求"}
```

子代理或平台前置指令读取这些 JSONL 文件，并加载引用的规范。平台不支持子代理时，AI 应按照工作流直接读取相关规范。

## 规范应包含什么

规范应包含项目可执行的工程约定，而非泛泛的最佳实践：

- 文件应放在哪里。
- 应如何表达错误处理。
- API、钩子和命令的输入/输出契约。
- 禁止采用的模式。
- 需要测试的情况。
- 项目专属易错点及规避方法。

AI 在实施或调试中学到新规则时，应更新 `.trellis/spec/`，不要只在聊天中总结。

## 本地定制位置

| 需求 | 修改位置 |
| --- | --- |
| 添加新规范层 | `.trellis/spec/<package>/<layer>/index.md` 及对应指南文件。 |
| 修改 monorepo 规范映射 | `.trellis/config.yaml` 中的 `packages` / `default_package` / `spec_scope`。 |
| 修改 AI 实施前读取的规范 | 任务的 `implement.jsonl`。 |
| 修改 AI 检查时读取的规范 | 任务的 `check.jsonl`。 |
| 修改何时更新规范 | `.trellis/workflow.md` 中的阶段 3.3 和 `trellis-update-spec` 技能。 |

## 边界

`.trellis/spec/` 是用户项目的规范，不是 Trellis 内置模板的永久副本。AI 应鼓励用户根据实际项目代码更新规范，不应将 Trellis 默认模板视为不可修改的文档。
