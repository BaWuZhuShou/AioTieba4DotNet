using AioTieba4DotNet.Models.Shared;
using AioTieba4DotNet.Models.Threads;

namespace AioTieba4DotNet.Models.Users;

/// <summary>
///     回复消息列表
/// </summary>
public class ReplyMessages : Containers<ReplyMessage>
{
    /// <summary>
    ///     初始化回复消息列表
    /// </summary>
    /// <param name="objs">消息集合</param>
    /// <param name="page">分页信息</param>
    public ReplyMessages(List<ReplyMessage> objs, PageT page) : base(objs)
    {
        Page = page;
    }

    /// <summary>
    ///     分页信息
    /// </summary>
    public PageT Page { get; init; }
}
