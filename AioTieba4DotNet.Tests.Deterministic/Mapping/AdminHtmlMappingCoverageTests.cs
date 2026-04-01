#nullable enable
using System;
using AioTieba4DotNet.Internal.Mapping;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AioTieba4DotNet.Tests.Mapping;

[TestClass]
public sealed class AdminHtmlMappingCoverageTests
{
    [TestMethod]
    public void AdminHtmlParsing_HandlesAttributePortraitPagingAndHashEdgeCases()
    {
        var attributeValue = AdminHtmlParsing.GetAttributeValue("<a data-name=\"贴吧\"></a>", "data-name");
        var missingAttribute = AdminHtmlParsing.GetAttributeValue("<a></a>", "href");
        var emptyPortrait = AdminHtmlParsing.ExtractPortraitFromHomeHref(" ");
        var missingPortrait = AdminHtmlParsing.ExtractPortraitFromHomeHref("/home/main?fr=home");
        var portraitWithFragment = AdminHtmlParsing.ExtractPortraitFromHomeHref("/home/main?id=tb.1.user#/feed");
        var portraitWithQuery = AdminHtmlParsing.ExtractPortraitFromHomeHref("/home/main?id=tb.1.user&amp;fr=home");
        var noPagination = AdminHtmlParsing.ParseCommonPage("<div class=\"breadcrumbs\"><em>0</em></div>");
        var countOnly = AdminHtmlParsing.ParseCommonPage("<div class=\"breadcrumbs\"><em>5</em></div>");
        var paginationWithoutActive = AdminHtmlParsing.ParseCommonPage("""
                                                                       <div class="breadcrumbs"><em>7</em></div>
                                                                       <div class="tbui_pagination"><li><a>1</a></li><li><a>2</a></li></div>
                                                                       """);
        var fullPagination = AdminHtmlParsing.ParseCommonPage("""
                                                              <div class="breadcrumbs"><em>42</em></div>
                                                              <div class="tbui_pagination"><li><a>1</a></li><li class="active"><a>2</a></li><li><a>3</a></li>(4)</div>
                                                              """);
        var yearless = AdminHtmlParsing.ParseYearlessDateTime("03-05 12:34");
        var fullDateTimeNoSpace = AdminHtmlParsing.ParseFullDateTime("2024-05-0607:08");
        var imageHash =
            AdminHtmlParsing.ExtractImageHash("https://imgsrc.baidu.com/forum/pic/item/abcdef123456.jpg?foo=bar");
        var missingHash = AdminHtmlParsing.ExtractImageHash("https://imgsrc.baidu.com/forum/pic/item/not-a-jpg.png");

        Assert.AreEqual("贴吧", attributeValue);
        Assert.AreEqual(string.Empty, missingAttribute);
        Assert.AreEqual(string.Empty, emptyPortrait);
        Assert.AreEqual(string.Empty, missingPortrait);
        Assert.AreEqual("tb.1.user", portraitWithFragment);
        Assert.AreEqual("tb.1.user", portraitWithQuery);
        Assert.AreEqual((0, 0, 0, false, false), noPagination);
        Assert.AreEqual((1, 1, 5, false, false), countOnly);
        Assert.AreEqual((1, 1, 7, false, false), paginationWithoutActive);
        Assert.AreEqual((2, 4, 42, true, true), fullPagination);
        Assert.AreEqual(new DateTime(1904, 3, 5, 12, 34, 0), yearless);
        Assert.AreEqual(new DateTime(2024, 5, 6, 7, 8, 0), fullDateTimeNoSpace);
        Assert.AreEqual("abcdef123456", imageHash);
        Assert.AreEqual(string.Empty, missingHash);
    }

