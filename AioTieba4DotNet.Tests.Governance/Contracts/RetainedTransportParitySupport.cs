#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AioTieba4DotNet.Api.GetImages;
using AioTieba4DotNet.Api.GetLastReplyers;
using AioTieba4DotNet.Api.GetSelfFollowForums;
using AioTieba4DotNet.Api.GetSquareForums;
using AioTieba4DotNet.Api.SignForums;
using AioTieba4DotNet.Contracts;
using AioTieba4DotNet.Internal;
using AioTieba4DotNet.Session;
using AioTieba4DotNet.Transport;
using AioTieba4DotNet.Transport.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SessionAccount = AioTieba4DotNet.Session.Account;

namespace AioTieba4DotNet.Tests.Governance.Contracts;

internal static class RetainedTransportParitySupport
{
    internal static ParityMaskRule[] MaskRules { get; } =
    [
        new("timestamp", "<masked:timestamp>", ["timestamp", "ts", "_timestamp"]),
        new("nonce", "<masked:nonce>", ["nonce", "noncestr"]),
        new("token", "<masked:token>", ["token", "access_token", "refresh_token"]),
        new("cursor", "<masked:cursor>", ["cursor", "next_cursor"])
    ];

    internal static TransportParityComparison Compare(TransportParityObservation expected, TransportParityObservation actual)
    {
        var diffs = new List<TransportParityDiff>();
        CompareScalar(diffs, "scheme", expected.Scheme, actual.Scheme);
        CompareScalar(diffs, "method", expected.Method, actual.Method);
        CompareScalar(diffs, "host", expected.Host, actual.Host);
        CompareScalar(diffs, "path", expected.Path, actual.Path);
        CompareSequence(diffs, "query.keys", expected.QueryFields.Select(static field => field.Name), actual.QueryFields.Select(static field => field.Name));
        CompareSequence(diffs, "query.fields", expected.QueryFields.Select(static field => field.DisplayValue), actual.QueryFields.Select(static field => field.DisplayValue));
        CompareSequence(diffs, "form.keys", expected.FormFields.Select(static field => field.Name), actual.FormFields.Select(static field => field.Name));
        CompareSequence(diffs, "form.fields", expected.FormFields.Select(static field => field.DisplayValue), actual.FormFields.Select(static field => field.DisplayValue));
        CompareSequence(diffs, "headers", expected.Headers.Select(static header => header.DisplayValue), actual.Headers.Select(static header => header.DisplayValue));
        CompareSequence(diffs, "cookies", expected.Cookies, actual.Cookies);
        CompareSequence(diffs, "signatureInputSequence", expected.SignatureInputSequence, actual.SignatureInputSequence);
        CompareSequence(diffs, "fallbackTransportPath", expected.FallbackTransportPath, actual.FallbackTransportPath);
        CompareScalar(diffs, "errorNormalization.exceptionType", expected.ErrorNormalization.ExceptionType, actual.ErrorNormalization.ExceptionType);
        CompareScalar(diffs, "errorNormalization.message", expected.ErrorNormalization.Message, actual.ErrorNormalization.Message);

        var appliedMaskIds = expected.AppliedMaskIds
            .Concat(actual.AppliedMaskIds)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(static id => id, StringComparer.Ordinal)
            .ToArray();

        return new TransportParityComparison(diffs.Count == 0, appliedMaskIds, diffs.ToArray(), expected, actual);
    }

    internal static TransportParityObservation ObserveRequest(CapturedHttpRequest request, IReadOnlyList<string>? fallbackTransportPath = null)
    {
        var query = ParseNameValuePairs(request.Uri.Query.TrimStart('?'));
        var form = string.Equals(request.ContentType, "application/x-www-form-urlencoded", StringComparison.OrdinalIgnoreCase)
            ? ParseNameValuePairs(request.BodyText ?? string.Empty)
            : [];

        var queryFields = CanonicalizeFields(query, out var queryMasks);
        var formFields = CanonicalizeFields(form, out var formMasks);
        var headerEntries = CanonicalizeHeaders(request.Headers);
        var cookies = ExtractCookies(request.Headers);
        var signatureInputSequence = formFields
            .Where(static field => !string.Equals(field.Name, "sign", StringComparison.Ordinal))
            .Select(static field => field.DisplayValue)
            .ToArray();

        return new TransportParityObservation(
            request.Uri.Scheme,
            request.Method,
            request.Uri.Host,
            request.Uri.AbsolutePath,
            queryFields,
            formFields,
            headerEntries,
            cookies,
            signatureInputSequence,
            fallbackTransportPath ?? [],
            TransportErrorNormalization.None,
            queryMasks.Concat(formMasks).Distinct(StringComparer.Ordinal).OrderBy(static id => id, StringComparer.Ordinal).ToArray());
    }

