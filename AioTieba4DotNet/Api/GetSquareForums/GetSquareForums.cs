using AioTieba4DotNet.Attributes;
using AioTieba4DotNet.Internal;
using AioTieba4DotNet.Internal.Mapping;
using AioTieba4DotNet.Models.Forums;
using AioTieba4DotNet.Session;
using AioTieba4DotNet.Transport;
using Google.Protobuf;

namespace AioTieba4DotNet.Api.GetSquareForums;

[RequireBduss]
[PythonApi("aiotieba.api.get_square_forums")]
internal sealed class GetSquareForums(ITiebaHttpCore httpCore, ITiebaWsCore wsCore)
{
    private const int Cmd = 309653;

    private static byte[] PackProto(Account account, string cname, int pn, int rn)
    {
        var req = new GetForumSquareReqIdl
        {
            Data = new GetForumSquareReqIdl.Types.DataReq
            {
                Common = new CommonReq { BDUSS = account.Bduss, ClientVersion = Const.MainVersion },
                ClassName = cname,
                Pn = pn,
                Rn = rn
            }
        };

        return req.ToByteArray();
    }

    private static SquareForums ParseResponse(byte[] body)
    {
        var res = GetForumSquareResIdl.Parser.ParseFrom(body);
        ApiResponseValidator.CheckError(res.Error.Errorno, res.Error.Errmsg);
        return SquareForumsMapper.FromTbData(res.Data);
    }

    public async Task<SquareForums> RequestHttpAsync(string cname, int pn, int rn,
        CancellationToken cancellationToken = default)
    {
        var data = PackProto(httpCore.Account!, cname, pn, rn);
        var requestUri =
            new UriBuilder("http", Const.AppBaseHost, 80, "/c/f/forum/getForumSquare") { Query = $"cmd={Cmd}" }.Uri;

        var result = await httpCore.SendAppProtoAsync(requestUri, data, cancellationToken);
        return ParseResponse(result);
    }

    public async Task<SquareForums> RequestWsAsync(string cname, int pn, int rn,
        CancellationToken cancellationToken = default)
    {
        var data = PackProto(wsCore.Account!, cname, pn, rn);
        var response = await wsCore.SendAsync(Cmd, data, cancellationToken: cancellationToken);
        return ParseResponse(response.Payload.Data.ToByteArray());
    }
}
