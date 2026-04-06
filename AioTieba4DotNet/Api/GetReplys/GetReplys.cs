using AioTieba4DotNet.Internal;
using AioTieba4DotNet.Internal.Mapping;
using AioTieba4DotNet.Models.Users;
using AioTieba4DotNet.Transport;
using Google.Protobuf;

namespace AioTieba4DotNet.Api.GetReplys;

internal sealed class GetReplys(ITiebaHttpCore httpCore, ITiebaWsCore wsCore)
{
    private const int Cmd = 303007;

    private static byte[] PackProto(Account account, int pn)
    {
        var req = new ReplyMeReqIdl
        {
            Data = new ReplyMeReqIdl.Types.DataReq
            {
                Pn = pn.ToString(),
                Common = new CommonReq { BDUSS = account.Bduss, ClientVersion = Const.MainVersion }
            }
        };

        return req.ToByteArray();
    }

    private static ReplyMessages ParseBody(byte[] body)
    {
        var resProto = ReplyMeResIdl.Parser.ParseFrom(body);
        ApiResponseValidator.CheckError(resProto.Error.Errorno, resProto.Error.Errmsg);
        return ReplyMessagesMapper.FromTbData(resProto.Data);
    }

    public async Task<ReplyMessages> RequestAsync(int pn, CancellationToken cancellationToken = default)
    {
        return await RequestHttpAsync(pn, cancellationToken);
    }

    public async Task<ReplyMessages> RequestHttpAsync(int pn, CancellationToken cancellationToken = default)
    {
        var data = PackProto(httpCore.Account!, pn);
        var requestUri = new UriBuilder("http", Const.AppBaseHost, 80, "/c/u/feed/replyme") { Query = $"cmd={Cmd}" }
            .Uri;
        var result = await httpCore.SendAppProtoAsync(requestUri, data, cancellationToken);
        return ParseBody(result);
    }

    public async Task<ReplyMessages> RequestWsAsync(int pn, CancellationToken cancellationToken = default)
    {
        var data = PackProto(wsCore.Account!, pn);
        var response = await wsCore.SendAsync(Cmd, data, cancellationToken: cancellationToken);
        return ParseBody(response.Payload.Data.ToByteArray());
    }
}
