# MCP 配置

初始化 Trellis 规范时，推荐使用 GitNexus 和 ABCoder，因为它们能向代理提供架构与 AST 上下文。它们是工具选择，不是平台要求。通过代理宿主提供的 MCP 机制配置即可。

## GitNexus

GitNexus 从仓库构建代码知识图谱，可用于分析模块边界、执行流、依赖关系、影响范围及图查询。

### 安装与索引

```bash
# 在仓库根目录运行。
npx gitnexus analyze

# 检查索引状态。
npx gitnexus status

# 代码变化使分析过期时，重新建立索引。
npx gitnexus analyze
```

索引写入 `.gitnexus/`。仅在项目已使用向量嵌入时保留相应设置；否则普通索引足以完成规范初始化。

### MCP 服务命令

在宿主的 MCP 配置中使用以下服务命令：

```bash
npx -y gitnexus mcp
```

### 常用工具

| 工具 | 用途 |
| --- | --- |
| `gitnexus_query` | 按概念查找执行流和功能区域 |
| `gitnexus_context` | 查看符号的调用者、被调用者、引用及参与的流程 |
| `gitnexus_impact` | 修改符号前了解影响范围 |
| `gitnexus_detect_changes` | 收尾前检查变更符号和受影响流程 |
| `gitnexus_cypher` | 直接执行图查询 |
| `gitnexus_list_repos` | 列出已索引仓库 |

## ABCoder

ABCoder 将代码解析为 UniAST，提供精确的包、文件和节点级结构。可用于查看签名、类型形状、实现、依赖与反向引用。

### 安装

```bash
go install github.com/cloudwego/abcoder@latest
abcoder --help
```

### 解析仓库

```bash
abcoder parse /absolute/path/to/package \
  --lang typescript \
  --name package-name \
  --output ~/abcoder-asts
```

对于 monorepo，按包解析并使用稳定的 `--name`，使任务笔记能引用相同仓库名。

### MCP 服务命令

在宿主的 MCP 配置中使用以下服务命令：

```bash
abcoder mcp ~/abcoder-asts
```

### 常用工具

| 工具 | 层级 | 用途 |
| --- | --- | --- |
| `list_repos` | 1 | 列出已解析仓库 |
| `get_repo_structure` | 2 | 查看包和文件 |
| `get_package_structure` | 3 | 查看包内节点 |
| `get_file_structure` | 3 | 查看文件中的函数、类、类型与签名 |
| `get_ast_node` | 4 | 获取代码、依赖、引用和实现 |

## 验证

配置后，从代理宿主确认两个 MCP 服务均可见。开始编写规范前，分别对每个服务执行一次简单查询。

```bash
ls .gitnexus/meta.json
ls ~/abcoder-asts/*.json
```
