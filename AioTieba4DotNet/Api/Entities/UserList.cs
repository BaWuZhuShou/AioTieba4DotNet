using AioTieba4DotNet.Api.GetThreads.Entities;
using AioTieba4DotNet.Entities;

namespace AioTieba4DotNet.Api.Entities;

public class UserList : Containers<UserInfo>
{
    public PageT Page { get; set; } = new();

    public UserList(List<UserInfo> objs, PageT page) : base(objs)
    {
        Page = page;
    }
}
