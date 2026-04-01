#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AioTieba4DotNet.Api.SignGrowth;
using AioTieba4DotNet.Api.SignForums;
using AioTieba4DotNet.Internal;
using AioTieba4DotNet.Session;
using AioTieba4DotNet.Transport;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SignApi = AioTieba4DotNet.Api.Sign.Sign;

namespace AioTieba4DotNet.Tests.Api.Sign;

[TestClass]
public sealed class SignFamilyApiTests
{
    [TestMethod]
    public async Task Sign_RequestAsync_PacksForumSignAppForm()
    {
        var httpCore = new RecordingHttpCore();
        var api = new SignApi(httpCore);

        var success = await api.RequestAsync("lol欧服", 7356044);

        Assert.IsTrue(success);
        Assert.AreEqual("/c/c/forum/sign", httpCore.LastAppFormUri?.AbsolutePath);
        Assert.AreEqual("7356044", httpCore.GetAppFormValue("fid"));
        Assert.AreEqual("lol欧服", httpCore.GetAppFormValue("kw"));
        Assert.AreEqual("tbs", httpCore.GetAppFormValue("tbs"));
    }

    [TestMethod]
    public async Task SignForums_RequestAsync_PacksHybridWebForm()
    {
        var httpCore = new RecordingHttpCore
        {
            CustomResponse = "{" +
                             "\"error_code\":0," +
                             "\"error_msg\":\"\"," +
                             "\"error\":{\"errno\":0,\"errmsg\":\"\"}" +
                             "}"
        };
        var api = new SignForums(httpCore);

        var success = await api.RequestAsync();

        Assert.IsTrue(success);
        Assert.AreEqual("/c/c/forum/msign", httpCore.LastCustomRequest?.RequestUri?.AbsolutePath);
        Assert.AreEqual("hybrid", httpCore.LastCustomRequest!.Headers.GetValues("Subapp-Type").Single());
        Assert.AreEqual(Const.MainVersion, httpCore.GetCustomFormValue("_client_version"));
        Assert.AreEqual("hybrid", httpCore.GetCustomFormValue("subapp_type"));
    }

    [TestMethod]
    public async Task SignForums_RequestAsync_ThrowsWhenNestedServerErrorIsPresent()
    {
        var httpCore = new RecordingHttpCore
        {
            CustomResponse = "{" +
                             "\"error_code\":0," +
                             "\"error_msg\":\"\"," +
                             "\"error\":{\"errno\":3250002,\"errmsg\":\"denied\"}" +
                             "}"
        };
        var api = new SignForums(httpCore);

        var exception = await Assert.ThrowsAsync<TieBaServerException>(() => api.RequestAsync());

        Assert.Contains("denied", exception.Message);
    }

    [TestMethod]
    public async Task SignForums_RequestAsync_UsesEmptyMessageWhenNestedErrmsgIsMissing()
    {
        var httpCore = new RecordingHttpCore
        {
            CustomResponse = "{" +
                             "\"error_code\":0," +
                             "\"error_msg\":\"\"," +
                             "\"error\":{\"errno\":3250002}" +
                             "}"
        };
        var api = new SignForums(httpCore);

        var exception = await Assert.ThrowsAsync<TieBaServerException>(() => api.RequestAsync());

        Assert.AreEqual(3250002, exception.Code);
    }

    [TestMethod]
    public async Task SignForums_RequestAsync_AllowsMissingNestedErrorObject()
    {
        var httpCore = new RecordingHttpCore
        {
            CustomResponse = "{" +
                             "\"error_code\":0," +
                             "\"error_msg\":\"\"" +
                             "}"
        };
        var api = new SignForums(httpCore);

        var success = await api.RequestAsync();

        Assert.IsTrue(success);
        Assert.AreEqual("/c/c/forum/msign", httpCore.LastCustomRequest?.RequestUri?.AbsolutePath);
    }