    internal static TransportParityObservation ObserveSignature(IEnumerable<KeyValuePair<string, string>> fields)
    {
        var list = fields.ToList();
        var canonicalFields = CanonicalizeFields(list, out var masks);
        var signatureInputSequence = canonicalFields
            .Where(static field => !string.Equals(field.Name, "sign", StringComparison.Ordinal))
            .Select(static field => field.DisplayValue)
            .ToArray();

        return new TransportParityObservation(
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty,
            [],
            canonicalFields,
            [],
            [],
            signatureInputSequence,
            [],
            TransportErrorNormalization.None,
            masks);
    }

    internal static TransportParityObservation ObserveErrorNormalization(Exception exception)
    {
        return new TransportParityObservation(
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty,
            [],
            [],
            [],
            [],
            [],
            [],
            new TransportErrorNormalization(exception.GetType().Name, exception.Message),
            []);
    }

    internal static async Task<CapturedHttpRequest> CaptureGetImagesRequestAsync(Uri imageUri)
    {
        var handler = new TransportParityCapturingHttpMessageHandler(_ => CreateBinaryResponse("image/jpeg", [1, 2, 3]));
        using var httpClient = new HttpClient(handler, disposeHandler: true);
        var api = new GetImages(new HttpCore(httpClient));
        _ = await api.RequestBytesAsync(imageUri);
        return handler.SingleCapture();
    }

    internal static async Task<CapturedHttpRequest> CaptureSignForumsRequestAsync()
    {
        var handler = new TransportParityCapturingHttpMessageHandler(_ => CreateJsonResponse("{\"error_code\":\"0\",\"error_msg\":\"\",\"error\":{\"errno\":0,\"errmsg\":\"\"}}"));
        using var httpClient = new HttpClient(handler, disposeHandler: true);
        var api = new SignForums(new HttpCore(httpClient));
        _ = await api.RequestAsync();
        return handler.SingleCapture();
    }

    internal static async Task<CapturedHttpRequest> CaptureSelfFollowForumsRequestAsync()
    {
        var handler = new TransportParityCapturingHttpMessageHandler(_ => CreateJsonResponse("{\"error_code\":0,\"has_more\":0,\"forum_list\":[]}"));
        using var httpClient = new HttpClient(handler, disposeHandler: true);
        var httpCore = new HttpCore(httpClient);
        var account = new SessionAccount(new string('b', 192), new string('s', 64)) { Tbs = "sample-tbs" };
        httpCore.SetAccount(account);
        var api = new GetSelfFollowForums(httpCore);
        _ = await api.RequestAsync(2, 20);
        return handler.SingleCapture();
    }

    internal static async Task<CapturedHttpRequest> CaptureLastReplyersRequestAsync()
    {
        var handler = new TransportParityCapturingHttpMessageHandler(_ => throw new HttpRequestException("capture complete"));
        using var httpClient = new HttpClient(handler, disposeHandler: true);
        var api = new GetLastReplyers(new HttpCore(httpClient), new TransportParityRecordingWsCore());
        try
        {
            _ = await api.RequestHttpAsync("forum-name", 1, 30, Models.ThreadSortType.Reply, false);
        }
        catch (TiebaTransportException)
        {
        }

        return handler.SingleCapture();
    }

    internal static async Task<CapturedHttpRequest> CaptureSquareForumsRequestAsync()
    {
        var handler = new TransportParityCapturingHttpMessageHandler(_ => throw new HttpRequestException("capture complete"));
        using var httpClient = new HttpClient(handler, disposeHandler: true);
        var httpCore = new HttpCore(httpClient);
        httpCore.SetAccount(new SessionAccount(new string('b', 192), new string('s', 64)));
        var api = new GetSquareForums(httpCore, new TransportParityRecordingWsCore());
        try
        {
            _ = await api.RequestHttpAsync("cinema", 1, 20);
        }
        catch (TiebaTransportException)
        {
        }

        return handler.SingleCapture();
    }

