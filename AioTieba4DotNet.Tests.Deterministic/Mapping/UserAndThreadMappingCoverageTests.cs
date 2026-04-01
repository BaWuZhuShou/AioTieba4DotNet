#nullable enable
using System.Collections.Generic;
using AioTieba4DotNet.Api.Login.Entities;
using AioTieba4DotNet.Internal.Mapping;
using AioTieba4DotNet.Models.Contents;
using AioTieba4DotNet.Models.Shared;
using AioTieba4DotNet.Models.Threads;
using AioTieba4DotNet.Models.Users;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;

namespace AioTieba4DotNet.Tests.Mapping;

[TestClass]
public sealed class UserAndThreadMappingCoverageTests
{
    [TestMethod]
    public void UserInfoLoginMapper_FromTbData_NormalizesPortraitAndDefaultsMissingFields()
    {
        var mapped = UserInfoLoginMapper.FromTbData(new JObject
        {
            ["id"] = 42,
            ["portrait"] = "tb.1.login?012345678901",
            ["name"] = "login-user"
        });

        var fallback = UserInfoLoginMapper.FromTbData(new JObject());

        Assert.AreEqual(42L, mapped.UserId);
        Assert.AreEqual("tb.1.login", mapped.Portrait);
        Assert.AreEqual("login-user", mapped.UserName);
        Assert.AreEqual(0L, fallback.UserId);
        Assert.AreEqual(string.Empty, fallback.Portrait);
        Assert.AreEqual(string.Empty, fallback.UserName);
    }

    [TestMethod]
    public void UserInfoGuInfoWebMapper_FromTbData_HandlesUidNamedUsersAndPortraitFallbacks()
    {
        var trimmed = UserInfoGuInfoWebMapper.FromTbData(new JObject
        {
            ["uid"] = 42,
            ["uname"] = "42",
            ["portrait"] = "tb.1.web?from=pc",
            ["show_nickname"] = "Web Nick"
        });
        var fallback = UserInfoGuInfoWebMapper.FromTbData(new JObject
        {
            ["uid"] = 43,
            ["uname"] = "safe-user",
            ["portrait"] = "tb.1.raw"
        });

        Assert.AreEqual(42L, trimmed.UserId);
        Assert.AreEqual(string.Empty, trimmed.UserName);
        Assert.AreEqual("tb.1.web", trimmed.Portrait);
        Assert.AreEqual("Web Nick", trimmed.NickNameNew);
        Assert.AreEqual(43L, fallback.UserId);
        Assert.AreEqual("safe-user", fallback.UserName);
        Assert.AreEqual("tb.1.raw", fallback.Portrait);
        Assert.AreEqual(string.Empty, fallback.NickNameNew);
    }

    [TestMethod]
    public void UserInfoUfMapper_FromTbData_HandlesNullAndPortraitNormalization()
    {
        var empty = UserInfoUfMapper.FromTbData(null);
        var trimmed = UserInfoUfMapper.FromTbData(new JObject
        {
            ["id"] = 42,
            ["portrait"] = "tb.1.uf?012345678901",
            ["name"] = "uf-user",
            ["is_like"] = 1
        });
        var raw = UserInfoUfMapper.FromTbData(new JObject
        {
            ["id"] = 43,
            ["portrait"] = "tb.1.raw",
            ["name"] = "raw-user",
            ["is_like"] = 0
        });

        Assert.AreEqual(0L, empty.UserId);
        Assert.AreEqual(string.Empty, empty.Portrait);
        Assert.AreEqual(string.Empty, empty.UserName);
        Assert.IsFalse(empty.IsLike);
        Assert.AreEqual(42L, trimmed.UserId);
        Assert.AreEqual("tb.1.uf", trimmed.Portrait);
        Assert.AreEqual(string.Empty, trimmed.UserName);
        Assert.IsTrue(trimmed.IsLike);
        Assert.AreEqual(43L, raw.UserId);
        Assert.AreEqual("tb.1.raw", raw.Portrait);
        Assert.AreEqual(string.Empty, raw.UserName);
        Assert.IsFalse(raw.IsLike);
    }

