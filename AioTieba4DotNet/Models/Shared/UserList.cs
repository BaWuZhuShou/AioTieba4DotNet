using AioTieba4DotNet.Models.Threads;

namespace AioTieba4DotNet.Models.Shared;

/// <summary>
///     用户列表实体
/// </summary>
public class UserList : Containers<UserInfo>
{
    /// <summary>
    ///     构造函数
    /// </summary>
    /// <param name="objs">用户列表</param>
    /// <param name="page">页码信息</param>
    public UserList(List<UserInfo> objs, PageT page) : base(objs)
    {
        Page = page;
    }

    /// <summary>
    ///     页码信息
    /// </summary>
    public PageT Page { get; set; } = new();
}
