using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AioTieba4DotNet.Testing;
using JetBrains.Annotations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AioTieba4DotNet.Tests.Api.UnfollowForum;

[TestClass]
[TestCategory(TestCategoryNames.Live)]
[TestCategory(TestCategoryNames.ForumExtensions)]
[TestSubject(typeof(TiebaClient))]
public sealed class UnfollowForumTest : TestBase
{
    [TestMethod]
    public async Task UnfollowAsync_UsesSafeForumFixtureAndRestoresOriginalState()
    {
        var fixture = await RequireSafeForumFixtureAsync(nameof(UnfollowAsync_UsesSafeForumFixtureAndRestoresOriginalState));
        var wasFollowing = await IsForumFollowedAsync(fixture.Fid);
        Cleanup.RecordObject(TestCategoryNames.ForumExtensions, "forum", fixture.Fid.ToString(),
            TestCleanupObjectRelation.MutationTarget, $"safe forum follow state for {fixture.ResolvedName}");
        Cleanup.Register(TestCategoryNames.ForumExtensions,
            $"restore follow state for {fixture.ResolvedName}",
            wasFollowing ? "re-follow the safe forum" : "keep the safe forum unfollowed",
            async cancellationToken =>
        {
            var isFollowingNow = await IsForumFollowedAsync(fixture.Fid, cancellationToken);
            if (wasFollowing == isFollowingNow)
                return;

            if (wasFollowing)
                await Client.Forums.FollowAsync(fixture.Fid, cancellationToken);
            else
                await Client.Forums.UnfollowAsync(fixture.Fid, cancellationToken);
        });

        if (!wasFollowing)
        {
            var resetResult = await Client.Forums.FollowAsync(fixture.Fid);
            Assert.IsTrue(resetResult);
            Assert.IsTrue(await IsForumFollowedAsync(fixture.Fid));
        }

        var success = await Client.Forums.UnfollowAsync(fixture.ResolvedName);

        Assert.IsTrue(success);
        Assert.IsFalse(await IsForumFollowedAsync(fixture.Fid));
    }

    private async Task<bool> IsForumFollowedAsync(ulong fid, CancellationToken cancellationToken = default)
    {
        for (var page = 1;; page++)
        {
            var forums = await Client.Forums.GetSelfFollowForumsAsync(page, 200, cancellationToken);
            if (forums.Any(forum => forum.Fid == fid))
                return true;

            if (!forums.HasMore)
                return false;
        }
    }
}
