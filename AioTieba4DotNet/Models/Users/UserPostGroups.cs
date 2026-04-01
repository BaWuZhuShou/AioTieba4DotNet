using AioTieba4DotNet.Models.Shared;

namespace AioTieba4DotNet.Models.Users;

/// <summary>
///     用户历史回复分组列表。
/// </summary>
public class UserPostGroups : Containers<UserPosts>
{
    /// <summary>
    ///     构造函数
    /// </summary>
    /// <param name="objs">用户历史回复列表集合</param>
    public UserPostGroups(List<UserPosts> objs) : base(objs)
    {
    }

    /// <summary>
    ///     构造函数
    /// </summary>
    /// <param name="collection">用户历史回复列表集合</param>
    public UserPostGroups(IEnumerable<UserPosts>? collection) : base(collection)
    {
    }
}
