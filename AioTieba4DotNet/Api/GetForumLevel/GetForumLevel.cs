using AioTieba4DotNet.Attributes;
using AioTieba4DotNet.Internal;
using AioTieba4DotNet.Internal.Mapping;
using AioTieba4DotNet.Models.Forums;
using AioTieba4DotNet.Transport;
using Google.Protobuf;

namespace AioTieba4DotNet.Api.GetForumLevel;

[RequireBduss]
[PythonApi("aiotieba.api.get_forum_level")]
internal sealed class GetForumLevel(ITiebaHttpCore httpCore)
{
    private const int Cmd = 301005;

    private static byte[] PackProto(Account account, ulong fid)
    {
        var req = new GetLevelInfoReqIdl
        {
            Data = new GetLevelInfoReqIdl.Types.DataReq
            {
                ForumId = (long)fid,
                Common = new CommonReq { BDUSS = account.Bduss, ClientVersion = Const.MainVersion }
            }
        };

        return req.ToByteArray();
    }

    private static ForumLevelInfo ParseResponse(byte[] body)
    {
        var res = GetLevelInfoResIdl.Parser.ParseFrom(body);
        ApiResponseValidator.CheckError(res.Error.Errorno, res.Error.Errmsg);
        return ForumLevelInfoMapper.FromTbData(res.Data);
    }

    public async Task<ForumLevelInfo> RequestAsync(ulong fid, CancellationToken cancellationToken = default)
    {
        var data = PackProto(httpCore.Account!, fid);
        var requestUri =
            new UriBuilder("http", Const.AppBaseHost, 80, "/c/f/forum/getLevelInfo") { Query = $"cmd={Cmd}" }.Uri;

        var result = await httpCore.SendAppProtoAsync(requestUri, data, cancellationToken);
        return ParseResponse(result);
    }
}
