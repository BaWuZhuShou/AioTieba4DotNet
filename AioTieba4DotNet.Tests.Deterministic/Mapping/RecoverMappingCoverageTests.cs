#nullable enable
using AioTieba4DotNet.Internal.Mapping;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;

namespace AioTieba4DotNet.Tests.Mapping;

[TestClass]
public sealed class RecoverMappingCoverageTests
{
    [TestMethod]
    public void RecoverMapper_FromTbData_ReturnsDefaultRecover_WhenSourceIsNull()
    {
        var mapped = RecoverMapper.FromTbData(null);

        Assert.AreEqual(string.Empty, mapped.Text);
        Assert.AreEqual(0L, mapped.Tid);
        Assert.AreEqual(0L, mapped.Pid);
        Assert.IsNotNull(mapped.User);
        Assert.AreEqual(string.Empty, mapped.User.UserName);
        Assert.IsFalse(mapped.IsFloor);
        Assert.IsFalse(mapped.IsHide);
    }

    [TestMethod]
    public void RecoverMapper_FromTbData_PrefersPostInfo_AndReadsBooleanAndIntegerTokens()
    {
        var mapped = RecoverMapper.FromTbData(JObject.Parse(
            """
            {
              "thread_info": {
                "tid": 1001,
                "abstract": "thread abstract",
                "user_name": "thread-user",
                "user_nickname": "thread-nick",
                "portrait": "tb.1.thread?012345678901"
              },
              "post_info": {
                "pid": 2002,
                "abstract": "post abstract",
                "user_name": "post-user",
                "user_nickname": "post-nick",
                "portrait": "tb.1.post?012345678901"
              },
              "op_info": {
                "name": "operator-a",
                "time": 123456
              },
              "is_foor": true,
              "is_frs_mask": 1
            }
            """));

        Assert.AreEqual("post abstract", mapped.Text);
        Assert.AreEqual(1001L, mapped.Tid);
        Assert.AreEqual(2002L, mapped.Pid);
        Assert.AreEqual("post-user", mapped.User.UserName);
        Assert.AreEqual("post-nick", mapped.User.NickNameNew);
        Assert.AreEqual("tb.1.post", mapped.User.Portrait);
        Assert.AreEqual("operator-a", mapped.OperatorShowName);
        Assert.AreEqual(123456, mapped.OperatorTime);
        Assert.IsTrue(mapped.IsFloor);
        Assert.IsTrue(mapped.IsHide);
    }

    [TestMethod]
    public void RecoverMapper_FromTbData_UsesThreadInfoAndStringBooleanFallbacks()
    {
        var mapped = RecoverMapper.FromTbData(JObject.Parse(
            """
            {
              "thread_info": {
                "tid": 3003,
                "abstract": "thread only",
                "user_name": "thread-user",
                "user_nickname": "thread-nick",
                "portrait": "tb.1.thread?abcdef"
              },
              "post_info": {},
              "op_info": {
                "name": "operator-b",
                "time": 7890
              },
              "is_foor": "false",
              "is_frs_mask": "unexpected"
            }
            """));

        Assert.AreEqual("thread only", mapped.Text);
        Assert.AreEqual(3003L, mapped.Tid);
        Assert.AreEqual(0L, mapped.Pid);
        Assert.AreEqual("thread-user", mapped.User.UserName);
        Assert.AreEqual("tb.1.thread", mapped.User.Portrait);
        Assert.AreEqual("operator-b", mapped.OperatorShowName);
        Assert.AreEqual(7890, mapped.OperatorTime);
        Assert.IsFalse(mapped.IsFloor);
        Assert.IsFalse(mapped.IsHide);
    }

    [TestMethod]
    public void RecoverMapper_FromTbData_HandlesMissingNestedObjects_AndAllBooleanTokenShapes()
    {
        var missingNested = RecoverMapper.FromTbData(JObject.Parse(
            """
            {
              "thread_info": {},
              "is_foor": null,
              "is_frs_mask": 0
            }
            """));
        var stringTrue = RecoverMapper.FromTbData(JObject.Parse(
            """
            {
              "thread_info": {
                "tid": 5
              },
              "is_foor": "1",
              "is_frs_mask": "0"
            }
            """));
        var boolFalse = RecoverMapper.FromTbData(JObject.Parse(
            """
            {
              "post_info": {
                "pid": 6
              },
              "is_foor": false,
              "is_frs_mask": true
            }
            """));

        Assert.AreEqual(string.Empty, missingNested.Text);
        Assert.AreEqual(0L, missingNested.Tid);
        Assert.AreEqual(0L, missingNested.Pid);
        Assert.AreEqual(string.Empty, missingNested.OperatorShowName);
        Assert.AreEqual(0, missingNested.OperatorTime);
        Assert.IsFalse(missingNested.IsFloor);
        Assert.IsFalse(missingNested.IsHide);
        Assert.IsFalse(stringTrue.IsHide);
        Assert.IsTrue(stringTrue.IsFloor);
        Assert.IsFalse(boolFalse.IsFloor);
        Assert.IsTrue(boolFalse.IsHide);
    }

    [TestMethod]
    public void RecoverContentMapper_FromTbData_HandlesNullSkipsNonTextAndUsesEmptyHashFallback()
    {
        var empty = RecoverContentMapper.FromTbData(null);
        var mapped = RecoverContentMapper.FromTbData(JObject.Parse(
            """
            {
              "content_detail": [
                { "type": 2, "value": "ignored" },
                { "type": 1, "value": "text value" },
                { "type": 1 }
              ],
              "all_pics": [
                { "url": "https://imgsrc.baidu.com/forum/pic/item/0123456789abcdef0123456789abcdef.jpg", "width": 10, "height": 20 },
                { "url": "https://example.com/nohash.png" }
              ]
            }
            """));

        Assert.AreEqual(0, empty.Frags.Count);
        Assert.AreEqual(2, mapped.Texts.Count);
        Assert.AreEqual("text value", mapped.Texts[0].Text);
        Assert.AreEqual(string.Empty, mapped.Texts[1].Text);
        Assert.AreEqual(2, mapped.Images.Count);
        Assert.AreEqual("0123456789abcdef0123456789abcdef", mapped.Images[0].Hash);
        Assert.AreEqual(string.Empty, mapped.Images[1].Hash);
        Assert.AreEqual(0, mapped.Images[1].ShowWidth);
        Assert.AreEqual(0, mapped.Images[1].ShowHeight);
        Assert.AreEqual(0, mapped.Frags[0].Index);
        Assert.AreEqual(mapped.Frags.Count - 1, mapped.Frags[^1].Index);
    }
}
