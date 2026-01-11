using AioTieba4DotNet.Api.Entities;
using AioTieba4DotNet.Entities;

namespace AioTieba4DotNet.Api.GetUserContents.Entities;

/// <summary>
///     用户历史回复信息列表的列表
/// </summary>
public class UserPostss : Containers<UserPosts>
{
    public UserPostss(List<UserPosts> objs) : base(objs)
    {
    }

    public UserPostss(IEnumerable<UserPosts>? collection) : base(collection)
    {
    }

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
