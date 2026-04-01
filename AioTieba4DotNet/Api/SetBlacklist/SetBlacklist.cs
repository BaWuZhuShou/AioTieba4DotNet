using AioTieba4DotNet.Transport;
using AioTieba4DotNet.Attributes;
using AioTieba4DotNet.Internal;
using AioTieba4DotNet.Session;
using AioTieba4DotNet.Models;
using Google.Protobuf;

namespace AioTieba4DotNet.Api.SetBlacklist;

[RequireBduss]
[PythonApi("aiotieba.api.set_blacklist")]
internal class SetBlacklist(ITiebaHttpCore httpCore)
{
    private readonly ITiebaHttpCore _httpCore = httpCore;

    private const int Cmd = 309697;

    private static byte[] PackProto(Account account, long userId, BlacklistType type)
    {
        var req = new SetUserBlackReqIdl
        {
            Data = new SetUserBlackReqIdl.Types.DataReq
            {
                BlackUid = userId,
                Common =
                    new CommonReq { BDUSS = account.Bduss, ClientType = 2, ClientVersion = Const.MainVersion },
                PermList = new SetUserBlackReqIdl.Types.DataReq.Types.PermissionList
                {
                    Follow = (type & BlacklistType.Follow) != 0 ? 1 : 2,
                    Interact = (type & BlacklistType.Interact) != 0 ? 1 : 2,
                    Chat = (type & BlacklistType.Chat) != 0 ? 1 : 2
                }
            }
        };

        return req.ToByteArray();
    }

    private static void ParseBody(byte[] body)
    {
        var resProto = SetUserBlackResIdl.Parser.ParseFrom(body);
        ApiResponseValidator.CheckError(resProto.Error.Errorno, resProto.Error.Errmsg);
    }

    public async Task<bool> RequestAsync(long userId, BlacklistType type, CancellationToken cancellationToken = default)
    {
        var data = PackProto(_httpCore.Account!, userId, type);
        var requestUri =
            new UriBuilder("https", Const.AppBaseHost, 443, "/c/c/user/setUserBlack") { Query = $"cmd={Cmd}" }.Uri;
        var result = await _httpCore.SendAppProtoAsync(requestUri, data, cancellationToken);
        ParseBody(result);
        return true;
    }
}
