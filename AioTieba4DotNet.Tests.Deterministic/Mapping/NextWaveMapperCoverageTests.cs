#nullable enable
using System;
using AioTieba4DotNet.Internal.Mapping;
using AioTieba4DotNet.Models.Forums;
using AioTieba4DotNet.Models.Threads;
using AioTieba4DotNet.Models.Users;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;

namespace AioTieba4DotNet.Tests.Mapping;

[TestClass]
public sealed class NextWaveMapperCoverageTests
{
    [TestMethod]
    public void ForumAndUserMappers_CoverMissingListsAndFallbackTokens()
    {
        var followForums = FollowForumsMapper.FromTbData(new JObject
        {
            ["forum_list"] = new JObject
            {
                ["non-gconforum"] = new JArray
                {
                    new JObject
                    {
                        ["id"] = 7,
                        ["name"] = "forum-a",
                        ["level_id"] = 3,
                        ["cur_score"] = 4
                    },
                    new JValue("skip")
                },
                ["gconforum"] = new JArray
                {
                    new JObject
                    {
                        ["id"] = 8,
                        ["name"] = "forum-b",
                        ["level_id"] = 5,
                        ["cur_score"] = 6
                    }
                }
            },
            ["has_more"] = 1
        });
        var followFallback = FollowForumsMapper.FromTbData(new JObject { ["has_more"] = 0 });

        var selfFollowForumsV1 = SelfFollowForumsV1Mapper.FromTbData(new JObject
        {
            ["list"] = new JArray
            {
                new JObject
                {
                    ["forum_id"] = 11,
                    ["forum_name"] = "forum-c",
                    ["level_id"] = 2
                },
                new JObject()
            },
            ["page"] = new JObject
            {
                ["cur_page"] = 2,
                ["total_page"] = 1
            }
        });
        var selfFollowFallback = SelfFollowForumsV1Mapper.FromTbData(new JObject());

        var memberUsersWithoutPagination = MemberUsersMapper.FromHtml("""
            <div class="name_wrap"><a title="Alice &amp; Bob" href="/home/main?id=tb.1.alice&amp;fr=home"><span class="level_7"></span></a></div>
            """);
        var memberUsersWithPagination = MemberUsersMapper.FromHtml("""
            <div class="tbui_pagination"><li class="active">2</li>(4)</div>
            <div class="name_wrap"><a title="Charlie" href="/home/main?id=tb.1.charlie"><span class="level_0"></span></a></div>
            """);

        var userInfoWebTrimmed = UserInfoGuInfoWebMapper.FromTbData(new JObject
        {
            ["uid"] = 42,
            ["uname"] = "42",
            ["portrait"] = "tb.1.web?from=pc",
            ["show_nickname"] = "Web Nick"
        });
        var userInfoWebRaw = UserInfoGuInfoWebMapper.FromTbData(new JObject
        {
            ["uid"] = 43,
            ["uname"] = "safe-user",
            ["portrait"] = "tb.1.raw"
        });

        var userInfoTUidParsed = UserInfoTUidMapper.FromTbData(new global::User
        {
            Id = 44,
            Portrait = "tb.1.uid?012345678901",
            Name = "uid-user",
            NameShow = "Uid User",
            TiebaUid = "123456",
            TbAge = "2.5",
            Intro = "hello",
            NewGodData = new global::User.Types.NewGodInfo { Status = 1 }
        });
        var userInfoTUidFallback = UserInfoTUidMapper.FromTbData(new global::User
        {
            Id = 45,
            Portrait = "tb.1.raw",
            Name = "raw-user",
            NameShow = "Raw User",
            TiebaUid = string.Empty,
            TbAge = string.Empty,
            Intro = string.Empty,
            NewGodData = new global::User.Types.NewGodInfo()
        });

        var blacklistWithPerms = BlacklistUserMapper.FromTbData(new JObject
        {
            ["id"] = 46,
            ["portrait"] = "tb.1.black?012345678901",
            ["name"] = "blocked-user",
            ["name_show"] = "Blocked User",
            ["perm_list"] = new JObject
            {
                ["follow"] = 1,
                ["interact"] = 0,
                ["chat"] = 1
            }
        });
        var blacklistFallback = BlacklistUserMapper.FromTbData(new JObject
        {
            ["id"] = 47,
            ["portrait"] = "tb.1.raw",
            ["name"] = "raw-user",
            ["name_show"] = "Raw User"
        });

        var recoverPageNull = RecoverPageMapper.FromTbData(null);
        var recoverPageValues = RecoverPageMapper.FromTbData(new JObject
        {
            ["pn"] = 2,
            ["rn"] = 20,
            ["has_more"] = 1
        });

        var virtualImageFromThreadEnabled = VirtualImagePfMapper.FromTbData(new global::ThreadInfo
        {
            CustomFigure = new global::ThreadInfo.Types.CustomFigure { BackgroundValue = "bg" },
            CustomState = new global::ThreadInfo.Types.CustomState { Content = "state" }
        });
        var virtualImageFromThreadDisabled = VirtualImagePfMapper.FromTbData(new global::ThreadInfo
        {
            CustomFigure = new global::ThreadInfo.Types.CustomFigure { BackgroundValue = string.Empty },
            CustomState = new global::ThreadInfo.Types.CustomState { Content = string.Empty }
        });
        var virtualImageFromUserEnabled = VirtualImagePfMapper.FromTbData(new global::User.Types.VirtualImageInfo
        {
            IssetVirtualImage = 1,
            PersonalState = new global::User.Types.VirtualImageInfo.Types.PersonalState { Text = "on" }
        });
        var virtualImageFromUserDisabled = VirtualImagePfMapper.FromTbData(new global::User.Types.VirtualImageInfo());

        Assert.AreEqual(2, followForums.Count);
        Assert.AreEqual(7UL, followForums[0].Fid);
        Assert.AreEqual("forum-a", followForums[0].Fname);
        Assert.AreEqual(8UL, followForums[1].Fid);
        Assert.IsTrue(followForums.HasMore);
        Assert.AreEqual(0, followFallback.Count);
        Assert.IsFalse(followFallback.HasMore);

        Assert.AreEqual(2, selfFollowForumsV1.Count);
        Assert.AreEqual(11UL, selfFollowForumsV1[0].Fid);
        Assert.IsFalse(selfFollowForumsV1.HasMore);
        Assert.IsTrue(selfFollowForumsV1.Page.HasPrevious);
        Assert.AreEqual(0, selfFollowFallback.Count);
        Assert.AreEqual(0, selfFollowFallback.Page.CurrentPage);
        Assert.IsFalse(selfFollowFallback.Page.HasMore);

        Assert.AreEqual(1, memberUsersWithoutPagination.Count);
        Assert.AreEqual(1, memberUsersWithoutPagination.Page.CurrentPage);
        Assert.AreEqual(1, memberUsersWithoutPagination.Page.TotalPage);
        Assert.IsFalse(memberUsersWithoutPagination.Page.HasMore);
        Assert.AreEqual("Alice & Bob", memberUsersWithoutPagination[0].UserName);
        Assert.AreEqual("tb.1.alice", memberUsersWithoutPagination[0].Portrait);
        Assert.AreEqual(7, memberUsersWithoutPagination[0].Level);
        Assert.AreEqual(1, memberUsersWithPagination.Count);
        Assert.AreEqual(2, memberUsersWithPagination.Page.CurrentPage);
        Assert.AreEqual(4, memberUsersWithPagination.Page.TotalPage);
        Assert.IsTrue(memberUsersWithPagination.Page.HasMore);
        Assert.IsTrue(memberUsersWithPagination.Page.HasPrevious);
        Assert.ThrowsExactly<ArgumentNullException>(() => new MemberUsers([], null!));

        Assert.AreEqual(42L, userInfoWebTrimmed.UserId);
        Assert.AreEqual(string.Empty, userInfoWebTrimmed.UserName);
        Assert.AreEqual("tb.1.web", userInfoWebTrimmed.Portrait);
        Assert.AreEqual("Web Nick", userInfoWebTrimmed.NickNameNew);
        Assert.AreEqual(43L, userInfoWebRaw.UserId);
        Assert.AreEqual("safe-user", userInfoWebRaw.UserName);
        Assert.AreEqual("tb.1.raw", userInfoWebRaw.Portrait);

        Assert.AreEqual(44L, userInfoTUidParsed.UserId);
        Assert.AreEqual("tb.1.uid", userInfoTUidParsed.Portrait);
        Assert.AreEqual(123456L, userInfoTUidParsed.TiebaUid);
        Assert.AreEqual(2.5F, userInfoTUidParsed.Age);
        Assert.AreEqual("hello", userInfoTUidParsed.Sign);
        Assert.IsTrue(userInfoTUidParsed.IsGod);
        Assert.AreEqual(45L, userInfoTUidFallback.UserId);
        Assert.AreEqual("tb.1.raw", userInfoTUidFallback.Portrait);
        Assert.AreEqual(0L, userInfoTUidFallback.TiebaUid);
        Assert.AreEqual(0F, userInfoTUidFallback.Age);
        Assert.IsFalse(userInfoTUidFallback.IsGod);

        Assert.AreEqual(46L, blacklistWithPerms.UserId);
        Assert.AreEqual("tb.1.black", blacklistWithPerms.Portrait);
        Assert.IsTrue(blacklistWithPerms.BlockFollow);
        Assert.IsFalse(blacklistWithPerms.BlockInteract);
        Assert.IsTrue(blacklistWithPerms.BlockChat);
        Assert.AreEqual(47L, blacklistFallback.UserId);
        Assert.IsFalse(blacklistFallback.BlockFollow);
        Assert.IsFalse(blacklistFallback.BlockInteract);
        Assert.IsFalse(blacklistFallback.BlockChat);

        Assert.AreEqual(0, recoverPageNull.CurrentPage);
        Assert.AreEqual(0, recoverPageNull.PageSize);
        Assert.AreEqual(2, recoverPageValues.CurrentPage);
        Assert.IsTrue(recoverPageValues.HasMore);
        Assert.IsTrue(recoverPageValues.HasPrevious);

        Assert.IsTrue(virtualImageFromThreadEnabled.Enabled);
        Assert.AreEqual("state", virtualImageFromThreadEnabled.State);
        Assert.IsFalse(virtualImageFromThreadDisabled.Enabled);
        Assert.AreEqual(string.Empty, virtualImageFromThreadDisabled.State);
        Assert.IsTrue(virtualImageFromUserEnabled.Enabled);
        Assert.AreEqual("on", virtualImageFromUserEnabled.State);
        Assert.IsFalse(virtualImageFromUserDisabled.Enabled);
        Assert.AreEqual(string.Empty, virtualImageFromUserDisabled.State);
    }

