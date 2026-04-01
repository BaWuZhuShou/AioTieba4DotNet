using AioTieba4DotNet.Attributes;
using AioTieba4DotNet.Internal;
using AioTieba4DotNet.Models.Forums;
using AioTieba4DotNet.Transport;
using Newtonsoft.Json.Linq;

namespace AioTieba4DotNet.Api.GetRoomListByFid;

[RequireBduss]
[PythonApi("aiotieba.api.get_roomlist_by_fid")]
internal sealed class GetRoomListByFid(ITiebaHttpCore httpCore) : JsonApiBase(httpCore)
{
    private static RoomList ParseRoomList(string body)
    {
        var json = ParseBody(body);
        var roomEntries = new List<Dictionary<string, object?>>();
        var groups = json["data"]?["list"] as JArray;
        if (groups is null)
            return new RoomList(roomEntries);

        foreach (var group in groups.OfType<JObject>())
        {
            var rooms = group["room_list"] as JArray;
            if (rooms is null)
                continue;

            foreach (var room in rooms.OfType<JObject>())
                roomEntries.Add(room.ToObject<Dictionary<string, object?>>()!);
        }

        return new RoomList(roomEntries);
    }

    public async Task<RoomList> RequestAsync(ulong fid, CancellationToken cancellationToken = default)
    {
        var data = new List<KeyValuePair<string, string>>
        {
            new("BDUSS", HttpCore.Account?.Bduss ?? string.Empty),
            new("_client_version", Const.MainVersion),
            new("call_from", "frs"),
            new("fid", fid.ToString())
        };

        var requestUri = new UriBuilder(Const.AppInsecureScheme, Const.AppBaseHost, 80, "/c/f/chat/getRoomListByFid")
            .Uri;
        var result = await HttpCore.SendAppFormAsync(requestUri, data, cancellationToken);
        return ParseRoomList(result);
    }
}
