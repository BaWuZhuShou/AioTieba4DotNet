#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using AioTieba4DotNet.Models;
using AioTieba4DotNet.Models.Admins;
using AioTieba4DotNet.Models.Contents;
using AioTieba4DotNet.Models.Forums;
using AioTieba4DotNet.Models.Messages;
using AioTieba4DotNet.Models.Shared;
using AioTieba4DotNet.Models.Threads;
using AioTieba4DotNet.Models.Users;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AioTieba4DotNet.Tests.ModelCoverage;

[TestClass]
public sealed class ModelCoverageTests
{
    [TestMethod]
    public void Containers_SupportEnumerationAndGuardMethods()
    {
        var fromList = new TestContainers(["a", "b"]);
        var fromEnumerable = new TestContainers((IEnumerable<string>?)new[] { "c" });
        var fromNullEnumerable = new TestContainers((IEnumerable<string>?)null);

        Assert.IsTrue(fromList.Any());
        Assert.AreEqual("a", fromList[0]);
        CollectionAssert.AreEqual(new[] { "a", "b" }, fromList.ToArray());
        CollectionAssert.AreEqual(new[] { "c" }, fromEnumerable.Objs.ToArray());
        Assert.IsFalse(fromNullEnumerable.Any());
        Assert.ThrowsExactly<NotImplementedException>(() => fromList.SetItem(0, "x"));
        Assert.ThrowsExactly<NotImplementedException>(() => fromList.RemoveAt(0));
    }

    [TestMethod]
    public void UserInfo_UsesPreferredNamesAndComparerBranches()
    {
        var preferred = new UserInfo { UserId = 1, UserName = "user", NickNameOld = "old", NickNameNew = "new", Portrait = "tb.1.safe" };
        var fallbackOld = new UserInfo { UserId = 1, UserName = "user", NickNameOld = "old" };
        var fallbackUser = new UserInfo { UserId = 2, UserName = "user-2" };
        var fallbackPortrait = new UserInfo { UserId = 3, NickNameNew = "nick", Portrait = "tb.1.other" };
        var fallbackId = new UserInfo { UserId = 4 };
        var comparer = UserInfo.UserIdComparer;

        Assert.AreEqual("new", preferred.NickName);
        Assert.AreEqual("new", preferred.ShowName);
        Assert.AreEqual("user", preferred.LogName);
        Assert.AreEqual("user", preferred.ToString());
        Assert.AreEqual("old", fallbackOld.ShowName);
        Assert.AreEqual("user-2", fallbackUser.ShowName);
        Assert.AreEqual("nick/tb.1.other", fallbackPortrait.LogName);
        Assert.AreEqual("tb.1.other", fallbackPortrait.ToString());
        Assert.AreEqual("4", fallbackId.LogName);
        Assert.AreEqual("4", fallbackId.ToString());
        Assert.IsTrue(preferred.Equals(fallbackOld));
        Assert.IsFalse(preferred.Equals("not-user"));
        Assert.AreEqual(preferred.GetHashCode(), fallbackOld.GetHashCode());
        Assert.IsTrue(comparer.Equals(preferred, preferred));
        Assert.IsFalse(comparer.Equals(null, preferred));
        Assert.IsFalse(comparer.Equals(preferred, null));
        Assert.IsTrue(comparer.Equals(preferred, fallbackOld));
        Assert.AreEqual(preferred.UserId.GetHashCode(), comparer.GetHashCode(preferred));
    }

