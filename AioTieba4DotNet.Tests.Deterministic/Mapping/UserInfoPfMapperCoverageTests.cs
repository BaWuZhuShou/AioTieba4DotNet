#nullable enable
using System;
using AioTieba4DotNet.Internal.Mapping;
using AioTieba4DotNet.Models;
using AioTieba4DotNet.Models.Users;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AioTieba4DotNet.Tests.Mapping;

[TestClass]
public sealed class UserInfoPfMapperCoverageTests
{
    [TestMethod]
    public void FromTbData_ThrowsWhenUserIsMissing()
    {
        var data = new ProfileResIdl.Types.DataRes();

        try
        {
            _ = UserInfoPfMapper.FromTbData(data);
            Assert.Fail("Expected InvalidOperationException was not thrown.");
        }
        catch (InvalidOperationException exception)
        {
            StringAssert.Contains(exception.Message, "data.User is null");
        }
    }

    [TestMethod]
    public void FromTbData_MapsOptionalFieldsFlagsAndDefaults()
    {
        var data = new ProfileResIdl.Types.DataRes
        {
            User = new User
            {
                Id = 123,
                Portrait = "tb.1.safe?012345678901",
                Name = "safe-user",
                NameShow = "Safe User",
                TiebaUid = "778899",
                UserGrowth = new User.Types.UserGrowth { LevelId = 9 },
                Gender = 2,
                TbAge = "3.5",
                PostNum = 11,
                FansNum = 12,
                ConcernNum = 13,
                MyLikeNum = 14,
                Intro = "hello",
                IpAddress = "127.0.0.1",
                Iconinfo = { new User.Types.Icon { Name = "vip" }, new User.Types.Icon { Name = string.Empty } },
                VirtualImageInfo =
                    new User.Types.VirtualImageInfo
                    {
                        IssetVirtualImage = 1,
                        PersonalState =
                            new User.Types.VirtualImageInfo.Types.PersonalState { Text = "state-text" }
                    },
                NewTshowIcon = { new User.Types.TshowInfo { Name = "badge" } },
                NewGodData = new User.Types.NewGodInfo { Status = 1 },
                PrivSets = new User.Types.PrivSets { Like = 2, Reply = 3 }
            },
            AntiStat = new ProfileResIdl.Types.DataRes.Types.Anti { BlockStat = 1, HideStat = 1, DaysTofree = 31 },
            UserAgreeInfo = new ProfileResIdl.Types.DataRes.Types.UserAgreeInfo { TotalAgreeNum = 88 }
        };

        var mapped = UserInfoPfMapper.FromTbData(data);

        Assert.AreEqual(123L, mapped.UserId);
        Assert.AreEqual("tb.1.safe", mapped.Portrait);
        Assert.AreEqual("safe-user", mapped.UserName);
        Assert.AreEqual("Safe User", mapped.NickNameNew);
        Assert.AreEqual(778899L, mapped.TiebaUid);
        Assert.AreEqual(9, mapped.GLevel);
        Assert.AreEqual((Gender)2, mapped.Gender);
        Assert.AreEqual(3.5f, mapped.Age);
        Assert.AreEqual(11L, mapped.PostNum);
        Assert.AreEqual(88, mapped.AgreeNum);
        Assert.AreEqual(12L, mapped.FanNum);
        Assert.AreEqual(13L, mapped.FollowNum);
        Assert.AreEqual(14L, mapped.ForumNum);
        Assert.AreEqual("hello", mapped.Sign);
        Assert.AreEqual("127.0.0.1", mapped.Ip);
        CollectionAssert.AreEqual(new[] { "vip" }, (System.Collections.ICollection)mapped.Icons);
        Assert.IsTrue(mapped.VImage.Enabled);
        Assert.AreEqual("state-text", mapped.VImage.State);
        Assert.IsTrue(mapped.IsVip);
        Assert.IsTrue(mapped.IsGod);
        Assert.IsTrue(mapped.IsBlocked);
        Assert.AreEqual((PrivLike)2, mapped.PrivLike);
        Assert.AreEqual((PrivReply)3, mapped.PrivReply);
    }

    [TestMethod]
    public void FromTbData_UsesFallbackDefaultsWhenOptionalDataIsMissing()
    {
        var data = new ProfileResIdl.Types.DataRes
        {
            User = new User
            {
                Id = 456,
                Portrait = "tb.1.other",
                Gender = 1,
                TiebaUid = string.Empty,
                TbAge = string.Empty,
                VirtualImageInfo = new User.Types.VirtualImageInfo()
            },
            AntiStat = new ProfileResIdl.Types.DataRes.Types.Anti { BlockStat = 1, HideStat = 0, DaysTofree = 99 }
        };

        var mapped = UserInfoPfMapper.FromTbData(data);

        Assert.AreEqual(0L, mapped.TiebaUid);
        Assert.AreEqual(0, mapped.GLevel);
        Assert.AreEqual(0f, mapped.Age);
        Assert.AreEqual(0, mapped.AgreeNum);
        Assert.AreEqual(string.Empty, mapped.UserName);
        Assert.AreEqual(string.Empty, mapped.NickNameNew);
        Assert.AreEqual(string.Empty, mapped.Sign);
        Assert.AreEqual(string.Empty, mapped.Ip);
        Assert.AreEqual(0, mapped.Icons.Count);
        Assert.IsFalse(mapped.VImage.Enabled);
        Assert.AreEqual(string.Empty, mapped.VImage.State);
        Assert.IsFalse(mapped.IsVip);
        Assert.IsFalse(mapped.IsGod);
        Assert.IsFalse(mapped.IsBlocked);
        Assert.AreEqual(PrivLike.Public, mapped.PrivLike);
        Assert.AreEqual(PrivReply.All, mapped.PrivReply);
    }

    [TestMethod]
    public void FromTbData_HandlesNonTrimmedPortraitAndPartialAntiStat()
    {
        var data = new ProfileResIdl.Types.DataRes
        {
            User = new User
            {
                Id = 789,
                Portrait = "tb.1.partial",
                Name = "partial-user",
                NameShow = "Partial User",
                TiebaUid = "900",
                TbAge = "1",
                IpAddress = "Earth",
                NewGodData = new User.Types.NewGodInfo { Status = 0 },
                NewTshowIcon = { new User.Types.TshowInfo { Name = "badge" } }
            },
            AntiStat = new ProfileResIdl.Types.DataRes.Types.Anti { BlockStat = 1, HideStat = 1, DaysTofree = 30 }
        };

        var mapped = UserInfoPfMapper.FromTbData(data);

        Assert.AreEqual("tb.1.partial", mapped.Portrait);
        Assert.AreEqual(900L, mapped.TiebaUid);
        Assert.AreEqual(1f, mapped.Age);
        Assert.AreEqual("Earth", mapped.Ip);
        Assert.IsTrue(mapped.IsVip);
        Assert.IsFalse(mapped.IsGod);
        Assert.IsFalse(mapped.IsBlocked);
    }
}
