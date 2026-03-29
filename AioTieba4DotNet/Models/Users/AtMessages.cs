using AioTieba4DotNet.Models.Shared;
using AioTieba4DotNet.Models.Threads;

namespace AioTieba4DotNet.Models.Users;

public class AtMessages : Containers<AtMessage>
{
    public AtMessages(List<AtMessage> objs, PageT page) : base(objs)
    {
        Page = page;
    }

    public PageT Page { get; init; }
}
