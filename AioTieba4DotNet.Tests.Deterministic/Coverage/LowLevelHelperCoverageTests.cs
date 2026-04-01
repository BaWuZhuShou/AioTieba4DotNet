#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using AioTieba4DotNet.Contracts;
using AioTieba4DotNet.Internal;
using AioTieba4DotNet.Internal.Mapping;
using AioTieba4DotNet.Models.Admins;
using AioTieba4DotNet.Models.Contents;
using AioTieba4DotNet.Models.Forums;
using AioTieba4DotNet.Models.Threads;
using AioTieba4DotNet.Session;
using AioTieba4DotNet.Transport.Http;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AioTieba4DotNet.Tests.Coverage;

[TestClass]
public sealed class LowLevelHelperCoverageTests
{
    [TestMethod]
    public void ConstantsOptionsAndSessionState_CoverFallbackBranches()
    {
        Assert.AreEqual(0, Const.BASE32_LEN(0));
        Assert.AreEqual(8, Const.BASE32_LEN(1));
        Assert.AreEqual(32, Const.BASE32_LEN(20));
        Assert.AreEqual(40, Const.BASE32_LEN(21));
        Assert.AreEqual(32, Const.TbcSha1Base32Size);
        Assert.IsFalse(string.IsNullOrWhiteSpace(Const.Version));

        var service = new TiebaOptionsValidationService();
        var good = new TiebaOptions();
        var bad = new TiebaOptions { Stoken = "st" };

        Assert.AreEqual(ValidateOptionsResult.Skip, service.Validate("custom", good));
        Assert.AreEqual(ValidateOptionsResult.Success, service.Validate(Options.DefaultName, good));
        var failure = service.Validate(null, bad);
        Assert.IsFalse(failure.Succeeded);
        StringAssert.Contains(string.Join(" ", failure.Failures ?? Array.Empty<string>()), "Stoken cannot be supplied without Bduss.");

        var guestFromNull = TiebaSessionState.FromAccount(null);
        var guestFromBlank = TiebaSessionState.FromAccount(new Account());
        var pendingAuth = TiebaSessionState.FromAccount(new Account(new string('b', 192)));
        var readyAuth = TiebaSessionState.FromAccount(new Account(new string('b', 192))
        {
            Tbs = "tbs-1",
            ClientId = "client-1",
            SampleId = "sample-1",
            ZId = "z-1"
        });

        Assert.IsFalse(guestFromNull.IsAuthenticated);
        Assert.IsFalse(guestFromBlank.IsAuthenticated);
        Assert.AreEqual(TiebaSessionKind.Guest, guestFromNull.Kind);
        Assert.AreEqual(TiebaSessionResourceState.Unavailable, guestFromNull.TbsState);
        Assert.AreEqual(TiebaSessionResourceState.Pending, guestFromNull.WebSocketState);
        Assert.AreEqual(TiebaSessionKind.Authenticated, pendingAuth.Kind);
        Assert.AreEqual(TiebaSessionResourceState.Pending, pendingAuth.TbsState);
        Assert.AreEqual(TiebaSessionResourceState.Pending, pendingAuth.ClientState);
        Assert.AreEqual(TiebaSessionResourceState.Pending, pendingAuth.ZIdState);
        Assert.IsTrue(pendingAuth.IsAuthenticated);
        Assert.AreEqual(TiebaSessionResourceState.Ready, readyAuth.TbsState);
        Assert.AreEqual(TiebaSessionResourceState.Ready, readyAuth.ClientState);
        Assert.AreEqual(TiebaSessionResourceState.Ready, readyAuth.ZIdState);
    }

    [TestMethod]
    public async Task HttpFactoryAndPolicy_CoverRequestKindsRetryAndTimeoutBranches()
    {
        var appForm = await TiebaHttpRequestFactory.CreateMessageAsync(
            TiebaHttpRequestDescriptor.AppForm(new Uri("https://tiebac.baidu.com/app-form"),
                [new KeyValuePair<string, string>("foo", "bar"), new KeyValuePair<string, string>("space", "a b")]),
            default);
        var appProto = await TiebaHttpRequestFactory.CreateMessageAsync(
            TiebaHttpRequestDescriptor.AppProto(new Uri("https://tiebac.baidu.com/app-proto"), [1, 2, 3]),
            default);
        var webGet = await TiebaHttpRequestFactory.CreateMessageAsync(
            TiebaHttpRequestDescriptor.WebGet(new Uri("https://tieba.baidu.com/web-get"),
                [new KeyValuePair<string, string>("a", "1"), new KeyValuePair<string, string>("b", "2")], true),
            default);
        var webForm = await TiebaHttpRequestFactory.CreateMessageAsync(
            TiebaHttpRequestDescriptor.WebForm(new Uri("https://tieba.baidu.com/web-form"),
                [new KeyValuePair<string, string>("x", "y")]),
            default);
        var customDescriptor = CreateCustomDescriptor();

        Assert.AreEqual(HttpMethod.Post, appForm.Method);
        Assert.AreEqual(Const.AppBaseHost, appForm.Headers.Host);
        StringAssert.Contains(await appForm.Content!.ReadAsStringAsync(), "foo=bar");
        Assert.IsInstanceOfType(appProto.Content, typeof(MultipartFormDataContent));
        Assert.AreEqual("protobuf", appProto.Headers.GetValues("x_bd_data_type").Single());
        Assert.AreEqual(HttpMethod.Get, webGet.Method);
        StringAssert.Contains(webGet.RequestUri!.Query, "a=1");
        StringAssert.Contains(webGet.RequestUri.Query, "b=2");
        Assert.AreEqual(HttpMethod.Post, webForm.Method);
        StringAssert.Contains(await webForm.Content!.ReadAsStringAsync(), "x=y");
        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => TiebaHttpRequestFactory.CreateMessageAsync(customDescriptor));

