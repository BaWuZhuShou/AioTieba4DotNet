using System.Threading.Tasks;
using AioTieba4DotNet.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AioTieba4DotNet.Tests.Api.DelBawu;

[TestClass]
public sealed class DelBaWuSafetyTest : TestBase
{
    [TestMethod]
    [TestCategory(TestCategoryNames.Live)]
    [TestCategory(TestCategoryNames.ForumExtensions)]
    [Ignore("Requires an approved bawu/admin fixture in lol欧服 before destructive moderation can run. Keep this in the nightly/manual lane until that fixture exists.")]
    public async Task DelBaWuAsync_RequiresApprovedSafeFixture()
    {
        EnsureAuthenticated();
        ITiebaClient tiebaClient = Client;
        await tiebaClient.Forums.DelBaWuAsync("lol欧服", "fixture-portrait", "manager");
    }
}