    [TestMethod]
    public void ContentAndFragments_FormatAndSerializeAsExpected()
    {
        var fragText = new FragText { Text = "hello" };
        var fragEmoji = new FragEmoji { Id = "1", Desc = "smile" };
        var fragAt = new FragAt { Text = "@safe", UserId = 42 };
        var fragLinkExternal = new FragLink
        {
            Text = "https://safe",
            Title = "Safe",
            RawUrl = new Uri("https://tieba.baidu.com/mo/q/checkurl?url=https%3A%2F%2Fexample.com%2Ftarget")
        };
        var fragLinkInternal = new FragLink
        {
            Text = "/p/1",
            Title = "Thread",
            RawUrl = new Uri("https://tieba.baidu.com/p/1")
        };
        var fragTiebaPlus = new FragTiebaPlus { Text = "promo", Url = new Uri("https://example.com/promo") };
        var fragImage = new FragImage
        {
            Src = "https://imgsrc.baidu.com/forum/pic/item/0123456789abcdef0123456789abcdef.jpg",
            BigSrc = "https://example.com/big.jpg",
            OriginSrc = "https://example.com/origin.jpg",
            OriginSize = 100,
            ShowWidth = 10,
            ShowHeight = 20,
            Hash = "0123456789abcdef0123456789abcdef"
        };
        var fragVoice = new FragVoice { Md5 = "voice-md5", Duration = 3 };
        var fragVideo = new FragVideo { Src = "https://example.com/video.mp4", CoverSrc = "https://example.com/cover.jpg", Width = 1280, Height = 720, Duration = 30, ViewNum = 9 };
        var content = new Content
        {
            Texts = [fragText],
            Emojis = [fragEmoji],
            Images = [fragImage],
            Ats = [fragAt],
            Links = [fragLinkExternal],
            TiebaPluses = [fragTiebaPlus],
            Video = fragVideo,
            Voice = fragVoice,
            Frags = [fragText, fragEmoji, fragAt, fragLinkExternal, fragTiebaPlus, fragImage, fragVideo, fragVoice]
        };

        Assert.AreEqual("hello@safehttps://safepromo", content.Text);
        StringAssert.Contains(content.ToString(), nameof(Content.Emojis));
        StringAssert.Contains(content.ToString(), nameof(Content.Video));
        Assert.AreEqual("https://example.com/target", fragLinkExternal.Url.ToString());
        Assert.AreSame(fragLinkInternal.RawUrl, fragLinkInternal.Url);
        Assert.IsTrue(fragLinkExternal.IsExternal);
        Assert.IsFalse(fragLinkInternal.IsExternal);
        Assert.IsTrue(fragVoice.IsValid());
        Assert.IsFalse(new FragVoice().IsValid());
        Assert.IsTrue(fragVideo.IsValid());
        Assert.IsFalse(new FragVideo().IsValid());
        Assert.AreEqual("FragText", fragText.GetFragType());
        Assert.AreEqual("FragEmoji", fragEmoji.GetFragType());
        Assert.AreEqual("FragAt", fragAt.GetFragType());
        Assert.AreEqual("FragImage", fragImage.GetFragType());
        Assert.AreEqual("FragVoice", fragVoice.GetFragType());
        Assert.AreEqual("FragVideo", fragVideo.GetFragType());
        Assert.AreEqual("FragLink", fragLinkExternal.GetFragType());
        Assert.AreEqual("FragTiebaPlus", fragTiebaPlus.GetFragType());
        Assert.AreEqual("0", fragText.ToDict()["type"]);
        Assert.AreEqual("2", fragEmoji.ToDict()["type"]);
        Assert.AreEqual("4", fragAt.ToDict()["type"]);
        Assert.AreEqual("3", fragImage.ToDict()["type"]);
        Assert.AreEqual("10", fragVoice.ToDict()["type"]);
        Assert.AreEqual("5", fragVideo.ToDict()["type"]);
        Assert.AreEqual("1", fragLinkExternal.ToDict()["type"]);
        Assert.AreEqual(0, fragTiebaPlus.ToDict().Count);
        StringAssert.Contains(fragLinkExternal.ToString(), nameof(FragLink.IsExternal));
        StringAssert.Contains(fragVideo.ToString(), nameof(FragVideo.ViewNum));
    }

