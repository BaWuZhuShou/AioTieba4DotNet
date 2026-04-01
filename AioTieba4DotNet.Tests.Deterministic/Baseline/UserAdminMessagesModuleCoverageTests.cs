#nullable enable
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AioTieba4DotNet.Models;
using AioTieba4DotNet.Models.Admins;
using AioTieba4DotNet.Models.Messages;
using AioTieba4DotNet.Models.Shared;
using AioTieba4DotNet.Models.Users;
using AioTieba4DotNet.Modules;
using AioTieba4DotNet.Protocols;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AioTieba4DotNet.Tests.Baseline;

[TestClass]
public sealed class UserAdminMessagesModuleCoverageTests
{
    [TestMethod]
    public async Task UserModule_DelegatesRemainingMembersToInternalProtocol()
    {
        var protocol = new RecordingUserProtocol();
        var module = new UserModule(protocol);

        Assert.AreEqual("tbs-123", await module.GetTbsAsync());
        Assert.IsNull(await module.GetBasicInfoAsync(1));
        Assert.IsNull(await module.GetProfileAsync(1));
        Assert.IsNull(await module.GetProfileAsync("tb.1.safe"));
        Assert.IsTrue(await module.BlockAsync(123UL, "tb.1.safe", 3, "reason"));
        Assert.IsTrue(await module.BlockAsync("csharp", "tb.1.safe", 4, "reason-2"));
        Assert.IsTrue(await module.UnfollowAsync("tb.1.safe"));
        Assert.IsNull(await module.GetFollowsAsync(42, 2));
        Assert.IsNull(await module.GetFansAsync(43, 3));
        Assert.IsNull(await module.GetPanelInfoAsync("safe-user"));
        Assert.IsNull(await module.GetUserInfoJsonAsync("safe-user"));
        Assert.IsNull(await module.GetAtsAsync(4));
        Assert.IsNull(await module.GetRepliesAsync(5));
        Assert.IsNull(await module.GetBlacklistAsync());
        Assert.IsTrue(await module.RemoveFanAsync(44));

        Assert.AreEqual(44L, protocol.LastUserId);
        Assert.AreEqual("csharp", protocol.LastForumName);
        Assert.AreEqual("tb.1.safe", protocol.LastPortrait);
        Assert.AreEqual(4, protocol.LastDay);
        Assert.AreEqual("reason-2", protocol.LastReason);
        Assert.AreEqual("safe-user", protocol.LastLookupText);
        Assert.AreEqual(5, protocol.LastPageNumber);
    }

    [TestMethod]
    public async Task AdminModule_DelegatesRemainingMembersToInternalProtocol()
    {
        var protocol = new RecordingAdminProtocol();
        var module = new AdminModule(protocol);

        Assert.IsTrue(await module.AddBaWuAsync("csharp", "target-user", BawuType.Manager));
        Assert.IsTrue(await module.DelBaWuAsync("csharp", "tb.1.safe", BawuType.Manager));
        Assert.IsTrue(await module.AddBawuBlacklistAsync("csharp", 11));
        Assert.IsTrue(await module.DelBawuBlacklistAsync("csharp", 12));
        Assert.IsNull(await module.GetBawuBlacklistAsync("csharp", 2));
        Assert.IsNull(await module.GetBawuPostLogsAsync("csharp", new BawuPostLogQueryOptions()));
        Assert.IsNull(await module.GetBawuUserLogsAsync("csharp", new BawuUserLogQueryOptions()));
        Assert.IsNull(await module.GetUnblockAppealsAsync("csharp", 3, 4));
        Assert.IsTrue(await module.HandleUnblockAppealsAsync("csharp", new long[] { 1001, 1002 }, true));
        Assert.IsNull(await module.GetBlocksAsync("csharp", "target-user", 5));
        Assert.IsTrue(await module.BlockAsync("csharp", "tb.1.safe", 6, "spam"));
        Assert.IsTrue(await module.UnblockAsync("csharp", 13));

        Assert.AreEqual("csharp", protocol.LastForumName);
        Assert.AreEqual("target-user", protocol.LastUserName);
        Assert.AreEqual("tb.1.safe", protocol.LastPortrait);
        Assert.AreEqual(13L, protocol.LastUserId);
        Assert.AreEqual(6, protocol.LastDay);
        Assert.AreEqual("spam", protocol.LastReason);
        Assert.IsTrue(protocol.LastRefuse);
        CollectionAssert.AreEqual(new long[] { 1001, 1002 }, (System.Collections.ICollection)protocol.LastAppealIds!);
    }

