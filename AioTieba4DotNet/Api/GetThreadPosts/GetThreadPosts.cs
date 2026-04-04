using System.Diagnostics.CodeAnalysis;
using AioTieba4DotNet.Internal;
using AioTieba4DotNet.Internal.Mapping;
using AioTieba4DotNet.Models.Threads;
using AioTieba4DotNet.Transport;
using Google.Protobuf;

namespace AioTieba4DotNet.Api.GetThreadPosts;

/// <summary>
///     获取主题帖内回复列表的 API (PB页)
/// </summary>
/// <param name="httpCore">Http 核心组件</param>
/// <param name="wsCore">Websocket 核心组件</param>
internal class GetThreadPosts(
    ITiebaHttpCore httpCore,
    ITiebaWsCore wsCore)
{
    private const int Cmd = 302001;

    [SuppressMessage("Major Code Smell", "S107:Methods should not have too many parameters",
        Justification =
            "The protobuf request packer mirrors the upstream thread-post options one-to-one so transport packing stays explicit and protocol-aligned.")]
    private static byte[] PackProto(long tid, int pn, int rn, int sort, bool onlyThreadAuthor, bool withComments,
        int commentRn, bool commentSortByAgree, string? bduss)
    {
        var request = new PbPageReqIdl
        {
            Data = new PbPageReqIdl.Types.DataReq
            {
                Common = new CommonReq
                {
                    ClientType = 2,
                    ClientVersion = Const.MainVersion,
                    BDUSS = withComments ? bduss ?? string.Empty : string.Empty
                },
                Kz = tid,
                Pn = pn,
                Rn = rn > 1 ? rn : 2,
                R = sort,
                Lz = onlyThreadAuthor ? 1 : 0,
                WithFloor = withComments ? 1 : 0,
                FloorRn = commentRn,
                FloorSortType = commentSortByAgree ? 1 : 0
            }
        };
        return request.ToByteArray();
    }

    private static Posts ParseBody(byte[] body)
    {
        var resProto = PbPageResIdl.Parser.ParseFrom(body);
        ApiResponseValidator.CheckError(resProto.Error.Errorno, resProto.Error.Errmsg);

        return PostsMapper.FromTbData(resProto.Data);
    }

    /// <summary>
    ///     通过 HTTP 获取主题帖内回复列表
    /// </summary>
    /// <param name="tid">主题帖 ID (tid)</param>
    /// <param name="pn">页码</param>
    /// <param name="rn">每页请求数量</param>
    /// <param name="sort">排序方式 (0:按回复时间, 1:按发布时间, 2:热门排序)</param>
    /// <param name="onlyThreadAuthor">是否只看楼主</param>
    /// <param name="withComments">是否包含楼中楼回复</param>
    /// <param name="commentRn">每层楼显示的楼中楼数量</param>
    /// <param name="commentSortByAgree">楼中楼是否按点赞数排序</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>回复列表实体</returns>
    [SuppressMessage("Major Code Smell", "S107:Methods should not have too many parameters",
        Justification =
            "The HTTP request surface intentionally matches the Tieba thread-post query options without introducing an extra abstraction layer.")]
    public async Task<Posts> RequestHttpAsync(long tid, int pn, int rn, int sort, bool onlyThreadAuthor,
        bool withComments, int commentRn, bool commentSortByAgree, CancellationToken cancellationToken = default)
    {
        var data = PackProto(tid, pn, rn, sort, onlyThreadAuthor, withComments, commentRn, commentSortByAgree,
            httpCore.Account?.Bduss);
        var requestUri = new UriBuilder("https", Const.AppBaseHost, 443, "/c/f/pb/page") { Query = $"cmd={Cmd}" }.Uri;

        var result = await httpCore.SendAppProtoAsync(requestUri, data, cancellationToken);
        return ParseBody(result);
    }

    /// <summary>
    ///     通过 Websocket 获取主题帖内回复列表
    /// </summary>
    /// <param name="tid">主题帖 ID (tid)</param>
    /// <param name="pn">页码</param>
    /// <param name="rn">每页请求数量</param>
    /// <param name="sort">排序方式 (0:按回复时间, 1:按发布时间, 2:热门排序)</param>
    /// <param name="onlyThreadAuthor">是否只看楼主</param>
    /// <param name="withComments">是否包含楼中楼回复</param>
    /// <param name="commentRn">每层楼显示的楼中楼数量</param>
    /// <param name="commentSortByAgree">楼中楼是否按点赞数排序</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>回复列表实体</returns>
    [SuppressMessage("Major Code Smell", "S107:Methods should not have too many parameters",
        Justification =
            "The WebSocket request surface intentionally matches the Tieba thread-post query options without introducing an extra abstraction layer.")]
    public async Task<Posts> RequestWsAsync(long tid, int pn, int rn, int sort, bool onlyThreadAuthor,
        bool withComments, int commentRn, bool commentSortByAgree, CancellationToken cancellationToken = default)
    {
        var data = PackProto(tid, pn, rn, sort, onlyThreadAuthor, withComments, commentRn, commentSortByAgree,
            wsCore.Account?.Bduss);
        var response = await wsCore.SendAsync(Cmd, data, cancellationToken: cancellationToken);
        return ParseBody(response.Payload.Data.ToByteArray());
    }
}
