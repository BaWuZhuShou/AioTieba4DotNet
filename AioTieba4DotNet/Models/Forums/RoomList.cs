using AioTieba4DotNet.Models.Shared;

namespace AioTieba4DotNet.Models.Forums;

/// <summary>
///     获取指定贴吧后返回的房间列表
/// </summary>
public sealed class RoomList : Containers<Dictionary<string, object?>>
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="RoomList"/> class.
    /// </summary>
    /// <param name="objs">一个房间字段字典列表。</param>
    public RoomList(List<Dictionary<string, object?>> objs) : base(objs)
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="RoomList"/> class.
    /// </summary>
    /// <param name="collection">一个房间字段字典集合。</param>
    public RoomList(IEnumerable<Dictionary<string, object?>>? collection) : base(collection)
    {
    }
}
