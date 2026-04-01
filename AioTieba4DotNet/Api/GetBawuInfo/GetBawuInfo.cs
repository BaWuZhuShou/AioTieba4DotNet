using AioTieba4DotNet.Attributes;
using AioTieba4DotNet.Internal;
using AioTieba4DotNet.Internal.Mapping;
using AioTieba4DotNet.Models.Admins;
using AioTieba4DotNet.Transport;
using Google.Protobuf;

namespace AioTieba4DotNet.Api.GetBawuInfo;

[PythonApi("aiotieba.api.get_bawu_info")]
internal sealed class GetBawuInfo(ITiebaHttpCore httpCore, ITiebaWsCore wsCore)
{
    private const int Cmd = 301007;

    private static byte[] PackProto(ulong fid)
    {
        var request = new GetBawuInfoReqIdl
        {
            Data = new GetBawuInfoReqIdl.Types.DataReq
            {
                Common = new CommonReq { ClientVersion = Const.MainVersion },
                Fid = fid
            }
        };

        return request.ToByteArray();
    }

    private static BawuInfo ParseResponse(byte[] body)
    {
        var response = GetBawuInfoResIdl.Parser.ParseFrom(body);
        ApiResponseValidator.CheckError(response.Error.Errorno, response.Error.Errmsg);
        return BawuInfoMapper.FromTbData(response.Data);
    }

    public async Task<BawuInfo> RequestHttpAsync(ulong fid, CancellationToken cancellationToken = default)
    {
        var data = PackProto(fid);
        var requestUri = new UriBuilder("http", Const.AppBaseHost, 80, "/c/f/forum/getBawuInfo")
        {
            Query = $"cmd={Cmd}"
        }.Uri;

        var result = await httpCore.SendAppProtoAsync(requestUri, data, cancellationToken);
        return ParseResponse(result);
    }

    public async Task<BawuInfo> RequestWsAsync(ulong fid, CancellationToken cancellationToken = default)
    {
        var data = PackProto(fid);
        var response = await wsCore.SendAsync(Cmd, data, cancellationToken: cancellationToken);
        return ParseResponse(response.Payload.Data.ToByteArray());
    }
}
