using AioTieba4DotNet.Exceptions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AioTieba4DotNet.Tests.Api;

[TestClass]
public class RequestModeTest
{
    [TestMethod]
    public void TiebaOptions_DefaultTransportMode_IsAuto()
    {
        var options = new TiebaOptions();

        Assert.AreEqual(TiebaTransportMode.Auto, options.TransportMode);
    }

    [TestMethod]
    public void TiebaClient_RejectsStokenWithoutBduss()
    {
        try
        {
            _ = new TiebaClient(new TiebaOptions { Stoken = "stoken-only" });
            Assert.Fail("Expected TiebaConfigurationException was not thrown.");
        }
        catch (TiebaConfigurationException)
        {
        }
    }
}
