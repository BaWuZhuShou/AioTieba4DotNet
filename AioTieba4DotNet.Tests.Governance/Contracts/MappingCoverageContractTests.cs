#nullable enable
using AioTieba4DotNet.Internal.Mapping;
using AioTieba4DotNet.Models;
using AioTieba4DotNet.Tests.Platform.Contracts;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;

namespace AioTieba4DotNet.Tests.Governance.Contracts;

[TestClass]
[TestCategory(OnlineTestParityCategories.SharedSeams)]
public sealed class MappingCoverageContractTests
{
    [TestMethod]
    public void UserInfoTMapper_ProtobufUserPreservesThreadAuthorPublicFields()
    {
        var mapped = UserInfoTMapper.FromTbData(CreateFullUser());

        Assert.IsNotNull(mapped);
        Assert.AreEqual(123, mapped.UserId);
        Assert.AreEqual("portrait-token", mapped.Portrait);
        Assert.AreEqual("author_name", mapped.UserName);
        Assert.AreEqual("author_show", mapped.NickNameNew);
        Assert.AreEqual(456789, mapped.TiebaUid);
        Assert.AreEqual(12, mapped.Level);
        Assert.AreEqual(8, mapped.GLevel);
        Assert.AreEqual(Gender.Female, mapped.Gender);
        Assert.AreEqual(6.5f, mapped.Age);
        Assert.AreEqual(34, mapped.PostNum);
        Assert.AreEqual(56, mapped.FanNum);
        Assert.AreEqual(78, mapped.FollowNum);
        Assert.AreEqual(9, mapped.ForumNum);
        Assert.AreEqual("author intro", mapped.Sign);
        Assert.AreEqual("ip location", mapped.Ip);
        CollectionAssert.AreEqual(new[] { "badge" }, mapped.Icons);
        Assert.IsTrue(mapped.IsBawu);
        Assert.IsTrue(mapped.IsVip);
        Assert.IsTrue(mapped.IsGod);
        Assert.AreEqual(PrivLike.Hide, mapped.PrivLike);
        Assert.AreEqual(PrivReply.Fans, mapped.PrivReply);
    }

    [TestMethod]
    public void UserInfoMapper_ProtobufUserPreservesSharedPublicFields()
    {
        var mapped = UserInfoMapper.FromTbData(CreateFullUser());

        Assert.IsNotNull(mapped);
        Assert.AreEqual(123, mapped.UserId);
        Assert.AreEqual("portrait-token", mapped.Portrait);
        Assert.AreEqual("author_show", mapped.NickNameNew);
        Assert.AreEqual(456789, mapped.TiebaUid);
        Assert.AreEqual(6.5f, mapped.Age);
        Assert.AreEqual("author intro", mapped.Sign);
        Assert.AreEqual("ip location", mapped.Ip);
        Assert.AreEqual(PrivLike.Hide, mapped.PrivLike);
        Assert.AreEqual(PrivReply.Fans, mapped.PrivReply);
    }

    [TestMethod]
    public void UserInfoTUidMapper_InvalidNumericStringsKeepDefaultValues()
    {
        var mapped = UserInfoTUidMapper.FromTbData(new User
        {
            Name = "minimal",
            TiebaUid = "not-a-number",
            TbAge = "unknown"
        });

        Assert.AreEqual(0, mapped.TiebaUid);
        Assert.AreEqual(0f, mapped.Age);
        Assert.AreEqual(string.Empty, mapped.Sign);
        Assert.AreEqual(string.Empty, mapped.Ip);
        Assert.IsFalse(mapped.IsVip);
        Assert.IsFalse(mapped.IsGod);
        Assert.AreEqual(PrivLike.Public, mapped.PrivLike);
        Assert.AreEqual(PrivReply.All, mapped.PrivReply);
    }

    [TestMethod]
    public void UserInfoGuInfoAppMapper_MapsAppUserInfoThroughSharedUserFields()
    {
        var user = CreateFullUser();
        user.NewTshowIcon.Clear();
        user.VipInfo = new User.Types.UserVipInfo { VStatus = 3 };

        var mapped = UserInfoGuInfoAppMapper.FromTbData(user);

        Assert.AreEqual("author_show", mapped.NickNameNew);
        Assert.AreEqual(456789, mapped.TiebaUid);
        Assert.AreEqual(6.5f, mapped.Age);
        Assert.AreEqual("author intro", mapped.Sign);
        Assert.AreEqual("ip location", mapped.Ip);
        Assert.IsTrue(mapped.IsVip);
    }