    [TestMethod]
    public void TinyMessageAndRoomHelpers_ExposeExpectedFallbacks()
    {
        var fragUnknown = new FragUnknown { Type = "mystery", Text = "opaque payload" };
        var roomList = new RoomList((IEnumerable<Dictionary<string, object?>>)
        [
            new Dictionary<string, object?> { ["room_id"] = 1001L, ["room_name"] = "safe-room" }
        ]);
        var emptyRoomList = new RoomList((IEnumerable<Dictionary<string, object?>>?)null);
        var knownMessage = new WsMessage
        {
            GroupId = 1,
            GroupType = (int)GroupType.PrivateMessage,
            MsgId = 2,
            MsgType = (int)MsgType.Text,
            Text = "hello",
            User = new UserInfo { UserId = 42, UserName = "sender" }
        };
        var unknownMessage = new WsMessage
        {
            GroupId = 3,
            GroupType = 999,
            MsgId = 4,
            MsgType = 999,
            Text = "mystery",
            User = new UserInfo { UserId = 43, UserName = "other" }
        };
        var knownGroup = new WsMsgGroup
        {
            GroupId = 5,
            GroupType = (int)GroupType.Chatroom,
            Messages = [knownMessage]
        };
        var unknownGroup = new WsMsgGroup { GroupId = 6, GroupType = 999 };
        var unknownDict = fragUnknown.ToDict();

        Assert.AreEqual("FragUnknown", fragUnknown.GetFragType());
        Assert.AreEqual("mystery", unknownDict["type"]);
        Assert.AreEqual("opaque payload", unknownDict["text"]);
        StringAssert.Contains(fragUnknown.ToString(), nameof(FragUnknown.Type));
        Assert.AreEqual(1, roomList.Count);
        Assert.AreEqual("safe-room", roomList[0]["room_name"]);
        Assert.AreEqual(0, emptyRoomList.Count);
        Assert.AreEqual(GroupType.PrivateMessage, knownMessage.GroupTypeValue);
        Assert.AreEqual(GroupType.Unknown, unknownMessage.GroupTypeValue);
        Assert.AreEqual(MsgType.Text, knownMessage.MsgTypeValue);
        Assert.AreEqual(MsgType.Unknown, unknownMessage.MsgTypeValue);
        Assert.AreEqual(GroupType.Chatroom, knownGroup.GroupTypeValue);
        Assert.AreEqual(GroupType.Unknown, unknownGroup.GroupTypeValue);
    }

