# 执行计划

## 阶段门槛

- [x] 用户同意创建任务并进入规划。
- [x] 用户补充自动本地 Git 提交要求，已并入 PRD 和设计。
- [x] 研究结果、最终 PRD、设计、执行计划及非空上下文清单完整并经检查。
- [x] 用户回复“是”确认最终方案，已执行 `task.py start`。

## 实施步骤与分工

### 1. 固定范围与兼容性基线

- [x] 记录当前 Git 变更、已暂存文件、受影响文件及业务文件哈希，区分既有初始化内容和本次变更。
- [x] 读取相关规范和 `trellis-before-dev`；主会话负责协调和项目规范，实施/检查使用 Trellis 子代理，明确互不覆盖的文件所有权。
- [x] 为本地阶段、日志和 hook 确立翻译后的可见文本与原英文读取兼容范围。

### 2. 翻译并同步稳定入口

- [x] 中文化根 `AGENTS.md`、`.trellis/workflow.md`、config、runtime agents、Codex agents/config、技能及其引用说明。
- [x] 配置和工作流统一检查通过后主会话自动提交；明确子代理交付到主会话的职责。
- [x] 精确调整 `.gitignore` 使 Trellis 技能 Markdown 可纳入提交，保持其他代理内容忽略。

### 3. 本地脚本与 hook 兼容

- [x] 中文化本地注释、docstring、帮助、提示、默认模板和自动提交说明。
- [x] `workflow_phase.py` 与 `session-start.py` 同步接受旧英文和新中文阶段标题。
- [x] `add_session.py` 与相关初始化模板同步支持英文/中文/混合日志的编号、表头、统计、日期和幂等标记。
- [x] 对上述有行为风险的调整添加标准库 `unittest` 回归验证；不为纯翻译逐行编写镜像测试。

### 4. 项目规范与历史可读资料

- [x] 中文化现有规范和共享指南，修正“维护规范使用英文”的冲突要求。
- [x] 主会话更新 `.trellis/spec/project-positioning.md`，接入总索引和相关层索引；写明 aiotieba 固定基线、语义对齐、.NET 习惯和证据边界。
- [x] 在项目自有规范中记录中文维护、自动提交和 Trellis 升级保留规则。
- [x] 在解析兼容性已验证后，翻译归档任务、日志、索引和 JSON/JSONL 人类可读字段；保留历史事实及原始证据。

### 5. 全范围检查

- [x] 派发 `trellis-check` 做全范围检查，自修必要问题并回报。
- [x] 检查中文覆盖、残留英文理由、标记配对、技能 frontmatter、TOML/JSON/JSONL 语法、相对链接和 Markdown 索引。
- [x] 检查未把长期目标写成已验证事实，未改变 .NET 支持范围、对齐基线、C# 接口或线上行为。
- [x] 校验业务文件哈希及范围外工作未被修改。

### 6. 自主提交并收尾

- [ ] 主会话审阅最终差异和检查结果，仅暂存本任务相关文件，创建中文工作提交；依据用户既有授权，不重复询问。
- [ ] 使用正常任务流程归档并记录中文会话，自动创建归档/日志提交。
- [ ] 返回实际提交哈希、完成范围、实际验证和剩余工作区状态。

## 验证命令与证据

以下命令是实施阶段的检查计划，不能写成已通过：

```bash
python3 .trellis/scripts/get_context.py
python3 .trellis/scripts/get_context.py --mode phase --platform codex
python3 .trellis/scripts/get_context.py --mode phase --step 1.4 --platform codex
python3 .trellis/scripts/get_context.py --mode phase --step 2.1 --platform codex
python3 .trellis/scripts/get_context.py --mode phase --step 3.4 --platform codex
python3 .trellis/scripts/get_context.py --mode packages
python3 .trellis/scripts/get_context.py --mode record
python3 .trellis/scripts/task.py current --json
python3 .trellis/scripts/task.py validate .trellis/tasks/09-26-trellis-chinese-aiotieba
python3 .trellis/scripts/task.py list-context .trellis/tasks/09-26-trellis-chinese-aiotieba
python3 -m unittest discover -s .trellis/scripts/tests -p 'test_*.py'
git diff --check
```

- 使用 `ast.parse` 检查所有受影响 Python 源码，无需写入字节码缓存；用 `tomllib` 和 `json` 检查结构化文件，配置加载继续使用现有解析器。
- 在临时目录验证旧/新阶段提取、平台路由、状态块、hook JSON 与代理上下文路径。
- 临时 Git 仓库验证中文新日志、英文旧日志、混合索引、轮换、幂等重试、归档/日志自动提交及无关已暂存文件隔离；不借此提交真实规划任务。
- 检查命令与真实接口标识保留；翻译本地明确持有的文本，原样保留外部工具诊断。
- `.trellis` 规范本身不在 VitePress 构建根，本轮不运行产品构建、在线测试、文档站依赖安装或代码生成。

## 阶段性证据与回滚点

- 研究文档提供原始契约和行号，不与完成证据混用。
- 实施检查记录写入本任务 `verification.md`，逐项对应 AC1–AC7。
- 所有回滚限定为本任务变更；不执行全仓库 reset、清理其他任务或强制覆盖忽略规则。
