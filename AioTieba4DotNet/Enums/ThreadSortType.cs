namespace AioTieba4DotNet.Enums;

/// <summary>
/// 主题帖排序
/// Note:
///     对于有热门分区的贴吧 0热门排序(HOT) 1按发布时间(CREATE) 2关注的人(FOLLOW) 34热门排序(HOT) >=5是按回复时间(REPLY)\n
///     对于无热门分区的贴吧 0按回复时间(REPLY) 1按发布时间(CREATE) 2关注的人(FOLLOW) >=3按回复时间(REPLY)
/// </summary>
public enum ThreadSortType
{
    /// <summary>
    /// 回复时间
    /// </summary>
    Reply = 5,

    /// <summary>
    /// 发布时间
    /// </summary>
    Create = 1,

    /// <summary>
    /// 热门排序
    /// </summary>
    Hot = 3,

    /// <summary>
    /// 关注的人
    /// </summary>
    Follow = 2
}
