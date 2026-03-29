using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AioTieba4DotNet.Tests.Api.DelBawu;

[TestClass]
public class DelBaWuSafetyTest : TestBase
{
    [TestMethod]
    [TestCategory("Live")]
    [Ignore("Requires an approved bawu/admin fixture in lol欧服 before destructive moderation can run. Keep this in the nightly/manual lane until that fixture exists.")]
    public async Task DelBaWuAsync_RequiresApprovedSafeFixture()
    {
        EnsureAuthenticated();
        global::AioTieba4DotNet.ITiebaClient tiebaClient = Client;
        await tiebaClient.Forums.DelBaWuAsync("lol欧服", "fixture-portrait", "manager");
    }
}
