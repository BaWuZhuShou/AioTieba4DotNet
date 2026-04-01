#nullable enable
using System;
using System.Threading;
using System.Threading.Tasks;
using AioTieba4DotNet.Models;
using AioTieba4DotNet.Models.Shared;
using AioTieba4DotNet.Models.Users;
using AioTieba4DotNet.Modules;
using AioTieba4DotNet.Protocols;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AioTieba4DotNet.Tests.Baseline;

[TestClass]
public class UserModuleBehaviorTests
{
    [TestMethod]
    public async Task UserModule_DelegatesGetSelfInfoToInternalProtocol()
    {
        var expected = new UserInfo { UserId = 123, UserName = "self-user" };
        var protocol = new RecordingUserProtocol(expected);
        var module = new UserModule(protocol);

        var actual = await module.GetSelfInfoAsync();

        Assert.AreSame(expected, actual);
    }

    [TestMethod]
    public async Task UserModule_DelegatesCompatibilitySelfInfoAndLoginToInternalProtocol()
    {
        var expected = new UserInfo { UserId = 123, UserName = "self-user" };
        var loginResult = new LoginResult { User = expected, Tbs = "tbs-123" };
        var protocol = new RecordingUserProtocol(expected)
        {
            SelfInfoInitNickname = expected,
            SelfInfoMoIndex = new UserInfo { UserId = 123, UserName = "mo-user" },
            LoginResult = loginResult
        };
        var module = new UserModule(protocol);

        var initNickname = await module.GetSelfInfoInitNicknameAsync();
        var moIndex = await module.GetSelfInfoMoIndexAsync();
        var login = await module.LoginAsync();

        Assert.AreSame(expected, initNickname);
        Assert.AreSame(protocol.SelfInfoMoIndex, moIndex);
        Assert.AreSame(loginResult, login);
    }

    [TestMethod]
    public void UserModule_ExposesUserContentCommand()
    {
        IUserModule module = new UserModule(new RecordingUserProtocol(new UserInfo()));

        Assert.AreEqual(UserContent.Cmd, module.UserContentCmd);
    }

    [TestMethod]
    public async Task UserModule_DelegatesSetBlacklistToInternalProtocol()
    {
        var protocol = new RecordingUserProtocol(new UserInfo());
        var module = new UserModule(protocol);

        var actual = await module.SetBlacklistAsync(456, BlacklistType.Chat);

        Assert.IsTrue(actual);
        Assert.AreEqual(456, protocol.LastBlacklistUserId);
        Assert.AreEqual(BlacklistType.Chat, protocol.LastBlacklistType);
    }

    [TestMethod]
    public async Task UserModule_DelegatesGetBlacklistLegacyToInternalProtocol()
    {
        var protocol = new RecordingUserProtocol(new UserInfo())
        {
            LegacyBlacklist = new BlacklistOldUsers([], new Models.Threads.PageT { CurrentPage = 3 })
        };
        var module = new UserModule(protocol);

        var actual = await module.GetBlacklistLegacyAsync(3, 40);

        Assert.AreSame(protocol.LegacyBlacklist, actual);
        Assert.AreEqual(3, protocol.LastLegacyBlacklistPn);
        Assert.AreEqual(40, protocol.LastLegacyBlacklistRn);
    }

    [TestMethod]
    public async Task UserModule_DelegatesAddBlacklistLegacyToInternalProtocol()
    {
        var protocol = new RecordingUserProtocol(new UserInfo());
        var module = new UserModule(protocol);

        var actual = await module.AddBlacklistLegacyAsync(456);

        Assert.IsTrue(actual);
        Assert.AreEqual(456, protocol.LastAddLegacyBlacklistUserId);
    }

    [TestMethod]
    public async Task UserModule_DelegatesRemoveBlacklistLegacyToInternalProtocol()
    {
        var protocol = new RecordingUserProtocol(new UserInfo());
        var module = new UserModule(protocol);

        var actual = await module.RemoveBlacklistLegacyAsync(789);

        Assert.IsTrue(actual);
        Assert.AreEqual(789, protocol.LastRemoveLegacyBlacklistUserId);
    }

    [TestMethod]
    public async Task UserModule_DelegatesGetHomepageToInternalProtocol()
    {
        var protocol = new RecordingUserProtocol(new UserInfo())
        {
            Homepage = new Homepage([], new UserInfoPf { VImage = new VirtualImagePf(), UserId = 123 })
        };
        var module = new UserModule(protocol);

        var actual = await module.GetHomepageAsync(123, 2);

        Assert.AreSame(protocol.Homepage, actual);
        Assert.AreEqual(123, protocol.LastHomepageUserId);
        Assert.AreEqual(2, protocol.LastHomepagePn);
    }

    [TestMethod]
    public async Task UserModule_DelegatesGetBasicInfoWebToInternalProtocol()
    {
        var expected = new UserInfoGuInfoWeb { UserId = 123, UserName = "web-user" };
        var protocol = new RecordingUserProtocol(new UserInfo())
        {
            BasicInfoWeb = expected
        };
        var module = new UserModule(protocol);

        var actual = await module.GetBasicInfoWebAsync(123);

        Assert.AreSame(expected, actual);
        Assert.AreEqual(123, protocol.LastBasicInfoWebUserId);
    }

