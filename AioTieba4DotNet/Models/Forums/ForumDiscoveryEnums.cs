namespace AioTieba4DotNet.Models.Forums;

/// <summary>
///     贴吧搜索排序方式
/// </summary>
public enum ForumSearchType
{
    /// <summary>
    ///     搜索全部
    /// </summary>
    All = 0,

    /// <summary>
    ///     按时间排序
    /// </summary>
    Time = 1,

    /// <summary>
    ///     按相关性排序
    /// </summary>
    Relation = 2
}

/// <summary>
///     吧签到排行榜类型
/// </summary>
public enum ForumRankType
{
    /// <summary>
    ///     今日
    /// </summary>
    Today = 0,

    /// <summary>
    ///     昨日
    /// </summary>
    Yesterday = 1,

    /// <summary>
    ///     本周
    /// </summary>
    Weekly = 2,

    /// <summary>
    ///     本月
    /// </summary>
    Monthly = 3
}

/// <summary>
///     图片尺寸选项
/// </summary>
public enum ForumImageSize
{
    /// <summary>
    ///     小图
    /// </summary>
    Small,

    /// <summary>
    ///     中图
    /// </summary>
    Medium,

    /// <summary>
    ///     原图
    /// </summary>
    Large
}

/// <summary>
///     图片格式
/// </summary>
public enum ForumImageFormat
{
    /// <summary>
    ///     未知格式
    /// </summary>
    Unknown,

    /// <summary>
    ///     JPEG
    /// </summary>
    Jpeg,

    /// <summary>
    ///     PNG
    /// </summary>
    Png,

    /// <summary>
    ///     BMP
    /// </summary>
    Bmp
}
