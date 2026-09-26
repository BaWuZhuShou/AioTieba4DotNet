#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AioTieba4DotNet.Contracts;
using Google.Protobuf;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace AioTieba4DotNet.Tests.Governance.Contracts;

// 仅执行预先声明的内存响应；任何意外请求都会失败，不创建网络 handler。
internal sealed class ForumLookupTestTransport : HttpMessageHandler, IHttpClientFactory
{
    internal const string FidPath = "/f/commit/share/fnameShareApi";
    internal const string DetailPath = "/c/f/forum/getforumdetail";
    internal const string ForumPath = "/c/f/frs/frsBottom";
    internal const string LoginPath = "/c/s/login";
    internal const string BlockPath = "/c/c/bawu/commitprison";
    internal const string CategoriesPath = "/c/c/bawu/goodlist";
    internal const string GoodPath = "/c/c/bawu/commitgood";
    internal const string UserForumPath = "/c/f/forum/getUserForumLevelInfo";
    internal const string LoginBody = "{\"error_code\":0,\"user\":{},\"anti\":{\"tbs\":\"fixture-tbs\"}}";
    internal const string SuccessBody = "{\"error_code\":0}";

    private readonly object _gate = new();
    private readonly Queue<(string Path, Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> Respond)> _steps = new();
    private readonly List<string> _requests = [];
    private readonly List<HttpClient> _httpClients = [];

    internal ITiebaClient CreateTiebaClient(bool authenticated = false)
    {
        return new TiebaClientFactory(this).CreateClient(new TiebaOptions
        {
            Bduss = authenticated ? new string('b', 192) : null,
            TransportMode = TiebaTransportMode.Http,
            RequestTimeout = Timeout.InfiniteTimeSpan,
            MaxReadRetryAttempts = 0
        });
    }

    public HttpClient CreateClient(string name)
    {
        var client = new HttpClient(this, false) { Timeout = Timeout.InfiniteTimeSpan };
        _httpClients.Add(client);
        return client;
    }

    internal void Expect(string path,
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> respond)
    {
        lock (_gate)
            _steps.Enqueue((path, respond));
    }

    internal void ExpectJson(string path, string body, Action<Dictionary<string, string>>? inspect = null)
    {
        Expect(path, async (request, ct) =>
        {
            if (inspect is not null)
            {
                var encoded = request.Method == HttpMethod.Get
                    ? request.RequestUri!.Query.TrimStart('?')
                    : await request.Content!.ReadAsStringAsync(ct);
                inspect(encoded.Split('&', StringSplitOptions.RemoveEmptyEntries)
                    .Select(pair => pair.Split('=', 2))
                    .ToDictionary(pair => WebUtility.UrlDecode(pair[0]),
                        pair => WebUtility.UrlDecode(pair.Length == 2 ? pair[1] : "")));
            }

            return JsonResponse(body);
        });
    }

    internal void ExpectFid(string name, ulong fid)
    {
        ExpectJson(FidPath, JsonConvert.SerializeObject(new { no = 0, data = new { fid } }),
            fields => Assert.AreEqual(name, fields["fname"]));
    }

    internal void ExpectDetail(ulong requestedId, ulong returnedId, string returnedName,
        int errorCode = 0, string errorMessage = "")
    {
        Expect(DetailPath, async (request, ct) =>
        {
            var body = await request.Content!.ReadAsByteArrayAsync(ct);
            var boundary = request.Content.Headers.ContentType!.Parameters.Single(p => p.Name == "boundary").Value!;
            var start = body.AsSpan().IndexOf("\r\n\r\n"u8) + 4;
            var end = body.AsSpan().LastIndexOf(Encoding.ASCII.GetBytes($"\r\n--{boundary.Trim('"')}--"));
            Assert.IsTrue(start >= 4 && end >= start, "Expected a multipart protobuf request.");
            var protobuf = GetForumDetailReqIdl.Parser.ParseFrom(body[start..end]);
            Assert.AreEqual(unchecked((long)requestedId), protobuf.Data.ForumId);

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(new GetForumDetailResIdl
                {
                    Error = new Error { Errorno = errorCode, Errmsg = errorMessage },
                    Data = new GetForumDetailResIdl.Types.DataRes
                    {
                        ForumInfo = new GetForumDetailResIdl.Types.DataRes.Types.RecommendForumInfo
                        {
                            ForumId = returnedId,
                            ForumName = returnedName
                        }
                    }
                }.ToByteArray())
            };
        });
    }

    internal void ExpectForum(string requestedName, ulong returnedId, string? returnedName)
    {
        ExpectJson(ForumPath, JsonConvert.SerializeObject(new
        {
            error_code = 0,
            forum = new
            {
                id = returnedId,
                name = returnedName,
                first_class = "",
                second_class = "",
                avatar = "",
                slogan = "",
                member_num = 1,
                post_num = 2,
                thread_num = 3
            }
        }), fields => Assert.AreEqual(requestedName, fields["kw"]));
    }

    internal static HttpResponseMessage JsonResponse(string body)
    {
        return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(body) };
    }

    internal void AssertComplete(params string[] paths)
    {
        lock (_gate)
        {
            CollectionAssert.AreEqual(paths, _requests.ToArray(), "The request order changed.");
            Assert.AreEqual(0, _steps.Count, "Not every expected request was sent.");
        }
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> respond;
        lock (_gate)
        {
            var path = request.RequestUri!.AbsolutePath;
            _requests.Add(path);
            Assert.IsTrue(_steps.Count > 0, $"Unexpected offline request: {path}");
            var step = _steps.Dequeue();
            Assert.AreEqual(step.Path, path, "The request order changed.");
            respond = step.Respond;
        }

        return respond(request, cancellationToken);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            foreach (var client in _httpClients)
                client.Dispose();
        base.Dispose(disposing);
    }
}