    [TestMethod]
    public async Task UserModule_DelegatesGetUserForumInfoByFidToInternalProtocol()
    {
        var expected = new UserForumInfo { Fname = "csharp" };
        var protocol = new RecordingUserProtocol(new UserInfo())
        {
            UserForumInfo = expected
        };
        var module = new UserModule(protocol);

        var actual = await module.GetUserForumInfoAsync(123UL, "tb.1.safe");

        Assert.AreSame(expected, actual);
        Assert.AreEqual(123UL, protocol.LastUserForumInfoFid);
        Assert.AreEqual("tb.1.safe", protocol.LastUserForumInfoPortrait);
    }

    [TestMethod]
    public async Task UserModule_DelegatesGetUserForumInfoByNameToInternalProtocol()
    {
        var expected = new UserForumInfo { Fname = "csharp" };
        var protocol = new RecordingUserProtocol(new UserInfo())
        {
            UserForumInfo = expected
        };
        var module = new UserModule(protocol);

        var actual = await module.GetUserForumInfoAsync("csharp", "tb.1.safe");

        Assert.AreSame(expected, actual);
        Assert.AreEqual("csharp", protocol.LastUserForumInfoFname);
        Assert.AreEqual("tb.1.safe", protocol.LastUserForumInfoPortrait);
    }

    [TestMethod]
    public async Task UserModule_DelegatesGetRankUsersToInternalProtocol()
    {
        var expected = new RankUsers([], new Models.Threads.PageT { CurrentPage = 4 });
        var protocol = new RecordingUserProtocol(new UserInfo())
        {
            RankUsers = expected
        };
        var module = new UserModule(protocol);

        var actual = await module.GetRankUsersAsync("csharp", 4);

        Assert.AreSame(expected, actual);
        Assert.AreEqual("csharp", protocol.LastRankUsersFname);
        Assert.AreEqual(4, protocol.LastRankUsersPn);
    }

    [TestMethod]
    public async Task UserModule_DelegatesSetNicknameLegacyToInternalProtocol()
    {
        var protocol = new RecordingUserProtocol(new UserInfo());
        var module = new UserModule(protocol);

        var actual = await module.SetNicknameLegacyAsync("legacy-name");

        Assert.IsTrue(actual);
        Assert.AreEqual("legacy-name", protocol.LastLegacyNickname);
    }

    [TestMethod]
    public async Task UserModule_DelegatesSetProfileToInternalProtocol()
    {
        var protocol = new RecordingUserProtocol(new UserInfo());
        var module = new UserModule(protocol);

        var actual = await module.SetProfileAsync("safe-name", "hello", Gender.Female);

        Assert.IsTrue(actual);
        Assert.AreEqual("safe-name", protocol.LastProfileNickname);
        Assert.AreEqual("hello", protocol.LastProfileSign);
        Assert.AreEqual(Gender.Female, protocol.LastProfileGender);
    }

    [TestMethod]
    public async Task UserModule_DelegatesGetUserByTiebaUidToInternalProtocol()
    {
        var expected = new UserInfoTUid { UserId = 123, TiebaUid = 778899 };
        var protocol = new RecordingUserProtocol(new UserInfo())
        {
            TiebaUidUser = expected
        };
        var module = new UserModule(protocol);

        var actual = await module.GetUserByTiebaUidAsync(778899);

        Assert.AreSame(expected, actual);
        Assert.AreEqual(778899L, protocol.LastTiebaUid);
    }

    private sealed class RecordingUserProtocol(UserInfo selfInfo) : IUserProtocol
    {
        public long? LastBlacklistUserId { get; private set; }

        public BlacklistType? LastBlacklistType { get; private set; }

        public int? LastLegacyBlacklistPn { get; private set; }

        public int? LastLegacyBlacklistRn { get; private set; }

        public int? LastHomepageUserId { get; private set; }

        public int? LastHomepagePn { get; private set; }

        public int? LastBasicInfoWebUserId { get; private set; }

        public ulong? LastUserForumInfoFid { get; private set; }

        public string? LastUserForumInfoFname { get; private set; }

        public string? LastUserForumInfoPortrait { get; private set; }

        public string? LastRankUsersFname { get; private set; }

        public int? LastRankUsersPn { get; private set; }

        public string? LastLegacyNickname { get; private set; }

        public long? LastAddLegacyBlacklistUserId { get; private set; }

        public long? LastRemoveLegacyBlacklistUserId { get; private set; }

        public string? LastProfileNickname { get; private set; }

        public string? LastProfileSign { get; private set; }

        public Gender? LastProfileGender { get; private set; }

        public long? LastTiebaUid { get; private set; }

        public BlacklistOldUsers LegacyBlacklist { get; init; } = new([], new Models.Threads.PageT());

        public Homepage Homepage { get; init; } = new([], new UserInfoPf { VImage = new VirtualImagePf() });