    [TestMethod]
    public void UserInfoPfMapper_ProfileUserPreservesCommonAndProfileFields()
    {
        var user = CreateFullUser();
        user.VirtualImageInfo = new User.Types.VirtualImageInfo
        {
            IssetVirtualImage = 1,
            PersonalState = new User.Types.VirtualImageInfo.Types.PersonalState { Text = "profile state" }
        };
        var data = new ProfileResIdl.Types.DataRes
        {
            User = user,
            AntiStat = new ProfileResIdl.Types.DataRes.Types.Anti
            {
                BlockStat = 1,
                HideStat = 1,
                DaysTofree = 31
            },
            UserAgreeInfo = new ProfileResIdl.Types.DataRes.Types.UserAgreeInfo { TotalAgreeNum = 99 }
        };

        var mapped = UserInfoPfMapper.FromTbData(data);

        Assert.AreEqual("author intro", mapped.Sign);
        Assert.AreEqual(6.5f, mapped.Age);
        Assert.AreEqual("ip location", mapped.Ip);
        Assert.AreEqual(99, mapped.AgreeNum);
        Assert.IsTrue(mapped.IsBlocked);
        Assert.IsTrue(mapped.VImage.Enabled);
        Assert.AreEqual("profile state", mapped.VImage.State);
    }

    [TestMethod]
    public void ThreadAndPostMappers_PreserveFieldsAvailableOnTheirOwnProtobufSources()
    {
        var thread = ThreadMapper.FromTbData(new ThreadInfo
        {
            Id = 10,
            Fid = 20,
            Fname = "thread_forum",
            FirstPostId = 30,
            Title = "thread title"
        });
        var post = PostMapper.FromTbData(new Post
        {
            Id = 40,
            Tid = 10,
            Content = { new PbContent { Type = 0, Text = "post text" } }
        });

        Assert.AreEqual(20, thread.Fid);
        Assert.AreEqual("thread_forum", thread.Fname);
        Assert.AreEqual(10, post.Tid);
    }

    [TestMethod]
    public void UserPostGroupsMapper_PreservesEachPostGroupUser()
    {
        var data = new UserPostResIdl.Types.DataRes();
        data.PostList.Add(CreatePostInfoList(1, "first_user", 101));
        data.PostList.Add(CreatePostInfoList(2, "second_user", 202));

        var mapped = UserPostGroupsMapper.FromTbData(data);

        Assert.AreEqual(2, mapped.Count);
        Assert.AreEqual(1, mapped[0][0].User?.UserId);
        Assert.AreEqual("first_user", mapped[0][0].User?.UserName);
        Assert.AreEqual(2, mapped[1][0].User?.UserId);
        Assert.AreEqual("second_user", mapped[1][0].User?.UserName);
    }

    [TestMethod]
    public void VirtualImageMapper_MissingThreadCustomFigureKeepsDisabledDefault()
    {
        var missing = VirtualImagePfMapper.FromTbData(new ThreadInfo());
        var present = VirtualImagePfMapper.FromTbData(new ThreadInfo
        {
            CustomFigure = new ThreadInfo.Types.CustomFigure { BackgroundValue = "enabled" },
            CustomState = new ThreadInfo.Types.CustomState { Content = "thread state" }
        });

        Assert.IsFalse(missing.Enabled);
        Assert.AreEqual(string.Empty, missing.State);
        Assert.IsTrue(present.Enabled);
        Assert.AreEqual("thread state", present.State);
    }

    [TestMethod]
    public void ShareThreadMapper_InvalidStringTidKeepsDefaultTid()
    {
        var mapped = ShareThreadMapper.FromTbData(new ThreadInfo.Types.OriginThreadInfo
        {
            Tid = "not-a-number",
            Title = "shared"
        });

        Assert.AreEqual(0, mapped.Tid);
        Assert.AreEqual("shared", mapped.Title);
    }

    [TestMethod]
    public void FragVoiceMapper_InvalidPostInfoDurationKeepsDefaultDuration()
    {
        var mapped = FragVoiceMapper.FromTbData(new PostInfoList.Types.PostInfoContent.Types.Abstract
        {
            VoiceMd5 = "voice-md5",
            DuringTime = "not-a-number"
        });

        Assert.AreEqual("voice-md5", mapped.Md5);
        Assert.AreEqual(0, mapped.Duration);
    }

