using AioTieba4DotNet.Abstractions;
using AioTieba4DotNet.Attributes;
using AioTieba4DotNet.Core;
using AioTieba4DotNet.Exceptions;
using AioTieba4DotNet.Enums;
using Google.Protobuf;

namespace AioTieba4DotNet.Api.AddPost;

/// <summary>
/// 发布回复 (Post) 的 API
/// </summary>
/// <param name="httpCore">Http 核心组件</param>
/// <param name="wsCore">Websocket 核心组件</param>
/// <param name="mode">请求模式</param>
[RequireBduss]
[PythonApi("aiotieba.api.add_post")]
public class AddPost(ITiebaHttpCore httpCore, ITiebaWsCore wsCore, TiebaRequestMode mode = TiebaRequestMode.Http)
    : ProtoApiWsBase<bool>(httpCore, wsCore, mode)
{
    private const int Cmd = 309731;

    private static AddPostReqIdl PackProto(Account account, string fname, ulong fid, long tid, string showName,
        string content)
    {
        var currentTs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var currentDt = DateTimeOffset.UtcNow;

        return new AddPostReqIdl
        {
            Data = new AddPostReqIdl.Types.DataReq
            {
                Common = new CommonReq
                {
                    BDUSS = account.Bduss,
                    ClientType = 2,
                    ClientVersion = Const.PostVersion,
                    ClientId = account.ClientId ?? "",
                    PhoneImei = "000000000000000",
                    From = "1008621x",
                    Cuid = account.CuidGalaxy2,
                    Timestamp = currentTs,
                    Model = "SM-G988N",
                    Tbs = account.Tbs ?? "",
                    NetType = 1,
                    Pversion = "1.0.3",
                    OsVersion = "9",
                    Brand = "samsung",
                    LegoLibVersion = "3.0.0",
                    Applist = "",
                    Stoken = account.Stoken,
                    ZId = account.ZId ?? "",
                    CuidGalaxy2 = account.CuidGalaxy2,
                    CuidGid = "",
                    C3Aid = account.C3Aid ?? "",
                    SampleId = account.SampleId ?? "",
                    ScrW = 720,
                    ScrH = 1280,
                    ScrDip = 1.5,
                    QType = 0,
                    IsTeenager = 0,
                    SdkVer = "2.34.0",
                    FrameworkVer = "3340042",
                    NawsGameVer = "1038000",
                    ActiveTimestamp = currentTs - 86400000L * 30,
                    FirstInstallTime = currentTs - 86400000L * 30,
                    LastUpdateTime = currentTs - 86400000L * 30,
                    EventDay = $"{currentDt.Year}{currentDt.Month:D2}{currentDt.Day:D2}",
                    AndroidId = account.AndroidId ?? "",
                    Cmode = 1,
                    StartScheme = "",
                    StartType = 1,
                    Idfv = "0",
                    Extra = "",
                    UserAgent = $"AioTieba4DotNet/{Const.Version}",
                    PersonalizedRecSwitch = 1,
                    DeviceScore = "0.4"
                },
                Anonymous = "1",
                CanNoForum = "0",
                IsFeedback = "0",
                TakephotoNum = "0",
                EntranceType = "0",
                VcodeTag = "12",
                NewVcode = "1",
                Content = content,
                Fid = fid.ToString(),
                VFid = "",
                VFname = "",
                Kw = fname,
                IsBarrage = "0",
                FromFourmId = fid.ToString(),
                Tid = tid.ToString(),
                IsAd = "0",
                PostFrom = "3",
                NameShow = showName,
                IsPictxt = "0",
                ShowCustomFigure = 0,
                IsShowBless = 0
            }
        };
    }

    private static bool ParseBody(byte[] body)
    {
        var resProto = AddPostResIdl.Parser.ParseFrom(body);
        CheckError(resProto.Error.Errorno, resProto.Error.Errmsg);

        if (resProto.Data?.Info?.NeedVcode == "1") throw new TiebaException("Need verify code");
        return true;
    }


    /// <summary>
    /// 发送发布回复请求
    /// </summary>
    /// <param name="fname">吧名</param>
    /// <param name="fid">吧 ID</param>
    /// <param name="tid">主题帖 ID</param>
    /// <param name="content">回复内容</param>
    /// <param name="showName">显示名称（可选）</param>
    /// <returns>是否成功</returns>
    public async Task<bool> RequestAsync(string fname, ulong fid, long tid, string content,
        string? showName = null)
    {
        return await ExecuteAsync(
            () => RequestHttpAsync(fname, fid, tid, content, showName),
            () => RequestWsAsync(fname, fid, tid, content, showName)
        );
    }

    public async Task<bool> RequestHttpAsync(string fname, ulong fid, long tid, string content,
        string? showName = null)
    {
        var account = HttpCore.Account!;
        var reqProto = PackProto(account, fname, fid, tid, showName ?? "", content);
        var data = reqProto.ToByteArray();

        var requestUri = new UriBuilder("http", Const.AppBaseHost, 80, "/c/c/post/add") { Query = $"cmd={Cmd}" }.Uri;

        var result = await HttpCore.SendAppProtoAsync(requestUri, data);
        return ParseBody(result);
    }

    public async Task<bool> RequestWsAsync(string fname, ulong fid, long tid, string content,
        string? showName = null)
    {
        var account = HttpCore.Account!;
        var reqProto = PackProto(account, fname, fid, tid, showName ?? "", content);
        var data = reqProto.ToByteArray();

        var response = await WsCore.SendAsync(Cmd, data);
        return ParseBody(response.Payload.Data.ToByteArray());
    }
}