        var infinitePolicy = TiebaHttpExecutionPolicy.FromOptions(new TiebaOptions
        {
            RequestTimeout = Timeout.InfiniteTimeSpan,
            MaxReadRetryAttempts = 0
        });
        Assert.IsFalse(infinitePolicy.HasReadRetries);

        var retryPolicy = TiebaHttpExecutionPolicy.FromOptions(new TiebaOptions
        {
            RequestTimeout = TimeSpan.FromMilliseconds(200),
            MaxReadRetryAttempts = 1
        });
        Assert.IsTrue(retryPolicy.HasReadRetries);

        var retryDescriptor = TiebaHttpRequestDescriptor.AppForm(new Uri("https://tiebac.baidu.com/retry"),
            [new KeyValuePair<string, string>("a", "1")]);
        var retryRequestCalls = 0;
        var retryResponse = await retryPolicy.SendAsync(new HttpClient(new SwitchingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK))),
            ct =>
            {
                retryRequestCalls++;
                if (retryRequestCalls == 1)
                    throw new IOException("first");

                return TiebaHttpRequestFactory.CreateMessageAsync(retryDescriptor, ct);
            },
            allowRetry: true,
            requestKind: TiebaHttpRequestKind.AppForm,
            cancellationToken: default);

        var noRetryRequestCalls = 0;
        var noRetryException = await Assert.ThrowsExactlyAsync<TiebaTransportException>(() => retryPolicy.SendAsync(
            new HttpClient(new SwitchingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK))),
            ct =>
            {
                noRetryRequestCalls++;
                throw new IOException("boom");
            },
            allowRetry: false,
            requestKind: TiebaHttpRequestKind.AppForm,
            cancellationToken: default));

        var timeoutPolicy = TiebaHttpExecutionPolicy.FromOptions(new TiebaOptions
        {
            RequestTimeout = TimeSpan.FromMilliseconds(200),
            MaxReadRetryAttempts = 0
        });
        var timeoutHandler = new SwitchingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var timeoutClient = new HttpClient(timeoutHandler);
        var timeoutResponse = await timeoutPolicy.SendAsync(timeoutClient,
            ct => TiebaHttpRequestFactory.CreateMessageAsync(retryDescriptor, ct),
            allowRetry: false,
            requestKind: TiebaHttpRequestKind.AppForm,
            cancellationToken: default);

        var infiniteResponse = await infinitePolicy.SendAsync(
            new HttpClient(new SwitchingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK))),
            ct => TiebaHttpRequestFactory.CreateMessageAsync(retryDescriptor, ct),
            allowRetry: false,
            requestKind: TiebaHttpRequestKind.AppForm,
            cancellationToken: default);

        Assert.AreEqual(2, retryRequestCalls);
        Assert.AreEqual(HttpStatusCode.OK, retryResponse.StatusCode);
        StringAssert.Contains(noRetryException.Message, "HTTP AppForm request");
        Assert.AreEqual(1, noRetryRequestCalls);
        Assert.AreEqual(HttpStatusCode.OK, timeoutResponse.StatusCode);
        Assert.AreEqual(1, timeoutHandler.CallCount);
        Assert.AreEqual(HttpStatusCode.OK, infiniteResponse.StatusCode);
    }

    [TestMethod]
    public void HelperModels_CoverEqualityTextUrlAndPagingBranches()
    {
        var block = new Block { UserId = 20, UserName = "blocked", NickNameOld = "blocked-nick", Day = 3 };
        var blockNoNickname = new Block { UserId = 21, UserName = "plain-user", Day = 1 };
        var blocks = new Blocks([block], new BlocksPage { HasMore = true, HasPrevious = false });
        var blocksNoMore = new Blocks([], new BlocksPage { HasMore = false, HasPrevious = true });
        var blocksNullException = Assert.ThrowsExactly<ArgumentNullException>(() => new Blocks([], null!));
        var squareForum = new SquareForum { Fid = 40, Fname = "square", MemberNum = 2, PostNum = 3, IsFollowed = true };
        var squareForums = new SquareForums([squareForum], new SquareForumsPage { HasMore = false, HasPrevious = false });
        var squareForumsMore = new SquareForums([], new SquareForumsPage { HasMore = true, HasPrevious = true });
        var internalLink = new FragLink
        {
            RawUrl = new Uri("https://example.com/forum/thread"),
            Title = "internal",
            Text = "body"
        };
        var externalLinkWithUrl = new FragLink
        {
            RawUrl = new Uri("https://tieba.baidu.com/mo/q/checkurl?url=https%3A%2F%2Fexample.com%2Fnext"),
            Text = "outer"
        };
        var externalLinkWithoutUrl = new FragLink
        {
            RawUrl = new Uri("https://tieba.baidu.com/mo/q/checkurl"),
            Text = "fallback"
        };
        var shareWithTitle = new ShareThread
        {
            Content = CreateContent("shared body"),
            Title = "shared title",
            AuthorId = 30,
            Fid = 31,
            Fname = "forum",
            Tid = 32,
            Pid = 33
        };
        var shareWithoutTitle = new ShareThread
        {
            Content = CreateContent("shared plain"),
            AuthorId = 34,
            Fid = 35,
            Fname = "forum2",
            Tid = 36,
            Pid = 37
        };
        var shareMapped = ShareThreadMapper.FromTbData(new global::ThreadInfo.Types.OriginThreadInfo
        {
            Title = "shared empty",
            Fid = 41,
            Fname = "forum-empty",
            Tid = "0"
        });

        Assert.AreEqual("blocked-nick", block.NickName);
        Assert.AreEqual("blocked-nick", block.ShowName);
        Assert.IsTrue(block.Equals(new Block { UserId = 20 }));
        Assert.IsFalse(block.Equals(new Block { UserId = 21 }));
        Assert.AreEqual("plain-user", blockNoNickname.ShowName);
        Assert.IsFalse(block.Equals(blockNoNickname));
        Assert.IsTrue(blocks.HasMore);
        Assert.IsFalse(blocksNoMore.HasMore);
        Assert.IsNotNull(blocksNullException);

        Assert.AreEqual("square", squareForum.Fname);
        Assert.IsTrue(squareForum.Equals(new SquareForum { Fid = 40 }));
        Assert.IsFalse(squareForum.Equals(new SquareForum { Fid = 41 }));
        Assert.IsFalse(squareForums.HasMore);
        Assert.IsTrue(squareForumsMore.HasMore);
        Assert.IsTrue(squareForumsMore.Page.HasPrevious);

        Assert.IsFalse(internalLink.IsExternal);
        Assert.AreSame(internalLink.RawUrl, internalLink.Url);
        Assert.IsTrue(externalLinkWithUrl.IsExternal);
        Assert.AreEqual("https://example.com/next", externalLinkWithUrl.Url.ToString());
        Assert.IsTrue(externalLinkWithoutUrl.IsExternal);
        Assert.AreSame(externalLinkWithoutUrl.RawUrl, externalLinkWithoutUrl.Url);

        Assert.AreEqual("shared title\n" + shareWithTitle.Content.Texts, shareWithTitle.Text);
        Assert.AreEqual(shareWithoutTitle.Content.Texts.ToString() ?? string.Empty, shareWithoutTitle.Text);
        Assert.AreEqual("shared empty", shareMapped.Title);
        Assert.AreEqual(0L, shareMapped.AuthorId);
        Assert.AreEqual(41L, shareMapped.Fid);
        Assert.AreEqual("forum-empty", shareMapped.Fname);
        Assert.AreEqual(0L, shareMapped.Tid);
        Assert.AreEqual(0L, shareMapped.Pid);
        Assert.IsNull(shareMapped.VoteInfo);
    }

    private static TiebaHttpRequestDescriptor CreateCustomDescriptor()
    {
        return (TiebaHttpRequestDescriptor)Activator.CreateInstance(
            typeof(TiebaHttpRequestDescriptor),
            BindingFlags.Instance | BindingFlags.NonPublic,
            binder: null,
            args: new object[] { (TiebaHttpRequestKind)999, new Uri("https://tiebac.baidu.com/custom"), null!, null!, false },
            culture: null)!;
    }

    private static Content CreateContent(string text)
    {
        return new Content
        {
            Texts = [new FragText { Text = text }],
            Frags = [new FragText { Text = text }]
        };
    }

    private sealed class SwitchingHandler : HttpMessageHandler
    {
        private readonly Func<int, HttpResponseMessage> _responseFactory;

        public SwitchingHandler(params Func<int, HttpResponseMessage>[] behaviors)
        {
            _responseFactory = call =>
            {
                var index = CallCount;
                if (index >= behaviors.Length)
                    return behaviors[^1](index);

                return behaviors[index](index);
            };
        }

        public int CallCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            CallCount++;
            return Task.FromResult(_responseFactory(CallCount - 1));
        }
    }

    private static async Task<TException> ThrowsExactlyAsync<TException>(Func<Task> action)
        where TException : Exception
    {
        try
        {
            await action();
        }
        catch (TException exception)
        {
            return exception;
        }

        Assert.Fail($"Expected exception of type {typeof(TException).Name} was not thrown.");
        throw new InvalidOperationException();
    }
}
