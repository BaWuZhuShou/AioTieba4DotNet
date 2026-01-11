using AioTieba4DotNet.Abstractions;
using AioTieba4DotNet.Api.GetUserContents.Entities;
using AioTieba4DotNet.Attributes;
using AioTieba4DotNet.Core;
using AioTieba4DotNet.Enums;
using Google.Protobuf;

namespace AioTieba4DotNet.Api.GetUserContents;

/// <summary>
///     获取用户发布回复列表的 API
/// </summary>
/// <param name="httpCore">Http 核心组件</param>
/// <param name="wsCore">Websocket 核心组件</param>
/// <param name="mode">请求模式</param>
[RequireBduss]
[PythonApi("aiotieba.api.get_user_contents.get_posts")]
public class GetPosts(
    ITiebaHttpCore httpCore,
    ITiebaWsCore wsCore,
    TiebaRequestMode mode = TiebaRequestMode.Http) : ProtoApiWsBase<UserPostss>(httpCore, wsCore, mode)
{
    private const int Cmd = 303002;

    private static byte[] PackProto(Account account, int userId, uint pn, uint rn, string version)
    {
        var userPostReqIdl = new UserPostReqIdl
        {
            Data = new UserPostReqIdl.Types.DataReq
            {
                Common = new CommonReq { ClientType = 2, BDUSS = account.Bduss, ClientVersion = version },
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

    /// <summary>
    ///     发送获取用户发布回复列表请求
    /// </summary>
    /// <param name="userId">用户 ID (uid)</param>
    /// <param name="pn">页码</param>
    /// <param name="rn">每页请求数量</param>
    /// <param name="version">客户端版本号</param>
    /// <returns>回复列表实体</returns>
    public async Task<UserPostss> RequestAsync(int userId, uint pn, uint rn, string version)
    {
        return await ExecuteAsync(
            () => RequestHttpAsync(userId, pn, rn, version),
            () => RequestWsAsync(userId, pn, rn, version)
        );
    }

    /// <summary>
    ///     通过 HTTP 获取用户发布回复列表
    /// </summary>
    /// <param name="userId">用户 ID (uid)</param>
    /// <param name="pn">页码</param>
    /// <param name="rn">每页请求数量</param>
    /// <param name="version">客户端版本号</param>
    /// <returns>回复列表实体</returns>
    public async Task<UserPostss> RequestHttpAsync(int userId, uint pn, uint rn, string version)
    {
        var data = PackProto(HttpCore.Account!, userId, pn, rn, version);
        var requestUri = new UriBuilder("https", Const.AppBaseHost, 443, "/c/u/feed/userpost") { Query = $"cmd={Cmd}" }
            .Uri;

        var result = await HttpCore.SendAppProtoAsync(requestUri, data);
        return ParseBody(result);
    }

    /// <summary>
    ///     通过 Websocket 获取用户发布回复列表
    /// </summary>
    /// <param name="userId">用户 ID (uid)</param>
    /// <param name="pn">页码</param>
    /// <param name="rn">每页请求数量</param>
    /// <param name="version">客户端版本号</param>
    /// <returns>回复列表实体</returns>
    public async Task<UserPostss> RequestWsAsync(int userId, uint pn, uint rn, string version)
    {
        var data = PackProto(WsCore.Account!, userId, pn, rn, version);
        var response = await WsCore.SendAsync(Cmd, data);
        return ParseBody(response.Payload.Data.ToByteArray());
    }
}
