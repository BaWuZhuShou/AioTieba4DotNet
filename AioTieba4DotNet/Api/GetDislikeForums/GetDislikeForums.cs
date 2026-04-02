using AioTieba4DotNet.Attributes;
using AioTieba4DotNet.Internal;
using AioTieba4DotNet.Internal.Mapping;
using AioTieba4DotNet.Models.Forums;
using AioTieba4DotNet.Session;
using AioTieba4DotNet.Transport;
using Google.Protobuf;

namespace AioTieba4DotNet.Api.GetDislikeForums;

[RequireBduss]
[PythonApi("aiotieba.api.get_dislike_forums")]
internal sealed class GetDislikeForums(ITiebaHttpCore httpCore, ITiebaWsCore wsCore)
{
    private const int Cmd = 309692;

    private static byte[] PackProto(Account account, int pn, int rn)
    {
        var req = new GetDislikeListReqIdl
        {
            Data = new GetDislikeListReqIdl.Types.DataReq
            {
                Common = new CommonReq { BDUSS = account.Bduss, ClientVersion = Const.MainVersion },
                Pn = pn,
                Rn = rn
            }
        };

        return req.ToByteArray();
    }

    private static DislikeForums ParseResponse(byte[] body)
    {
        var res = GetDislikeListResIdl.Parser.ParseFrom(body);
        ApiResponseValidator.CheckError(res.Error.Errorno, res.Error.Errmsg);
        return DislikeForumsMapper.FromTbData(res.Data);
    }

    public async Task<DislikeForums> RequestHttpAsync(int pn, int rn, CancellationToken cancellationToken = default)
    {
        var data = PackProto(httpCore.Account!, pn, rn);
        var requestUri =
            new UriBuilder("http", Const.AppBaseHost, 80, "/c/u/user/getDislikeList") { Query = $"cmd={Cmd}" }.Uri;

        var result = await httpCore.SendAppProtoAsync(requestUri, data, cancellationToken);
        return ParseResponse(result);
    }

    public async Task<DislikeForums> RequestWsAsync(int pn, int rn, CancellationToken cancellationToken = default)
    {
        var data = PackProto(wsCore.Account!, pn, rn);
        var response = await wsCore.SendAsync(Cmd, data, cancellationToken: cancellationToken);
        return ParseResponse(response.Payload.Data.ToByteArray());
    }
}
