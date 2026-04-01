#nullable enable
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AioTieba4DotNet.Models.Forums;
using AioTieba4DotNet.Testing;
using JetBrains.Annotations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AioTieba4DotNet.Tests.Api.Sign;

[TestClass]
[TestCategory(TestCategoryNames.Live)]
[TestCategory(TestCategoryNames.ForumExtensions)]
[TestSubject(typeof(TiebaClient))]
public sealed class SignTest : TestBase
{
    [TestMethod]
    public async Task SignAsync_UsesSafeFollowedForumFixture()
    {
        var fixture = await RequireSafeForumFixtureAsync(nameof(SignAsync_UsesSafeFollowedForumFixture));
        var before = await GetSelfFollowForumAsync(fixture.Fid);
        if (before is null)
            Assert.Inconclusive(
                $"Skipping {nameof(SignAsync_UsesSafeFollowedForumFixture)}: configured safe forum '{fixture.ResolvedName}' is not currently followed by the authenticated account.");

        if (before.IsSigned)
            Assert.Inconclusive(
                $"Skipping {nameof(SignAsync_UsesSafeFollowedForumFixture)}: configured safe forum '{fixture.ResolvedName}' is already signed for the current day.");

        var success = await Client.Forums.SignAsync(fixture.ResolvedName);

        Assert.IsTrue(success);
        var after = await GetSelfFollowForumAsync(fixture.Fid);
        Assert.IsNotNull(after);
        Assert.IsTrue(after.IsSigned);
    }

    [TestMethod]
    public async Task SignForumsAsync_WhenUnsignedFollowedForumsExist_CompletesBulkSignFlow()
    {
        EnsureAuthenticated();
        var unsignedForum = await GetFirstUnsignedFollowedForumAsync();
        if (unsignedForum is null)
            Assert.Inconclusive(
                $"Skipping {nameof(SignForumsAsync_WhenUnsignedFollowedForumsExist_CompletesBulkSignFlow)}: no unsigned followed forums are currently available for safe bulk-sign verification.");

        var success = await Client.Forums.SignForumsAsync();

        Assert.IsTrue(success);
        var refreshed = await GetSelfFollowForumAsync(unsignedForum.Fid);
        Assert.IsNotNull(refreshed);
        Assert.IsTrue(refreshed.IsSigned);
    }

    [TestMethod]
    public async Task SignGrowthAsync_CompletesGrowthTaskWhenAccountStateAllows()
    {
        EnsureAuthenticated();

        try
        {
            var success = await Client.Forums.SignGrowthAsync();
            Assert.IsTrue(success);
        }
        catch (TieBaServerException exception)
        {
            Assert.Inconclusive(
                $"Skipping {nameof(SignGrowthAsync_CompletesGrowthTaskWhenAccountStateAllows)}: the current account state rejected the daily growth-sign task. Server message: {exception.Message}");
        }
    }

    private async Task<SelfFollowForum?> GetSelfFollowForumAsync(ulong fid, CancellationToken cancellationToken = default)
    {
        for (var page = 1;; page++)
        {
            var forums = await Client.Forums.GetSelfFollowForumsAsync(page, 200, cancellationToken);
            var match = forums.FirstOrDefault(forum => forum.Fid == fid);
            if (match is not null)
                return match;

            if (!forums.HasMore)
                return null;
        }
    }

    private async Task<SelfFollowForum?> GetFirstUnsignedFollowedForumAsync(CancellationToken cancellationToken = default)
    {
        for (var page = 1;; page++)
        {
            var forums = await Client.Forums.GetSelfFollowForumsAsync(page, 200, cancellationToken);
            var match = forums.FirstOrDefault(forum => !forum.IsSigned);
            if (match is not null)
                return match;

            if (!forums.HasMore)
                return null;
        }
    }
}