    [TestMethod]
    public void ModelContainersAndEqualityMembers_ExposeExpectedBehavior()
    {
        var lastReplyerUser = new LastReplyerUser { UserId = 10, UserName = "user", NickNameOld = "nick", Portrait = "tb.1.safe" };
        var lastReplyer = new LastReplyer { UserId = 11, UserName = "reply-user", NickNameOld = "reply-nick" };
        var lastReplyerThread = new LastReplyerThread { Title = "thread", Pid = 99, User = lastReplyerUser, LastReplyer = lastReplyer };
        var lastReplyers = new LastReplyers([lastReplyerThread], new LastReplyersPage { HasMore = true }, new Forum());
        var appeal = new Appeal { UserName = "appeal-user", NickName = "Appeal Nick" };
        var appealFallback = new Appeal { UserName = "appeal-fallback" };
        var appeals = new Appeals([appeal], hasMore: true);
        var recoverUser = new RecoverUser { UserName = "recover-user", NickNameNew = "recover-nick", Portrait = "tb.1.recover" };
        var recoverPortraitOnly = new RecoverUser { Portrait = "tb.1.recover-only" };
        var block = new Block { UserId = 20, UserName = "blocked", NickNameOld = "blocked-nick", Day = 3 };
        var blocks = new Blocks([block], new BlocksPage { HasMore = true });
        var bawuUser = new BawuUser { UserId = 30, UserName = "bawu", NickNameNew = "bawu-nick", Portrait = "tb.1.bawu" };
        var bawuPortraitFallback = new BawuUser { UserId = 31, NickNameNew = "portrait-only", Portrait = "tb.1.portrait-only" };
        var bawuIdFallback = new BawuUser { UserId = 32 };
        var squareForum = new SquareForum { Fid = 40, Fname = "square" };
        var squareForums = new SquareForums([squareForum], new SquareForumsPage { HasMore = false });
        var tabMap = new TabMap([new KeyValuePair<string, int>("全部", 1)]);
        var thread = new Thread { Content = new Content { Frags = [new FragText { Text = "body" }] }, VirtualImage = new VirtualImagePf(), Title = "Title", Type = 71, User = new UserInfoT() };
        var wsGroups = new WsMsgGroups([new WsMsgGroup { GroupId = 1 }]);

        Assert.AreEqual("nick", lastReplyerUser.NickName);
        Assert.AreEqual("nick", lastReplyerUser.ShowName);
        Assert.AreEqual("user", lastReplyerUser.LogName);
        Assert.IsTrue(lastReplyerUser.Equals(new LastReplyerUser { UserId = 10 }));
        Assert.IsFalse(lastReplyerUser.Equals("not-a-user"));
        Assert.AreEqual("reply-nick", lastReplyer.ShowName);
        Assert.AreEqual("reply-user", lastReplyer.LogName);
        Assert.IsTrue(lastReplyer.Equals(new LastReplyer { UserId = 11 }));
        Assert.AreEqual("reply-fallback", new LastReplyer { UserName = "reply-fallback" }.ShowName);
        Assert.IsFalse(lastReplyer.Equals("not-a-replyer"));
        Assert.AreEqual("fallback-user", new LastReplyerUser { UserName = "fallback-user" }.ShowName);
        Assert.AreEqual("nick/tb.1.fallback", new LastReplyerUser { NickNameOld = "nick", Portrait = "tb.1.fallback" }.LogName);
        Assert.AreEqual("21", new LastReplyer { UserId = 21 }.LogName);
        Assert.AreEqual("thread", lastReplyerThread.Text);
        Assert.AreEqual(10L, lastReplyerThread.AuthorId);
        Assert.IsTrue(lastReplyerThread.Equals(new LastReplyerThread { Pid = 99 }));
        Assert.IsFalse(lastReplyerThread.Equals("not-a-thread"));
        Assert.IsTrue(lastReplyers.HasMore);
        Assert.AreSame(lastReplyers.Page, lastReplyers.Page);
        Assert.AreEqual("Appeal Nick", appeal.ShowName);
        Assert.AreEqual("appeal-fallback", appealFallback.ShowName);
        Assert.IsTrue(appeals.HasMore);
        Assert.AreEqual("recover-nick", recoverUser.ShowName);
        Assert.AreEqual("recover-user", recoverUser.LogName);
        Assert.AreEqual("recover-user", recoverUser.ToString());
        Assert.IsTrue(recoverUser.Equals(new RecoverUser { Portrait = "tb.1.recover" }));
        Assert.AreEqual("/tb.1.recover-only", recoverPortraitOnly.LogName);
        Assert.AreEqual("tb.1.recover-only", recoverPortraitOnly.ToString());
        Assert.AreEqual("blocked-nick", block.ShowName);
        Assert.IsTrue(block.Equals(new Block { UserId = 20 }));
        Assert.IsTrue(blocks.HasMore);
        var blacklistUser = new BawuBlacklistUser { UserId = 30, UserName = "blacklisted", Portrait = "tb.1.black" };
        var blacklistPortraitFallback = new BawuBlacklistUser { UserId = 31, Portrait = "tb.1.only" };
        var blacklistIdFallback = new BawuBlacklistUser { UserId = 32 };
        var blacklistUsers = new BawuBlacklistUsers([blacklistUser], new BawuBlacklistPage { HasMore = true });
        Assert.AreEqual("blacklisted", blacklistUser.LogName);
        Assert.AreEqual("tb.1.only", blacklistPortraitFallback.LogName);
        Assert.AreEqual("32", blacklistIdFallback.LogName);
        Assert.IsTrue(blacklistUser.Equals(new BawuBlacklistUser { UserId = 30 }));
        Assert.IsTrue(blacklistUsers.HasMore);
        Assert.AreEqual("bawu-nick", bawuUser.NickName);
        Assert.AreEqual("bawu-nick", bawuUser.ShowName);
        Assert.AreEqual("fallback-bawu", new BawuUser { UserName = "fallback-bawu" }.ShowName);
        Assert.AreEqual("bawu", bawuUser.LogName);
        Assert.AreEqual("portrait-only/tb.1.portrait-only", bawuPortraitFallback.LogName);
        Assert.AreEqual("32", bawuIdFallback.LogName);
        Assert.IsTrue(bawuUser.Equals(new BawuUser { UserId = 30 }));
        Assert.IsFalse(bawuUser.Equals("not-a-bawu-user"));
        Assert.AreEqual(30.GetHashCode(), bawuUser.GetHashCode());
        Assert.IsTrue(squareForum.Equals(new SquareForum { Fid = 40 }));
        Assert.IsFalse(squareForums.HasMore);
        Assert.IsTrue(tabMap.ContainsKey("全部"));
        Assert.IsTrue(tabMap.TryGetValue("全部", out var tabId));
        Assert.AreEqual(1, tabId);
        Assert.AreEqual("Title\nbody", thread.Text);
        Assert.IsTrue(thread.IsHelp);
        StringAssert.Contains(thread.ToString(), nameof(Thread.IsHelp));
        Assert.AreEqual(1, wsGroups.Count);
        Assert.ThrowsExactly<ArgumentNullException>(() => new Blocks([], null!));
        Assert.ThrowsExactly<ArgumentNullException>(() => new BawuBlacklistUsers([], null!));
        Assert.ThrowsExactly<ArgumentNullException>(() => new SquareForums([], null!));
        Assert.ThrowsExactly<ArgumentNullException>(() => new LastReplyers([], null!, new Forum()));
        Assert.ThrowsExactly<ArgumentNullException>(() => new LastReplyers([], new LastReplyersPage(), null!));
        Assert.ThrowsExactly<ArgumentNullException>(() => new TabMap(null!));
    }

