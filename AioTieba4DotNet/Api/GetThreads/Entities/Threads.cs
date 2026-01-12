using System.Text;

namespace AioTieba4DotNet.Api.GetThreads.Entities;

/// <summary>
///     主题帖列表
/// </summary>
public class Threads
{
    /// <summary>
    ///     页码信息
    /// </summary>
    public required PageT Page { get; set; }

    /// <summary>
    ///     吧信息
    /// </summary>
    public required ForumT Forum { get; set; }

    /// <summary>
    ///     主题帖列表
    /// </summary>
    public required List<Thread> Objs { get; set; }

    /// <summary>
    ///     分区字典
    /// </summary>
    public required Dictionary<string, int> TabDictionary { get; set; }

    /// <summary>
    ///     是否还有更多
    /// </summary>
    public bool HasMore => Page.HasMore;

    /// <summary>
    ///     从贴吧原始数据转换
    /// </summary>
    /// <param name="dataRes">Protobuf 吧页面响应数据</param>
    /// <returns>主题帖列表实体</returns>
    internal static Threads FromTbData(FrsPageResIdl.Types.DataRes dataRes)
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

        return new Threads
        {
            Page = PageT.FromTbData(dataRes.Page),
            Forum = forum,
            TabDictionary = dataRes.NavTabInfo.Tab.ToDictionary(p => p.TabName, p => p.TabId),
            Objs = threads
        };
    }

    /// <summary>
    ///     转换为字符串
    /// </summary>
    /// <returns>列表摘要信息</returns>
    public override string ToString()
    {
        var sb = new StringBuilder();
        sb.AppendLine(
            $"{nameof(Page)}: {Page}, {nameof(Forum)}: {Forum}, {nameof(TabDictionary)}: {TabDictionary}, {nameof(HasMore)}: {HasMore}");
        sb.AppendLine($"{nameof(Objs)}:");
        foreach (var obj in Objs) sb.AppendLine($"{obj}");

        return sb.ToString();
    }
}
