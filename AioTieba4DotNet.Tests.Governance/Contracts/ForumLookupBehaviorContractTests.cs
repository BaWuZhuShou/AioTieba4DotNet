#nullable enable
using System;
using System.Threading;
using System.Threading.Tasks;
using AioTieba4DotNet.Tests.Platform.Contracts;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static AioTieba4DotNet.Tests.Governance.Contracts.ForumLookupTestTransport;

namespace AioTieba4DotNet.Tests.Governance.Contracts;

[TestClass]
[TestCategory(OnlineTestContractCategories.Architecture)]
public sealed class ForumLookupBehaviorContractTests
{
    private const string ForumName = "lookup-forum";
    private const ulong ForumId = 101;
    private const ulong OtherForumId = 202;
    private static readonly TimeSpan BarrierTimeout = TimeSpan.FromSeconds(10);

    [TestMethod]
    public async Task GetFidColdThenHotSharesBothDirections()
    {
        using var transport = new ForumLookupTestTransport();
        using var client = transport.CreateTiebaClient();
        transport.ExpectFid(ForumName, ForumId);

        Assert.AreEqual(ForumId, await client.Forums.GetFidAsync(ForumName));
        Assert.AreEqual(ForumId, await client.Forums.GetFidAsync(ForumName));
        Assert.AreEqual(ForumName, await client.Forums.GetFnameAsync(ForumId));

        transport.AssertComplete(FidPath);
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("  ")]
    public async Task GetFidBlankNamesAreSentButSuccessfulIdsAreNotCached(string? name)
    {
        using var transport = new ForumLookupTestTransport();
        using var client = transport.CreateTiebaClient();
        transport.ExpectFid(name ?? "", ForumId);
        transport.ExpectFid(name ?? "", OtherForumId);
        transport.ExpectDetail(ForumId, ForumId, ForumName);

        Assert.AreEqual(ForumId, await client.Forums.GetFidAsync(name!));
        Assert.AreEqual(OtherForumId, await client.Forums.GetFidAsync(name!));
        Assert.AreEqual(ForumName, await client.Forums.GetFnameAsync(ForumId));

        transport.AssertComplete(FidPath, FidPath, DetailPath);
    }

    [TestMethod]
    [DataRow("{\"no\":0,\"data\":{\"fid\":0}}", -1, "fid is 0!")]
    [DataRow("{\"no\":403,\"error\":\"lookup denied\"}", 403, "lookup denied")]
    public async Task GetFidFailurePreservesServerErrorAndDoesNotCache(string response, int code, string message)
    {
        using var transport = new ForumLookupTestTransport();
        using var client = transport.CreateTiebaClient();
        transport.ExpectJson(FidPath, response);
        transport.ExpectFid(ForumName, ForumId);

        var error = await Assert.ThrowsExactlyAsync<TieBaServerException>(
            () => client.Forums.GetFidAsync(ForumName));
        Assert.AreEqual(code, error.Code);
        Assert.AreEqual($"Code: {code}, Message: {message}", error.Message);
        Assert.AreEqual(ForumId, await client.Forums.GetFidAsync(ForumName));
        Assert.AreEqual(ForumName, await client.Forums.GetFnameAsync(ForumId));

        transport.AssertComplete(FidPath, FidPath);
    }

    [TestMethod]
    public async Task GetFnameWritesResponseIdThenRequestIdIncludingReverseMapping()
    {
        using var transport = new ForumLookupTestTransport();
        using var client = transport.CreateTiebaClient();
        transport.ExpectDetail(ForumId, OtherForumId, ForumName);

        Assert.AreEqual(ForumName, await client.Forums.GetFnameAsync(ForumId));
        Assert.AreEqual(ForumName, await client.Forums.GetFnameAsync(OtherForumId));
        Assert.AreEqual(ForumName, await client.Forums.GetFnameAsync(ForumId));
        Assert.AreEqual(ForumId, await client.Forums.GetFidAsync(ForumName),
            "The second write must keep the requested ID as the reverse mapping.");

        transport.AssertComplete(DetailPath);
    }

