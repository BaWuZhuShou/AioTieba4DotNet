#nullable enable
using System;
using System.Threading;
using System.Threading.Tasks;
using AioTieba4DotNet.Enums;
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
    public async Task UserModule_DelegatesSetBlacklistToInternalProtocol()
    {
        var protocol = new RecordingUserProtocol(new UserInfo());
        var module = new UserModule(protocol);

        var actual = await module.SetBlacklistAsync(456, BlacklistType.Chat);

        Assert.IsTrue(actual);
        Assert.AreEqual(456, protocol.LastBlacklistUserId);
        Assert.AreEqual(BlacklistType.Chat, protocol.LastBlacklistType);
    }

    private sealed class RecordingUserProtocol(UserInfo selfInfo) : IUserProtocol
    {
        public long? LastBlacklistUserId { get; private set; }

        public BlacklistType? LastBlacklistType { get; private set; }

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

        public Task<AtMessages> GetAtsAsync(int pn, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<ReplyMessages> GetRepliesAsync(int pn, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<BlacklistUsers> GetBlacklistAsync(CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<bool> SetBlacklistAsync(long userId, BlacklistType type, CancellationToken cancellationToken = default)
        {
            LastBlacklistUserId = userId;
            LastBlacklistType = type;
            return Task.FromResult(true);
        }

        public Task<bool> RemoveFanAsync(long userId, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<UserPostss> GetPostsAsync(int userId, uint pn, uint rn, string version,
            CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public Task<UserThreads> GetThreadsAsync(int userId, uint pn, bool publicOnly,
            CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }
}
