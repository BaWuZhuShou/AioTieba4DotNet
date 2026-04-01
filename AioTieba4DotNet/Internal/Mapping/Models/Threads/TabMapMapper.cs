using AioTieba4DotNet.Models.Threads;

namespace AioTieba4DotNet.Internal.Mapping;

internal static class TabMapMapper
{
    internal static TabMap FromTbData(SearchPostForumResIdl.Types.DataRes? data)
    {
        var tabs = data?.ExactMatch?.TabInfo?.Select(static tab =>
            new KeyValuePair<string, int>(tab.TabName, tab.TabId)) ?? [];

        return new TabMap(tabs);
    }
}
