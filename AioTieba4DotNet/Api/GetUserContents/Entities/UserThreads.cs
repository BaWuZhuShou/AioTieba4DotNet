using AioTieba4DotNet.Api.Entities;
using AioTieba4DotNet.Entities;

namespace AioTieba4DotNet.Api.GetUserContents.Entities;

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

    /// <summary>
    ///     从贴吧原始数据转换
    /// </summary>
    /// <param name="dataRes">Protobuf 响应数据</param>
    /// <returns>用户历史发布主题帖列表实体</returns>
    internal static UserThreads FromTbData(UserPostResIdl.Types.DataRes dataRes)
    {
        List<UserThread> objs = [];
        objs.AddRange(dataRes.PostList.Select(UserThread.FromTbData));
        if (objs.Count == 0) return new UserThreads(objs);

        var user = UserInfo.FromTbData(dataRes.PostList[0]);
        foreach (var uthread in objs) uthread.User = user;

        return new UserThreads(objs);
    }
}
