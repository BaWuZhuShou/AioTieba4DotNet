using AioTieba4DotNet.Abstractions;
using AioTieba4DotNet.Api.GetThreadPosts.Entities;
using AioTieba4DotNet.Core;
using AioTieba4DotNet.Exceptions;
using AioTieba4DotNet.Enums;
using Google.Protobuf;

namespace AioTieba4DotNet.Api.GetThreadPosts;

/// <summary>
/// 获取帖子回复列表
/// </summary>
/// <param name="httpCore"></param>
/// <param name="wsCore"></param>
/// <param name="mode"></param>
public class GetThreadPosts(ITiebaHttpCore httpCore, ITiebaWsCore wsCore, TiebaRequestMode mode = TiebaRequestMode.Http)
    : ProtoApiWsBase<Posts>(httpCore, wsCore, mode)
{
    private const int Cmd = 302001;

    private static byte[] PackProto(long tid, int pn, int rn, int sort, bool onlyThreadAuthor, bool withComments, int commentRn, bool commentSortByAgree, string? bduss)
    {
        var request = new PbPageReqIdl
        {
            Data = new PbPageReqIdl.Types.DataReq
            {
                Common = new CommonReq
                {
                    ClientType = 2,
                    ClientVersion = Const.MainVersion,
                    BDUSS = withComments ? (bduss ?? string.Empty) : string.Empty
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
        CheckError(resProto.Error.Errorno, resProto.Error.Errmsg);

        return Posts.FromTbData(resProto.Data);
    }

    /// <summary>
    /// 异步请求
    /// </summary>
    /// <param name="tid">主题帖tid</param>
    /// <param name="pn">页码</param>
    /// <param name="rn">每页条数</param>
    /// <param name="sort">排序方式 0按回复时间 1按发布时间 2热门排序</param>
    /// <param name="onlyThreadAuthor">是否只看楼主</param>
    /// <param name="withComments">是否包含楼中楼</param>
    /// <param name="commentRn">楼中楼显示数量</param>
    /// <param name="commentSortByAgree">楼中楼是否按点赞数排序</param>
    /// <returns></returns>
    public async Task<Posts> RequestAsync(long tid, int pn, int rn, int sort, bool onlyThreadAuthor, bool withComments, int commentRn, bool commentSortByAgree)
    {
        return await ExecuteAsync(
            () => RequestHttpAsync(tid, pn, rn, sort, onlyThreadAuthor, withComments, commentRn, commentSortByAgree),
            () => RequestWsAsync(tid, pn, rn, sort, onlyThreadAuthor, withComments, commentRn, commentSortByAgree)
        );
    }

    public async Task<Posts> RequestHttpAsync(long tid, int pn, int rn, int sort, bool onlyThreadAuthor, bool withComments, int commentRn, bool commentSortByAgree)
    {
        var data = PackProto(tid, pn, rn, sort, onlyThreadAuthor, withComments, commentRn, commentSortByAgree, HttpCore.Account?.Bduss);
        var requestUri = new UriBuilder("https", Const.AppBaseHost, 443, "/c/f/pb/page")
        {
            Query = $"cmd={Cmd}"
        }.Uri;

        var responseMessage = await HttpCore.PackProtoRequestAsync(requestUri, data);
        var result = await responseMessage.Content.ReadAsByteArrayAsync();
        return ParseBody(result);
    }

    public async Task<Posts> RequestWsAsync(long tid, int pn, int rn, int sort, bool onlyThreadAuthor, bool withComments, int commentRn, bool commentSortByAgree)
    {
        var data = PackProto(tid, pn, rn, sort, onlyThreadAuthor, withComments, commentRn, commentSortByAgree, WsCore.Account?.Bduss);
        var response = await WsCore.SendAsync(Cmd, data);
        return ParseBody(response.Payload.Data.ToByteArray());
    }
}
