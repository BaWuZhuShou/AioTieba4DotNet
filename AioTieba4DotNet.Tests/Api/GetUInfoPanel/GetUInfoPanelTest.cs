using System;
using System.Threading.Tasks;
using AioTieba4DotNet.Exceptions;
using JetBrains.Annotations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace AioTieba4DotNet.Tests.Api.GetUInfoPanel;

[TestClass]
[TestCategory("Integration")]
[TestSubject(typeof(AioTieba4DotNet.Api.GetUInfoPanel.GetUInfoPanel))]
public class GetUInfoPanelTest : TestBase
{
    [TestMethod]
    public async Task TestRequest()
        {
            try
            {
                var getUInfoPanel = new AioTieba4DotNet.Api.GetUInfoPanel.GetUInfoPanel(HttpCore);
                var result = await getUInfoPanel.RequestAsync("tb.1.96cdea74.oMf7wnn0fhE75m_-zBX4Zw");
                Assert.IsNotNull(result);
                Console.WriteLine(JsonConvert.SerializeObject(result));
            }
            catch (TieBaServerException exception) when (exception.Code == 1130032)
            {
                Assert.Inconclusive($"Skipping blocked panel target in current environment: {exception.Message}");
            }
        }
    }