    internal static async Task<string[]> CaptureFallbackPathAsync(string operationName)
    {
        var events = new List<string>();
        using var httpClient = new HttpClient(new TransportParityCapturingHttpMessageHandler(_ => CreateJsonResponse("{}")), disposeHandler: true);
        var wsCore = new TransportParityRecordingWsCore(
            onConnectAsync: _ =>
            {
                events.Add("ws.connect");
                throw new WebSocketException(WebSocketError.NotAWebSocket, "simulated websocket failure");
            });
        var options = new TiebaOptions { Bduss = new string('b', 192), Stoken = new string('s', 64), TransportMode = TiebaTransportMode.Auto };
        using var session = new TiebaClientSession(options, new HttpCore(httpClient), wsCore);
        var dispatcher = new TiebaOperationDispatcher(session);
        var descriptor = new TiebaOperationDescriptor<string>(
            operationName,
            TiebaOperationCapabilities.WebSocketPreferred(),
            ExecuteHttpAsync: (_, _) =>
            {
                events.Add("http.execute");
                return Task.FromResult("ok");
            },
            ExecuteWebSocketAsync: (_, _) => Task.FromResult("ws"));

        _ = await dispatcher.ExecuteAsync(descriptor);
        return events.ToArray();
    }

    internal static Exception CaptureTransportNormalizationException()
    {
        var policy = new TiebaHttpExecutionPolicy(TimeSpan.FromSeconds(30), 0);
        using var client = new HttpClient(new TransportParityCapturingHttpMessageHandler(_ => throw new InvalidOperationException("handler should not be used")));
        return Assert.ThrowsExactlyAsync<TiebaTransportException>(async () =>
            await policy.SendAsync(client, _ => throw new HttpRequestException("network down"), false, TiebaHttpRequestKind.WebGet)).GetAwaiter().GetResult();
    }

    internal static Exception CaptureTimeoutNormalizationException()
    {
        var policy = new TiebaHttpExecutionPolicy(TimeSpan.FromMilliseconds(1), 0);
        using var client = new HttpClient(new TransportParityCapturingHttpMessageHandler(_ => CreateJsonResponse("{}")));
        return Assert.ThrowsExactlyAsync<TiebaTimeoutException>(async () =>
            await policy.SendAsync(client, async cancellationToken =>
            {
                await Task.Delay(50, cancellationToken);
                return new HttpRequestMessage(HttpMethod.Get, "http://example.com");
            }, false, TiebaHttpRequestKind.WebGet)).GetAwaiter().GetResult();
    }

    internal static TransportParityObservation CreateExpectedGetImagesUpstream(Uri imageUri)
    {
        return new TransportParityObservation(
            imageUri.Scheme,
            HttpMethod.Get.Method,
            imageUri.Host,
            imageUri.AbsolutePath,
            [],
            [],
            CanonicalizeHeaders(
            [
                new("Accept-Encoding", "gzip, deflate"),
                new("Cache-Control", "no-cache"),
                new("Connection", "keep-alive"),
                new("Referer", "tieba.baidu.com"),
                new("User-Agent", $"aiotieba/{Const.Version}")
            ]),
            [],
            [],
            [],
            TransportErrorNormalization.None,
            []);
    }

    internal static TransportParityObservation CreateExpectedWebFormUpstream(string path, IEnumerable<KeyValuePair<string, string>> fields, bool includeCookies)
    {
        var fieldList = fields.ToList();
        var formFields = CanonicalizeFields(fieldList, out var masks);
        return new TransportParityObservation(
            "http",
            HttpMethod.Post.Method,
            Const.WebBaseHost,
            path,
            [],
            formFields,
            CanonicalizeHeaders(
            [
                new("Accept-Encoding", "gzip, deflate"),
                new("Cache-Control", "no-cache"),
                new("Connection", "keep-alive"),
                new("Content-Type", "application/x-www-form-urlencoded"),
                new("Subapp-Type", "hybrid"),
                new("User-Agent", $"aiotieba/{Const.Version}")
            ]),
            includeCookies ? [CookiePair("BDUSS", new string('b', 192)), CookiePair("STOKEN", new string('s', 64))] : [],
            formFields.Select(static field => field.DisplayValue).ToArray(),
            [],
            TransportErrorNormalization.None,
            masks);
    }

    internal static TransportParityObservation CreateExpectedAppProtoUpstream(string path, string query)
    {
        var uri = new UriBuilder("http", Const.AppBaseHost, 80, path) { Query = query }.Uri;
        return new TransportParityObservation(
            uri.Scheme,
            HttpMethod.Post.Method,
            uri.Host,
            uri.AbsolutePath,
            CanonicalizeFields(ParseNameValuePairs(query), out _),
            [],
            CanonicalizeHeaders(
            [
                new("Accept-Encoding", "gzip"),
                new("Connection", "keep-alive"),
                new("Host", Const.AppBaseHost),
                new("User-Agent", $"aiotieba/{Const.Version}"),
                new("x_bd_data_type", "protobuf")
            ]),
            [],
            [],
            [],
            TransportErrorNormalization.None,
            []);
    }

