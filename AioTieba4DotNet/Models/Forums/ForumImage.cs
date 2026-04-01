namespace AioTieba4DotNet.Models.Forums;

/// <summary>
///     图片原始字节结果
/// </summary>
public sealed class ForumImageBytes
{
    /// <summary>
    ///     原始字节数据
    /// </summary>
    public byte[] Data { get; init; } = [];

    /// <summary>
    ///     是否为空结果
    /// </summary>
    public bool IsEmpty => Data.Length == 0;
}

/// <summary>
///     图片读取结果
/// </summary>
public sealed class ForumImage
{
    /// <summary>
    ///     原始字节数据
    /// </summary>
    public byte[] Data { get; init; } = [];

    /// <summary>
    ///     图片格式
    /// </summary>
    public ForumImageFormat Format { get; init; }

    /// <summary>
    ///     宽度
    /// </summary>
    public int Width { get; init; }

    /// <summary>
    ///     高度
    /// </summary>
    public int Height { get; init; }

    /// <summary>
    ///     是否为空结果
    /// </summary>
    public bool IsEmpty => Data.Length == 0 || Width <= 0 || Height <= 0;
}
