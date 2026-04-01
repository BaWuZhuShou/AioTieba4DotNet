using AioTieba4DotNet.Attributes;
using AioTieba4DotNet.Internal;
using AioTieba4DotNet.Internal.Mapping;
using AioTieba4DotNet.Models.Users;
using AioTieba4DotNet.Transport;
using Google.Protobuf;

namespace AioTieba4DotNet.Api.TiebaUid2UserInfo;

[PythonApi("aiotieba.api.tieba_uid2user_info")]
internal sealed class TiebaUid2UserInfo(ITiebaHttpCore httpCore, ITiebaWsCore wsCore)
{
    private const int Cmd = 309702;

    public async Task<UserInfoTUid> RequestHttpAsync(long tiebaUid, CancellationToken cancellationToken = default)
    {
        var data = PackProto(tiebaUid);
        var requestUri =
            new UriBuilder("http", Const.AppBaseHost, 80, "/c/u/user/getUserByTiebaUid") { Query = $"cmd={Cmd}" }.Uri;

        var response = await httpCore.SendAppProtoAsync(requestUri, data, cancellationToken);
        return ParseResponse(response);
    }

    public async Task<UserInfoTUid> RequestWsAsync(long tiebaUid, CancellationToken cancellationToken = default)
    {
        var data = PackProto(tiebaUid);
        var response = await wsCore.SendAsync(Cmd, data, cancellationToken: cancellationToken);
        return ParseResponse(response.Payload.Data.ToByteArray());
    }

    private static byte[] PackProto(long tiebaUid)
    {
        var req = new GetUserByTiebaUidReqIdl
        {
            Data = new GetUserByTiebaUidReqIdl.Types.DataReq
            {
                Common = new CommonReq { ClientVersion = Const.MainVersion }, TiebaUid = tiebaUid.ToString()
            }
        };

        return req.ToByteArray();
    }

    private static UserInfoTUid ParseResponse(byte[] body)
    {
        var response = GetUserByTiebaUidResIdl.Parser.ParseFrom(body);
        ApiResponseValidator.CheckError(response.Error.Errorno, response.Error.Errmsg);
        return UserInfoTUidMapper.FromTbData(response.Data.User);
    }
}