    internal static TransportParityObservation CreateExpectedFallbackPath(string[] path)
    {
        return new TransportParityObservation(string.Empty, string.Empty, string.Empty, string.Empty, [], [], [], [], [], path, TransportErrorNormalization.None, []);
    }

    internal static TransportParityObservation CreateExpectedUpstreamStatusError(int code, string reason)
    {
        return new TransportParityObservation(string.Empty, string.Empty, string.Empty, string.Empty, [], [], [], [], [], [], new TransportErrorNormalization("HTTPStatusError", $"({code}, {reason})"), []);
    }

    internal static TransportParityObservation CreateExpectedUpstreamTimeoutError(string target)
    {
        return new TransportParityObservation(string.Empty, string.Empty, string.Empty, string.Empty, [], [], [], [], [], [], new TransportErrorNormalization("aiohttp.ServerTimeoutError", $"Connection timeout to host {target}"), []);
    }

    private static HttpResponseMessage CreateJsonResponse(string json)
    {
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
    }

    private static HttpResponseMessage CreateBinaryResponse(string mediaType, byte[] body)
    {
        var content = new ByteArrayContent(body);
        content.Headers.ContentType = new MediaTypeHeaderValue(mediaType);
        return new HttpResponseMessage(HttpStatusCode.OK) { Content = content };
    }

    private static void CompareScalar(List<TransportParityDiff> diffs, string path, string expected, string actual)
    {
        if (!string.Equals(expected, actual, StringComparison.Ordinal))
            diffs.Add(new TransportParityDiff(path, expected, actual));
    }

    private static void CompareSequence(List<TransportParityDiff> diffs, string path, IEnumerable<string> expected, IEnumerable<string> actual)
    {
        var expectedArray = expected.ToArray();
        var actualArray = actual.ToArray();
        if (!expectedArray.SequenceEqual(actualArray, StringComparer.Ordinal))
            diffs.Add(new TransportParityDiff(path, string.Join(" | ", expectedArray), string.Join(" | ", actualArray)));
    }

    private static TransportParityField[] CanonicalizeFields(IEnumerable<KeyValuePair<string, string>> fields, out string[] maskIds)
    {
        var appliedMasks = new HashSet<string>(StringComparer.Ordinal);
        var result = fields.Select(field =>
        {
            var mask = MaskRules.FirstOrDefault(rule => rule.AppliesTo(field.Key));
            if (mask is null)
                return new TransportParityField(field.Key, field.Value, null);

            appliedMasks.Add(mask.MaskId);
            return new TransportParityField(field.Key, mask.ReplacementValue, mask.MaskId);
        }).ToArray();

        maskIds = appliedMasks.OrderBy(static id => id, StringComparer.Ordinal).ToArray();
        return result;
    }