    [TestMethod]
    [DataRow(0L, 202L)]
    [DataRow(101L, 0L)]
    public async Task GetFnameUnconditionallyWritesRequestedIdEvenWhenAnIdIsZero(long requestedId, long returnedId)
    {
        using var transport = new ForumLookupTestTransport();
        using var client = transport.CreateTiebaClient();
        transport.ExpectDetail((ulong)requestedId, (ulong)returnedId, ForumName);

        Assert.AreEqual(ForumName, await client.Forums.GetFnameAsync((ulong)requestedId));
        Assert.AreEqual(ForumName, await client.Forums.GetFnameAsync((ulong)requestedId));
        if (requestedId != 0)
            Assert.AreEqual((ulong)requestedId, await client.Forums.GetFidAsync(ForumName));
        else
            Assert.AreEqual(ForumName, await client.Forums.GetFnameAsync((ulong)returnedId));

        transport.AssertComplete(DetailPath);
    }

    [TestMethod]
    public async Task GetFnameWhitespaceIsAHitButResponseIdIsNotRemembered()
    {
        using var transport = new ForumLookupTestTransport();
        using var client = transport.CreateTiebaClient();
        transport.ExpectDetail(ForumId, OtherForumId, "  ");
        transport.ExpectDetail(OtherForumId, OtherForumId, ForumName);

        Assert.AreEqual("  ", await client.Forums.GetFnameAsync(ForumId));
        Assert.AreEqual("  ", await client.Forums.GetFnameAsync(ForumId));
        Assert.AreEqual(ForumId, await client.Forums.GetFidAsync("  "));
        Assert.AreEqual(ForumName, await client.Forums.GetFnameAsync(OtherForumId));

        transport.AssertComplete(DetailPath, DetailPath);
    }

    [TestMethod]
    public async Task GetFnameEmptyNameWritesReverseMappingButRemainsANameCacheMiss()
    {
        using var transport = new ForumLookupTestTransport();
        using var client = transport.CreateTiebaClient();
        transport.ExpectDetail(ForumId, OtherForumId, "");
        transport.ExpectDetail(ForumId, OtherForumId, ForumName);

        Assert.AreEqual("", await client.Forums.GetFnameAsync(ForumId));
        Assert.AreEqual(ForumId, await client.Forums.GetFidAsync(""));
        Assert.AreEqual(ForumName, await client.Forums.GetFnameAsync(ForumId));

        transport.AssertComplete(DetailPath, DetailPath);
    }

    [TestMethod]
    public async Task GetDetailByNameUsesIdLookupAndRemembersResponseIdentity()
    {
        using var transport = new ForumLookupTestTransport();
        using var client = transport.CreateTiebaClient();
        transport.ExpectFid("requested-name", ForumId);
        transport.ExpectDetail(ForumId, OtherForumId, ForumName);

        var detail = await client.Forums.GetDetailAsync("requested-name");
        Assert.AreEqual(OtherForumId, detail.Fid);
        Assert.AreEqual(ForumName, detail.Fname);
        Assert.AreEqual(OtherForumId, await client.Forums.GetFidAsync(ForumName));
        Assert.AreEqual(ForumName, await client.Forums.GetFnameAsync(OtherForumId));
        Assert.AreEqual("requested-name", await client.Forums.GetFnameAsync(ForumId));

        transport.AssertComplete(FidPath, DetailPath);
    }

    [TestMethod]
    [DataRow(101L, "")]
    [DataRow(101L, "  ")]
    [DataRow(0L, ForumName)]
    public async Task GetDetailDoesNotRememberZeroIdsOrBlankNames(long returnedId, string returnedName)
    {
        using var transport = new ForumLookupTestTransport();
        using var client = transport.CreateTiebaClient();
        transport.ExpectDetail(ForumId, (ulong)returnedId, returnedName);
        transport.ExpectFid(returnedName, OtherForumId);

        var detail = await client.Forums.GetDetailAsync(ForumId);
        Assert.AreEqual((ulong)returnedId, detail.Fid);
        Assert.AreEqual(returnedName, detail.Fname);
        Assert.AreEqual(OtherForumId, await client.Forums.GetFidAsync(returnedName));

        transport.AssertComplete(DetailPath, FidPath);
    }

    [TestMethod]
    public async Task GetFnameFailureDoesNotPublishEitherMapping()
    {
        using var transport = new ForumLookupTestTransport();
        using var client = transport.CreateTiebaClient();
        transport.ExpectDetail(ForumId, OtherForumId, ForumName, 409, "detail denied");
        transport.ExpectDetail(OtherForumId, OtherForumId, "retry-name");
        transport.ExpectDetail(ForumId, OtherForumId, ForumName);

        var error = await Assert.ThrowsExactlyAsync<TieBaServerException>(
            () => client.Forums.GetFnameAsync(ForumId));
        Assert.AreEqual(409, error.Code);
        Assert.AreEqual("Code: 409, Message: detail denied", error.Message);
        Assert.AreEqual("retry-name", await client.Forums.GetFnameAsync(OtherForumId));
        Assert.AreEqual(ForumName, await client.Forums.GetFnameAsync(ForumId));
        Assert.AreEqual(ForumName, await client.Forums.GetFnameAsync(OtherForumId));
        Assert.AreEqual(ForumId, await client.Forums.GetFidAsync(ForumName));

        transport.AssertComplete(DetailPath, DetailPath, DetailPath);
    }

