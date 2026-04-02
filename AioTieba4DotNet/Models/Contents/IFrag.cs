using System.Diagnostics.CodeAnalysis;

namespace AioTieba4DotNet.Models.Contents;

/// <summary>
///     碎片基类
/// </summary>
[SuppressMessage("Naming", "S101:Types should be named in PascalCase",
    Justification = "The public abstract frag base type keeps its historical name for compatibility with the existing consumer-facing model contract.")]
public abstract class IFrag
{
    /// <summary>
    ///     文本内容
    /// </summary>
    public virtual string Text { get; init; } = "";

    /// <summary>
    ///     索引
    /// </summary>
    public int Index { get; internal set; }

    /// <summary>
    ///     获取碎片类型
    /// </summary>
    /// <returns></returns>
    public abstract string GetFragType();

    /// <summary>
    ///     转换为字典用于序列化
    /// </summary>
    /// <returns></returns>
    public abstract Dictionary<string, object> ToDict();
}
