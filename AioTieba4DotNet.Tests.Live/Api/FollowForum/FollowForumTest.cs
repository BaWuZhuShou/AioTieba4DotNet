using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AioTieba4DotNet.Testing;
using JetBrains.Annotations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AioTieba4DotNet.Tests.Api.FollowForum;

[TestClass]
[TestCategory(TestCategoryNames.Live)]
[TestCategory(TestCategoryNames.ForumExtensions)]
[TestSubject(typeof(TiebaClient))]
public sealed class FollowForumTest : TestBase
{
    [TestMethod]
    public async Task FollowAsync_UsesSafeForumFixtureAndRestoresOriginalState()
    {
        var fixture = await RequireSafeForumFixtureAsync(nameof(FollowAsync_UsesSafeForumFixtureAndRestoresOriginalState));
        var wasFollowing = await IsForumFollowedAsync(fixture.Fid);
        Cleanup.RecordObject(TestCategoryNames.ForumExtensions, "forum", fixture.Fid.ToString(),
            TestCleanupObjectRelation.MutationTarget, $"safe forum follow state for {fixture.ResolvedName}");
        Cleanup.Register(TestCategoryNames.ForumExtensions,
            $"restore follow state for {fixture.ResolvedName}",
            wasFollowing ? "re-follow the safe forum" : "undo the temporary follow state",
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

        if (wasFollowing)
        {
            var resetResult = await Client.Forums.UnfollowAsync(fixture.Fid);
            Assert.IsTrue(resetResult);
            Assert.IsFalse(await IsForumFollowedAsync(fixture.Fid));
        }

        var success = await Client.Forums.FollowAsync(fixture.ResolvedName);

        Assert.IsTrue(success);
        Assert.IsTrue(await IsForumFollowedAsync(fixture.Fid));
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