    [TestMethod]
    public void ThreadAndTabMappers_CoverFallbackCollectionsAndMatchedAuthors()
    {
        var tabsNull = TabMapMapper.FromTbData(null);
        var tabsMapped = TabMapMapper.FromTbData(new global::SearchPostForumResIdl.Types.DataRes
        {
            ExactMatch = new global::SearchPostForumResIdl.Types.DataRes.Types.SearchForum
            {
                ForumId = 7356044,
                ForumName = "csharp",
                TabInfo =
                {
                    new global::FrsTabInfo { TabId = 1, TabName = "全部" },
                    new global::FrsTabInfo { TabId = 2, TabName = "精华" }
                }
            }
        });

        var threadsData = new global::FrsPageResIdl.Types.DataRes
        {
            Forum = new global::FrsPageResIdl.Types.DataRes.Types.ForumInfo
            {
                Id = 7356044,
                Name = "csharp",
                FirstClass = "dev",
                SecondClass = "dotnet",
                MemberNum = 10,
                PostNum = 20,
                ThreadNum = 30
            },
            Page = new global::Page
            {
                CurrentPage = 2,
                TotalPage = 5,
                HasMore = 1,
                HasPrev = 1
            },
            NavTabInfo = new global::FrsPageResIdl.Types.DataRes.Types.NavTabInfo(),
            ForumRule = new global::FrsPageResIdl.Types.DataRes.Types.ForumRuleStatus { HasForumRule = 1 }
        };
        threadsData.Forum.Managers.Add(new global::FrsPageResIdl.Types.DataRes.Types.ForumInfo.Types.Manager());
        threadsData.NavTabInfo.Tab.Add(new global::FrsTabInfo { TabId = 1, TabName = "全部" });
        threadsData.NavTabInfo.Tab.Add(new global::FrsTabInfo { TabId = 2, TabName = "精华" });
        threadsData.UserList.Add(new global::User
        {
            Id = 42,
            Name = "author-a",
            NameShow = "Author A",
            Portrait = "tb.1.author-a?012345678901"
        });
        threadsData.ThreadList.Add(new global::ThreadInfo
        {
            Id = 1001,
            FirstPostId = 2001,
            Title = "thread-a",
            AuthorId = 42,
            Author = new global::User { Id = 42, Name = "author-a", NameShow = "Author A", Portrait = "tb.1.author-a?012345678901" },
            ThreadType = 71,
            TabId = 1,
            IsGood = 1,
            IsTop = 1,
            IsShareThread = 1,
            IsFrsMask = 1,
            IsLivepost = 1,
            ViewNum = 100,
            ReplyNum = 5,
            ShareNum = 6,
            Agree = new global::Agree { AgreeNum = 7, DisagreeNum = 8 },
            CreateTime = 9,
            LastTimeInt = 10,
            OriginThreadInfo = new global::ThreadInfo.Types.OriginThreadInfo
            {
                Title = "origin-title",
                Fname = "origin-forum",
                Tid = "9001",
                Pid = 77,
                Content = { new global::PbContent { Type = 0, Text = "shared body", Uid = 42 } }
            },
            CustomFigure = new global::ThreadInfo.Types.CustomFigure { BackgroundValue = "bg" },
            CustomState = new global::ThreadInfo.Types.CustomState { Content = "state" }
        });
        threadsData.ThreadList.Add(new global::ThreadInfo
        {
            Id = 1002,
            FirstPostId = 2002,
            Title = "thread-b",
            AuthorId = 99,
            Author = new global::User { Id = 99, Name = "author-b", NameShow = "Author B", Portrait = "tb.1.author-b" },
            ThreadType = 70,
            TabId = 2,
            IsGood = 0,
            IsTop = 0,
            IsShareThread = 0,
            IsFrsMask = 0,
            IsLivepost = 0,
            ViewNum = 11,
            ReplyNum = 12,
            ShareNum = 13,
            CreateTime = 14,
            LastTimeInt = 15,
            CustomFigure = new global::ThreadInfo.Types.CustomFigure { BackgroundValue = string.Empty },
            CustomState = new global::ThreadInfo.Types.CustomState { Content = string.Empty }
        });

        var threadsMapped = ThreadsMapper.FromTbData(threadsData);

        Assert.AreEqual(0, tabsNull.Count);
        Assert.AreEqual(2, tabsMapped.Count);
        Assert.AreEqual(1, tabsMapped["全部"]);
        Assert.AreEqual(2, tabsMapped["精华"]);

        Assert.AreEqual(2, threadsMapped.Objs.Count);
        Assert.AreEqual(7356044L, threadsMapped.Forum.Fid);
        Assert.AreEqual("csharp", threadsMapped.Forum.Fname);
        Assert.AreEqual(2, threadsMapped.Page.CurrentPage);
        Assert.IsTrue(threadsMapped.HasMore);
        Assert.IsTrue(threadsMapped.TabDictionary.ContainsKey("全部"));
        Assert.AreEqual("thread-a", threadsMapped.Objs[0].Title);
        Assert.AreEqual("tb.1.author-a", threadsMapped.Objs[0].User!.Portrait);
        Assert.IsTrue(threadsMapped.Objs[0].IsHelp);
        Assert.IsTrue(threadsMapped.Objs[0].IsShare);
        Assert.IsTrue(threadsMapped.Objs[0].IsHide);
        Assert.IsTrue(threadsMapped.Objs[0].IsLivePost);
        Assert.IsTrue(threadsMapped.Objs[0].VirtualImage.Enabled);
        Assert.AreEqual("state", threadsMapped.Objs[0].VirtualImage.State);
        Assert.AreEqual("thread-b", threadsMapped.Objs[1].Title);
        var fallbackUser = threadsMapped.Objs[1].User;
        Assert.IsNotNull(fallbackUser);
        Assert.AreEqual(string.Empty, fallbackUser.UserName);
        Assert.AreEqual(0L, fallbackUser.UserId);
        Assert.IsFalse(threadsMapped.Objs[1].IsHelp);
        Assert.IsFalse(threadsMapped.Objs[1].VirtualImage.Enabled);
        Assert.AreEqual(string.Empty, threadsMapped.Objs[1].VirtualImage.State);
    }
}
