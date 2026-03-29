using AioTieba4DotNet.Abstractions;
using AioTieba4DotNet.Attributes;
using AioTieba4DotNet.Core;
using AioTieba4DotNet.Enums;
using Google.Protobuf;

namespace AioTieba4DotNet.Api.SetBlacklist;

[RequireBduss]
[PythonApi("aiotieba.api.set_blacklist")]
internal class SetBlacklist(ITiebaHttpCore httpCore) : ProtoApiBase(httpCore)
{
    private const int Cmd = 309697;

    private static byte[] PackProto(Account account, long userId, BlacklistType type)
    {
        var req = new SetUserBlackReqIdl
        {
            Data = new SetUserBlackReqIdl.Types.DataReq
            {
                BlackUid = userId,
                Common = new CommonReq { BDUSS = account.Bduss, ClientType = 2, ClientVersion = Const.MainVersion },
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
        CheckError(resProto.Error.Errorno, resProto.Error.Errmsg);
    }

    public async Task<bool> RequestAsync(long userId, BlacklistType type, CancellationToken cancellationToken = default)
    {
        var data = PackProto(HttpCore.Account!, userId, type);
        var requestUri = new UriBuilder("https", Const.AppBaseHost, 443, "/c/c/user/setUserBlack") { Query = $"cmd={Cmd}" }.Uri;
        var result = await HttpCore.SendAppProtoAsync(requestUri, data, cancellationToken);
        ParseBody(result);
        return true;
    }
}