    private static TransportParityHeader[] CanonicalizeHeaders(IEnumerable<KeyValuePair<string, string>> headers)
    {
        return headers
            .GroupBy(static header => header.Key, StringComparer.OrdinalIgnoreCase)
            .Select(group => new TransportParityHeader(group.Key, group.Select(static item => item.Value).ToArray()))
            .OrderBy(static header => header.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static List<KeyValuePair<string, string>> ParseNameValuePairs(string input)
    {
        var result = new List<KeyValuePair<string, string>>();
        if (string.IsNullOrWhiteSpace(input))
            return result;

        foreach (var segment in input.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var separatorIndex = segment.IndexOf('=');
            if (separatorIndex < 0)
            {
                result.Add(new KeyValuePair<string, string>(Uri.UnescapeDataString(segment), string.Empty));
                continue;
            }

            var key = Uri.UnescapeDataString(segment[..separatorIndex].Replace('+', ' '));
            var value = Uri.UnescapeDataString(segment[(separatorIndex + 1)..].Replace('+', ' '));
            result.Add(new KeyValuePair<string, string>(key, value));
        }

        return result;
    }

    private static string[] ExtractCookies(IEnumerable<KeyValuePair<string, string>> headers)
    {
        var cookieHeader = headers.FirstOrDefault(static pair => string.Equals(pair.Key, "Cookie", StringComparison.OrdinalIgnoreCase)).Value;
        if (string.IsNullOrWhiteSpace(cookieHeader))
            return [];

        return cookieHeader.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    private static string CookiePair(string name, string value) => $"{name}={value}";
}

internal sealed class TransportParityCapturingHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
{
    private readonly List<CapturedHttpRequest> _captures = [];

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var bodyBytes = request.Content is null ? null : await request.Content.ReadAsByteArrayAsync(cancellationToken);
        var bodyText = bodyBytes is null ? null : Encoding.UTF8.GetString(bodyBytes);
        var headers = request.Headers
            .SelectMany(static header => header.Value.Select(value => new KeyValuePair<string, string>(header.Key, value)))
            .Concat(request.Content?.Headers.SelectMany(static header => header.Value.Select(value => new KeyValuePair<string, string>(header.Key, value))) ?? [])
            .ToArray();

        _captures.Add(new CapturedHttpRequest(
            request.Method.Method,
            request.RequestUri ?? throw new InvalidOperationException("Captured request must have a URI."),
            headers,
            request.Content?.Headers.ContentType?.MediaType,
            bodyBytes,
            bodyText));

        return responder(request);
    }

    internal CapturedHttpRequest SingleCapture()
    {
        Assert.AreEqual(1, _captures.Count, "Expected exactly one captured HTTP request for the retained transport parity support.");
        return _captures[0];
    }
}

internal sealed class TransportParityRecordingWsCore(Func<CancellationToken, Task>? onConnectAsync = null) : ITiebaWsCore
{
    public SessionAccount? Account { get; private set; }

    public void SetAccount(SessionAccount newAccount)
    {
        Account = newAccount;
    }

    public Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        return onConnectAsync?.Invoke(cancellationToken) ?? Task.CompletedTask;
    }

    public Task SendAsync(WSReq req, CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("The retained transport parity support does not send raw websocket frames.");
    }

    public Task<WSRes> SendAsync(int cmd, byte[] data, bool encrypt = true, CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("The retained transport parity support does not use websocket request/response sends.");
    }

    public async IAsyncEnumerable<WSRes> ListenAsync([System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;
        yield break;
    }

    public Task CloseAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}

internal sealed record CapturedHttpRequest(
    string Method,
    Uri Uri,
    IReadOnlyList<KeyValuePair<string, string>> Headers,
    string? ContentType,
    byte[]? BodyBytes,
    string? BodyText);

internal sealed record TransportParityComparison(
    bool Match,
    IReadOnlyList<string> AppliedMaskIds,
    IReadOnlyList<TransportParityDiff> Diffs,
    TransportParityObservation Expected,
    TransportParityObservation Actual)
{
    internal string ToFailureMessage(string auditUnit)
    {
        var diffText = Diffs.Count == 0
            ? "<none>"
            : string.Join(Environment.NewLine, Diffs.Select(static diff => $"- {diff.Path}: expected '{diff.Expected}' actual '{diff.Actual}'"));
        return $"Transport parity diff for '{auditUnit}' must stay deterministic.{Environment.NewLine}{diffText}";
    }
}

internal sealed record TransportParityObservation(
    string Scheme,
    string Method,
    string Host,
    string Path,
    IReadOnlyList<TransportParityField> QueryFields,
    IReadOnlyList<TransportParityField> FormFields,
    IReadOnlyList<TransportParityHeader> Headers,
    IReadOnlyList<string> Cookies,
    IReadOnlyList<string> SignatureInputSequence,
    IReadOnlyList<string> FallbackTransportPath,
    TransportErrorNormalization ErrorNormalization,
    IReadOnlyList<string> AppliedMaskIds);

internal sealed record TransportParityField(string Name, string Value, string? MaskId)
{
    public string DisplayValue => $"{Name}={Value}";
}

internal sealed record TransportParityHeader(string Name, IReadOnlyList<string> Values)
{
    public string DisplayValue => $"{Name}: {string.Join(", ", Values)}";
}

internal sealed record TransportParityDiff(string Path, string Expected, string Actual);

internal sealed record TransportErrorNormalization(string ExceptionType, string Message)
{
    internal static TransportErrorNormalization None { get; } = new(string.Empty, string.Empty);
}

internal sealed record ParityMaskRule(string MaskId, string ReplacementValue, IReadOnlyList<string> FieldNames)
{
    internal bool AppliesTo(string fieldName)
    {
        return FieldNames.Contains(fieldName, StringComparer.OrdinalIgnoreCase);
    }
}
