# 研究：请求与映射层的内部结构

- 研究问题：在保持 v3 公开行为稳定的前提下，`Api/`、`Internal/Mapping/` 与 `ProtoGenerator/` 中有哪些具备源码证据、适合分阶段实施的维护性改进？
- 范围：内部；仅检查手写请求、映射及生成器代码。未研究 Clients、Protocols、Transport、Session 的实现，也未审计测试治理。
- 日期：2026-09-26
- 活动任务：`.trellis/tasks/09-26-internal-architecture-evolution`

## 研究发现

### 判断与优先级

建议保留现有分层，优先处理两个局部问题，不引入请求框架、映射框架、插件注册表或新依赖。

跨研究收敛决定（主会话 2026-09-26 指示）：本轮实施建议优先公开兼容基线和吧信息查询依赖收敛，下面两项均记录为后续候选，不加入本轮实施。表中的 P1 / P2 仅表示本研究范围内的相对优先级，“适合首批”是对独立后续重构批次的评估，并非本轮已批准范围。

| 优先级 | 问题 | 维护收益 | 是否适合首批 |
| --- | --- | --- | --- |
| P1 | `ContentMapper` 的片段分类、列表聚合及收尾重复，同时夹杂入口特有语义 | 将同一片段对象加入分类列表和总序列、维护最终索引的规则集中；使入口差异更易看见 | 适合，但须先冻结各入口行为；中等兼容风险，应独立迁移与回退 |
| P2 | 三个主题读取请求重复实现相同 WS 空载荷与 protobuf 解析失败转换 | 将这三个请求的相同错误边界集中，减少后续修复遗漏 | 适合独立的小步试点；收益较窄，不能扩大到其他请求 |

不建议首批重做 ProtoGenerator，也不建议统一所有请求的 `CommonReq`、错误字段、URL 或分页设置。它们的差异中包含已有线协议与错误行为。

### 找到的文件

| 文件 | 作用 |
| --- | --- |
| `AioTieba4DotNet/Internal/Mapping/Models/Contents/ContentMapper.cs` | 五个内容入口，负责分派片段、聚合分类与总序列、追加媒体并赋索引 |
| `AioTieba4DotNet/Internal/Mapping/Models/Contents/FragVoiceMapper.cs` | 多种来源的语音字段转换；其中 Abstract 重载与 ContentMapper 内联转换不等价 |
| `AioTieba4DotNet/Internal/Mapping/Models/Contents/FragVideoMapper.cs` | `VideoInfo` 与 `PbContent` 的视频转换 |
| `AioTieba4DotNet/Internal/Mapping/Models/Threads/ThreadMapper.cs` | 普通主题和转发来源内容入口的直接上层调用者 |
| `AioTieba4DotNet/Internal/Mapping/Models/Threads/ShareThreadMapper.cs` | 转发来源内容映射调用者 |
| `AioTieba4DotNet/Internal/Mapping/Models/Threads/PostMapper.cs`、`CommentMapper.cs` | `IEnumerable<PbContent>` 内容入口的调用者 |
| `AioTieba4DotNet/Internal/Mapping/Models/Users/UserPostMapper.cs`、`UserThreadMapper.cs` | 用户内容入口的调用者 |
| `AioTieba4DotNet/Api/GetThreads/GetThreads.cs` | 主题列表的请求打包、族系解析与 WS 错误边界 |
| `AioTieba4DotNet/Api/GetThreadPosts/GetThreadPosts.cs` | 楼层读取的请求打包、族系解析与 WS 错误边界 |
| `AioTieba4DotNet/Api/GetComments/GetComments.cs` | 楼中楼读取的请求打包、族系解析与 WS 错误边界 |
| `AioTieba4DotNet/Api/JsonApiBase.cs`、`ApiResponseValidator.cs` | 已有 JSON 校验与业务错误转换复用点 |
| `AioTieba4DotNet/Api/GetUserContents/GetPosts.cs`、`GetUserThreads.cs` | 相同上游请求族系但不同输入与映射语义的示例 |
| `AioTieba4DotNet/Internal/Mapping/Models/Shared/UserProtoMapping.cs` | 已有的小粒度用户转换复用点 |
| `AioTieba4DotNet/Internal/Mapping/Models/Admins/AdminHtmlParsing.cs` | 已有的吧务 HTML 解析复用点 |
| `ProtoGenerator/ProtoGenerationPlanner.cs`、`ProtoGenerationTarget.cs` | 生成目标发现、排序与目标描述 |
| `ProtoGenerator/ProtocExecutor.cs`、`ProtoGeneratorApp.cs` | 外部进程执行边界和 CLI 编排 |
| `ProtoGenerator/ProtoGenerator.csproj` | 随包 protoc 配置；保留既有平台范围 |