    [TestMethod]
    public void FragLinkMapper_MissingOrRelativeUrlKeepsNonNullFallbackUrl()
    {
        var pbLink = FragLinkMapper.FromTbData(new PbContent
        {
            Type = 1,
            Text = "pb title",
            Link = string.Empty
        });
        var abstractLink = FragLinkMapper.FromTbData(new PostInfoList.Types.PostInfoContent.Types.Abstract
        {
            Type = 1,
            Text = "abstract title",
            Link = "/relative"
        });

        Assert.AreEqual("pb title", pbLink.Title);
        Assert.AreEqual(string.Empty, pbLink.Text);
        Assert.AreEqual("about:blank", pbLink.RawUrl.ToString());
        Assert.AreEqual("abstract title", abstractLink.Title);
        Assert.AreEqual("/relative", abstractLink.Text);
        Assert.AreEqual("about:blank", abstractLink.RawUrl.ToString());
    }

    [TestMethod]
    public void UserPortraitMappers_NormalizeShortQuerySuffixAtQuestionMark()
    {
        Assert.AreEqual("login", UserInfoLoginMapper.FromTbData(new JObject
        {
            ["portrait"] = "login?x=1"
        }).Portrait);
        Assert.AreEqual("json", UserInfoJsonMapper.FromTbData(new JObject
        {
            ["portrait"] = "json?x=1"
        }).Portrait);
        Assert.AreEqual("panel", UserInfoPanelMapper.FromTbData(new JObject
        {
            ["portrait"] = "panel?x=1"
        }).Portrait);
        Assert.AreEqual("self", UserInfoSelfMoIndexMapper.FromTbData(new JObject
        {
            ["portrait"] = "self?x=1"
        }).Portrait);
        Assert.AreEqual("web", UserInfoGuInfoWebMapper.FromTbData(new JObject
        {
            ["uid"] = 1,
            ["uname"] = "web_user",
            ["portrait"] = "web?x=1"
        }).Portrait);
        Assert.AreEqual("forum-user", UserInfoUfMapper.FromTbData(new JObject
        {
            ["portrait"] = "forum-user?x=1"
        }).Portrait);
        Assert.AreEqual("old-blacklist", BlacklistOldUserMapper.FromTbData(
            new UserMuteQueryResIdl.Types.DataRes.Types.MuteUser
            {
                Portrait = "old-blacklist?x=1"
            }).Portrait);

        var lastReplyersData = new FrsPageResIdl4lp.Types.DataRes
        {
            Forum = new FrsPageResIdl4lp.Types.DataRes.Types.ForumInfo
            {
                Id = 9,
                Name = "forum"
            }
        };
        lastReplyersData.ThreadList.Add(new ThreadInfo
        {
            Id = 1,
            Author = new User
            {
                Id = 2,
                Portrait = "last-replyer?x=1"
            }
        });

        Assert.AreEqual("last-replyer", LastReplyersMapper.FromTbData(lastReplyersData)[0].User.Portrait);
    }

    private static User CreateFullUser()
    {
        var user = new User
        {
            Id = 123,
            Name = "author_name",
            NameShow = "author_show",
            Portrait = "portrait-token?from=test",
            TiebaUid = "456789",
            LevelId = 12,
            UserGrowth = new User.Types.UserGrowth { LevelId = 8 },
            Gender = (int)Gender.Female,
            TbAge = "6.5",
            PostNum = 34,
            FansNum = 56,
            ConcernNum = 78,
            MyLikeNum = 9,
            Intro = "author intro",
            IpAddress = "ip location",
            IsBawu = 1,
            PrivSets = new User.Types.PrivSets
            {
                Like = (int)PrivLike.Hide,
                Reply = (int)PrivReply.Fans
            },
            NewGodData = new User.Types.NewGodInfo { Status = 1 }
        };
        user.Iconinfo.Add(new User.Types.Icon { Name = "badge" });
        user.Iconinfo.Add(new User.Types.Icon());
        user.NewTshowIcon.Add(new User.Types.TshowInfo { Name = "vip" });
        return user;
    }

    private static PostInfoList CreatePostInfoList(long userId, string userName, ulong postId)
    {
        var post = new PostInfoList
        {
            ForumId = 11,
            ThreadId = 22,
            UserId = userId,
            UserName = userName,
            UserPortrait = $"{userName}_portrait",
            NameShow = $"{userName}_show"
        };
        post.Content.Add(new PostInfoList.Types.PostInfoContent
        {
            PostId = postId,
            CreateTime = 1234
        });
        return post;
    }
}
