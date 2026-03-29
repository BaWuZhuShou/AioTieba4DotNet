using AioTieba4DotNet.Models.Shared;

namespace AioTieba4DotNet.Models.Users;

/// <summary>
///     用户历史发布主题帖列表
/// </summary>
public class UserThreads : Containers<UserThread>
{
    /// <summary>
    ///     构造函数
    /// </summary>
    /// <param name="objs">用户历史发布主题帖列表</param>
    public UserThreads(List<UserThread> objs) : base(objs)
    {
    }

    /// <summary>
    ///     构造函数
    /// </summary>
    /// <param name="collection">用户历史发布主题帖列表</param>
    public UserThreads(IEnumerable<UserThread>? collection) : base(collection)
    {
    }
}
