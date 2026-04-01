#nullable enable
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AioTieba4DotNet.Models;
using AioTieba4DotNet.Models.Shared;
using AioTieba4DotNet.Models.Users;
using AioTieba4DotNet.Modules;
using AioTieba4DotNet.Protocols;
using AioTieba4DotNet.Tests.Infrastructure;
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
    public async Task UserModule_DelegatesGetBlacklistToInternalProtocol()
    {
        var expected = CreateBlacklistUsers(
            new List<BlacklistUser>
            {
                new() { UserId = 456 }
            });
        var protocol = new RecordingUserProtocol(new UserInfo())
        {
            BlacklistUsers = expected
        };
        var module = new UserModule(protocol);

        var actual = await module.GetBlacklistAsync();

        Assert.AreSame(expected, actual);
    }

    [TestMethod]
    public async Task UserModule_DelegatesGetBlacklistOldToInternalProtocol()
    {
        var protocol = new RecordingUserProtocol(new UserInfo())
        {
            BlacklistOldUsers = CreateBlacklistOldUsers(new List<BlacklistOldUser>(), new Models.Threads.PageT { CurrentPage = 3 })
        };
        var module = new UserModule(protocol);

        var actual = await module.GetBlacklistOldAsync(3, 40);

        Assert.AreSame(protocol.BlacklistOldUsers, actual);
        Assert.AreEqual(3, protocol.LastMutedBlacklistPn);
        Assert.AreEqual(40, protocol.LastMutedBlacklistRn);
    }

    [TestMethod]
    public async Task UserModule_DelegatesAddBlacklistOldToInternalProtocol()
    {
        var protocol = new RecordingUserProtocol(new UserInfo());
        var module = new UserModule(protocol);

        var actual = await module.AddBlacklistOldAsync(456);

        Assert.IsTrue(actual);
        Assert.AreEqual(456, protocol.LastAddMutedBlacklistUserId);
    }

    [TestMethod]
    public async Task UserModule_DelegatesRemoveBlacklistOldToInternalProtocol()
    {
        var protocol = new RecordingUserProtocol(new UserInfo());
        var module = new UserModule(protocol);

        var actual = await module.RemoveBlacklistOldAsync(789);

        Assert.IsTrue(actual);
        Assert.AreEqual(789, protocol.LastRemoveMutedBlacklistUserId);
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
    public async Task UserModule_DelegatesGetBasicInfoAppToInternalProtocol()
    {
        var expected = new UserInfoGuInfoApp { UserId = 123, UserName = "app-user" };
        var protocol = new RecordingUserProtocol(new UserInfo())
        {
            BasicInfoApp = expected
        };
        var module = new UserModule(protocol);

        var actual = await module.GetUserInfoAppAsync(123);

        Assert.AreSame(expected, actual);
        Assert.AreEqual(123, protocol.LastBasicInfoAppUserId);
    }

    [TestMethod]
    public async Task UserModule_DelegatesGetUserInfoWebToInternalProtocol()
    {
        var expected = new UserInfoGuInfoWeb { UserId = 123, UserName = "web-user" };
        var protocol = new RecordingUserProtocol(new UserInfo())
        {
            BasicInfoWeb = expected
        };
        var module = new UserModule(protocol);

        var actual = await module.GetUserInfoWebAsync(123);

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
    public async Task UserModule_DelegatesSetNicknameToInternalProtocol()
    {
        var protocol = new RecordingUserProtocol(new UserInfo());
        var module = new UserModule(protocol);

        var actual = await module.SetNicknameAsync("safe-name");

        Assert.IsTrue(actual);
        Assert.AreEqual("safe-name", protocol.LastNickname);
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

    [TestMethod]
    public async Task UserModule_DelegatesGetPostsToInternalProtocol_AndReturnsUserPostGroups()
    {
        var expected = new UserPostGroups([new UserPosts([], 34, 56)]);
        var protocol = new RecordingUserProtocol(new UserInfo())
        {
            UserPostGroups = expected
        };
        var module = new UserModule(protocol);

        var actual = await module.GetPostsAsync(123, 2, 3, "8.9.8.5");

        Assert.AreSame(expected, actual);
        Assert.AreEqual(123, protocol.LastPostsUserId);
        Assert.AreEqual((uint)2, protocol.LastPostsPn);
        Assert.AreEqual((uint)3, protocol.LastPostsRn);
        Assert.AreEqual("8.9.8.5", protocol.LastPostsVersion);
    }

    [TestMethod]
    public void UserModule_PublicContracts_FreezePeerFamilies_AndRemovedNamesStayGone()
    {
        var userSource = RepositorySourceTextAssert.ReadRepositoryFiles(
            "AioTieba4DotNet/Contracts/IUserModule.cs",
            "AioTieba4DotNet/Protocols/IUserProtocol.cs",
            "AioTieba4DotNet/Modules/UserModule.cs");

        RepositorySourceTextAssert.ContainsAll(
            userSource,
            "GetUserInfoAppAsync",
            "GetUserInfoWebAsync",
            "GetBlacklistAsync",
            "GetBlacklistOldAsync",
            "AddBlacklistOldAsync",
            "RemoveBlacklistOldAsync",
            "SetNicknameAsync",
            "SetProfileAsync",
            "UserPostGroups");
        RepositorySourceTextAssert.DoesNotContainAny(
            userSource,
            "GetBlacklistPermissionsAsync",
            "SetBlacklistPermissionsAsync",
            "GetBlacklistMutedAsync",
            "AddBlacklistMutedAsync",
            "RemoveBlacklistMutedAsync",
            "GetBlacklistLegacyAsync",
            "AddBlacklistLegacyAsync",
            "RemoveBlacklistLegacyAsync",
            "GetBasicInfoAppAsync",
            "GetBasicInfoWebAsync",
            "SetNicknameLegacyAsync",
            "UserPostss");
    }

    private static BlacklistUsers CreateBlacklistUsers(List<BlacklistUser> users) =>
        (BlacklistUsers)Activator.CreateInstance(typeof(BlacklistUsers), users)!;

    private static BlacklistOldUsers CreateBlacklistOldUsers(List<BlacklistOldUser> users,
        Models.Threads.PageT page) =>
        (BlacklistOldUsers)Activator.CreateInstance(typeof(BlacklistOldUsers), users, page)!;

    private sealed class RecordingUserProtocol(UserInfo selfInfo) : IUserProtocol
    {
        public long? LastBlacklistUserId { get; private set; }

        public BlacklistType? LastBlacklistType { get; private set; }

        public int? LastMutedBlacklistPn { get; private set; }

        public int? LastMutedBlacklistRn { get; private set; }

        public int? LastHomepageUserId { get; private set; }

        public int? LastHomepagePn { get; private set; }

        public int? LastBasicInfoAppUserId { get; private set; }

        public int? LastBasicInfoWebUserId { get; private set; }

        public ulong? LastUserForumInfoFid { get; private set; }

        public string? LastUserForumInfoFname { get; private set; }

        public string? LastUserForumInfoPortrait { get; private set; }

        public string? LastRankUsersFname { get; private set; }

        public int? LastRankUsersPn { get; private set; }

        public string? LastNickname { get; private set; }

        public long? LastAddMutedBlacklistUserId { get; private set; }

        public long? LastRemoveMutedBlacklistUserId { get; private set; }

        public string? LastProfileNickname { get; private set; }

        public string? LastProfileSign { get; private set; }

        public Gender? LastProfileGender { get; private set; }

        public long? LastTiebaUid { get; private set; }

        public int? LastPostsUserId { get; private set; }

        public uint? LastPostsPn { get; private set; }

        public uint? LastPostsRn { get; private set; }

        public string? LastPostsVersion { get; private set; }

        public BlacklistOldUsers BlacklistOldUsers { get; init; } =
            CreateBlacklistOldUsers(new List<BlacklistOldUser>(), new Models.Threads.PageT());

        public BlacklistUsers BlacklistUsers { get; init; } =
            CreateBlacklistUsers(new List<BlacklistUser>());

        public Homepage Homepage { get; init; } = new([], new UserInfoPf { VImage = new VirtualImagePf() });

        public UserInfoGuInfoApp BasicInfoApp { get; init; } = new();

        public UserInfoGuInfoWeb BasicInfoWeb { get; init; } = new();

        public UserForumInfo UserForumInfo { get; init; } = new();

        public RankUsers RankUsers { get; init; } = new([], new Models.Threads.PageT());

        public UserInfoTUid TiebaUidUser { get; init; } = new();

        public UserPostGroups UserPostGroups { get; init; } = new(new List<UserPosts>());

        public UserInfo SelfInfoInitNickname { get; init; } = new();

        public UserInfo SelfInfoMoIndex { get; init; } = new();

        public LoginResult LoginResult { get; init; } = new() { User = new UserInfo(), Tbs = "tbs-123" };

        public Task<string> GetTbsAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public Task<UserInfoGuInfoApp> GetUserInfoAppAsync(int userId, CancellationToken cancellationToken = default)
        {
            LastBasicInfoAppUserId = userId;
            return Task.FromResult(BasicInfoApp);
        }

        public Task<UserInfoPf> GetProfileAsync(int userId, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<UserInfoPf> GetProfileAsync(string portraitOrUserName, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

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

        public Task<BlacklistUsers> GetBlacklistAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(BlacklistUsers);

        public Task<BlacklistOldUsers> GetBlacklistOldAsync(int pn, int rn,
            CancellationToken cancellationToken = default)
        {
            LastMutedBlacklistPn = pn;
            LastMutedBlacklistRn = rn;
            return Task.FromResult(BlacklistOldUsers);
        }

        public Task<bool> SetBlacklistAsync(long userId, BlacklistType type,
            CancellationToken cancellationToken = default)
        {
            LastBlacklistUserId = userId;
            LastBlacklistType = type;
            return Task.FromResult(true);
        }

        public Task<bool> AddBlacklistOldAsync(long userId, CancellationToken cancellationToken = default)
        {
            LastAddMutedBlacklistUserId = userId;
            return Task.FromResult(true);
        }

        public Task<bool> RemoveBlacklistOldAsync(long userId, CancellationToken cancellationToken = default)
        {
            LastRemoveMutedBlacklistUserId = userId;
            return Task.FromResult(true);
        }

        public Task<bool> RemoveFanAsync(long userId, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<UserInfoGuInfoWeb> GetUserInfoWebAsync(int userId, CancellationToken cancellationToken = default)
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

        public Task<bool> SetNicknameAsync(string nickName, CancellationToken cancellationToken = default)
        {
            LastNickname = nickName;
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

        public Task<UserPostGroups> GetPostsAsync(int userId, uint pn, uint rn, string version,
            CancellationToken cancellationToken = default)
        {
            LastPostsUserId = userId;
            LastPostsPn = pn;
            LastPostsRn = rn;
            LastPostsVersion = version;
            return Task.FromResult(UserPostGroups);
        }

        public Task<UserThreads> GetThreadsAsync(int userId, uint pn, bool publicOnly,
            CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }
}
