using AioTieba4DotNet.Models.Shared;

namespace AioTieba4DotNet.Models.Users;

/// <summary>
///     用户历史回复信息列表的列表
/// </summary>
public class UserPostss : Containers<UserPosts>
{
    /// <summary>
    ///     构造函数
    /// </summary>
    /// <param name="objs">用户历史回复列表集合</param>
    public UserPostss(List<UserPosts> objs) : base(objs)
    {
    }

    /// <summary>
    ///     构造函数
    /// </summary>
    /// <param name="collection">用户历史回复列表集合</param>
    public UserPostss(IEnumerable<UserPosts>? collection) : base(collection)
    {
    }
}
