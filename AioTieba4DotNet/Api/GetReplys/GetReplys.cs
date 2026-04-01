using AioTieba4DotNet.Transport;
using AioTieba4DotNet.Attributes;
using AioTieba4DotNet.Internal;
using AioTieba4DotNet.Session;
using AioTieba4DotNet.Internal.Mapping;
using AioTieba4DotNet.Models.Users;
using Google.Protobuf;

namespace AioTieba4DotNet.Api.GetReplys;

[RequireBduss]
[PythonApi("aiotieba.api.get_replys")]
internal class GetReplys(ITiebaHttpCore httpCore)
{
    private readonly ITiebaHttpCore _httpCore = httpCore;

    private const int Cmd = 303007;

    private static byte[] PackProto(Account account, int pn)
    {
        var req = new ReplyMeReqIdl
        {
            Data = new ReplyMeReqIdl.Types.DataReq
            {
                Pn = pn.ToString(),
                Common = new CommonReq { BDUSS = account.Bduss, ClientVersion = Const.MainVersion }
            }
        };

        return req.ToByteArray();
    }

    private static ReplyMessages ParseBody(byte[] body)
    {
        var resProto = ReplyMeResIdl.Parser.ParseFrom(body);
        ApiResponseValidator.CheckError(resProto.Error.Errorno, resProto.Error.Errmsg);
        return ReplyMessagesMapper.FromTbData(resProto.Data);
    }

    public async Task<ReplyMessages> RequestAsync(int pn, CancellationToken cancellationToken = default)
    {
        var data = PackProto(_httpCore.Account!, pn);
        var requestUri = new UriBuilder("https", Const.AppBaseHost, 443, "/c/u/feed/replyme") { Query = $"cmd={Cmd}" }.Uri;
        var result = await _httpCore.SendAppProtoAsync(requestUri, data, cancellationToken);
        return ParseBody(result);
    }
}