    [TestMethod]
    public async Task MessagesModule_DelegatesRemainingMembersToInternalProtocol()
    {
        var protocol = new RecordingMessagesProtocol();
        var module = new MessagesModule(protocol);
        var message = new WsMessage
        {
            GroupId = 10,
            GroupType = 6,
            MsgId = 20,
            MsgType = 1,
            Text = "hello",
            User = new UserInfo { UserId = 1, UserName = "sender" },
            CreateTime = 123
        };

        Assert.IsNull(await module.GetAtsAsync(2));
        Assert.IsNull(await module.GetRepliesAsync(3));
        Assert.IsNull(await module.GetGroupMessagesAsync(4));
        Assert.IsNull(await module.GetGroupMessagesAsync(new long[] { 7, 8 }, 5));
        Assert.AreEqual(998L, await module.SendMessageAsync("tb.1.safe", "content"));
        Assert.IsTrue(await module.SetMessageReadAsync(message));

        Assert.AreEqual("tb.1.safe", protocol.LastLookupText);
        Assert.AreEqual("content", protocol.LastContent);
        Assert.AreEqual(5, protocol.LastGetType);
        CollectionAssert.AreEqual(new long[] { 7, 8 }, (System.Collections.ICollection)protocol.LastGroupIds!);
        Assert.AreSame(message, protocol.LastMessage);
    }

    private sealed class RecordingUserProtocol : IUserProtocol
    {
        public long LastUserId { get; private set; }
        public string? LastForumName { get; private set; }
        public string? LastPortrait { get; private set; }
        public int LastDay { get; private set; }
        public string? LastReason { get; private set; }
        public string? LastLookupText { get; private set; }
        public int LastPageNumber { get; private set; }

        public Task<string> GetTbsAsync(CancellationToken cancellationToken = default) => Task.FromResult("tbs-123");
        public Task<UserInfoGuInfoApp> GetBasicInfoAsync(int userId, CancellationToken cancellationToken = default)
        {
            LastUserId = userId;
            return Task.FromResult<UserInfoGuInfoApp>(null!);
        }

        public Task<UserInfoPf> GetProfileAsync(int userId, CancellationToken cancellationToken = default)
        {
            LastUserId = userId;
            return Task.FromResult<UserInfoPf>(null!);
        }

        public Task<UserInfoPf> GetProfileAsync(string portraitOrUserName, CancellationToken cancellationToken = default)
        {
            LastLookupText = portraitOrUserName;
            return Task.FromResult<UserInfoPf>(null!);
        }

        public Task<bool> BlockAsync(ulong fid, string portrait, int day, string reason, CancellationToken cancellationToken = default)
        {
            LastUserId = (long)fid;
            LastPortrait = portrait;
            LastDay = day;
            LastReason = reason;
            return Task.FromResult(true);
        }

        public Task<bool> BlockAsync(string fname, string portrait, int day, string reason, CancellationToken cancellationToken = default)
        {
            LastForumName = fname;
            LastPortrait = portrait;
            LastDay = day;
            LastReason = reason;
            return Task.FromResult(true);
        }

        public Task<bool> FollowAsync(string portrait, CancellationToken cancellationToken = default) => Task.FromResult(true);
        public Task<bool> UnfollowAsync(string portrait, CancellationToken cancellationToken = default)
        {
            LastPortrait = portrait;
            return Task.FromResult(true);
        }

        public Task<UserList> GetFollowsAsync(long userId, int pn, CancellationToken cancellationToken = default)
        {
            LastUserId = userId;
            LastPageNumber = pn;
            return Task.FromResult<UserList>(null!);
        }

        public Task<UserList> GetFansAsync(long userId, int pn, CancellationToken cancellationToken = default)
        {
            LastUserId = userId;
            LastPageNumber = pn;
            return Task.FromResult<UserList>(null!);
        }

        public Task<UserInfoPanel> GetPanelInfoAsync(string nameOrPortrait, CancellationToken cancellationToken = default)
        {
            LastLookupText = nameOrPortrait;
            return Task.FromResult<UserInfoPanel>(null!);
        }

        public Task<UserInfoJson> GetUserInfoJsonAsync(string username, CancellationToken cancellationToken = default)
        {
            LastLookupText = username;
            return Task.FromResult<UserInfoJson>(null!);
        }

