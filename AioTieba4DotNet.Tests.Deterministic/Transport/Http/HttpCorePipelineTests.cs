#nullable enable
using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AioTieba4DotNet.Internal;
using AioTieba4DotNet.Session;
using AioTieba4DotNet.Transport.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AioTieba4DotNet.Tests.Transport.Http;

[TestClass]
public class HttpCorePipelineTests
{
    [TestMethod]
    public async Task SendAppFormAsync_PreservesSigningAndHeaders()
    {
        var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("app-form-response", Encoding.UTF8)
        });
        var httpCore = new HttpCore(new HttpClient(handler));
        var data = new List<KeyValuePair<string, string>> { new("key1", "value1"), new("key2", "12345") };

        var result = await httpCore.SendAppFormAsync(new Uri("https://tieba.baidu.com/c/s/login"), data);

        Assert.AreEqual("app-form-response", result);
        Assert.AreEqual(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.AreEqual($"aiotieba/{Const.Version}", handler.LastRequest.Headers.UserAgent.ToString());
        Assert.AreEqual(Const.AppBaseHost, handler.LastRequest.Headers.Host);
        Assert.AreEqual("application/x-www-form-urlencoded",
            handler.LastRequest.Content!.Headers.ContentType!.MediaType);

        var body = await handler.LastRequest.Content!.ReadAsStringAsync();
        Assert.AreEqual("key1=value1&key2=12345&sign=9732aa652304b3770aba8902323a05a7", body);
    }

    [TestMethod]
    public async Task SendAppProtoAsync_PreservesMultipartAndProtoHeaders()
    {
        var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent(new byte[] { 9, 8, 7 })
        });
        var httpCore = new HttpCore(new HttpClient(handler));
        var payload = new byte[] { 1, 2, 3, 4 };

        var result =
            await httpCore.SendAppProtoAsync(new Uri("https://tiebac.baidu.com/c/f/frs/page?cmd=301001"), payload);

        CollectionAssert.AreEqual(new byte[] { 9, 8, 7 }, result);
        Assert.AreEqual(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.AreEqual($"aiotieba/{Const.Version}", handler.LastRequest.Headers.UserAgent.ToString());
        Assert.AreEqual("protobuf", handler.LastRequest.Headers.GetValues("x_bd_data_type").Single());
        Assert.AreEqual(Const.AppBaseHost, handler.LastRequest.Headers.Host);
        Assert.AreEqual("*/*", handler.LastRequest.Headers.Accept.Single().MediaType);
        CollectionAssert.Contains(handler.LastRequest.Headers.Connection.ToList(), "keep-alive");

        var boundary = handler.LastRequest.Content!.Headers.ContentType!.Parameters
            .Single(parameter => parameter.Name == "boundary").Value!;
        Assert.DoesNotContain('"', boundary);

        var contentDisposition =
            ((MultipartFormDataContent)handler.LastRequest.Content).First().Headers.ContentDisposition;
        Assert.AreEqual("data", contentDisposition!.Name!.Trim('"'));
        Assert.AreEqual("file", contentDisposition.FileName!.Trim('"'));

        var rawBody = await handler.LastRequest.Content.ReadAsByteArrayAsync();
        CollectionAssert.Contains(rawBody, (byte)1);
        CollectionAssert.Contains(rawBody, (byte)4);
    }

    [TestMethod]
    public async Task SendWebGetAsync_PreservesQueryAndWebHeaders()
    {
        var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("web-get-response", Encoding.UTF8)
        });
        var httpCore = new HttpCore(new HttpClient(handler));

        var result = await httpCore.SendWebGetAsync(new Uri("http://tieba.baidu.com/i/sys/user_json"),
            new List<KeyValuePair<string, string>> { new("un", "hello world"), new("ie", "utf-8") });

        Assert.AreEqual("web-get-response", result);
        Assert.AreEqual(HttpMethod.Get, handler.LastRequest!.Method);
        Assert.AreEqual("un=hello+world&ie=utf-8", handler.LastRequest.RequestUri!.Query.TrimStart('?'));
        Assert.AreEqual($"aiotieba/{Const.Version}", handler.LastRequest.Headers.UserAgent.ToString());
        CollectionAssert.AreEquivalent(new[] { "gzip", "deflate" },
            handler.LastRequest.Headers.AcceptEncoding.Select(header => header.Value).ToArray());
        Assert.IsTrue(handler.LastRequest.Headers.CacheControl!.NoCache);
        CollectionAssert.Contains(handler.LastRequest.Headers.Connection.ToList(), "keep-alive");
        Assert.AreEqual("*/*", handler.LastRequest.Headers.Accept.Single().MediaType);
    }

    [TestMethod]
    public async Task SendWebFormAsync_PreservesPlainFormPostWithoutSignatureHeaders()
    {
        var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("web-form-response", Encoding.UTF8)
        });
        var httpCore = new HttpCore(new HttpClient(handler));

        var result = await httpCore.SendWebFormAsync(new Uri("https://tieba.baidu.com/f/commit/share/fnameShare"),
            new List<KeyValuePair<string, string>> { new("kw", "dotnet"), new("fid", "1") });

        Assert.AreEqual("web-form-response", result);
        Assert.AreEqual(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.IsFalse(handler.LastRequest.Headers.Contains("x_bd_data_type"));
        Assert.IsFalse(handler.LastRequest.Headers.Contains("Host"));
        var body = await handler.LastRequest.Content!.ReadAsStringAsync();
        Assert.AreEqual("kw=dotnet&fid=1", body);
    }

    [TestMethod]
    public async Task SendWebGetAsync_RetriesTransientFailures_WhenConfigured()
    {
        var attempts = 0;
        var handler = new RecordingHandler(_ =>
        {
            attempts++;
            if (attempts == 1) throw new HttpRequestException("transient");

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("retry-success", Encoding.UTF8)
            };
        });
        var httpCore =
            new HttpCore(new TiebaOptions { RequestTimeout = Timeout.InfiniteTimeSpan, MaxReadRetryAttempts = 1 },
                new HttpClient(handler));

        var result = await httpCore.SendWebGetAsync(new Uri("http://tieba.baidu.com/i/sys/user_json"),
            new List<KeyValuePair<string, string>> { new("un", "tester") });

        Assert.AreEqual("retry-success", result);
        Assert.AreEqual(2, attempts);
    }

    [TestMethod]
    public async Task SendWebGetAsync_TimeoutBudgetIsNotRetriedAcrossAttempts()
    {
        var attempts = 0;
        var handler = new RecordingHandler(async (_, cancellationToken) =>
        {
            attempts++;
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("unreachable", Encoding.UTF8)
            };
        });
        var httpCore =
            new HttpCore(new TiebaOptions { RequestTimeout = TimeSpan.FromMilliseconds(50), MaxReadRetryAttempts = 1 },
                new HttpClient(handler));

        try
        {
            await httpCore.SendWebGetAsync(new Uri("http://tieba.baidu.com/i/sys/user_json"),
                new List<KeyValuePair<string, string>> { new("un", "tester") });
            Assert.Fail("Expected TiebaTimeoutException was not thrown.");
        }
        catch (TiebaTimeoutException)
        {
        }

        Assert.AreEqual(1, attempts);
    }

    [TestMethod]
    public async Task SendWebGetAsync_Timeout_IsStandardizedAsTiebaTimeoutException()
    {
        var handler = new RecordingHandler(async (_, cancellationToken) =>
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("unreachable", Encoding.UTF8)
            };
        });
        var httpCore = new HttpCore(new TiebaOptions { RequestTimeout = TimeSpan.FromMilliseconds(50) },
            new HttpClient(handler));

        try
        {
            await httpCore.SendWebGetAsync(new Uri("http://tieba.baidu.com/i/sys/user_json"),
                new List<KeyValuePair<string, string>> { new("un", "tester") });
            Assert.Fail("Expected TiebaTimeoutException was not thrown.");
        }
        catch (TiebaTimeoutException)
        {
        }
    }

    [TestMethod]
    public void CreatePrimaryHandler_PreservesCookiesAndCompressionDefaults()
    {
        var handler = TiebaHttpClientFactory.CreatePrimaryHandler();

        Assert.IsTrue(handler.UseCookies);
        Assert.IsNotNull(handler.CookieContainer);
        Assert.AreEqual(DecompressionMethods.GZip, handler.AutomaticDecompression);
    }

    private sealed class RecordingHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _asyncResponseFactory;

        internal RecordingHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
        {
            _asyncResponseFactory = (request, _) => Task.FromResult(responseFactory(request));
        }

        internal RecordingHandler(
            Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> responseFactory)
        {
            _asyncResponseFactory = responseFactory;
        }

        internal HttpRequestMessage? LastRequest { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            LastRequest = await CloneAsync(request, cancellationToken);
            return await _asyncResponseFactory(request, cancellationToken);
        }

        private static async Task<HttpRequestMessage> CloneAsync(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var clone = new HttpRequestMessage(request.Method, request.RequestUri);
            foreach (var header in request.Headers)
                clone.Headers.TryAddWithoutValidation(header.Key, header.Value);

            if (request.Content == null) return clone;

            var bytes = await request.Content.ReadAsByteArrayAsync(cancellationToken);
            var content = request.Content is MultipartFormDataContent multipart
                ? CloneMultipartContent(multipart)
                : new ByteArrayContent(bytes);

            foreach (var header in request.Content.Headers)
                content.Headers.TryAddWithoutValidation(header.Key, header.Value);

            clone.Content = content;
            return clone;
        }

        private static HttpContent CloneMultipartContent(MultipartFormDataContent original)
        {
            var boundary = original.Headers.ContentType!.Parameters.Single(parameter => parameter.Name == "boundary")
                .Value!;
            var clone = new MultipartFormDataContent(boundary);

            foreach (var part in original)
            {
                var partBytes = part.ReadAsByteArrayAsync().GetAwaiter().GetResult();
                var partClone = new ByteArrayContent(partBytes);
                foreach (var header in part.Headers)
                    partClone.Headers.TryAddWithoutValidation(header.Key, header.Value);
                clone.Add(partClone);
            }

            clone.Headers.ContentType!.Parameters.Clear();
            foreach (var parameter in original.Headers.ContentType.Parameters)
                clone.Headers.ContentType.Parameters.Add(new System.Net.Http.Headers.NameValueHeaderValue(
                    parameter.Name,
                    parameter.Value));

            return clone;
        }
    }
}
