## 变更概述
- 做了什么：
- 为什么要做：

## 关联事项
- Closes #
- Related #

## SemVer 影响
请只勾选一项；如果不确定，请勾选 `Unsure` 并写明疑点。

- [ ] MAJOR
- [ ] MINOR
- [ ] PATCH
- [ ] Unsure

SemVer 规则来源：[`../.junie/guidelines.md`](../.junie/guidelines.md) 中的 **11.5 MAJOR / MINOR / PATCH 判定规则** 与 **11.7 PR / Release 快速检查清单**。

判定依据：
- 

> 提醒：若同时存在新增能力和破坏性调整，请按最高影响级别判定；若公开签名未变，但文档承诺、默认行为、异常时机、鉴权检查或请求模式回退发生变化，仍按契约变更处理。

## 兼容性 / 迁移
- [ ] 无破坏性变更
- [ ] 存在破坏性变更
- [ ] 需要在 PR 描述或 Release Notes 中写迁移说明

迁移 / 兼容说明：
- 

## 文档 / 契约
- [ ] 无用户可见契约变化
- [ ] README 已更新
- [ ] docs/* 已更新
- [ ] XML docs 已更新
- [ ] 用户可见行为已变化，但文档仍需补充

## Release Notes 分类
请勾选最匹配的一项；如需跳过 changelog，请明确写出。

- [ ] `feat` / `feature` / `enhancement`
- [ ] `fix` / `bug`
- [ ] `refactor`
- [ ] `perf`
- [ ] `docs` / `documentation`
- [ ] `test` / `tests`
- [ ] `chore` / `dependencies`
- [ ] `build` / `ci`
- [ ] `skip-changelog`

补充说明：
- 

> 上述分类应与 `.github/release.yml` 使用的 PR label 保持一致，便于后续自动生成 release notes。

## 验证
- [ ] 已执行相关 tests/checks
- [ ] 不适用

已执行的验证：
- 