    [TestMethod]
    public void ForumImageAndUserHistoryModels_ExposeExpectedConvenienceMembers()
    {
        var imageBytes = new ForumImageBytes { Data = [1, 2, 3] };
        var emptyImageBytes = new ForumImageBytes();
        var forumImage = new ForumImage { Data = [1], Width = 10, Height = 20, Format = ForumImageFormat.Png };
        var emptyForumImage = new ForumImage();
        var userPost = new UserPost { Contents = new Content { Frags = [new FragText { Text = "reply" }] }, Pid = 12, Fid = 34, Tid = 56 };
        var userPosts = new UserPosts([userPost], 34, 56);
        var userPostsFromEnumerable = new UserPosts((IEnumerable<UserPost>?)new[] { userPost }, 34, 56);
        var userPostss = new UserPostss([userPosts]);
        var userPostssFromEnumerable = new UserPostss((IEnumerable<UserPosts>?)new[] { userPosts });
        var userThread = new UserThread { Contents = new Content { Frags = [new FragText { Text = "thread body" }] }, Title = "thread title", Type = 70 };
        var userThreads = new UserThreads([userThread]);
        var userThreadsFromEnumerable = new UserThreads((IEnumerable<UserThread>?)new[] { userThread });
        var threadsEntry = new Thread { Content = new Content { Frags = [new FragText { Text = "body" }] }, VirtualImage = new VirtualImagePf(), Title = "Title", Type = 70, User = new UserInfoT() };
        var threads = new Threads
        {
            Page = new PageT { HasMore = true },
            Forum = new ForumT { Fname = "forum-name" },
            Objs = [threadsEntry],
            TabDictionary = new Dictionary<string, int> { ["全部"] = 1 }
        };

        Assert.IsFalse(imageBytes.IsEmpty);
        Assert.IsTrue(emptyImageBytes.IsEmpty);
        Assert.IsFalse(forumImage.IsEmpty);
        Assert.IsTrue(emptyForumImage.IsEmpty);
        Assert.AreEqual(34L, userPosts.Fid);
        Assert.AreEqual(56L, userPosts.Tid);
        Assert.AreEqual(1, userPosts.Count);
        Assert.AreEqual(1, userPostsFromEnumerable.Count);
        Assert.AreEqual(1, userPostss.Count);
        Assert.AreEqual(1, userPostssFromEnumerable.Count);
        Assert.AreEqual("thread title\nthread body", userThread.Text);
        Assert.IsFalse(userThread.IsHelp);
        Assert.AreEqual(1, userThreads.Count);
        Assert.AreEqual(1, userThreadsFromEnumerable.Count);
        Assert.IsTrue(threads.HasMore);
        StringAssert.Contains(threads.ToString(), nameof(Threads.TabDictionary));
        StringAssert.Contains(threads.ToString(), nameof(Threads.Objs));
    }

    private sealed class TestContainers : Containers<string>
    {
        public TestContainers(List<string> objs) : base(objs)
        {
        }

        public TestContainers(IEnumerable<string>? objs) : base(objs)
        {
        }
    }
}
