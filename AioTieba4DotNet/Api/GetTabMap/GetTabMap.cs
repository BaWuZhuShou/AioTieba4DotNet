using AioTieba4DotNet.Attributes;
using AioTieba4DotNet.Internal;
using AioTieba4DotNet.Internal.Mapping;
using AioTieba4DotNet.Models.Threads;
using AioTieba4DotNet.Session;
using AioTieba4DotNet.Transport;
using Google.Protobuf;

namespace AioTieba4DotNet.Api.GetTabMap;

[RequireBduss]
[PythonApi("aiotieba.api.get_tab_map")]
internal sealed class GetTabMap(ITiebaHttpCore httpCore, ITiebaWsCore wsCore)
{
    private const int Cmd = 309466;

    private static byte[] PackProto(Account account, string fname)
    {
        var request = new SearchPostForumReqIdl
        {
            Data = new SearchPostForumReqIdl.Types.DataReq
            {
                Common = new CommonReq { BDUSS = account.Bduss, ClientVersion = Const.MainVersion },
                Fname = fname
            }
        };

        return request.ToByteArray();
    }

    private static TabMap ParseResponse(byte[] body)
    {
        var response = SearchPostForumResIdl.Parser.ParseFrom(body);
        ApiResponseValidator.CheckError(response.Error.Errorno, response.Error.Errmsg);
        return TabMapMapper.FromTbData(response.Data);
    }

    public async Task<TabMap> RequestHttpAsync(string fname, CancellationToken cancellationToken = default)
    {
        var data = PackProto(httpCore.Account!, fname);
        var requestUri =
            new UriBuilder("http", Const.AppBaseHost, 80, "/c/f/forum/searchPostForum") { Query = $"cmd={Cmd}" }.Uri;

        var result = await httpCore.SendAppProtoAsync(requestUri, data, cancellationToken);
        return ParseResponse(result);
    }

    public async Task<TabMap> RequestWsAsync(string fname, CancellationToken cancellationToken = default)
    {
        var data = PackProto(wsCore.Account!, fname);
        var response = await wsCore.SendAsync(Cmd, data, cancellationToken: cancellationToken);
        return ParseResponse(response.Payload.Data.ToByteArray());
    }
}