### 痛点一：内容聚合规则重复，入口差异不够显式

#### 源码证据

`ContentMapper.cs` 当前为 724 行，不能仅凭行数认定问题；实际问题是相同规则由多个分支分别维护：

- `ContentMapper.cs:10`、`:210`、`:388`、`:564` 分别建立同样的分类列表、总片段列表及音视频槽位。
- `ContentMapper.cs:28`、`:228`、`:406` 在每次映射时建立数组键的处理表，并用 `Where(...Contains(type)).Select(...).FirstOrDefault()` 查找并执行处理器（`:114`、`:305`、`:463`）。`IEnumerable<PbContent>` 入口则在 `:588` 使用 switch。相同文本、链接、图片、表情等分类规则需要在不同机制中同步。
- 分类列表与 `Frags` 的双重加入重复出现，例如文本 `:34`、`:234`、`:412`、`:604`。这些位置都需要保留同一个实例，而不仅是相同字段值，因为最终在 `:719` 修改 `IFrag.Index`。
- `Content` 对象组装重复于 `:176`、`:368`、`:484`、`:705`；最终索引重复调用于 `:173`、`:365`、`:481`、`:538`、`:702`。
- 波及路径明确：`ThreadMapper.cs:21` / `ShareThreadMapper.cs:12`、`PostMapper.cs:25` / `CommentMapper.cs:18`、`UserPostMapper.cs:10` / `UserThreadMapper.cs:12` 都依赖它，跨主题与用户内容读取。

#### 必须保留的入口差异

以下是当前源码行为，不等于已独立核实的上游预期。首批重构应保留它们，任何纠错另行决策。

| 入口 | 当前差异与证据 |
| --- | --- |
| `FromTbData(ThreadInfo.Types.OriginThreadInfo)` | `Content` 中 type 10 转成语音（`:86`）；type 5 / 34 忽略（`:129`）；末尾依次追加 `VideoInfo` 和 `VoiceInfo[0]`（`:155`、`:164`）。内嵌语音仍留在 `Frags`，独立语音可覆盖最终 `Voice` 槽位 |
| `FromTbData(ThreadInfo?)` | null 返回非 null 的空语音、空视频对象（`:194`）；非 null 对象中 type 10 / 5 / 34 忽略（`:319`）；使用末尾媒体信息（`:347`、`:356`） |
| `FromTbData(PostInfoContent)` | type 2 与 11 都映射为表情（`:431`），不同于普通 `PbContent` 中 type 2 为 at；type 10 使用 `double.TryParse` 再除以 1000、强转为 int（`:442`），未知类型输出到 Console（`:477`） |
| `FromTbData(PostInfoList)` | 先映射 FirstPostContent，再按序追加 `Media.Type != 5` 的图片、宽度大于零的视频、第一条语音；最后重新赋所有索引（`:499`） |
| `FromTbData(IEnumerable<PbContent>?)` | null 返回空语音、空视频对象（`:548`），空枚举的音视频槽位为 null（`:578`）；type 10 / 5 分别映射语音／视频（`:679`、`:689`）；无 default 分支，因此未知类型不输出 |

另有一个容易被“复用”误改的例子：`FragVoiceMapper.cs:18` 的 Abstract 重载使用 `int.TryParse`，而 `ContentMapper.cs:445` 使用 `double.TryParse`。例如 `DuringTime="1500.5"` 在允许该小数格式的区域性下，两者会分别得到 0 秒和 1 秒。因此不能未经行为冻结就用已有重载替换。将解析统一为 invariant culture 同样是行为变更，不应夹带。

#### 建议改进边界

