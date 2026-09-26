# 吧信息查询解耦验证记录

## 起点与范围

- 日期：2026-09-26，分支 `refactor/internal-architecture`。
- 子任务 A 的工作提交为 `9b1afc1`，79 项离线契约已通过；基线来自产品提交 `b9a86af79cf94db6d7f33abe8bd1e04e41f63678`。
- 只修改统一组合入口和 Forum/Admin/Thread/User 四个协议，新增两个 internal 窄接口及 ForumIdentityResolver。
- Governance 的两处在线夹具内部构造同步适配，只编译、不执行在线类。新增一个窄依赖结构契约。
- A 的 `public-api.txt`、快照提取器及 39 项查询行为预期保持不变。Session、Transport、Api 请求族系、DTO、ForumInfoCache 实现与依赖版本均未改变。

## 实施阶段验证

- 使用 `source /tmp/aiotieba-task-env.sh` 提供 SDK `10.0.201`。
- 锁定还原通过；完整 Release 构建通过，0 错误、55 个已有警告（CS1591 47 项、CS1573 2 项、CS9107 6 项），与原始构建的警告集合一致。
- A 的七类白名单加 `ForumLookupArchitectureContractTests`：80/80 通过、0 跳过。
- 实施结果：`/tmp/aiotieba-lookup-decoupling-results/lookup-decoupling-implementation.trx`。
- `git diff --check` 通过；文档/证据脚本仍因缺少既有 `local-verification.manifest.json` 退出 1，未降低门槛。

## 精确语义核对

- 普通 ID 请求、详情请求及管理 ID 请求仍分别使用 `GetFidAsync`、`GetDetailAsync`、`${operationName}ResolveFid`。
- Admin 命中短路；未命中进行业务认证/TBS 准备后，直接发送原查询，不重新查缓存。
- GetFname 保留按响应 ID 的条件回填与按请求 ID 的无条件第二次回填；空/空白判定保持。
- Thread 的 15 处 ID、2 处名称和 1 处分类调用只改依赖目标，原顺序不变；User 的 1 处 ID 调用不改顺序。
- 每客户端保留一份原缓存和一份身份服务，查询服务不依赖业务协议；分类实现仍是同一个 ForumProtocol。

## 证据边界

最终全范围检查与适用 lint 结果由本任务 `check-result.md` 记录。未运行真实在线、未证明总体覆盖率；四个既有治理证据文件仍缺失，不能称全部治理检查通过。

## 独立最终检查

- Trellis 检查代理再次完整还原/构建并执行 8 类白名单：80/80 通过、0 跳过；结果 `/tmp/aiotieba-decoupling-check-results/forum-lookup-decoupling-check.trx`。
- 本轮 8 个产品 C# 文件及 2 个测试 C# 文件的范围内风格检查通过；tracked/untracked 空白检查通过。
- 产品、Online、Governance 输出目录的产品 DLL SHA-256 均为 `b0b4362d1af8c7ca66e8771b0791f9639bcf231c18ff7e28d5053e11a5e790d9`，无旧副本混入。此值用于识别本次测试产物，不要求后续构建字节完全相同。
- 无需修改阶段 B 产品实现；公开快照和阶段 A 行为断言保持不变。
