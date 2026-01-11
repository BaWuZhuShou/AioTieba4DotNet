using AioTieba4DotNet.Api.Entities;
using AioTieba4DotNet.Entities;

namespace AioTieba4DotNet.Api.GetUserContents.Entities;

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

    /// <summary>
    ///     从贴吧原始数据转换
    /// </summary>
    /// <param name="dataRes">Protobuf 响应数据</param>
    /// <returns>用户历史回复列表的列表实体</returns>
    public static UserPostss FromTbData(UserPostResIdl.Types.DataRes dataRes)
    {
        List<UserPosts> objs = [];
        objs.AddRange(dataRes.PostList.Select(UserPosts.FromTbData));
        if (objs.Count == 0) return new UserPostss(objs);
        var postInfoList = dataRes.PostList[0];
        var user = UserInfo.FromTbData(postInfoList);
        foreach (var userPost in objs.SelectMany(obj => obj)) userPost.User = user;

        return new UserPostss(objs);
    }
}
