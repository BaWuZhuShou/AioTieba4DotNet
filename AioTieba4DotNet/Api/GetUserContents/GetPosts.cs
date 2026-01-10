using AioTieba4DotNet.Abstractions;
using Google.Protobuf;
using AioTieba4DotNet.Api.GetUserContents.Entities;
using AioTieba4DotNet.Core;
using AioTieba4DotNet.Enums;
using AioTieba4DotNet.Exceptions;

namespace AioTieba4DotNet.Api.GetUserContents;

public class GetPosts(
    ITiebaHttpCore httpCore,
    ITiebaWsCore wsCore,
    TiebaRequestMode mode = TiebaRequestMode.Http) : ProtoApiWsBase<UserPostss>(httpCore, wsCore, mode)
{
    private const int Cmd = 303002;

    private static byte[] PackProto(Account account, int userId, uint pn, uint rn, string version)
    {
        var userPostReqIdl = new UserPostReqIdl()
        {
            Data = new UserPostReqIdl.Types.DataReq()
            {
                Common = new CommonReq()
                {
                    ClientType = 2,
                    BDUSS = account.Bduss,
                    ClientVersion = version,
                },
                UserId = userId,
                Pn = pn,
                Rn = rn,
                NeedContent = 1
            }
        };
        return userPostReqIdl.ToByteArray();
    }

    private static UserPostss ParseBody(byte[] body)
    {
        var resProto = UserPostResIdl.Parser.ParseFrom(body);
        CheckError(resProto.Error.Errorno, resProto.Error.Errmsg);

        var dataForum = resProto.Data;
        return UserPostss.FromTbData(dataForum);
    }

    public async Task<UserPostss> RequestAsync(int userId, uint pn, uint rn, string version)
    {
        return await ExecuteAsync(
            () => RequestHttpAsync(userId, pn, rn, version),
            () => RequestWsAsync(userId, pn, rn, version)
        );
    }

    public async Task<UserPostss> RequestHttpAsync(int userId, uint pn, uint rn, string version)
    {
        var data = PackProto(HttpCore.Account!, userId, pn, rn, version);
        var requestUri = new UriBuilder("https", Const.AppBaseHost, 443, "/c/u/feed/userpost")
        {
            Query = $"cmd={Cmd}"
        }.Uri;

        var result = await HttpCore.SendAppProtoAsync(requestUri, data);
        return ParseBody(result);
    }

    public async Task<UserPostss> RequestWsAsync(int userId, uint pn, uint rn, string version)
    {
        var data = PackProto(WsCore.Account!, userId, pn, rn, version);
        var response = await WsCore.SendAsync(Cmd, data);
        return ParseBody(response.Payload.Data.ToByteArray());
    }
}
