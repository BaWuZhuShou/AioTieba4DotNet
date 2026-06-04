#nullable enable
using System;
using AioTieba4DotNet.Internal.Mapping;
using AioTieba4DotNet.Tests.Platform.Contracts;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AioTieba4DotNet.Tests.Governance.Contracts;

[TestClass]
[TestCategory(OnlineTestParityCategories.Admins)]
public sealed class AdminHtmlParsingContractTests
{
    [TestMethod]
    public void ParseYearlessDateTime_HyphenatedMonthDayFormat_ReturnsSentinelYearDateTime()
    {
        var parsed = AdminHtmlParsing.ParseYearlessDateTime("08-22 12:54");

        Assert.AreEqual(new DateTime(1904, 8, 22, 12, 54, 0, DateTimeKind.Unspecified), parsed);
    }

    [TestMethod]
    public void ParseYearlessDateTime_LiveChineseMonthDayFormat_ReturnsSentinelYearDateTime()
    {
        var parsed = AdminHtmlParsing.ParseYearlessDateTime("08月22日 12:54");

        Assert.AreEqual(new DateTime(1904, 8, 22, 12, 54, 0, DateTimeKind.Unspecified), parsed);
    }

    [TestMethod]
    public void BawuUserLogsMapper_ExtraIntermediateCells_UsesTrailingOperatorAndTimeCells()
    {
        const string html = """
                            <div class="breadcrumbs"><em>1</em></div>
                            <table><tbody>
                              <tr>
                                <td><a href="/home/main?id=tb.1.example.abc#/">target user</a></td>
                                <td>封禁</td>
                                <td>3 天</td>
                                <td>扩展列</td>
                                <td>杯中茶茶中杯</td>
                                <td>2026-08-22 12:54</td>
                              </tr>
                            </tbody></table>
                            <div class="tbui_pagination"><ul><li class="active">1</li></ul><span>(1)</span></div>
                            """;

        var logs = BawuUserLogsMapper.FromTbData(html);

        Assert.AreEqual(1, logs.Objs.Count);
        Assert.AreEqual("杯中茶茶中杯", logs.Objs[0].OperatorUserName);
        Assert.AreEqual(new DateTime(2026, 8, 22, 12, 54, 0), logs.Objs[0].OperationTime);
    }
}
