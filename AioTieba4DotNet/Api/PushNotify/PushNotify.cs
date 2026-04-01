using AioTieba4DotNet.Attributes;
using AioTieba4DotNet.Internal.Mapping;
using AioTieba4DotNet.Models.Messages;

namespace AioTieba4DotNet.Api.PushNotify;

[PythonApi("aiotieba.api.push_notify")]
internal static class PushNotify
{
    public static IReadOnlyList<WsNotify> ParseBody(byte[] body)
    {
        var response = PushNotifyResIdl.Parser.ParseFrom(body);
        return response.MultiMsg.Select(WsNotifyMapper.FromTbData).ToList();
    }
}
