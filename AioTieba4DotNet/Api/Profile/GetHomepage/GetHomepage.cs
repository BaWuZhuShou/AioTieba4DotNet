using AioTieba4DotNet.Attributes;
using AioTieba4DotNet.Internal;
using AioTieba4DotNet.Internal.Mapping;
using AioTieba4DotNet.Models.Users;
using AioTieba4DotNet.Transport;
using Google.Protobuf;

namespace AioTieba4DotNet.Api.Profile.GetHomepage;

[PythonApi("aiotieba.api.profile.get_homepage")]
internal sealed class GetHomepage(ITiebaHttpCore httpCore, ITiebaWsCore wsCore)
{
    private const int Cmd = 303012;

    public async Task<Homepage> RequestHttpAsync(int userId, int pn, CancellationToken cancellationToken = default)
    {
        var data = PackProto(userId, pn);
        var requestUri = new UriBuilder("http", Const.AppBaseHost, 80, "/c/u/user/profile")
        {
            Query = $"cmd={Cmd}"
        }.Uri;

        var response = await httpCore.SendAppProtoAsync(requestUri, data, cancellationToken);
        return ParseResponse(response);
    }

    public async Task<Homepage> RequestWsAsync(int userId, int pn, CancellationToken cancellationToken = default)
    {
        var data = PackProto(userId, pn);
        var response = await wsCore.SendAsync(Cmd, data, cancellationToken: cancellationToken);
        return ParseResponse(response.Payload.Data.ToByteArray());
    }

    private static byte[] PackProto(int userId, int pn)
    {
        var req = new ProfileReqIdl
        {
            Data = new ProfileReqIdl.Types.DataReq
            {
                Common = new CommonReq { ClientType = 2, ClientVersion = Const.MainVersion },
                Uid = userId,
                NeedPostCount = 1,
                Page = pn
            }
        };

        return req.ToByteArray();
    }

    private static Homepage ParseResponse(byte[] body)
    {
        var response = ProfileResIdl.Parser.ParseFrom(body);
        ApiResponseValidator.CheckError(response.Error.Errorno, response.Error.Errmsg);
        return HomepageMapper.FromTbData(response.Data);
    }
}
