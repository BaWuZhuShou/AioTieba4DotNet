# 仓库分析

目标是在编写规则前发现项目的真实架构。不要从通用规范模板出发填空；先读代码，再让规范结构与代码对应。

## 分析顺序

1. 阅读现有 `.trellis/spec/`，记录哪些文件仍是模板、已经过期，哪些已针对项目定制。
2. 检查包清单、构建脚本、工作区配置与顶层文档，识别包和运行时层次。
3. 使用 GitNexus 分析执行流、模块分组、依赖枢纽和影响敏感区域。
4. 使用 ABCoder 或语言原生工具获取精确签名、类型、类边界和实现示例。
5. 将任何发现写成规范规则前，直接阅读有代表性的源码与测试文件。

## 应记录的内容

| 领域 | 问题 |
| --- | --- |
| 包边界 | 每个包负责什么？哪些导入跨越边界？ |
| 运行时层次 | 哪些代码属于 CLI、后端、前端、worker、共享库、测试专用或工具？ |
| 核心抽象 | 哪些类型、服务、存储、命令、路由或适配器决定系统结构？ |
| 数据流 | 用户输入从哪里进入、如何校验、状态持久化在哪里？ |
| 错误处理 | 如何表达、记录、展示和测试失败？ |
| 配置 | 默认值、环境配置、生成文件和模板放在哪里？ |
| 测试 | 哪些测试风格可作为新增工作的可靠范例？ |

## GitNexus 用法

先看全局，再检查具体符号：

```text
gitnexus_query({query: "CLI 命令执行流"})
gitnexus_query({query: "模板生成与迁移"})
gitnexus_context({name: "SymbolName"})
gitnexus_cypher({query: "MATCH (n)-[r]->(m) RETURN n.name, type(r), m.name LIMIT 30"})
```

用 GitNexus 结果定位重要文件与流程。检查相关源码前，不要把图谱输出作为最终权威依据。

## ABCoder 用法

规范需要精确代码形状时使用 ABCoder：

```text
list_repos()
get_repo_structure({repo_name: "package-name"})
get_file_structure({repo_name: "package-name", file_path: "src/example.ts"})
get_ast_node({repo_name: "package-name", node_ids: [{mod_path: "...", pkg_path: "...", name: "SymbolName"}]})
```

ABCoder 尤其适合记录构造模式、函数签名、类型契约和引用链。

## 分析笔记

分析时保留简短笔记，包括：

- 包或层的名称。
- 定义本地模式的文件。
- 规范应传达的规则。
- 旧代码、注释、测试或迁移路径中发现的反模式。
- 应创建、删除、重命名或合并的规范文件。
