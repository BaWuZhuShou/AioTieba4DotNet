# 收敛吧信息查询职责与协议依赖

## 目标与依赖

在 [子任务 A](../09-26-architecture-compatibility-baseline/prd.md) 的基线通过并完成工作提交后，收敛吧信息查询和缓存加载，缩小内部协议依赖。覆盖父任务 R1–R5；不得与 A 的基线建立并行修改产品。

## 问题依据

- `ForumProtocol.cs:39–55` 与 `AdminProtocol.cs:314–333` 重复维护 ID 请求加载。
- Thread 实际使用论坛协议的 3 类查询，User 只使用 ID 查询，却都依赖完整 `IForumProtocol`。
- GetFname 通过 GetDetail 回填缓存，拆分时若反向依赖原 ForumProtocol 会形成循环。精确行为和所有权见父任务 [协议研究](../09-26-internal-architecture-evolution/research/protocol-dependencies.md)。

## 需求

- B-R1：吧身份查询和相关缓存写入有单一内部所有者；沿用每客户端既有缓存，不增加共享全局状态。
- B-R2：Thread/User 只依赖所需查询能力，不依赖整个论坛协议；保留 Thread 的分类查询需要。
- B-R3：公开 API、操作名称、认证前置、请求次数/顺序、结果、异常、取消和缓存边界与 A 完全一致。
- B-R4：直接构造、DI 和工厂仍共用原组合根，资源所有权和生命周期保持。

## 验收标准

- [ ] B-AC1：A 的公开快照不变，A 的行为表征在新实现上通过；不得通过改预期值获得通过。
- [ ] B-AC2：Thread/User 的字段和构造依赖中没有 `IForumProtocol`，只有明确的窄查询能力；对象图无循环。
- [ ] B-AC3：Forum/Admin 的 ID 网络请求组装只保留一处；各入口不同的认证/缓存策略仍可直接阅读，不隐藏于可选委托或策略开关。
- [ ] B-AC4：同客户端共享解析/缓存、不同客户端隔离；GetFname 的两次回填、分类查询顺序及管理操作认证仍被测试断言。
- [ ] B-AC5：锁定还原、Release 构建、离线白名单及结构检查通过；整组产品变更有独立工作提交，可整体撤销。

## 范围外

不修改基础校验规则、Cache 的键/TTL/容量/释放策略、请求合并、Session/Transport、API 请求族系、DTO、mapper 或生成代码；不把观测到的旧行为差异一并修正。
