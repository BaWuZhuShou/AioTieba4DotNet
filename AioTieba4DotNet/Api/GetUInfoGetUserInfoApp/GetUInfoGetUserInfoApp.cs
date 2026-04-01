using AioTieba4DotNet.Transport;
using AioTieba4DotNet.Models.Users;
using AioTieba4DotNet.Attributes;
using AioTieba4DotNet.Internal;
using AioTieba4DotNet.Session;
using Google.Protobuf;

namespace AioTieba4DotNet.Api.GetUInfoGetUserInfoApp;

/// <summary>
///     获取用户基础信息的 API (App端)
/// </summary>
/// <param name="httpCore">Http 核心组件</param>
[PythonApi("aiotieba.api.get_uinfo_getuserinfo_app")]
internal class GetUInfoGetUserInfoApp(ITiebaHttpCore httpCore)
{
    private readonly ITiebaHttpCore _httpCore = httpCore;

    private const int Cmd = 303024;

    private static byte[] PackProto(int userId)
    {
        var reqProto = new GetUserInfoReqIdl { Data = new GetUserInfoReqIdl.Types.DataReq { UserId = userId } };
        return reqProto.ToByteArray();
    }

    private static UserInfoGuInfoApp ParseBody(byte[] body)
    {
        var resProto = GetUserInfoResIdl.Parser.ParseFrom(body);
        ApiResponseValidator.CheckError(resProto.Error.Errorno, resProto.Error.Errmsg);

        var dataUser = resProto.Data.User;
        return Internal.Mapping.UserInfoGuInfoAppMapper.FromTbData(dataUser);
    }

    /// <summary>
    ///     发送获取用户基础信息请求
    /// </summary>
    /// <param name="userId">用户 ID (uid)</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户基础信息</returns>
    public async Task<UserInfoGuInfoApp> RequestAsync(int userId, CancellationToken cancellationToken = default)
    {
        var data = PackProto(userId);
        var requestUri = new UriBuilder("http", Const.AppBaseHost, 80, "/c/u/user/getuserinfo") { Query = $"cmd={Cmd}" }
            .Uri;
        var result = await _httpCore.SendAppProtoAsync(requestUri, data, cancellationToken);
        return ParseBody(result);
    }
}
