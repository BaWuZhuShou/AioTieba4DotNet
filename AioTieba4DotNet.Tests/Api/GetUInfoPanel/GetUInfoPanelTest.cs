using System;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AioTieba4DotNet.Tests.Api.GetUInfoPanel;

[TestClass]
[TestSubject(typeof(AioTieba4DotNet.Api.GetUInfoPanel.GetUInfoPanel))]
public class GetUInfoPanelTest : TestBase
{
    [TestMethod]
    public async Task TestRequest()
    {
        var getUInfoPanel = new AioTieba4DotNet.Api.GetUInfoPanel.GetUInfoPanel(HttpCore);
        // 使用 portrait 进行查询，这里暂时留空或使用测试值
        var result = await getUInfoPanel.RequestAsync("tb.1.96cdea74.oMf7wnn0fhE75m_-zBX4Zw");
        Assert.IsNotNull(result);
        Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(result));
    }
}