1. 保留五个入口及全部现有 fragment mapper，先提取私有聚合对象或等效私有辅助方法，集中分类列表、`Frags`、最终音视频槽位和索引收尾。
2. 同一语义的 `PbContent` 基础类型分派可以共用；type 5 / 10 / 34、未知类型、独立媒体追加与 null 输入仍由明确的入口规则控制。避免含糊的单一“通用模式”吞掉差异。
3. 聚合对象持有每次调用独立的可变状态；不能用静态共享列表。进入分类列表与 `Frags` 的必须是同一个 fragment 实例。
4. 保留独立媒体的追加顺序、重复语音保留规则、最后一次音视频槽位赋值和全部索引。不要顺手去重媒体、改变空值或替换 Console 输出。
5. 保留 .proto、生成类型、公开 DTO 与其他 mapper 的签名和语义；无需代码生成或新依赖。

建议的离线行为证据范围：每个入口覆盖支持类型、未知类型、null/empty 差异、混合顺序、多个音视频、分类与总列表引用一致、最终 Index、Abstract 语音的整数／小数／无效字符串。具体测试落点与执行命令交由测试治理研究和主会话确定；本研究未运行或声称现有测试覆盖这些情况。

收益判断是降低多处同步修改成本；未做性能测量，不把消除临时字典或闭包直接宣称为已证实性能提升。

### 痛点二：三个主题读取请求重复维护同一 WS 载荷边界

#### 源码证据

下列两段结构在三个请求中相同，差异是操作名称、返回类型及族系 ParseBody：

- `GetThreads.cs:48` / `:58`：Payload 缺失或 Data 为空时抛 `TiebaWebSocketUnavailableException`，只捕获 `InvalidProtocolBufferException` 并保留内部异常。
- `GetThreadPosts.cs:59` / `:69`：相同结构，消息使用 `get-posts`。
- `GetComments.cs:49` / `:59`：相同结构，消息使用 `get-comments`。

例如 `GetComments.cs:103` 在发送后调用 `ParseWsBody(ExtractWsBody(response))`；HTTP 路径 `:87` 直接调用 `ParseBody`。维护空载荷及非法 protobuf 分类时，需要同步三套相同逻辑。

#### 建议改进边界

- 可在 `Api/` 内建立一个小型 internal 静态 helper，接受响应、现有族系解析委托和操作标识，仅处理这两个已有 WS 规则；三个类保留请求打包及 `ParseBody`。
- 精确保留 `get-threads` / `get-posts` / `get-comments` 的消息文本、内部异常、空载荷判定及 try/catch 范围；HTTP 继续直接解析，不经过该 helper。
- 不捕获 `TieBaServerException`、取消、一般 `Exception` 或普通映射异常；不把失败转为空 DTO，也不在 helper 中执行 HTTP 回退。helper 只生成当前已有信号，不能新增回退决策。
- 不推广到 `AddPost`、用户内容等其他 WS API。它们目前没有相同转换，例如 `AddPost.cs:148` 与 `GetUserContents/GetPosts.cs:79` 直接读取响应；扩展会改变既有失败行为。
- 族系错误对象缺省行为保留：`GetComments.cs:44` 容忍缺失 Error；`GetThreads.cs:41`、`GetThreadPosts.cs:54` 直接取 Error。不能借提取 helper 统一这部分。

验证应至少比较三族系成功值、空 Payload、空 Data、非法 protobuf、非零服务端错误及 HTTP 解析行为；取消应保持原样传递。错误分类与回退的调用链验证由拥有相关层的研究／实施代理补足。

该项改变范围小、可单独回退，但收益也小于内容映射收敛。如果任务希望首批只交付一个主题，优先内容映射，暂缓此项；如果需要先练习完整兼容性迁移流程，可把这三个端点作为独立试点。不能仅以减少几十行代码证明宏观架构更先进。

### 现有合理设计，应继续保留

