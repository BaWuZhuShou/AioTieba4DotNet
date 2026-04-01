using AioTieba4DotNet.Attributes;
using AioTieba4DotNet.Internal;
using AioTieba4DotNet.Internal.Mapping;
using AioTieba4DotNet.Models;
using AioTieba4DotNet.Models.Forums;
using AioTieba4DotNet.Transport;
using Google.Protobuf;

namespace AioTieba4DotNet.Api.GetLastReplyers;

[PythonApi("aiotieba.api.get_last_replyers")]
internal sealed class GetLastReplyers(ITiebaHttpCore httpCore, ITiebaWsCore wsCore)
{
    private readonly ITiebaHttpCore _httpCore = httpCore;
    private readonly ITiebaWsCore _wsCore = wsCore;

    private const int Cmd = 301001;
    private const string ClientVersion = "6.0.1";

    private static byte[] PackProto(string fname, int pn, int rn, ThreadSortType sort, bool isGood)
    {
        var req = new FrsPageReqIdl4lp
        {
            Data = new FrsPageReqIdl4lp.Types.DataReq
            {
                Common = new CommonReq { ClientType = 2, ClientVersion = ClientVersion },
                Kw = fname,
                Pn = pn == 1 ? 0 : pn,
                Rn = rn,
                RnNeed = rn + 5,
                IsGood = isGood ? 1 : 0,
                SortType = (int)sort
            }
        };

        return req.ToByteArray();
    }

    private static LastReplyers ParseResponse(byte[] body)
    {
        var res = FrsPageResIdl4lp.Parser.ParseFrom(body);
        ApiResponseValidator.CheckError(res.Error.Errorno, res.Error.Errmsg);
        return LastReplyersMapper.FromTbData(res.Data);
    }

    public async Task<LastReplyers> RequestHttpAsync(string fname, int pn, int rn, ThreadSortType sort, bool isGood,
        CancellationToken cancellationToken = default)
    {
        var data = PackProto(fname, pn, rn, sort, isGood);
        var requestUri = new UriBuilder("http", Const.AppBaseHost, 80, "/c/f/frs/page")
        {
            Query = $"cmd={Cmd}"
        }.Uri;

        var result = await _httpCore.SendAppProtoAsync(requestUri, data, cancellationToken);
        return ParseResponse(result);
    }

    public async Task<LastReplyers> RequestWsAsync(string fname, int pn, int rn, ThreadSortType sort, bool isGood,
        CancellationToken cancellationToken = default)
    {
        var data = PackProto(fname, pn, rn, sort, isGood);
        var response = await _wsCore.SendAsync(Cmd, data, cancellationToken: cancellationToken);
        return ParseResponse(response.Payload.Data.ToByteArray());
    }
}
