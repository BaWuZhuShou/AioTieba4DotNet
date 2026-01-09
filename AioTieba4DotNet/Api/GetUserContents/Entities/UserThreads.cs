using AioTieba4DotNet.Api.Entities;
using AioTieba4DotNet.Entities;

namespace AioTieba4DotNet.Api.GetUserContents.Entities;

public class UserThreads : Containers<UserThread>
{
    public UserThreads(List<UserThread> objs) : base(objs)
    {
    }

    public UserThreads(IEnumerable<UserThread>? collection) : base(collection)
    {
    }

    public static UserThreads FromTbData(UserPostResIdl.Types.DataRes dataRes)
    {
        List<UserThread> objs = [];
        objs.AddRange(dataRes.PostList.Select(UserThread.FromTbData));
        if (objs.Count == 0) return new UserThreads(objs);
        
        var user = UserInfo.FromTbData(dataRes.PostList[0]);
        foreach (var uthread in objs)
        {
            uthread.User = user;
        }

        return new UserThreads(objs);
    }
}