        public UserInfoGuInfoWeb BasicInfoWeb { get; init; } = new();

        public UserForumInfo UserForumInfo { get; init; } = new();

        public RankUsers RankUsers { get; init; } = new([], new Models.Threads.PageT());

        public UserInfoTUid TiebaUidUser { get; init; } = new();

        public UserInfo SelfInfoInitNickname { get; init; } = new();

        public UserInfo SelfInfoMoIndex { get; init; } = new();

        public LoginResult LoginResult { get; init; } = new() { User = new UserInfo(), Tbs = "tbs-123" };

        public Task<string> GetTbsAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public Task<UserInfoGuInfoApp> GetBasicInfoAsync(int userId, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<UserInfoPf> GetProfileAsync(int userId, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<UserInfoPf> GetProfileAsync(string portraitOrUserName, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<bool> BlockAsync(ulong fid, string portrait, int day, string reason,
            CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public Task<bool> BlockAsync(string fname, string portrait, int day, string reason,
            CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public Task<bool> FollowAsync(string portrait, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<bool> UnfollowAsync(string portrait, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<UserList> GetFollowsAsync(long userId, int pn, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<UserList> GetFansAsync(long userId, int pn, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<UserInfoPanel> GetPanelInfoAsync(string nameOrPortrait, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<UserInfoJson> GetUserInfoJsonAsync(string username, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<UserInfo> GetSelfInfoAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(selfInfo);

        public Task<UserInfo> GetSelfInfoInitNicknameAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(SelfInfoInitNickname);

        public Task<UserInfo> GetSelfInfoMoIndexAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(SelfInfoMoIndex);

        public Task<LoginResult> LoginAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(LoginResult);

        public Task<AtMessages> GetAtsAsync(int pn, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<ReplyMessages> GetRepliesAsync(int pn, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<BlacklistUsers> GetBlacklistAsync(CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<BlacklistOldUsers> GetBlacklistLegacyAsync(int pn, int rn,
            CancellationToken cancellationToken = default)
        {
            LastLegacyBlacklistPn = pn;
            LastLegacyBlacklistRn = rn;
            return Task.FromResult(LegacyBlacklist);
        }

        public Task<bool> SetBlacklistAsync(long userId, BlacklistType type, CancellationToken cancellationToken = default)
        {
            LastBlacklistUserId = userId;
            LastBlacklistType = type;
            return Task.FromResult(true);
        }

        public Task<bool> AddBlacklistLegacyAsync(long userId, CancellationToken cancellationToken = default)
        {
            LastAddLegacyBlacklistUserId = userId;
            return Task.FromResult(true);
        }

        public Task<bool> RemoveBlacklistLegacyAsync(long userId, CancellationToken cancellationToken = default)
        {
            LastRemoveLegacyBlacklistUserId = userId;
            return Task.FromResult(true);
        }

        public Task<bool> RemoveFanAsync(long userId, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<UserInfoGuInfoWeb> GetBasicInfoWebAsync(int userId, CancellationToken cancellationToken = default)
        {
            LastBasicInfoWebUserId = userId;
            return Task.FromResult(BasicInfoWeb);
        }

        public Task<UserForumInfo> GetUserForumInfoAsync(ulong fid, string portrait,
            CancellationToken cancellationToken = default)
        {
            LastUserForumInfoFid = fid;
            LastUserForumInfoPortrait = portrait;
            return Task.FromResult(UserForumInfo);
        }

        public Task<UserForumInfo> GetUserForumInfoAsync(string fname, string portrait,
            CancellationToken cancellationToken = default)
        {
            LastUserForumInfoFname = fname;
            LastUserForumInfoPortrait = portrait;
            return Task.FromResult(UserForumInfo);
        }

        public Task<RankUsers> GetRankUsersAsync(string fname, int pn, CancellationToken cancellationToken = default)
        {
            LastRankUsersFname = fname;
            LastRankUsersPn = pn;
            return Task.FromResult(RankUsers);
        }

        public Task<Homepage> GetHomepageAsync(int userId, int pn, CancellationToken cancellationToken = default)
        {
            LastHomepageUserId = userId;
            LastHomepagePn = pn;
            return Task.FromResult(Homepage);
        }

        public Task<bool> SetNicknameLegacyAsync(string nickName, CancellationToken cancellationToken = default)
        {
            LastLegacyNickname = nickName;
            return Task.FromResult(true);
        }

        public Task<bool> SetProfileAsync(string nickName, string sign, Gender gender,
            CancellationToken cancellationToken = default)
        {
            LastProfileNickname = nickName;
            LastProfileSign = sign;
            LastProfileGender = gender;
            return Task.FromResult(true);
        }

        public Task<UserInfoTUid> GetUserByTiebaUidAsync(long tiebaUid, CancellationToken cancellationToken = default)
        {
            LastTiebaUid = tiebaUid;
            return Task.FromResult(TiebaUidUser);
        }

        public Task<UserPostss> GetPostsAsync(int userId, uint pn, uint rn, string version,
            CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public Task<UserThreads> GetThreadsAsync(int userId, uint pn, bool publicOnly,
            CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }
}