    [TestMethod]
    public void AdminHtmlParsing_ParseFullDateTime_RejectsUnsupportedFormat()
    {
        try
        {
            _ = AdminHtmlParsing.ParseFullDateTime("2024/05/06 07:08");
            Assert.Fail("Expected FormatException was not thrown.");
        }
        catch (FormatException exception)
        {
            StringAssert.Contains(exception.Message, "Unsupported admin datetime format");
        }
    }

    [TestMethod]
    public void BawuPostLogsMapper_FromTbData_MapsReplyAndThreadRows_AndSkipsInvalidRows()
    {
        var html = """
                   <div class="breadcrumbs"><em>2</em></div>
                   <div class="tbui_pagination"><li><a>1</a></li><li class="active"><a>2</a></li>(3)</div>
                   <table>
                     <tr><td>ignored</td></tr>
                     <tr><td>left</td><td>op</td><td>user</td><td>2024-05-06 07:08</td></tr>
                     <tr>
                       <td>
                         <div class="post_meta"><a href="/home/main?id=tb.1.reply#/feed"></a><time>03-05 12:34</time></div>
                         <h1><a href="/p/123456#7890">回复：原帖标题</a></h1>
                         <div>123456789012reply body</div>
                         <a href="https://img.example/origin.jpg"><img original="https://imgsrc.baidu.com/forum/pic/item/abcdef123456.jpg" /></a>
                       </td>
                       <td>删帖</td>
                       <td>operator-a</td>
                       <td>2024-05-06 07:08</td>
                     </tr>
                     <tr>
                       <td>
                         <div class="post_meta"><a href="/home/main?id=tb.1.thread&amp;fr=home"></a><time>03-06 13:35</time></div>
                         <h1><a href="/thread/not-a-tid#oops">普通标题</a></h1>
                         <div></div>
                       </td>
                       <td>恢复</td>
                       <td>operator-b</td>
                       <td>2024-05-0708:09</td>
                     </tr>
                   </table>
                   """;

        var mapped = BawuPostLogsMapper.FromTbData(html);

        Assert.AreEqual(2, mapped.Count);
        Assert.AreEqual(2, mapped.Page.CurrentPage);
        Assert.AreEqual(3, mapped.Page.TotalPage);
        Assert.AreEqual(2, mapped.Page.TotalCount);
        Assert.IsTrue(mapped.Page.HasMore);
        Assert.IsTrue(mapped.Page.HasPrevious);

        Assert.AreEqual(123456L, mapped[0].Tid);
        Assert.AreEqual(7890L, mapped[0].Pid);
        Assert.AreEqual("原帖标题", mapped[0].Title);
        Assert.AreEqual("reply body", mapped[0].Text);
        Assert.AreEqual("删帖", mapped[0].OperationType);
        Assert.AreEqual("tb.1.reply", mapped[0].PostPortrait);
        Assert.AreEqual(new DateTime(1904, 3, 5, 12, 34, 0), mapped[0].PostTime);
        Assert.AreEqual(new DateTime(2024, 5, 6, 7, 8, 0), mapped[0].OperationTime);
        Assert.AreEqual(1, mapped[0].Medias.Count);
        Assert.AreEqual("abcdef123456", mapped[0].Medias[0].Hash);

        Assert.AreEqual(0L, mapped[1].Tid);
        Assert.AreEqual(0L, mapped[1].Pid);
        Assert.AreEqual("普通标题", mapped[1].Title);
        Assert.AreEqual("普通标题", mapped[1].Text);
        Assert.AreEqual("恢复", mapped[1].OperationType);
        Assert.AreEqual("tb.1.thread", mapped[1].PostPortrait);
        Assert.AreEqual(new DateTime(2024, 5, 7, 8, 9, 0), mapped[1].OperationTime);
    }

