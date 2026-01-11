using AioTieba4DotNet.Api.GetThreads.Entities;
using AioTieba4DotNet.Entities;

namespace AioTieba4DotNet.Api.Entities;

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
