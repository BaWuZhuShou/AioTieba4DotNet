using System.Threading.Tasks;
using AioTieba4DotNet.Testing;
using JetBrains.Annotations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AioTieba4DotNet.Tests;

[TestClass]
[TestCategory(TestCategoryNames.Live)]
[TestCategory(TestCategoryNames.ThreadWriteModeration)]
[TestSubject(typeof(TiebaClient))]
public sealed class TiebaClientLiveModerationTests : TestBase
{
    [TestMethod]
    [Ignore("Requires admin privileges in the forum")]
    public async Task BlockAsync_WhenAdminPrivilegesExist_PerformsLiveModerationAction()
    {
        EnsureAuthenticated();

        var result = await Client.Admins.BlockAsync("DNF", "some_portrait", 1, "test");

        Assert.IsTrue(result);
    }
}
