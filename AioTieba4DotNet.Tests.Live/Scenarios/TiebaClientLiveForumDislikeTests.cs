using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AioTieba4DotNet.Testing;
using JetBrains.Annotations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AioTieba4DotNet.Tests;

[TestClass]
[TestCategory(TestCategoryNames.Live)]
[TestCategory(TestCategoryNames.ForumExtensions)]
[TestSubject(typeof(TiebaClient))]
public sealed class TiebaClientLiveForumDislikeTests : TestBase
{
    [TestMethod]
    public async Task DislikeAndUndislikeAsync_RoundTripSafeForumState()
    {
        var fixture = await RequireSafeForumFixtureAsync(nameof(DislikeAndUndislikeAsync_RoundTripSafeForumState));
        var wasDisliked = await IsForumDislikedAsync(fixture.Fid);
        Cleanup.RecordObject(TestCategoryNames.ForumExtensions, "forum", fixture.Fid.ToString(),
            TestCleanupObjectRelation.MutationTarget, $"safe forum dislike state for {fixture.ResolvedName}");
        Cleanup.Register(TestCategoryNames.ForumExtensions,
            $"restore dislike state for {fixture.ResolvedName}",
            wasDisliked ? "re-apply the original dislike state" : "undo the temporary dislike state",
            async cancellationToken =>
            {
                var isDislikedNow = await IsForumDislikedAsync(fixture.Fid, cancellationToken);
                if (wasDisliked == isDislikedNow)
                    return;

                if (wasDisliked)
                    await Client.Forums.DislikeAsync(fixture.Fid, cancellationToken);
                else
                    await Client.Forums.UndislikeAsync(fixture.Fid, cancellationToken);
            });

        if (wasDisliked)
        {
            var resetResult = await Client.Forums.UndislikeAsync(fixture.Fid);
            Assert.IsTrue(resetResult);
            Assert.IsFalse(await IsForumDislikedAsync(fixture.Fid));
        }

        var disliked = await Client.Forums.DislikeAsync(fixture.ResolvedName);
        Assert.IsTrue(disliked);
        Assert.IsTrue(await IsForumDislikedAsync(fixture.Fid));

        var undisliked = await Client.Forums.UndislikeAsync(fixture.ResolvedName);
        Assert.IsTrue(undisliked);
        Assert.IsFalse(await IsForumDislikedAsync(fixture.Fid));
    }

    private async Task<bool> IsForumDislikedAsync(ulong fid, CancellationToken cancellationToken = default)
    {
        for (var page = 1;; page++)
        {
            var forums = await Client.Forums.GetDislikeForumsAsync(page, 20, cancellationToken);
            if (forums.Any(forum => forum.Fid == fid))
                return true;

            if (!forums.HasMore)
                return false;
        }
    }
}