    [TestMethod]
    public void UserInfoGuInfoAppMapper_FromTbData_TrimsPortraitQueryAndPreservesFlags()
    {
        var trimmed = UserInfoGuInfoAppMapper.FromTbData(new global::User
        {
            Id = 42,
            Portrait = "tb.1.app?012345678901",
            Name = "app-user",
            NameShow = "App User",
            Gender = (int)AioTieba4DotNet.Models.Gender.Male,
            VipInfo = new global::User.Types.UserVipInfo { VStatus = 1 },
            NewGodData = new global::User.Types.NewGodInfo { Status = 1 },
            IpAddress = "127.0.0.1"
        });
        var raw = UserInfoGuInfoAppMapper.FromTbData(new global::User
        {
            Id = 43,
            Portrait = "tb.1.raw",
            Name = "raw-user",
            NameShow = "Raw User",
            Gender = (int)AioTieba4DotNet.Models.Gender.Female,
            VipInfo = new global::User.Types.UserVipInfo(),
            NewGodData = new global::User.Types.NewGodInfo(),
            IpAddress = "10.0.0.1"
        });

        Assert.AreEqual(42L, trimmed.UserId);
        Assert.AreEqual("tb.1.app", trimmed.Portrait);
        Assert.AreEqual("app-user", trimmed.UserName);
        Assert.AreEqual("App User", trimmed.NickNameOld);
        Assert.AreEqual(AioTieba4DotNet.Models.Gender.Male, trimmed.Gender);
        Assert.IsTrue(trimmed.IsVip);
        Assert.IsTrue(trimmed.IsGod);
        Assert.AreEqual("127.0.0.1", trimmed.Ip);
        Assert.AreEqual(43L, raw.UserId);
        Assert.AreEqual("tb.1.raw", raw.Portrait);
        Assert.AreEqual(AioTieba4DotNet.Models.Gender.Female, raw.Gender);
        Assert.IsFalse(raw.IsVip);
        Assert.IsFalse(raw.IsGod);
        Assert.AreEqual("10.0.0.1", raw.Ip);
    }

    [TestMethod]
    public void AtMessagesMapper_FromTbData_MapsEntriesAndPage()
    {
        var mapped = AtMessagesMapper.FromTbData(new JObject
        {
            ["at_list"] = new JArray
            {
                new JObject
                {
                    ["content"] = "reply body",
                    ["fname"] = "csharp",
                    ["thread_id"] = 11,
                    ["post_id"] = 22,
                    ["replyer"] = new JObject
                    {
                        ["id"] = 42,
                        ["portrait"] = "tb.1.replyer?012345678901",
                        ["name"] = "replyer",
                        ["name_show"] = "Replyer"
                    },
                    ["is_floor"] = 1,
                    ["is_first_post"] = 1,
                    ["time"] = 1711111111
                }
            },
            ["page"] = new JObject
            {
                ["page_size"] = 20,
                ["current_page"] = 2,
                ["total_page"] = 3,
                ["total_count"] = 44,
                ["has_more"] = 1,
                ["has_prev"] = 0
            }
        });

        Assert.AreEqual(1, mapped.Count);
        Assert.AreEqual("reply body", mapped[0].Content);
        Assert.AreEqual("csharp", mapped[0].Fname);
        Assert.AreEqual(42L, mapped[0].Replyer?.UserId);
        Assert.AreEqual("tb.1.replyer", mapped[0].Replyer?.Portrait);
        Assert.IsTrue(mapped[0].IsFloor);
        Assert.IsTrue(mapped[0].IsFirstPost);
        Assert.AreEqual(2, mapped.Page.CurrentPage);
        Assert.IsTrue(mapped.Page.HasMore);
        Assert.IsFalse(mapped.Page.HasPrevious);
    }

    [TestMethod]
    public void BlacklistUsersMapper_FromTbData_MapsUsers()
    {
        var mapped = BlacklistUsersMapper.FromTbData(new JObject
        {
            ["user_perm_list"] = new JArray
            {
                new JObject
                {
                    ["id"] = 42,
                    ["portrait"] = "tb.1.blocked?012345678901",
                    ["name"] = "blocked-user",
                    ["name_show"] = "Blocked",
                    ["perm_list"] = new JObject
                    {
                        ["follow"] = 1,
                        ["interact"] = 0,
                        ["chat"] = 1
                    }
                }
            }
        });

        Assert.AreEqual(1, mapped.Count);
        Assert.AreEqual(42L, mapped[0].UserId);
        Assert.AreEqual("tb.1.blocked", mapped[0].Portrait);
        Assert.AreEqual("blocked-user", mapped[0].UserName);
        Assert.IsTrue(mapped[0].BlockFollow);
        Assert.IsFalse(mapped[0].BlockInteract);
        Assert.IsTrue(mapped[0].BlockChat);
    }

