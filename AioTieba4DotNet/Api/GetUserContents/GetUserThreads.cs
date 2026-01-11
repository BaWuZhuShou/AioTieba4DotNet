using AioTieba4DotNet.Abstractions;
using AioTieba4DotNet.Api.GetUserContents.Entities;
using AioTieba4DotNet.Attributes;
using AioTieba4DotNet.Core;
using AioTieba4DotNet.Enums;
using AioTieba4DotNet.Exceptions;
using Google.Protobuf;

namespace AioTieba4DotNet.Api.GetUserContents;

/// <summary>
/// 获取用户发布主题帖列表的 API
/// </summary>
/// <param name="httpCore">Http 核心组件</param>
/// <param name="wsCore">Websocket 核心组件</param>
/// <param name="mode">请求模式</param>
[RequireBduss]
[PythonApi("aiotieba.api.get_user_contents.get_threads")]
public class GetUserThreads(
    ITiebaHttpCore httpCore,
    ITiebaWsCore wsCore,
    TiebaRequestMode mode = TiebaRequestMode.Http) : ProtoApiWsBase<UserThreads>(httpCore, wsCore, mode)
{
    private const int Cmd = 303002;

    private static byte[] PackProto(Account account, int userId, uint pn, bool publicOnly)
    {
        var userPostReqIdl = new UserPostReqIdl()
        {
            Data = new UserPostReqIdl.Types.DataReq()
            {
                Common = new CommonReq()
                {
                    ClientType = 2, BDUSS = account.Bduss, ClientVersion = Const.MainVersion,
                },
                UserId = userId,
                Pn = pn,
                IsThread = 1,
                NeedContent = 1,
                IsViewCard = publicOnly ? 2 : 1
            }
        };
        return userPostReqIdl.ToByteArray();
    }

    private static UserThreads ParseBody(byte[] body)
    {
        var resProto = UserPostResIdl.Parser.ParseFrom(body);
        CheckError(resProto.Error.Errorno, resProto.Error.Errmsg);

        return UserThreads.FromTbData(resProto.Data);
    }

    /// <summary>
    /// 发送获取用户发布主题帖列表请求
    /// </summary>
    /// <param name="userId">用户 ID (uid)</param>
    /// <param name="pn">页码</param>
    /// <param name="publicOnly">是否只获取公开的主题帖</param>
    /// <returns>主题帖列表实体</returns>
    public async Task<UserThreads> RequestAsync(int userId, uint pn, bool publicOnly)
    {
        return await ExecuteAsync(
            () => RequestHttpAsync(userId, pn, publicOnly),
            () => RequestWsAsync(userId, pn, publicOnly)
        );
    }

    public async Task<UserThreads> RequestHttpAsync(int userId, uint pn, bool publicOnly)
    {
        var data = PackProto(HttpCore.Account!, userId, pn, publicOnly);
        var requestUri = new UriBuilder("https", Const.AppBaseHost, 443, "/c/u/feed/userpost") { Query = $"cmd={Cmd}" }
            .Uri;

        var result = await HttpCore.SendAppProtoAsync(requestUri, data);
        return ParseBody(result);
    }

    public async Task<UserThreads> RequestWsAsync(int userId, uint pn, bool publicOnly)
    {
        var data = PackProto(WsCore.Account!, userId, pn, publicOnly);
        var response = await WsCore.SendAsync(Cmd, data);
        return ParseBody(response.Payload.Data.ToByteArray());
    }
}
