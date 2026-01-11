namespace AioTieba4DotNet.Attributes;

/// <summary>
///     标记该 API 对应的 Python 原版 aiotieba 接口路径
/// </summary>
/// <param name="path">Python 接口路径 (例如 "aiotieba.api.get_tbs")</param>
[AttributeUsage(AttributeTargets.Class)]
public class PythonApiAttribute(string path) : Attribute
{
    /// <summary>
    ///     原版 Python aiotieba 接口路径
    /// </summary>
    public string Path { get; } = path;
}