    [TestMethod]
    public void UserPostsMapper_FromTbData_AssignsContainerForumAndThreadIds()
    {
        var postList = CreateUserPostList(title: "ignored", forumId: 8, forumName: "forum", threadId: 9, postId: 10,
            userId: 42, userName: "author", portrait: "tb.1.author?012345678901", contentText: "body");
        postList.Content.Add(new global::PostInfoList.Types.PostInfoContent
        {
            PostId = 10,
            PostType = 1,
            CreateTime = 1711111111,
            PostContent = { new global::PostInfoList.Types.PostInfoContent.Types.Abstract { Type = 0, Text = "reply" } }
        });

        var mapped = UserPostsMapper.FromTbData(postList);

        Assert.AreEqual(1, mapped.Count);
        Assert.AreEqual(8L, mapped.Fid);
        Assert.AreEqual(9L, mapped.Tid);
        Assert.AreEqual(8L, mapped[0].Fid);
        Assert.AreEqual(9L, mapped[0].Tid);
        Assert.AreEqual(10, mapped[0].Pid);
        Assert.IsTrue(mapped[0].IsComment);
        Assert.AreEqual("reply", mapped[0].Contents.Text);
    }

    [TestMethod]
    public void UserThreadMapper_FromTbData_CoversAgreeFallbackAndTitleBranches()
    {
        var noAgree = CreateUserPostList(title: string.Empty, forumId: 8, forumName: "forum", threadId: 9, postId: 10,
            userId: 42, userName: "author", portrait: "tb.1.author?012345678901", contentText: "body");
        noAgree.ThreadType = 70;
        noAgree.Agree = null!;
        var mappedNoAgree = UserThreadMapper.FromTbData(noAgree);

        var withAgree = CreateUserPostList(title: "Help title", forumId: 8, forumName: "forum", threadId: 9, postId: 10,
            userId: 42, userName: "author", portrait: "tb.1.author?012345678901", contentText: "body");
        withAgree.ThreadType = 71;
        withAgree.Agree = new global::Agree { AgreeNum = 7, DisagreeNum = 2 };
        var mappedWithAgree = UserThreadMapper.FromTbData(withAgree);

        Assert.AreEqual(string.Empty, mappedNoAgree.Title);
        Assert.AreEqual("body", mappedNoAgree.Contents.Text);
        Assert.AreEqual(0L, mappedNoAgree.Agree);
        Assert.AreEqual(0L, mappedNoAgree.Disagree);
        Assert.IsFalse(mappedNoAgree.IsHelp);
        Assert.AreEqual("Help title", mappedWithAgree.Title);
        Assert.AreEqual("Help title\nbody", mappedWithAgree.Text);
        Assert.AreEqual(7L, mappedWithAgree.Agree);
        Assert.AreEqual(2L, mappedWithAgree.Disagree);
        Assert.IsTrue(mappedWithAgree.IsHelp);
    }

    [TestMethod]
    public void UserPostGroupsMapper_FromTbData_AssignsSharedUserAcrossNestedPosts()
    {
        var response = new global::UserPostResIdl.Types.DataRes();
        var postList = CreateUserPostList(title: "ignored", forumId: 8, forumName: "forum", threadId: 9, postId: 10,
            userId: 42, userName: "author", portrait: "tb.1.author?012345678901", contentText: "body");
        postList.Content.Add(new global::PostInfoList.Types.PostInfoContent
        {
            PostId = 10,
            PostType = 0,
            CreateTime = 1711111111,
            PostContent = { new global::PostInfoList.Types.PostInfoContent.Types.Abstract { Type = 0, Text = "reply" } }
        });
        response.PostList.Add(postList);

        var mapped = UserPostGroupsMapper.FromTbData(response);
        var empty = UserPostGroupsMapper.FromTbData(new global::UserPostResIdl.Types.DataRes());

        Assert.AreEqual(1, mapped.Count);
        Assert.AreEqual(1, mapped[0].Count);
        Assert.AreEqual("author", mapped[0][0].User?.UserName);
        Assert.AreEqual("tb.1.author", mapped[0][0].User?.Portrait);
        Assert.AreEqual(0, empty.Count);
    }

