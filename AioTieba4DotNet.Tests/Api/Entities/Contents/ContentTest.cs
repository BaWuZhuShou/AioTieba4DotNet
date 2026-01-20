using Microsoft.VisualStudio.TestTools.UnitTesting;
using AioTieba4DotNet.Api.Entities.Contents;
using System.Collections.Generic;

namespace AioTieba4DotNet.Tests.Api.Entities.Contents;

[TestClass]
public class ContentTest
{
    [TestMethod]
    public void TestFragsIndex()
    {
        // 构造模拟数据
        var pbContents = new List<PbContent>
        {
            new() { Type = 0, Text = "text1" }, // FragText
            new() { Type = 11, Text = "emoji1" }, // FragEmoji
            new() { Type = 3, Src = "img1" }     // FragImage
        };

        // 执行解析
        var content = Content.FromTbData(pbContents);

        // 验证索引
        Assert.HasCount(3, content.Frags);
        Assert.AreEqual(0, content.Frags[0].Index);
        Assert.AreEqual(1, content.Frags[1].Index);
        Assert.AreEqual(2, content.Frags[2].Index);

        Assert.IsInstanceOfType(content.Frags[0], typeof(FragText));
        Assert.IsInstanceOfType(content.Frags[1], typeof(FragEmoji));
        Assert.IsInstanceOfType(content.Frags[2], typeof(FragImage));
    }

    [TestMethod]
    public void TestToString()
    {
        var pbContents = new List<PbContent>
        {
            new() { Type = 0, Text = "hello " },
            new() { Type = 11, Text = "1", C = "emoji" },
            new() { Type = 0, Text = " world" }
        };

        var content = Content.FromTbData(pbContents);
        var str = content.ToString();

        System.Console.WriteLine($"[DEBUG_LOG] ToString result: {str}");

        Assert.Contains("Text: hello  world", str);
        Assert.Contains("Emojis: [FragEmoji Id: 1, Desc: emoji]", str);
        Assert.DoesNotContain("Images:", str); // 不应包含空列表
        Assert.Contains("Frags: [FragText Text: hello , FragEmoji Id: 1, Desc: emoji, FragText Text:  world]", str);
    }
}