    [TestMethod]
    public async Task SignGrowth_RequestAsync_PacksPageSignWebForm()
    {
        var httpCore = new RecordingHttpCore { WebFormResponse = "{\"no\":0,\"error\":\"\"}" };
        var api = new SignGrowth(httpCore);

        var success = await api.RequestAsync("page_sign");

        Assert.IsTrue(success);
        Assert.AreEqual("/mo/q/usergrowth/commitUGTaskInfo", httpCore.LastWebFormUri?.AbsolutePath);
        Assert.AreEqual("tbs", httpCore.GetWebFormValue("tbs"));
        Assert.AreEqual("page_sign", httpCore.GetWebFormValue("act_type"));
        Assert.AreEqual("-", httpCore.GetWebFormValue("cuid"));
    }

    [TestMethod]
    public async Task SignGrowth_RequestAsync_ThrowsWhenTaskCommitFails()
    {
        var httpCore = new RecordingHttpCore { WebFormResponse = "{\"no\":1,\"error\":\"growth denied\"}" };
        var api = new SignGrowth(httpCore);

        var exception = await Assert.ThrowsAsync<TieBaServerException>(() => api.RequestAsync("page_sign"));

        Assert.Contains("growth denied", exception.Message);
    }

    private sealed class RecordingHttpCore : ITiebaHttpCore
    {
        public string AppFormResponse { get; init; } = "{\"error_code\":0,\"error_msg\":\"\"}";
        public string WebFormResponse { get; init; } = "{\"no\":0,\"error\":\"\"}";
        public string CustomResponse { get; init; } = "{}";

        public Account? Account { get; private set; } = new(new string('a', 192), new string('b', 64)) { Tbs = "tbs" };

        public HttpClient HttpClient { get; } = new();

        public Uri? LastAppFormUri { get; private set; }
        public List<KeyValuePair<string, string>> LastAppFormData { get; private set; } = [];
        public Uri? LastWebFormUri { get; private set; }
        public List<KeyValuePair<string, string>> LastWebFormData { get; private set; } = [];
        public HttpRequestMessage? LastCustomRequest { get; private set; }
        public List<KeyValuePair<string, string>> LastCustomFormData { get; private set; } = [];

        public void SetAccount(Account newAccount)
        {
            Account = newAccount;
        }

        public async Task<string> SendAsync(Func<HttpRequestMessage> requestFactory, bool allowRetry = false,
            CancellationToken cancellationToken = default)
        {
            LastCustomRequest = requestFactory();
            LastCustomFormData = await ReadFormDataAsync(LastCustomRequest, cancellationToken);
            return CustomResponse;
        }

        public Task<string> SendAppFormAsync(Uri uri, List<KeyValuePair<string, string>> data,
            CancellationToken cancellationToken = default)
        {
            LastAppFormUri = uri;
            LastAppFormData = [.. data];
            return Task.FromResult(AppFormResponse);
        }

        public Task<byte[]> SendAppProtoAsync(Uri uri, byte[] data, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<string> SendWebGetAsync(Uri uri, List<KeyValuePair<string, string>> parameters,
            CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<string> SendWebFormAsync(Uri uri, List<KeyValuePair<string, string>> data,
            CancellationToken cancellationToken = default)
        {
            LastWebFormUri = uri;
            LastWebFormData = [.. data];
            return Task.FromResult(WebFormResponse);
        }

        public string GetAppFormValue(string key)
        {
            return LastAppFormData.Last(entry => entry.Key == key).Value;
        }

        public string GetWebFormValue(string key)
        {
            return LastWebFormData.Last(entry => entry.Key == key).Value;
        }

        public string GetCustomFormValue(string key)
        {
            return LastCustomFormData.Last(entry => entry.Key == key).Value;
        }

        private static async Task<List<KeyValuePair<string, string>>> ReadFormDataAsync(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            if (request.Content is null)
                return [];

            var payload = await request.Content.ReadAsStringAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(payload))
                return [];

            return payload.Split('&', StringSplitOptions.RemoveEmptyEntries)
                .Select(static part => part.Split('=', 2))
                .Select(static parts => new KeyValuePair<string, string>(
                    Uri.UnescapeDataString(parts[0].Replace('+', ' ')),
                    parts.Length > 1 ? Uri.UnescapeDataString(parts[1].Replace('+', ' ')) : string.Empty))
                .ToList();
        }
    }
}