- 请求与映射已有分离。`GetThreads.cs:19` 负责 pack，`:38` 先解析与检查服务端错误，再调用 `ThreadsMapper`；`ThreadsMapper.cs:10` 负责用户关联与吧标识附加。无需将业务 DTO 绑定到生成类型。
- `JsonApiBase.cs:14` 委托 `ApiResponseValidator.cs:13`；后者保留可配置错误字段与缺失错误码视为零。已有复用点足够简单，不需要再加通用响应封装。
- `UserProtoMapping.cs:8`、`:15`、`:22` 已集中头像规范化和 invariant 数字解析；`:29` 开始集中用户图标与标志转换。多个用户 mapper 已直接复用这一模式，说明小型明确 helper 符合现有结构。
- `AdminHtmlParsing` 已被三类吧务 mapper 复用。排行榜 mapper 表面有相似 HTML 处理，但分页结构和行选择规则不同；本次未把替换为统一 HTML 框架列为首批问题。
- `GetUserContents/GetPosts.cs:20` 与 `GetUserThreads.cs:20` 虽共用命令和 schema，前者有独立 version、rn，后者有 `IsThread`、`IsViewCard`。`docs/related/parity.md:329` / `:330` 还明确二者公开与传输语义不同，不能因同路径就合并操作概念。
- ProtoGenerator 已分开目标发现（`ProtoGenerationPlanner.cs:18`）、排序（`:32`）、执行参数（`ProtocExecutor.cs:39`）、执行接口（`:6`）、CLI 编排（`ProtoGeneratorApp.cs:16`）。参数使用 `ArgumentList`（`ProtocExecutor.cs:57`），输出内部类型的选项集中在 `:23`。没有证据支持为当前规模引入 DI 容器、插件式生成流水线或增量构建框架。
- 生成器保留随包工具优先、PATH 兜底（`ProtoGeneratorApp.cs:40`），按目标报告结果并返回非零失败码（`:73`、`:111`）。`ProtoGenerator.csproj:18` 至 `:20` 指向各系统 x64 工具；平台扩展不属于本轮内部结构收敛。

### 相关规范

- `.trellis/workflow.md`：研究落盘；当前处于规划，代码实施须等主会话收敛和评审。
- `.trellis/spec/backend/directory-structure.md`：API 与生成类型内部可见，公开 DTO 协议无关；保留公开命名空间、默认值和异常。
- `.trellis/spec/backend/mapping-and-codegen.md`：转换集中在 Internal/Mapping；保留缺省值，.proto 为源，不手改生成类。
- `.trellis/spec/backend/transport-guidelines.md`：请求族系负责协议细节；回退和 HTTP 所有权使用既有边界。
- `.trellis/spec/backend/error-handling.md`：保留 `TieBaServerException` 与取消语义；仅特定 WS 不可用触发回退。
- `.trellis/spec/project-positioning.md`：以固定上游为语义参照，内部重构不等于补齐上游行为。
- `.trellis/spec/guides/code-reuse-thinking-guide.md`、`cross-layer-thinking-guide.md`：先搜索已有能力，提取明确所有者，防止抽象泄漏和重复解释载荷。
- `AioTieba4DotNet/AGENTS.md`、`ProtoGenerator/AGENTS.md`、`.junie/guidelines.md`：内部 API 边界、生成规则、v3 / net10.0 支持面。

### 外部参考与版本

- 本次未访问外部文档或最新上游源码，判断来自当前仓库代码。
- 本地 `.trellis/spec/project-positioning.md` 记录的比较基线是 `lumina37/aiotieba` `v4.6.4`，提交 `04f8e431f87507a6228b42061c70d298b34317ff`。这是仓库已声明的基线；本次未独立验证上游对应函数。
- `Directory.Packages.props:7` / `:8` 固定 `Google.Protobuf`、`Google.Protobuf.Tools` 为 `3.33.3`；本次不升级依赖，也不依赖新版库能力来论证建议。

## 限制与未找到的内容

- 只读研究，没有修改代码、规范、任务元数据，没有执行 Git 操作、生成器、构建、测试或在线请求，也未读取凭据。
- 未审计 Clients、Protocols、Transport、Session 的源码与资源所有权；其中与 WS 回退关联的最终行为须由对应研究补足。
- 未审计测试数量、覆盖率和现有基线是否足以保障重构；本文提出的是应观察的行为清单，不是测试通过证明。
- 未核验固定上游源码，因此不将 ContentMapper 的入口差异判定为功能缺陷，也不授权顺手纠正。
- `ContentMapper` 所述行数仅作定位，真正证据是重复分类/聚合和入口语义差异；未测吞吐、分配或实际维护耗时。
- 没有发现要求更换 JSON/protobuf 栈、引入通用映射框架或整体重写生成器的源码证据。
