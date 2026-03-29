#nullable enable
using System;
using System.Threading;
using System.Threading.Tasks;
using AioTieba4DotNet.Models.Forums;
using AioTieba4DotNet.Modules;
using AioTieba4DotNet.Protocols;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AioTieba4DotNet.Tests.Baseline;

[TestClass]
public class ForumModuleBehaviorTests
{
    private const string CanonicalSafeForumName = "lol欧服";

    [TestMethod]
    public async Task ForumModule_DelegatesToInternalProtocol()
    {
        var expected = new Forum { Fid = 7356044, Fname = CanonicalSafeForumName };
        var protocol = new RecordingForumProtocol(expected);
        var module = new ForumModule(protocol);

        var actual = await module.GetForumAsync(CanonicalSafeForumName);

        Assert.AreSame(expected, actual);
        Assert.AreEqual(CanonicalSafeForumName, protocol.LastFname);
    }

    private sealed class RecordingForumProtocol(Forum forum) : IForumProtocol
    {
        public string? LastFname { get; private set; }

        public Task<ulong> GetFidAsync(string fname, CancellationToken cancellationToken = default) =>
            Task.FromResult(7356044UL);

        public Task<string> GetFnameAsync(ulong fid, CancellationToken cancellationToken = default) =>
            Task.FromResult(CanonicalSafeForumName);

        public Task<ForumDetail> GetDetailAsync(ulong fid, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<ForumDetail> GetDetailAsync(string fname, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<bool> LikeAsync(string fname, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<bool> UnlikeAsync(string fname, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<bool> SignAsync(string fname, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<Forum> GetForumAsync(string fname, CancellationToken cancellationToken = default)
        {
            LastFname = fname;
            return Task.FromResult(forum);
        }

        public Task<bool> DelBaWuAsync(string fname, string portrait, string baWuType,
            CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }
}
