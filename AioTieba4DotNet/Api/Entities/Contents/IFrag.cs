namespace AioTieba4DotNet.Api.Entities.Contents;

/// <summary>
///     碎片基类
/// </summary>
public interface IFrag
{
    /// <summary>
    ///     文本内容
    /// </summary>
    string Text { get; }

    /// <summary>
    ///     获取碎片类型
    /// </summary>
    /// <returns></returns>
    string GetFragType();

    /// <summary>
    ///     转换为字典用于序列化
    /// </summary>
    /// <returns></returns>
    Dictionary<string, object> ToDict();
}
