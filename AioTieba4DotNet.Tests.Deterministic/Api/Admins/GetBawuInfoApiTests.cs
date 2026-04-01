#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using AioTieba4DotNet.Api.GetBawuInfo;
using AioTieba4DotNet.Internal;
using AioTieba4DotNet.Session;
using AioTieba4DotNet.Transport;
using Google.Protobuf;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AioTieba4DotNet.Tests.Api.Admins;

[TestClass]
public sealed class GetBawuInfoApiTests
{
    private const ulong SafeForumId = 7356044;

    [TestMethod]
    public async Task RequestHttpAsync_PacksExpectedProtoAndMapsBawuRoles()
    {
        var httpCore = new RecordingHttpCore
        {
            AppProtoResponse = CreateResponseBytes()
        };
        var api = new GetBawuInfo(httpCore, new StubWsCore());
        using var cts = new CancellationTokenSource();

        var result = await api.RequestHttpAsync(SafeForumId, cts.Token);

        Assert.AreEqual("/c/f/forum/getBawuInfo", httpCore.LastAppProtoUri?.AbsolutePath);
        Assert.AreEqual("cmd=301007", httpCore.LastAppProtoUri?.Query.TrimStart('?'));
        Assert.AreEqual(cts.Token, httpCore.LastAppProtoCancellationToken);

        var request = GetBawuInfoReqIdl.Parser.ParseFrom(httpCore.LastAppProtoData);
        Assert.AreEqual((long)SafeForumId, (long)request.Data.Fid);
        Assert.AreEqual(Const.MainVersion, request.Data.Common.ClientVersion);

        Assert.AreEqual(1, result.Admins.Count);
        Assert.AreEqual(1, result.Managers.Count);
        Assert.AreEqual(2, result.All.Count);
        Assert.AreEqual("admin-user", result.Admins[0].UserName);
        Assert.AreEqual("manager-user", result.Managers[0].UserName);
    }

    private static byte[] CreateResponseBytes() => new GetBawuInfoResIdl
    {
        Error = new Error { Errorno = 0, Errmsg = string.Empty },
        Data = new GetBawuInfoResIdl.Types.DataRes
        {
            BawuTeamInfo = new GetBawuInfoResIdl.Types.DataRes.Types.BawuTeam
            {
                TotalNum = 2,
                BawuTeamList =
                {
                    new GetBawuInfoResIdl.Types.DataRes.Types.BawuTeam.Types.BawuRoleDes
                    {
                        RoleName = "吧主",
                        RoleInfo =
                        {
                            new GetBawuInfoResIdl.Types.DataRes.Types.BawuTeam.Types.BawuRoleDes.Types.BawuRoleInfoPub
                            {
                                UserId = 1,
                                Portrait = "tb.1.admin",
                                UserName = "admin-user",
                                NameShow = "Admin",
                                UserLevel = 18
                            }
                        }
                    },
                    new GetBawuInfoResIdl.Types.DataRes.Types.BawuTeam.Types.BawuRoleDes
                    {
                        RoleName = "小吧主",
                        RoleInfo =
                        {
                            new GetBawuInfoResIdl.Types.DataRes.Types.BawuTeam.Types.BawuRoleDes.Types.BawuRoleInfoPub
                            {
                                UserId = 2,
                                Portrait = "tb.1.manager",
                                UserName = "manager-user",
                                NameShow = "Manager",
                                UserLevel = 12
                            }
                        }
                    }
                }
            }
        }
    }.ToByteArray();

    private sealed class RecordingHttpCore : ITiebaHttpCore
    {
        public byte[] AppProtoResponse { get; init; } = [];

        public Account? Account { get; } = new();

        public HttpClient HttpClient { get; } = new();

        public Uri? LastAppProtoUri { get; private set; }

        public byte[] LastAppProtoData { get; private set; } = [];

        public CancellationToken LastAppProtoCancellationToken { get; private set; }

        public void SetAccount(Account newAccount) => throw new NotImplementedException();

        public Task<string> SendAsync(Func<HttpRequestMessage> requestFactory, bool allowRetry = false,
            CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public Task<string> SendAppFormAsync(Uri uri, List<KeyValuePair<string, string>> data,
            CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public Task<byte[]> SendAppProtoAsync(Uri uri, byte[] data, CancellationToken cancellationToken = default)
        {
            LastAppProtoUri = uri;
            LastAppProtoData = data.ToArray();
            LastAppProtoCancellationToken = cancellationToken;
            return Task.FromResult(AppProtoResponse);
        }

        public Task<string> SendWebGetAsync(Uri uri, List<KeyValuePair<string, string>> parameters,
            CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public Task<string> SendWebFormAsync(Uri uri, List<KeyValuePair<string, string>> data,
            CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }

    private sealed class StubWsCore : ITiebaWsCore
    {
        public Account? Account { get; } = new();

        public void SetAccount(Account newAccount) => throw new NotImplementedException();

        public Task ConnectAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public Task SendAsync(WSReq req, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<WSRes> SendAsync(int cmd, byte[] data, bool encrypt = true,
            CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public async IAsyncEnumerable<WSRes> ListenAsync(
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.CompletedTask;
            yield break;
        }

        public Task CloseAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
