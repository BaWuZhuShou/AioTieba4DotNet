using System.Threading.Tasks;
using AioTieba4DotNet.Testing;
using JetBrains.Annotations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AioTieba4DotNet.Tests.Api.InitZId;

[TestClass]
[TestCategory(TestCategoryNames.Integration)]
[TestCategory(TestCategoryNames.MessagingClient)]
[TestSubject(typeof(AioTieba4DotNet.Api.InitZId.InitZId))]
public sealed class InitZIdIntegrationTests : TestBase
{
    [TestMethod]
    public async Task RequestAsync_WhenCredentialsExist_ReturnsLiveResultOrExplicitSkip()
    {
        EnsureAuthenticated();

        var initZId = new AioTieba4DotNet.Api.InitZId.InitZId(HttpCore);

        try
        {
            var result = await initZId.RequestAsync();
            Assert.IsNotNull(result);
        }
        catch (TiebaTransportException exception)
        {
            Assert.Inconclusive($"Skipping InitZId live request in this environment: {exception.Message}");
        }
    }
}