    [TestMethod]
    public void UserThreadsMapper_FromTbData_AssignsSharedUserAcrossThreads()
    {
        var response = new global::UserPostResIdl.Types.DataRes();
        response.PostList.Add(CreateUserPostList(title: "Thread title", forumId: 8, forumName: "forum", threadId: 9,
            postId: 10, userId: 42, userName: "author", portrait: "tb.1.author?012345678901", contentText: "body"));

        var mapped = UserThreadsMapper.FromTbData(response);
        var empty = UserThreadsMapper.FromTbData(new global::UserPostResIdl.Types.DataRes());

        Assert.AreEqual(1, mapped.Count);
        Assert.AreEqual("Thread title", mapped[0].Title);
        Assert.AreEqual("body", mapped[0].Contents.Text);
        Assert.AreEqual("author", mapped[0].User?.UserName);
        Assert.AreEqual("tb.1.author", mapped[0].User?.Portrait);
        Assert.AreEqual(0, empty.Count);
    }

    [TestMethod]
    public void ShareThreadMapper_FromTbData_MapsVoteInfoIdentifiersAndContent()
    {
        var origin = new global::ThreadInfo.Types.OriginThreadInfo
        {
            Title = "Shared title",
            Fid = 8,
            Fname = "forum",
            Tid = "9",
            Pid = 10,
            PollInfo = new global::PollInfo
            {
                Title = "Vote",
                IsMulti = 1,
                TotalPoll = 100,
                TotalNum = 2,
                Options = { new global::PollInfo.Types.PollOption { Num = 60, Text = "Option A" } }
            }
        };
        origin.Content.Add(new global::PbContent { Type = 0, Text = "shared text", Uid = 42 });

        var mapped = ShareThreadMapper.FromTbData(origin);

        Assert.AreEqual("Shared title", mapped.Title);
        Assert.AreEqual("shared text", mapped.Content.Text);
        Assert.AreEqual(42L, mapped.AuthorId);
        Assert.AreEqual(8L, mapped.Fid);
        Assert.AreEqual("forum", mapped.Fname);
        Assert.AreEqual(9L, mapped.Tid);
        Assert.AreEqual(10L, mapped.Pid);
        Assert.IsNotNull(mapped.VoteInfo);
        Assert.AreEqual("Vote", mapped.VoteInfo!.Title);
        Assert.IsTrue(mapped.VoteInfo.IsMulti);
        Assert.AreEqual(1, mapped.VoteInfo.Options.Count);
        StringAssert.Contains(mapped.ToString(), "Shared title");
    }

    [TestMethod]
    public void ThreadMapper_FromTbData_HandlesNullAndShareBranches()
    {
        var empty = ThreadMapper.FromTbData(null);
        var noAgree = ThreadMapper.FromTbData(new global::ThreadInfo
        {
            Title = "No agree"
        });
        var shared = ThreadMapper.FromTbData(new global::ThreadInfo
        {
            Title = "Thread title",
            Id = 123,
            FirstPostId = 456,
            AuthorId = 42,
            Author = new global::User
            {
                Id = 42,
                Name = "author",
                NameShow = "Author",
                Portrait = "tb.1.author?012345678901",
                IsBawu = 1,
                PrivSets = new global::User.Types.PrivSets { Like = 0, Reply = 0 }
            },
            FirstPostContent = { new global::PbContent { Type = 0, Text = "body" } },
            ThreadType = 71,
            TabId = 9,
            IsGood = 1,
            IsTop = 1,
            IsShareThread = 1,
            IsFrsMask = 1,
            IsLivepost = 1,
            PollInfo = new global::PollInfo
            {
                Title = "Vote",
                IsMulti = 1,
                Options = { new global::PollInfo.Types.PollOption { Num = 1, Text = "Option" } }
            },
            OriginThreadInfo = new global::ThreadInfo.Types.OriginThreadInfo
            {
                Title = "Shared title",
                Fid = 8,
                Fname = "forum",
                Tid = "789",
                Pid = 99,
                Content = { new global::PbContent { Type = 0, Text = "shared body", Uid = 42 } }
            },
            Agree = new global::Agree { AgreeNum = 7, DisagreeNum = 2 },
            ViewNum = 100,
            ReplyNum = 5,
            ShareNum = 6,
            CreateTime = 1,
            LastTimeInt = 2
        });

        Assert.IsNotNull(empty.Content);
        Assert.IsNotNull(empty.VirtualImage);
        Assert.AreEqual(0, empty.Content.Frags.Count);
        Assert.AreEqual(0, noAgree.Agree);
        Assert.AreEqual(0, noAgree.Disagree);
        Assert.AreEqual("Thread title", shared.Title);
        Assert.AreEqual("body", shared.Content.Text);
        Assert.AreEqual(42L, shared.User!.UserId);
        Assert.IsTrue(shared.IsGood);
        Assert.IsTrue(shared.IsTop);
        Assert.IsTrue(shared.IsShare);
        Assert.IsTrue(shared.IsHide);
        Assert.IsTrue(shared.IsLivePost);
        Assert.IsNotNull(shared.VoteInfo);
        Assert.IsNotNull(shared.ShareOrigin);
        Assert.AreEqual(7, shared.Agree);
        Assert.AreEqual(2, shared.Disagree);
        Assert.AreEqual(100, shared.ViewNum);
    }

