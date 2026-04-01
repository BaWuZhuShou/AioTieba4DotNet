#nullable enable
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AioTieba4DotNet.Models.Admins;
using AioTieba4DotNet.Modules;
using AioTieba4DotNet.Protocols;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AioTieba4DotNet.Tests.Baseline;

[TestClass]
public sealed class AdminModuleBehaviorTests
{
    [TestMethod]
    public async Task AdminModule_DelegatesGetBawuInfoToInternalProtocol()
    {
        var expected = new BawuInfo
        {
            Admins = [new BawuUser { UserId = 1, UserName = "admin-user", NickNameNew = "Admin" }],
            All = [new BawuUser { UserId = 1, UserName = "admin-user", NickNameNew = "Admin" }]
        };
        var protocol = new RecordingAdminProtocol(expected);
        var module = new AdminModule(protocol);

        var actual = await module.GetBawuInfoAsync("lol欧服");

        Assert.AreSame(expected, actual);
        Assert.AreEqual("lol欧服", protocol.LastForumName);
    }

    [TestMethod]
    public async Task AdminModule_DelegatesGetBawuPermToInternalProtocol()
    {
        var protocol = new RecordingAdminProtocol(new BawuInfo())
        {
            BawuPermResult = new BawuPerm { Permissions = BawuPermType.Unblock | BawuPermType.Recover }
        };
        var module = new AdminModule(protocol);

        var actual = await module.GetBawuPermAsync("lol欧服", "tb.1.target");

        Assert.AreEqual(BawuPermType.Unblock | BawuPermType.Recover, actual.Permissions);
        Assert.AreEqual("lol欧服", protocol.LastForumName);
        Assert.AreEqual("tb.1.target", protocol.LastPortrait);
    }

    [TestMethod]
    public async Task AdminModule_DelegatesSetBawuPermToInternalProtocol()
    {
        var protocol = new RecordingAdminProtocol(new BawuInfo());
        var module = new AdminModule(protocol);

        var actual = await module.SetBawuPermAsync("lol欧服", "tb.1.target",
            BawuPermType.UnblockAppeal | BawuPermType.RecoverAppeal);

        Assert.IsTrue(actual);
        Assert.AreEqual("lol欧服", protocol.LastForumName);
        Assert.AreEqual("tb.1.target", protocol.LastPortrait);
        Assert.AreEqual(BawuPermType.UnblockAppeal | BawuPermType.RecoverAppeal, protocol.LastPermissions);
    }

    private sealed class RecordingAdminProtocol(BawuInfo bawuInfo) : IAdminProtocol
    {
        public string? LastForumName { get; private set; }

        public string? LastPortrait { get; private set; }

        public BawuPermType? LastPermissions { get; private set; }

        public BawuPerm BawuPermResult { get; init; } = new();

        public Task<bool> AddBawuAsync(string fname, string userName, BawuType bawuType,
            CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<bool> DelBawuAsync(string fname, string portrait, BawuType bawuType,
            CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<bool> AddBawuBlacklistAsync(string fname, long userId,
            CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<bool> DelBawuBlacklistAsync(string fname, long userId,
            CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<BawuBlacklistUsers> GetBawuBlacklistAsync(string fname, int pn,
            CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<BawuInfo> GetBawuInfoAsync(string fname, CancellationToken cancellationToken = default)
        {
            LastForumName = fname;
            return Task.FromResult(bawuInfo);
        }

        public Task<BawuPerm> GetBawuPermAsync(string fname, string portrait,
            CancellationToken cancellationToken = default)
        {
            LastForumName = fname;
            LastPortrait = portrait;
            return Task.FromResult(BawuPermResult);
        }

        public Task<bool> SetBawuPermAsync(string fname, string portrait, BawuPermType permissions,
            CancellationToken cancellationToken = default)
        {
            LastForumName = fname;
            LastPortrait = portrait;
            LastPermissions = permissions;
            return Task.FromResult(true);
        }

        public Task<BawuPostLogs> GetBawuPostLogsAsync(string fname, BawuPostLogQueryOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<BawuUserLogs> GetBawuUserLogsAsync(string fname, BawuUserLogQueryOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<Appeals> GetUnblockAppealsAsync(string fname, int pn, int rn,
            CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<bool> HandleUnblockAppealsAsync(string fname, IReadOnlyList<long> appealIds, bool refuse,
            CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<Blocks> GetBlocksAsync(string fname, string userName, int pn,
            CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<bool> BlockAsync(ulong fid, string portrait, int day, string reason,
            CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<bool> BlockAsync(string fname, string portrait, int day, string reason,
            CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<bool> UnblockAsync(ulong fid, long userId, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<bool> UnblockAsync(string fname, long userId, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}
