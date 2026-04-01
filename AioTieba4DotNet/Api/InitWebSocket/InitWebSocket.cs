using AioTieba4DotNet.Attributes;
using AioTieba4DotNet.Internal.Mapping;
using AioTieba4DotNet.Models.Messages;
using AioTieba4DotNet.Transport;
using AioTieba4DotNet.Transport.WebSockets;

namespace AioTieba4DotNet.Api.InitWebSocket;

[RequireBduss]
[PythonApi("aiotieba.api.init_websocket")]
internal sealed class InitWebSocket(ITiebaWsCore wsCore)
{
    private const int Cmd = 1001;

    public async Task<IReadOnlyList<WsMsgGroupInfo>> RequestAsync(CancellationToken cancellationToken = default)
    {
        var account = wsCore.Account
            ?? throw new InvalidOperationException("An authenticated account is required for websocket initialization.");

        var payload = new TiebaWebSocketHandshakeBuilder().Pack(account);
        var response = await wsCore.SendAsync(Cmd, payload, encrypt: false, cancellationToken: cancellationToken);
        return ParseResponse(response.Payload.Data.ToByteArray());
    }

    private static IReadOnlyList<WsMsgGroupInfo> ParseResponse(byte[] body)
    {
        var response = UpdateClientInfoResIdl.Parser.ParseFrom(body);
        ApiResponseValidator.CheckError(response.Error.Errorno, response.Error.Errmsg);
        return response.Data.GroupInfo.Select(WsMsgGroupInfoMapper.FromTbData).ToList();
    }
}