    [TestMethod]
    public void BawuPostLogsMapper_HandlesMissingHashTitleFallbackAndHrefParsingFailures()
    {
        var html = """
                   <div class="breadcrumbs"><em>1</em></div>
                   <table>
                     <tr>
                       <td>
                         <div class="post_meta"><a href="/home/main?id=tb.1.safe"></a><time>03-05 12:34</time></div>
                         <h1><a href="/p/not-a-tid">内层标题</a></h1>
                         <div>123456789012正文内容</div>
                         <a href="https://img.example/origin.png"><img original="https://imgsrc.baidu.com/forum/pic/item/not-a-jpg.png" /></a>
                       </td>
                       <td>操作</td>
                       <td>operator</td>
                       <td>2024-05-06 07:08</td>
                     </tr>
                     <tr>
                       <td>
                         <div class="post_meta"><a href="/home/main?id=tb.1.reply"></a><time>03-05 12:34</time></div>
                         <h1><a href="/p/123#not-a-pid" title="回复：来自属性">内层标题</a></h1>
                         <div>123456789012</div>
                       </td>
                       <td>操作2</td>
                       <td>operator2</td>
                       <td>2024-05-06 07:09</td>
                     </tr>
                   </table>
                   """;

        var mapped = BawuPostLogsMapper.FromTbData(html);

        Assert.AreEqual(2, mapped.Count);
        Assert.AreEqual(0L, mapped[0].Tid);
        Assert.AreEqual(0L, mapped[0].Pid);
        Assert.AreEqual("内层标题", mapped[0].Title);
        Assert.AreEqual("内层标题\n正文内容", mapped[0].Text);
        Assert.AreEqual(string.Empty, mapped[0].Medias[0].Hash);
        Assert.AreEqual(123L, mapped[1].Tid);
        Assert.AreEqual(0L, mapped[1].Pid);
        Assert.AreEqual("内层标题", mapped[1].Title);
        Assert.AreEqual("内层标题\n123456789012", mapped[1].Text);
    }

    [TestMethod]
    public void BawuPostLogsMapper_HandlesPlainTitlesWithAndWithoutBodyText()
    {
        var html = """
                   <div class="breadcrumbs"><em>1</em></div>
                   <table>
                     <tr>
                       <td>
                         <div class="post_meta"><a href="/home/main?id=tb.1.author"></a><time>03-05 12:34</time></div>
                         <h1><a href="/p/222#333">普通标题</a></h1>
                         <div>123456789012正文内容</div>
                       </td>
                       <td>操作</td>
                       <td>operator</td>
                       <td>2024-05-06 07:08</td>
                     </tr>
                     <tr>
                       <td>
                         <div class="post_meta"><a href="/home/main?id=tb.1.author"></a><time>03-05 12:34</time></div>
                         <h1><a href="/p/444#555">普通标题</a></h1>
                         <div></div>
                       </td>
                       <td>操作2</td>
                       <td>operator2</td>
                       <td>2024-05-06 07:09</td>
                     </tr>
                     <tr>
                       <td>
                         <div class="post_meta"><a href="/home/main?id=tb.1.author"></a><time>03-05 12:34</time></div>
                         <h1><a href="/p/666#666">回复：同帖标题</a></h1>
                         <div>123456789012reply body</div>
                       </td>
                       <td>操作3</td>
                       <td>operator3</td>
                       <td>2024-05-06 07:10</td>
                     </tr>
                   </table>
                   """;

        var mapped = BawuPostLogsMapper.FromTbData(html);

        Assert.AreEqual(3, mapped.Count);
        Assert.AreEqual(0L, mapped[0].Pid);
        Assert.AreEqual("普通标题", mapped[0].Title);
        Assert.AreEqual("普通标题\n正文内容", mapped[0].Text);
        Assert.AreEqual(0L, mapped[1].Pid);
        Assert.AreEqual("普通标题", mapped[1].Title);
        Assert.AreEqual("普通标题", mapped[1].Text);
        Assert.AreEqual(0L, mapped[2].Pid);
        Assert.AreEqual("回复：同帖标题", mapped[2].Title);
        Assert.AreEqual("回复：同帖标题\nreply body", mapped[2].Text);
    }
}
