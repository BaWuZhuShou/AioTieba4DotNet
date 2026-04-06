using AioTieba4DotNet.Internal;
using AioTieba4DotNet.Internal.Mapping;
using AioTieba4DotNet.Models.Users;
using AioTieba4DotNet.Transport;
using Google.Protobuf;

namespace AioTieba4DotNet.Api.Profile.GetUInfoProfile;

/// <summary>
///     获取用户详细主页信息的 API
/// </summary>
/// <typeparam name="T">请求参数类型 (支持 int/long 作为 uid，或 string 作为 portrait/用户名)</typeparam>
/// <param name="httpCore">Http 核心组件</param>
internal sealed class GetUInfoProfile<T>(ITiebaHttpCore httpCore, ITiebaWsCore wsCore)
{
    private const int Cmd = 303012;

    private static byte[] PackProto<TP>(TP uidOrPortrait)
    {
        if (!(typeof(TP) == typeof(string) || typeof(TP) == typeof(int) || typeof(TP) == typeof(long)))
            throw new InvalidOperationException(
                $"TP's type is {typeof(TP)} now.TP must be either string, int or long.");

        var reqProto = new ProfileReqIdl
        {
            Data = new ProfileReqIdl.Types.DataReq
            {
                Common = new CommonReq { ClientType = 2, ClientVersion = Const.MainVersion },
                NeedPostCount = 1,
                Page = 1
            }
        };

        if (typeof(TP) == typeof(int) || typeof(TP) == typeof(long)) reqProto.Data.Uid = Convert.ToInt64(uidOrPortrait);

        if (typeof(TP) == typeof(string)) reqProto.Data.FriendUidPortrait = Convert.ToString(uidOrPortrait);

        return reqProto.ToByteArray();
    }


    private static UserInfoPf ParseBody(byte[] body)
    {
        var resProto = ProfileResIdl.Parser.ParseFrom(body);
        ApiResponseValidator.CheckError(resProto.Error.Errorno, resProto.Error.Errmsg);

        var resProtoData = resProto.Data;
        return UserInfoPfMapper.FromTbData(resProtoData);
    }

    /// <summary>
    ///     发送获取用户详细主页信息请求
    /// </summary>
    /// <param name="requestParams">uid (int/long) 或 portrait/用户名 (string)</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户详细主页信息</returns>
    public Task<UserInfoPf> RequestAsync(T requestParams, CancellationToken cancellationToken = default)
    {
        return RequestHttpAsync(requestParams, cancellationToken);
    }

    public async Task<UserInfoPf> RequestHttpAsync(T requestParams, CancellationToken cancellationToken = default)
    {
        var data = PackProto(requestParams);
        var requestUri = new UriBuilder("http", Const.AppBaseHost, 80, "/c/u/user/profile") { Query = $"cmd={Cmd}" }
            .Uri;
        var result = await httpCore.SendAppProtoAsync(requestUri, data, cancellationToken);
        return ParseBody(result);
    }

    public async Task<UserInfoPf> RequestWsAsync(T requestParams, CancellationToken cancellationToken = default)
    {
        var data = PackProto(requestParams);
        var response = await wsCore.SendAsync(Cmd, data, cancellationToken: cancellationToken);
        return ParseBody(response.Payload.Data.ToByteArray());
    }
}
