using AioTieba4DotNet.Models.Shared;
using AioTieba4DotNet.Models.Threads;

namespace AioTieba4DotNet.Models.Users;

/// <summary>
///     @ 消息列表
/// </summary>
public class AtMessages : Containers<AtMessage>
{
    /// <summary>
    ///     初始化 @ 消息列表
    /// </summary>
    /// <param name="objs">消息集合</param>
    /// <param name="page">分页信息</param>
    public AtMessages(List<AtMessage> objs, PageT page) : base(objs)
    {
        Page = page;
    }

    /// <summary>
    ///     分页信息
    /// </summary>
    public PageT Page { get; init; }
}
