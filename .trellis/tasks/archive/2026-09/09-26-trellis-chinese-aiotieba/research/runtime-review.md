# 运行时审查与自修记录

- 日期：2026-09-26。
- 角色：已派发的 `trellis-check`；未再派发代理，未提交或归档真实项目任务。
- 范围：`.trellis/scripts/` 全部本地 Python 脚本、4 个回归测试文件，以及 `.codex/hooks/` 三个入口；最终共 35 个 Python 文件。
- 原始对比基线：`/tmp/trellis-zh-20260926-01a0dd3b-baseline`。多数脚本原先未跟踪，因此以该快照核对，未将 `git diff` 当作唯一差异来源。
- 审查依据：本任务 PRD、设计、执行计划、两份研究及 `.trellis/spec/trellis-localization.md`；按 `trellis-check`、`trellis-before-dev` 进行审查与局部自修。

## 已修复发现

### 1. 归档自动提交实际会纳入其他归档任务

- 文件：`common/safe_commit.py`、`common/task_store.py`、`tests/test_archive_scope.py`。
- 问题：`safe_archive_paths_to_add(task_name=...)` 实际返回整个 `.trellis/tasks/archive`，与“只提交本任务”的说明不符。关联子任务也按整个目录暂存，可能带入无关笔记。
- 修复：移动操作产生的真实 `archive_dest` 经 `cmd_archive`、`_auto_commit_archive` 传递至辅助函数；辅助函数只返回该目标及实际更新的子任务 `task.json`。删除无调用需求的整棵归档树回退，目标参数必须显式提供。任务实施者同步调用位置，检查者修改辅助函数并添加回归。
- 证据：全仓搜索仅发现 `task_store.py` 一个辅助函数调用。两个真实临时 Git 仓库测试验证其他月份、当前月份及同名旧归档的暂存差异、未暂存差异与磁盘字节完整保留；关联子任务的无关笔记同样保留。独立用例证明提交采用传入的目标月份，不在提交时重新推断日期。

### 2. 中文默认摘要改变旧会话的重试指纹

- 文件：`add_session.py`、`tests/test_journal_localization.py`。
- 问题：旧版省略 `--summary` 时，英文默认摘要参与记录指纹；直接将默认摘要翻成中文后，相同标题与幂等键会产生不同指纹。修复前在临时仓库复现：旧记录后用新默认值重试，会话总数由 1 变为 2。
- 修复：可见默认摘要统一为 `DEFAULT_SUMMARY` 的中文文案；仅在指纹载荷内将这个明确默认占位值归一到 `LEGACY_DEFAULT_SUMMARY` 的原英文机器身份。真实用户摘要和原始历史标记不改写。
- 证据：新增跨升级用例确认旧日志及索引逐字节不变、会话编号保持 1，提交后再用同一幂等键重试不产生新提交；另一个用例确认新日志的可见默认摘要仍为中文。日志专项最终 12 项通过。

### 3. 忽略路径失败提示错误地声称 Git 状态未改变

- 文件：`common/safe_commit.py`。
- 问题：原提示“未修改 Git 状态”并不准确。临时仓库实测 `git add allowed.txt ignored.txt` 返回失败，但 `allowed.txt` 已进入暂存区。
- 修复：提示改为“尚未创建提交；git add 失败前可能已暂存部分路径，请检查 git status”。不改变暂存、忽略保护或回滚行为。

## 其余审查结论

- 通过 AST 去除说明性文字后的结构对比，区分自然语言翻译与真实控制流变化；另行比较条件表达式、字典读取、正则及字符串消费者，未发现状态枚举、Git 参数或机器字段被误译。
- 中英文阶段索引兼容同时覆盖 `workflow_phase.py` 与 `session-start.py`；平台过滤及 hook 事件外层结构保持原有协议。
- 独立核对实施者对 `truncate_utf8` 的修复：旧实现会在完整多字节字符边界留下不完整首字节；当前实现保留完整字符，仅移除不完整末尾序列。逐字节边界、中文文件截断、角色隔离与原始事件键的 15 项上下文测试通过。
- 日志测试覆盖中英文及混合标题/计数、跨 Git 引用最大编号、旧日期与指纹、4/5/6 列表头、轮换、失败恢复、重复调用、提交证据及无关暂存内容隔离。
- 任务脚本的结构变化限于本轮所需的精确归档目标传递、中文文案插值及恢复提示。恢复命令使用 `shlex.join`，`git add` 和 `git commit` 都带同一组精确路径；作者的 4 项 CLI 测试包括实际执行带空格、单引号和 `$()` 字面的恢复命令。
- 仍保留的英文归属于 Git/CLI 参数、平台名、外部诊断读取、协议标记、历史格式兼容、原始提交证据及上述旧默认摘要指纹身份；不以“没有 ASCII 字符”作为验收方式。
- 规范建议已交主会话：说明默认占位文案参与指纹时须保持旧机器身份，并记录归档只提交精确目标和子任务元数据。

## 未修复发现与边界

本轮审查范围内未发现其他待修复问题。未运行产品构建、业务测试、在线接口或全局 CLI 写操作；没有作出产品接口对齐完成或生产行为已验证的结论。

## 实际验证

| 验证 | 结果 |
| --- | --- |
| `python3 -B -m unittest discover -s .trellis/scripts/tests -p 'test_archive_scope.py' -v` | 2 项通过，真实 Git 操作仅在临时仓库 |
| `python3 -B -m unittest discover -s .trellis/scripts/tests -p 'test_journal_localization.py' -v` | 最终修复后 12 项通过 |
| `python3 -B -m unittest discover -s .trellis/scripts/tests -p 'test_context_localization.py' -v` | 15 项通过 |
| 全部本地 Python `ast.parse` | 最终 35 个文件通过 |
| 全部 Python 文件末尾空白检查 | 0 处 |
| `git diff --check` | 通过 |
| Python 静态类型检查 | 本仓库未配置对应检查器；语法检查不冒充静态类型检查 |

主会话在默认摘要兼容修复前已运行过 31 项组合测试；最终源码新增两项日志用例后由主会话统一运行最终组合检查并记录结果。本记录不把修复前组合结果当作修复后结果。
