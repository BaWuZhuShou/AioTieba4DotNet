using AioTieba4DotNet.Transport;
using AioTieba4DotNet.Models.Forums;
using AioTieba4DotNet.Attributes;
using AioTieba4DotNet.Internal;
using AioTieba4DotNet.Session;
using Google.Protobuf;

namespace AioTieba4DotNet.Api.GetForumDetail;

/// <summary>
///     获取贴吧详情信息的 API
/// </summary>
/// <param name="httpCore">Http 核心组件</param>
[PythonApi("aiotieba.api.get_forum_detail")]
internal class GetForumDetail(ITiebaHttpCore httpCore)
{
    private readonly ITiebaHttpCore _httpCore = httpCore;

    private const int Cmd = 303021;

    private static byte[] PackProto(long fid)
    {
        var reqIdl = new GetForumDetailReqIdl
        {
            Data = new GetForumDetailReqIdl.Types.DataReq
            {
                Common = new CommonReq { ClientType = 2, ClientVersion = Const.MainVersion }, ForumId = fid
            }
        };
        return reqIdl.ToByteArray();
    }

    private static ForumDetail ParseBody(byte[] body)
    {
        var resProto = GetForumDetailResIdl.Parser.ParseFrom(body);
        ApiResponseValidator.CheckError(resProto.Error.Errorno, resProto.Error.Errmsg);

        var dataForum = resProto.Data;

        return Internal.Mapping.ForumDetailMapper.FromTbData(dataForum);
    }

    /// <summary>
    ///     发送获取贴吧详情信息请求
    /// </summary>
    /// <param name="fid">吧 ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>贴吧详情信息</returns>
    public async Task<ForumDetail> RequestAsync(long fid, CancellationToken cancellationToken = default)
    {
        var data = PackProto(fid);
        var requestUri =
            new UriBuilder("https", Const.AppBaseHost, 443, "/c/f/forum/getforumdetail") { Query = $"cmd={Cmd}" }.Uri;

        var result = await _httpCore.SendAppProtoAsync(requestUri, data, cancellationToken);
        return ParseBody(result);
    }
}
