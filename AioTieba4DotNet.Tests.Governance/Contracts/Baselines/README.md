# 公开 API 元数据基线

`public-api.txt` 是内部架构重构前的固定预期。`PublicApiBaselineContractTests` 只读取嵌入资源并比较，不写入、不自动接受差异。

## 首次来源

- 产品源码提交：`b9a86af79cf94db6d7f33abe8bd1e04e41f63678`。
- SDK：`10.0.201`，沿用仓库 `global.json`，未调整 pin。
- 构建：`dotnet restore AioTieba4DotNet.sln --locked-mode`，然后 `dotnet build AioTieba4DotNet.sln --configuration Release --no-restore`。
- 原始产品 DLL SHA-256：`cb074149d844004e1959936d9a9a51e804a59540d7d0aa2bc0d675dd903fd5eb`。首次生成读取已保留的原始 DLL 副本，生成时产品目录相对该提交无差异。
- 初始快照：163 个类型，137 个构造器，340 个普通方法／运算符，593 个属性／索引器，85 个字段／枚举项，共 1,318 行；原产品没有公开事件。格式化器另有事件测试夹具。

SDK、时间、机器路径、程序集文件散列不属于稳定文本。这里的 DLL 散列标识首次实际使用的产物，不要求以后构建的 DLL 字节完全一致。

## 编码维度与边界

逐行采用 ordinal 排序，按类型／构造器／方法／属性／事件／字段输出。类型使用包含命名空间、嵌套关系及 CLR 泛型 arity 的名称；泛型参数以 `!`（类型）或 `!!`（方法）区分。数组秩、指针、按引用类型不省略。字符串按 JSON 转义，数值采用 invariant culture。

- 枚举全部外部可见类型，包括外部派生类可访问的嵌套类型；不按源码目录排除实际公开类型。
- 记录声明的 public、protected、protected internal 成员。基类与接口关系保留，继承的成员由其原声明类型表达；外部依赖程序集的成员不在本库快照内。
- 类型记录种类、可见性、抽象／密封、泛型参数、方差、约束、基类、接口、嵌套归属和枚举底层类型。
- 成员记录类型、静态／实例、虚／抽象／final／newslot、调用约定、参数名称与顺序、in／out／按引用、可选标志、默认值、必要／可选自定义修饰符。属性／索引器与事件保留可访问的完整访问器签名；不可从外部访问的访问器只记录可见性。
- `NullabilityInfoContext` 记录返回值、参数、属性、事件和字段的读／写可空状态及递归泛型／数组元素状态。类型、泛型参数和泛型方法另保留可空性编码属性与上下文，以保护基类实参和约束，例如 `RoomList : Containers<Dictionary<string, object?>>`；普通成员的编译器可空性编码由上述结构化状态表达。
- 接口实现和泛型约束行上的 `NullableAttribute` 通过 .NET 内置 `MetadataReader` 读取，因为反射的 `Type.CustomAttributes` 不包含这两种关系行。文本以目标类型名关联十六进制属性 blob，不保存 metadata token；这些 blob 只含可空性标志及属性编码头。基类、接口、类型／方法泛型约束均有真实 `string?` / `string` 对照夹具。
- 保留常量和枚举数值；保留调用及序列化相关属性、构造参数、命名参数（包括参数／返回值／访问器上的属性）。不实例化属性或产品对象。
- 忽略编译器生成、异步／迭代状态机、调试展示和静态分析抑制属性；具体排除项集中在 `PublicApiSnapshot.IncludeAttribute`。其余属性默认保留，包括 extension、params、init／readonly／required、序列化及空值分析标记。

此格式不记录 IL／方法体、资源、程序集身份／版本／签名、类型转发、字段布局／偏移／封送、安全声明、私有实现、外部程序集实现以及源码注释。未知函数指针会显式失败，必须扩展格式与测试后再接受，不能静默省略。它不是通用 ApiCompat 或完整二进制兼容性检查；运行时语义、序列化执行结果、在线行为和覆盖率仍需分别验证。

类型／泛型方法的原始可空上下文和关系行编码是一层保守保护：编译器版本或内部成员变化可能让编译器选择不同的默认上下文，产生公开语义未变的差异。出现此情况应检查并改进编码归一化，使用原始产品 DLL 重生成后证明语义不变，不能因内部重构失败就接受新产品快照。此方案要求普通文件程序集，不支持动态或无文件程序集；不宣称完全消除了编译器编码敏感性。

## 显式生成候选文本

从仓库根目录执行，先提供 `global.json` 允许的 SDK 并完成上述还原／完整 Release 构建。维护者必须先确认产品源码来源；内部重构阶段不得用新产物重写原始预期。下面只生成 `/tmp` 候选文件，最后的 `diff` 非零代表有差异，需要人工评审。

```bash
snapshot_repo="$PWD"
snapshot_tool="$(mktemp -d /tmp/aiotieba-api-snapshot.XXXXXX)"
cat > "$snapshot_tool/Generator.csproj" <<EOF
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType><TargetFramework>net10.0</TargetFramework>
    <LangVersion>14.0</LangVersion><Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <Compile Include="$snapshot_repo/AioTieba4DotNet.Tests.Governance/Contracts/PublicApiSnapshot.cs" Link="PublicApiSnapshot.cs" />
  </ItemGroup>
</Project>
EOF
cat > "$snapshot_tool/Program.cs" <<'EOF'
using System.IO;
using System.Reflection;
using System.Runtime.Loader;
using AioTieba4DotNet.Tests.Governance.Contracts;
AssemblyLoadContext.Default.Resolving += (_, name) =>
{
    var path = Path.Combine(args[2], name.Name + ".dll");
    return File.Exists(path) ? AssemblyLoadContext.Default.LoadFromAssemblyPath(path) : null;
};
File.WriteAllText(args[1], PublicApiSnapshot.Create(Assembly.LoadFrom(args[0])));
EOF
dotnet run --project "$snapshot_tool/Generator.csproj" -- \
  "$snapshot_repo/AioTieba4DotNet/bin/Release/net10.0/AioTieba4DotNet.dll" \
  "$snapshot_tool/public-api.candidate.txt" \
  "$snapshot_repo/AioTieba4DotNet.Tests.Governance/bin/Release/net10.0"
diff -u AioTieba4DotNet.Tests.Governance/Contracts/Baselines/public-api.txt \
  "$snapshot_tool/public-api.candidate.txt"
```

首次生成使用上述逻辑读取保存的原 DLL。候选生成工具留在临时目录，产品和测试都没有接受开关。以后经明确契约变更评审才可人工替换文本，同时更新来源说明；普通内部重构应保持零差异。
