using AioTieba4DotNet.Attributes;
using AioTieba4DotNet.Internal;
using AioTieba4DotNet.Internal.Mapping;
using AioTieba4DotNet.Models.Users;
using AioTieba4DotNet.Transport;
using Google.Protobuf;

namespace AioTieba4DotNet.Api.GetBlacklistOld;

[RequireBduss]
[PythonApi("aiotieba.api.get_blacklist_old")]
internal sealed class GetBlacklistOld(ITiebaHttpCore httpCore, ITiebaWsCore wsCore)
{
    private const int Cmd = 303028;

    public async Task<BlacklistOldUsers> RequestHttpAsync(int pn, int rn, CancellationToken cancellationToken = default)
    {
        var data = PackProto(httpCore.Account!, pn, rn);
        var requestUri =
            new UriBuilder("http", Const.AppBaseHost, 80, "/c/u/user/userMuteQuery") { Query = $"cmd={Cmd}" }.Uri;

        var response = await httpCore.SendAppProtoAsync(requestUri, data, cancellationToken);
        return ParseResponse(response);
    }

    public async Task<BlacklistOldUsers> RequestWsAsync(int pn, int rn, CancellationToken cancellationToken = default)
    {
        var data = PackProto(wsCore.Account!, pn, rn);
        var response = await wsCore.SendAsync(Cmd, data, cancellationToken: cancellationToken);
        return ParseResponse(response.Payload.Data.ToByteArray());
    }

    private static byte[] PackProto(Account account, int pn, int rn)
    {
        var req = new UserMuteQueryReqIdl
        {
            Data = new UserMuteQueryReqIdl.Types.DataReq
            {
                Common = new CommonReq { BDUSS = account.Bduss, ClientVersion = Const.MainVersion },
                Pn = (uint)pn,
                Rn = (uint)rn
            }
        };

        return req.ToByteArray();
    }

    private static BlacklistOldUsers ParseResponse(byte[] body)
    {
        var response = UserMuteQueryResIdl.Parser.ParseFrom(body);
        ApiResponseValidator.CheckError(response.Error.Errorno, response.Error.Errmsg);
        return BlacklistOldUsersMapper.FromTbData(response.Data);
    }
}