        public Task<UserInfo> GetSelfInfoAsync(CancellationToken cancellationToken = default) => Task.FromResult(new UserInfo());
        public Task<UserInfo> GetSelfInfoInitNicknameAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new UserInfo());
        public Task<UserInfo> GetSelfInfoMoIndexAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new UserInfo());
        public Task<LoginResult> LoginAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new LoginResult { User = new UserInfo() });
        public Task<AtMessages> GetAtsAsync(int pn, CancellationToken cancellationToken = default)
        {
            LastPageNumber = pn;
            return Task.FromResult<AtMessages>(null!);
        }

        public Task<ReplyMessages> GetRepliesAsync(int pn, CancellationToken cancellationToken = default)
        {
            LastPageNumber = pn;
            return Task.FromResult<ReplyMessages>(null!);
        }

        public Task<BlacklistUsers> GetBlacklistAsync(CancellationToken cancellationToken = default) => Task.FromResult<BlacklistUsers>(null!);
        public Task<BlacklistOldUsers> GetBlacklistLegacyAsync(int pn, int rn, CancellationToken cancellationToken = default) => Task.FromResult<BlacklistOldUsers>(null!);
        public Task<bool> SetBlacklistAsync(long userId, BlacklistType type, CancellationToken cancellationToken = default) => Task.FromResult(true);
        public Task<bool> AddBlacklistLegacyAsync(long userId, CancellationToken cancellationToken = default) => Task.FromResult(true);
        public Task<bool> RemoveBlacklistLegacyAsync(long userId, CancellationToken cancellationToken = default) => Task.FromResult(true);
        public Task<bool> RemoveFanAsync(long userId, CancellationToken cancellationToken = default)
        {
            LastUserId = userId;
            return Task.FromResult(true);
        }

        public Task<UserInfoGuInfoWeb> GetBasicInfoWebAsync(int userId, CancellationToken cancellationToken = default) => Task.FromResult<UserInfoGuInfoWeb>(null!);
        public Task<UserForumInfo> GetUserForumInfoAsync(ulong fid, string portrait, CancellationToken cancellationToken = default) => Task.FromResult<UserForumInfo>(null!);
        public Task<UserForumInfo> GetUserForumInfoAsync(string fname, string portrait, CancellationToken cancellationToken = default) => Task.FromResult<UserForumInfo>(null!);
        public Task<RankUsers> GetRankUsersAsync(string fname, int pn, CancellationToken cancellationToken = default) => Task.FromResult<RankUsers>(null!);
        public Task<Homepage> GetHomepageAsync(int userId, int pn, CancellationToken cancellationToken = default) => Task.FromResult<Homepage>(null!);
        public Task<bool> SetNicknameLegacyAsync(string nickName, CancellationToken cancellationToken = default) => Task.FromResult(true);
        public Task<bool> SetProfileAsync(string nickName, string sign, Gender gender, CancellationToken cancellationToken = default) => Task.FromResult(true);
        public Task<UserInfoTUid> GetUserByTiebaUidAsync(long tiebaUid, CancellationToken cancellationToken = default) => Task.FromResult<UserInfoTUid>(null!);
        public Task<UserPostss> GetPostsAsync(int userId, uint pn, uint rn, string version, CancellationToken cancellationToken = default) => Task.FromResult<UserPostss>(null!);
        public Task<UserThreads> GetThreadsAsync(int userId, uint pn, bool publicOnly, CancellationToken cancellationToken = default) => Task.FromResult<UserThreads>(null!);
    }

    private sealed class RecordingAdminProtocol : IAdminProtocol
    {
        public string? LastForumName { get; private set; }
        public string? LastUserName { get; private set; }
        public string? LastPortrait { get; private set; }
        public long LastUserId { get; private set; }
        public int LastDay { get; private set; }
        public string? LastReason { get; private set; }
        public IReadOnlyList<long>? LastAppealIds { get; private set; }
        public bool LastRefuse { get; private set; }

        public Task<bool> AddBaWuAsync(string fname, string userName, BawuType bawuType, CancellationToken cancellationToken = default)
        {
            LastForumName = fname;
            LastUserName = userName;
            return Task.FromResult(true);
        }

        public Task<bool> DelBaWuAsync(string fname, string portrait, BawuType bawuType, CancellationToken cancellationToken = default)
        {
            LastForumName = fname;
            LastPortrait = portrait;
            return Task.FromResult(true);
        }

        public Task<bool> AddBawuBlacklistAsync(string fname, long userId, CancellationToken cancellationToken = default)
        {
            LastForumName = fname;
            LastUserId = userId;
            return Task.FromResult(true);
        }

        public Task<bool> DelBawuBlacklistAsync(string fname, long userId, CancellationToken cancellationToken = default)
        {
            LastForumName = fname;
            LastUserId = userId;
            return Task.FromResult(true);
        }

        public Task<BawuBlacklistUsers> GetBawuBlacklistAsync(string fname, int pn = 1, CancellationToken cancellationToken = default)
        {
            LastForumName = fname;
            return Task.FromResult<BawuBlacklistUsers>(null!);
        }

        public Task<BawuInfo> GetBawuInfoAsync(string fname, CancellationToken cancellationToken = default) => Task.FromResult<BawuInfo>(null!);
        public Task<BawuPerm> GetBawuPermAsync(string fname, string portrait, CancellationToken cancellationToken = default) => Task.FromResult<BawuPerm>(null!);
        public Task<bool> SetBawuPermAsync(string fname, string portrait, BawuPermType permissions, CancellationToken cancellationToken = default) => Task.FromResult(true);

        public Task<BawuPostLogs> GetBawuPostLogsAsync(string fname, BawuPostLogQueryOptions? options = null, CancellationToken cancellationToken = default)
        {
            LastForumName = fname;
            return Task.FromResult<BawuPostLogs>(null!);
        }

        public Task<BawuUserLogs> GetBawuUserLogsAsync(string fname, BawuUserLogQueryOptions? options = null, CancellationToken cancellationToken = default)
        {
            LastForumName = fname;
            return Task.FromResult<BawuUserLogs>(null!);
        }

        public Task<Appeals> GetUnblockAppealsAsync(string fname, int pn = 1, int rn = 20, CancellationToken cancellationToken = default)
        {
            LastForumName = fname;
            return Task.FromResult<Appeals>(null!);
        }

        public Task<bool> HandleUnblockAppealsAsync(string fname, IReadOnlyList<long> appealIds, bool refuse = false, CancellationToken cancellationToken = default)
        {
            LastForumName = fname;
            LastAppealIds = appealIds;
            LastRefuse = refuse;
            return Task.FromResult(true);
        }

        public Task<Blocks> GetBlocksAsync(string fname, string userName = "", int pn = 1, CancellationToken cancellationToken = default)
        {
            LastForumName = fname;
            LastUserName = userName;
            return Task.FromResult<Blocks>(null!);
        }

        public Task<bool> BlockAsync(string fname, string portrait, int day = 1, string reason = "", CancellationToken cancellationToken = default)
        {
            LastForumName = fname;
            LastPortrait = portrait;
            LastDay = day;
            LastReason = reason;
            return Task.FromResult(true);
        }

        public Task<bool> BlockAsync(ulong fid, string portrait, int day = 1, string reason = "", CancellationToken cancellationToken = default)
        {
            LastUserId = (long)fid;
            LastPortrait = portrait;
            LastDay = day;
            LastReason = reason;
            return Task.FromResult(true);
        }

        public Task<bool> UnblockAsync(string fname, long userId, CancellationToken cancellationToken = default)
        {
            LastForumName = fname;
            LastUserId = userId;
            return Task.FromResult(true);
        }

        public Task<bool> UnblockAsync(ulong fid, long userId, CancellationToken cancellationToken = default)
        {
            LastUserId = userId == 0 ? (long)fid : userId;
            return Task.FromResult(true);
        }
    }

    private sealed class RecordingMessagesProtocol : IMessagesProtocol
    {
        public string? LastLookupText { get; private set; }
        public string? LastContent { get; private set; }
        public int LastGetType { get; private set; }
        public IReadOnlyList<long>? LastGroupIds { get; private set; }
        public WsMessage? LastMessage { get; private set; }

        public Task<AtMessages> GetAtsAsync(int pn, CancellationToken cancellationToken = default) => Task.FromResult<AtMessages>(null!);
        public Task<ReplyMessages> GetRepliesAsync(int pn, CancellationToken cancellationToken = default) => Task.FromResult<ReplyMessages>(null!);

        public Task<WsMsgGroups> GetGroupMessagesAsync(int getType, CancellationToken cancellationToken = default)
        {
            LastGetType = getType;
            return Task.FromResult<WsMsgGroups>(null!);
        }

        public Task<WsMsgGroups> GetGroupMessagesAsync(IReadOnlyList<long> groupIds, int getType, CancellationToken cancellationToken = default)
        {
            LastGroupIds = groupIds;
            LastGetType = getType;
            return Task.FromResult<WsMsgGroups>(null!);
        }

        public Task<long> SendMessageAsync(long userId, string content, CancellationToken cancellationToken = default) => Task.FromResult(0L);

        public Task<long> SendMessageAsync(string portraitOrUserName, string content, CancellationToken cancellationToken = default)
        {
            LastLookupText = portraitOrUserName;
            LastContent = content;
            return Task.FromResult(998L);
        }

        public Task<bool> SendChatroomMessageAsync(long chatroomId, ulong forumId, string text, IReadOnlyList<long>? atUserIds = null, int robotCode = -1, CancellationToken cancellationToken = default) => Task.FromResult(true);

        public Task<bool> SetMessageReadAsync(WsMessage message, CancellationToken cancellationToken = default)
        {
            LastMessage = message;
            return Task.FromResult(true);
        }

        public IReadOnlyList<WsNotify> ParsePushNotifications(byte[] payload) => [];
    }
}
