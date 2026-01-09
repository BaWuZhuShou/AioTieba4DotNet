using System.Text;
using AioTieba4DotNet.Api.Profile.GetUInfoProfile;

namespace AioTieba4DotNet.Api.GetThreads.Entities;

public class Threads
{
    public required PageT Page { get; set; }
    public required ForumT Forum { get; set; }
    public required List<Thread> Objs { get; set; }
    public required Dictionary<string, int> TabDictionary { get; set; }

    public bool HasMore
    {
        get => Page.HasMore;
    }

    public static Threads FromTbData(FrsPageResIdl.Types.DataRes dataRes)
    {
        var forum = ForumT.FromTbData(dataRes);
        var threads = dataRes.ThreadList.Select(Thread.FromTbData).ToList();
        var users = dataRes.UserList.ToDictionary(u => u.Id, UserInfoT.FromTbData);
        foreach (var thread in threads)
        {
            thread.Fname = forum.Fname;
            thread.Fid = forum.Fid;
            thread.User = users.GetValueOrDefault(thread.AuthorId) ?? new UserInfoT();
        }

        return new Threads()
        {
            Page = PageT.FromTbData(dataRes.Page),
            Forum = forum,
            TabDictionary = dataRes.NavTabInfo.Tab.ToDictionary(p => p.TabName, p => p.TabId),
            Objs = threads,
        };
    }

    public override string ToString()
    {
        var sb = new StringBuilder();
        sb.AppendLine(
            $"{nameof(Page)}: {Page}, {nameof(Forum)}: {Forum}, {nameof(TabDictionary)}: {TabDictionary}, {nameof(HasMore)}: {HasMore}");
        sb.AppendLine($"{nameof(Objs)}:");
        foreach (var obj in Objs)
        {
            sb.AppendLine(obj.ToString());
        }

        return sb.ToString();
    }
}