    [TestMethod]
    [DataRow(101L, null)]
    [DataRow(101L, "")]
    [DataRow(101L, "  ")]
    [DataRow(0L, ForumName)]
    public async Task GetForumNullOrBlankNamesAndZeroIdsDoNotPopulateIdentityCache(long id, string? name)
    {
        using var transport = new ForumLookupTestTransport();
        using var client = transport.CreateTiebaClient();
        transport.ExpectForum("requested-name", (ulong)id, name);
        transport.ExpectFid(name ?? "", OtherForumId);

        var forum = await client.Forums.GetForumAsync("requested-name");
        Assert.AreEqual(id, forum.Fid);
        Assert.AreEqual(name ?? "", forum.Fname);
        Assert.AreEqual(OtherForumId, await client.Forums.GetFidAsync(name ?? ""));

        transport.AssertComplete(ForumPath, FidPath);
    }

    [TestMethod]
    public async Task GetForumCacheIsSharedByForumUserThreadAndAdminModules()
    {
        using var transport = new ForumLookupTestTransport();
        using var client = transport.CreateTiebaClient(true);
        transport.ExpectForum("requested-name", ForumId, ForumName);
        transport.ExpectJson(UserForumPath,
            "{\"error_code\":0,\"data\":{\"forum_info\":{\"forum_name\":\"lookup-forum\"}}}",
            fields => Assert.AreEqual("101", fields["forum_id"]));
        transport.ExpectJson(LoginPath, LoginBody);
        ExpectGood(transport, "0");
        ExpectBlock(transport, "101");

        var forum = await client.Forums.GetForumAsync("requested-name");
        Assert.AreEqual((long)ForumId, forum.Fid);
        Assert.AreEqual(ForumId, await client.Forums.GetFidAsync(ForumName));
        Assert.AreEqual(ForumName, await client.Forums.GetFnameAsync(ForumId));
        var user = await client.Users.GetUserForumInfoAsync(ForumName, "portrait");
        Assert.AreEqual(ForumName, user.Fname);
        Assert.IsTrue(await client.Threads.GoodAsync(ForumName, 303, ""));
        Assert.IsTrue(await client.Admins.BlockAsync(ForumName, "portrait", 1, "reason"));

        transport.AssertComplete(ForumPath, UserForumPath, LoginPath, GoodPath, BlockPath);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task AlreadyCanceledTokenWinsForColdAndHotLookups(bool warmCache)
    {
        using var transport = new ForumLookupTestTransport();
        using var client = transport.CreateTiebaClient();
        if (warmCache)
        {
            transport.ExpectFid(ForumName, ForumId);
            Assert.AreEqual(ForumId, await client.Forums.GetFidAsync(ForumName));
        }
        using var source = new CancellationTokenSource();
        source.Cancel();
        Func<Task>[] operations =
        [
            () => client.Forums.GetFidAsync(ForumName, source.Token),
            () => client.Forums.GetFnameAsync(ForumId, source.Token),
            () => client.Forums.GetDetailAsync(ForumId, source.Token),
            () => client.Forums.GetForumAsync(ForumName, source.Token),
            () => client.Forums.GetCidAsync("", "", source.Token),
            () => client.Admins.BlockAsync("", "", 0, "", source.Token),
            () => client.Threads.GoodAsync(ForumName, 303, "", source.Token),
            () => client.Users.GetUserForumInfoAsync(ForumName, "portrait", source.Token)
        ];
        foreach (var operation in operations)
        {
            var error = await Assert.ThrowsExactlyAsync<OperationCanceledException>(operation);
            Assert.AreEqual(source.Token, error.CancellationToken);
        }

        transport.AssertComplete(warmCache ? [FidPath] : []);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task CancellationDuringLookupRemainsCancellationAndNextCallLoadsAgain(bool lookupName)
    {
        using var transport = new ForumLookupTestTransport();
        using var client = transport.CreateTiebaClient();
        using var source = new CancellationTokenSource();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var path = lookupName ? DetailPath : FidPath;
        transport.Expect(path, async (_, ct) =>
        {
            entered.SetResult();
            await Task.Delay(Timeout.InfiniteTimeSpan, ct);
            throw new AssertFailedException("Canceled transport must not produce a response.");
        });
        if (lookupName)
            transport.ExpectDetail(ForumId, OtherForumId, ForumName);
        else
            transport.ExpectFid(ForumName, ForumId);

        Task pending = lookupName
            ? client.Forums.GetFnameAsync(ForumId, source.Token)
            : client.Forums.GetFidAsync(ForumName, source.Token);
        try
        {
            await entered.Task.WaitAsync(BarrierTimeout);
        }
        finally
        {
            source.Cancel();
        }
        await Assert.ThrowsAsync<OperationCanceledException>(() => pending.WaitAsync(BarrierTimeout));
        Assert.IsTrue(pending.IsCanceled);
        if (lookupName)
        {
            Assert.AreEqual(ForumName, await client.Forums.GetFnameAsync(ForumId));
            Assert.AreEqual(ForumName, await client.Forums.GetFnameAsync(OtherForumId));
        }
        else
            Assert.AreEqual(ForumId, await client.Forums.GetFidAsync(ForumName));

        transport.AssertComplete(path, path);
    }

    [TestMethod]
    public async Task AdminColdCacheAuthenticatesThenLoadsTbsAndIdBeforeWriteAndWarmsSharedCache()
    {
        using var transport = new ForumLookupTestTransport();
        using var client = transport.CreateTiebaClient(true);
        transport.ExpectJson(LoginPath, LoginBody);
        transport.ExpectFid(ForumName, ForumId);
        ExpectBlock(transport, "101");
        ExpectBlock(transport, "101");

        Assert.IsTrue(await client.Admins.BlockAsync(ForumName, "portrait", 1, "reason"));
        Assert.AreEqual(ForumId, await client.Forums.GetFidAsync(ForumName));
        Assert.AreEqual(ForumName, await client.Forums.GetFnameAsync(ForumId));
        Assert.IsTrue(await client.Admins.BlockAsync(ForumName, "portrait", 1, "reason"));

        transport.AssertComplete(LoginPath, FidPath, BlockPath, BlockPath);
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task AdminValidInputRequiresBusinessAuthenticationForBothColdAndHotCache(bool warmCache)
    {
        using var transport = new ForumLookupTestTransport();
        using var client = transport.CreateTiebaClient();
        if (warmCache)
        {
            transport.ExpectFid(ForumName, ForumId);
            Assert.AreEqual(ForumId, await client.Forums.GetFidAsync(ForumName));
        }

        var error = await Assert.ThrowsExactlyAsync<TiebaAuthenticationException>(
            () => client.Admins.BlockAsync(ForumName, "portrait", 1, "reason"));
        Assert.AreEqual("Operation 'BlockAsync' requires an authenticated session with BDUSS.", error.Message);

        transport.AssertComplete(warmCache ? [FidPath] : []);
    }

    [TestMethod]
    public async Task AdminValidationPrecedenceDependsOnWhetherIdentityIsAlreadyCached()
    {
        using var transport = new ForumLookupTestTransport();
        using var client = transport.CreateTiebaClient();
        var forumError = await Assert.ThrowsExactlyAsync<ArgumentException>(
            () => client.Admins.BlockAsync("", "", 0, ""));
        Assert.AreEqual("fname", forumError.ParamName);
        var authError = await Assert.ThrowsExactlyAsync<TiebaAuthenticationException>(
            () => client.Admins.BlockAsync(ForumName, "", 0, ""));
        Assert.AreEqual("Operation 'BlockAsync' requires an authenticated session with BDUSS.", authError.Message);

        transport.ExpectFid(ForumName, ForumId);
        Assert.AreEqual(ForumId, await client.Forums.GetFidAsync(ForumName));
        var portraitError = await Assert.ThrowsExactlyAsync<ArgumentException>(
            () => client.Admins.BlockAsync(ForumName, "", 0, ""));
        Assert.AreEqual("portrait", portraitError.ParamName);
        var dayError = await Assert.ThrowsExactlyAsync<ArgumentOutOfRangeException>(
            () => client.Admins.BlockAsync(ForumName, "portrait", 0, ""));
        Assert.AreEqual("day", dayError.ParamName);
        Assert.AreEqual(0, dayError.ActualValue);

        transport.AssertComplete(FidPath);
    }

    [TestMethod]
    public async Task AdminEmptyTbsFailurePreservesConfigurationErrorAndDoesNotQueryIdentity()
    {
        using var transport = new ForumLookupTestTransport();
        using var client = transport.CreateTiebaClient(true);
        transport.ExpectJson(LoginPath, "{\"error_code\":0,\"user\":{},\"anti\":{\"tbs\":\"\"}}");
        transport.ExpectJson(LoginPath, LoginBody);
        transport.ExpectFid(ForumName, ForumId);
        ExpectBlock(transport, "101");

        var error = await Assert.ThrowsExactlyAsync<TiebaConfigurationException>(
            () => client.Admins.BlockAsync(ForumName, "portrait", 1, "reason"));
        Assert.AreEqual("TBS initialization returned an empty value.", error.Message);
        Assert.IsTrue(await client.Admins.BlockAsync(ForumName, "portrait", 1, "reason"));

        transport.AssertComplete(LoginPath, LoginPath, FidPath, BlockPath);
    }

    [TestMethod]
    public async Task AdminIdentityFailureDoesNotWriteAndRetryReusesOnlySuccessfulTbs()
    {
        using var transport = new ForumLookupTestTransport();
        using var client = transport.CreateTiebaClient(true);
        transport.ExpectJson(LoginPath, LoginBody);
        transport.ExpectJson(FidPath, "{\"no\":409,\"error\":\"lookup denied\"}");
        transport.ExpectFid(ForumName, ForumId);
        ExpectBlock(transport, "101");

        var error = await Assert.ThrowsExactlyAsync<TieBaServerException>(
            () => client.Admins.BlockAsync(ForumName, "portrait", 1, "reason"));
        Assert.AreEqual(409, error.Code);
        Assert.AreEqual("Code: 409, Message: lookup denied", error.Message);
        Assert.IsTrue(await client.Admins.BlockAsync(ForumName, "portrait", 1, "reason"));

        transport.AssertComplete(LoginPath, FidPath, FidPath, BlockPath);
    }

    [TestMethod]
    public async Task AdminStillQueriesIdentityWhenAnotherLookupWarmsCacheDuringTbsPreparation()
    {
        using var transport = new ForumLookupTestTransport();
        using var client = transport.CreateTiebaClient(true);
        var loginEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseLogin = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        transport.Expect(LoginPath, async (_, ct) =>
        {
            loginEntered.SetResult();
            await releaseLogin.Task.WaitAsync(ct);
            return JsonResponse(LoginBody);
        });
        transport.ExpectFid(ForumName, ForumId);
        transport.ExpectFid(ForumName, OtherForumId);
        ExpectBlock(transport, "202");

        var pending = client.Admins.BlockAsync(ForumName, "portrait", 1, "reason");
        try
        {
            await loginEntered.Task.WaitAsync(BarrierTimeout);
            Assert.AreEqual(ForumId, await client.Forums.GetFidAsync(ForumName));
            Assert.AreEqual(ForumName, await client.Forums.GetFnameAsync(ForumId));
        }
        finally
        {
            releaseLogin.TrySetResult();
        }
        Assert.IsTrue(await pending.WaitAsync(BarrierTimeout));
        Assert.AreEqual(OtherForumId, await client.Forums.GetFidAsync(ForumName));
        Assert.AreEqual(ForumName, await client.Forums.GetFnameAsync(OtherForumId));

        transport.AssertComplete(LoginPath, FidPath, FidPath, BlockPath);
    }

    [TestMethod]
    [DataRow("category", "7")]
    [DataRow("", "0")]
    [DataRow("  ", "0")]
    public async Task ThreadGoodPreparesTbsThenIdentityThenOptionalCategoryBeforeWriting(string category, string cid)
    {
        using var transport = new ForumLookupTestTransport();
        using var client = transport.CreateTiebaClient(true);
        transport.ExpectJson(LoginPath, LoginBody);
        transport.ExpectFid(ForumName, ForumId);
        if (!string.IsNullOrWhiteSpace(category))
            transport.ExpectJson(CategoriesPath,
                "{\"error_code\":0,\"cates\":[{\"class_name\":\"category\",\"class_id\":7}]}",
                fields => Assert.AreEqual(ForumName, fields["word"]));
        ExpectGood(transport, cid);

        Assert.IsTrue(await client.Threads.GoodAsync(ForumName, 303, category));
        Assert.AreEqual(ForumId, await client.Forums.GetFidAsync(ForumName));

        transport.AssertComplete(string.IsNullOrWhiteSpace(category)
            ? [LoginPath, FidPath, GoodPath]
            : [LoginPath, FidPath, CategoriesPath, GoodPath]);
    }

    [TestMethod]
    public async Task ThreadGoodCategoryFailurePreservesServerErrorAndDoesNotWrite()
    {
        using var transport = new ForumLookupTestTransport();
        using var client = transport.CreateTiebaClient(true);
        transport.ExpectJson(LoginPath, LoginBody);
        transport.ExpectFid(ForumName, ForumId);
        transport.ExpectJson(CategoriesPath, "{\"error_code\":401,\"error_msg\":\"category denied\"}");

        var error = await Assert.ThrowsExactlyAsync<TieBaServerException>(
            () => client.Threads.GoodAsync(ForumName, 303, "category"));
        Assert.AreEqual(401, error.Code);
        Assert.AreEqual("Code: 401, Message: category denied", error.Message);
        Assert.AreEqual(ForumId, await client.Forums.GetFidAsync(ForumName));

        transport.AssertComplete(LoginPath, FidPath, CategoriesPath);
    }

    [TestMethod]
    public async Task ThreadGoodAuthenticationFailurePrecedesIdentityAndCategory()
    {
        using var transport = new ForumLookupTestTransport();
        using var client = transport.CreateTiebaClient();

        var error = await Assert.ThrowsExactlyAsync<TiebaAuthenticationException>(
            () => client.Threads.GoodAsync(ForumName, 303, "category"));
        Assert.AreEqual("Operation 'GoodAsync' requires an authenticated session with BDUSS.", error.Message);

        transport.AssertComplete();
    }

    [TestMethod]
    public async Task EmptyCategoryShortCircuitsBeforeForumValidationOrAuthentication()
    {
        using var transport = new ForumLookupTestTransport();
        using var client = transport.CreateTiebaClient();

        Assert.AreEqual(0, await client.Forums.GetCidAsync("", "  "));
        Assert.AreEqual(0, await client.Forums.GetCidAsync(0UL, ""));
        var inputError = await Assert.ThrowsExactlyAsync<ArgumentException>(
            () => client.Forums.GetCidAsync("", "category"));
        Assert.AreEqual("fname", inputError.ParamName);
        var authError = await Assert.ThrowsExactlyAsync<TiebaAuthenticationException>(
            () => client.Forums.GetCidAsync(ForumName, "category"));
        Assert.AreEqual("Operation 'GetCidAsync' requires an authenticated session with BDUSS.", authError.Message);

        transport.AssertComplete();
    }

    [TestMethod]
    public async Task ClientsSharingHttpFactoryKeepIdentityCachesIsolated()
    {
        using var transport = new ForumLookupTestTransport();
        using var first = transport.CreateTiebaClient();
        using var second = transport.CreateTiebaClient();
        transport.ExpectFid(ForumName, ForumId);
        transport.ExpectFid(ForumName, OtherForumId);

        Assert.AreEqual(ForumId, await first.Forums.GetFidAsync(ForumName));
        Assert.AreEqual(OtherForumId, await second.Forums.GetFidAsync(ForumName));
        Assert.AreEqual(ForumId, await first.Forums.GetFidAsync(ForumName));
        Assert.AreEqual(OtherForumId, await second.Forums.GetFidAsync(ForumName));
        Assert.AreEqual(ForumName, await first.Forums.GetFnameAsync(ForumId));
        Assert.AreEqual(ForumName, await second.Forums.GetFnameAsync(OtherForumId));

        transport.AssertComplete(FidPath, FidPath);
    }

    private static void ExpectBlock(ForumLookupTestTransport transport, string fid)
    {
        transport.ExpectJson(BlockPath, SuccessBody, fields =>
        {
            Assert.AreEqual(fid, fields["fid"]);
            Assert.AreEqual("fixture-tbs", fields["tbs"]);
            Assert.AreEqual("portrait", fields["portrait"]);
            Assert.AreEqual("1", fields["day"]);
        });
    }

    private static void ExpectGood(ForumLookupTestTransport transport, string cid)
    {
        transport.ExpectJson(GoodPath, SuccessBody, fields =>
        {
            Assert.AreEqual("101", fields["fid"]);
            Assert.AreEqual(cid, fields["cid"]);
            Assert.AreEqual("303", fields["z"]);
            Assert.AreEqual("fixture-tbs", fields["tbs"]);
            Assert.AreEqual(ForumName, fields["word"]);
        });
    }
}