    [TestMethod]
    public void ThreadModels_TextPropertiesAndFlags_UseExpectedFormatting()
    {
        var content = new Content
        {
            Texts = [new FragText { Text = "hello" }],
            Frags = [new FragText { Text = "hello" }]
        };
        var post = new global::AioTieba4DotNet.Models.Threads.Post
        {
            Content = content,
            Sign = "from phone",
            Pid = 12,
            AuthorId = 34,
            Floor = 1,
            ReplyNum = 0,
            Agree = 0,
            Disagree = 0,
            CreateTime = 0
        };
        var share = new ShareThread
        {
            Content = content,
            Title = "Title",
            AuthorId = 34,
            Fid = 8,
            Fname = "forum",
            Tid = 9,
            Pid = 10,
            VoteInfo = new VoteInfo { Title = "Vote", Options = [] }
        };
        var userThread = new UserThread
        {
            Contents = content,
            Title = "Help title",
            Type = 71
        };

        Assert.AreEqual("hello\nfrom phone", post.Text);
        StringAssert.Contains(share.Text, "Title");
        StringAssert.Contains(share.ToString(), "forum");
        Assert.AreEqual("Help title\nhello", userThread.Text);
        Assert.IsTrue(userThread.IsHelp);
    }

    [TestMethod]
    public void CommentMapper_FromTbData_HandlesNullAndFullyTrimmedReplyPrefix()
    {
        var empty = CommentMapper.FromTbData(null);
        var replyOnly = CommentMapper.FromTbData(new global::SubPostList
        {
            Id = 10,
            AuthorId = 20,
            Author = new global::User { Id = 20, Name = "comment-author", NameShow = "Comment Author", Portrait = "tb.1.comment?012345678901" },
            Time = 1711111111,
            Content =
            {
                new global::PbContent { Type = 0, Text = "回复 " },
                new global::PbContent { Type = 2, Text = "@target", Uid = 30 },
                new global::PbContent { Type = 0, Text = " :" }
            }
        });

        Assert.IsNotNull(empty.Content);
        Assert.AreEqual(0, empty.Content.Frags.Count);
        Assert.AreEqual(10L, replyOnly.Pid);
        Assert.AreEqual(20L, replyOnly.AuthorId);
        Assert.AreEqual(30L, replyOnly.ReplyToId);
        Assert.AreEqual(0, replyOnly.Content.Frags.Count);
        Assert.AreEqual(0, replyOnly.Content.Texts.Count);
        Assert.AreEqual(0, replyOnly.Content.Ats.Count);
        Assert.AreEqual(0, replyOnly.Agree);
        Assert.AreEqual(0, replyOnly.Disagree);
        Assert.AreEqual(1711111111L, replyOnly.CreateTime);
    }

    private static global::PostInfoList CreateUserPostList(string title, ulong forumId, string forumName, ulong threadId,
        ulong postId, long userId, string userName, string portrait, string contentText)
    {
        return new global::PostInfoList
        {
            Title = title,
            ForumId = forumId,
            ForumName = forumName,
            ThreadId = threadId,
            PostId = postId,
            UserId = userId,
            UserName = userName,
            UserPortrait = portrait,
            NameShow = userName,
            FirstPostContent = { new global::PbContent { Type = 0, Text = contentText } }
        };
    }
}
