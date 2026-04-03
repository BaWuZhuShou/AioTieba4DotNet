using System.Text;

namespace AioTieba4DotNet.Models.Threads;

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
    ///     转换为字符串
    /// </summary>
    /// <returns>列表摘要信息</returns>
    public override string ToString()
    {
        var sb = new StringBuilder();
        var tabDictionary = string.Join(", ", TabDictionary.Select(static kvp => $"{kvp.Key}:{kvp.Value}"));
        sb.AppendLine(
            $"{nameof(Page)}: {Page}, {nameof(Forum)}: {Forum}, {nameof(TabDictionary)}: [{tabDictionary}], {nameof(HasMore)}: {HasMore}");
        sb.AppendLine($"{nameof(Objs)}:");
        foreach (var obj in Objs) sb.AppendLine($"{obj}");

        return sb.ToString();
    }
}
