using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;
using System.IO.Compression;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Text;
using AioTieba4DotNet.Models.Forums;
using AioTieba4DotNet.Models.Shared;
using AioTieba4DotNet.Session;
using Google.Protobuf;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AioTieba4DotNet.Transport.Chatrooms;

internal sealed class BlcpChatroomSender
{
    private const string BlcpHost = "common.lcs.baidu.com";
    private const int BlcpPort = 443;
    private const int ChatAppId = 10773430;
    private const int ChatSdkVersion = 11250036;
    private const string ChatVersion = "12.68.1.0";
    private const string LcmSdkVersion = "3460016";
    private const string LoginFrom = "1008550l";
    private const string UserAgent = "okhttp/3.11.0";
    private const string AndroidPlatform = "android";
    private const string LcmTokenHost = "pim.baidu.com";
    private const string Pkcs7Key = "AFD311832EDEEAEF";
    private const string Pkcs7Iv = "2011121211143000";
    private static readonly Encoding Utf8 = new UTF8Encoding(false);

    private static readonly Uri LcmTokenEndpoint =
        new UriBuilder("https", LcmTokenHost, 443, "/rest/5.0/generate_lcm_token").Uri;

    [SuppressMessage("Major Code Smell", "S107:Methods should not have too many parameters",
        Justification =
            "The BLCP send entrypoint preserves the protocol-shaped message contract as discrete arguments so transport packing remains explicit.")]
    public async Task<bool> SendMessageAsync(Account account, UserInfo selfInfo, ForumLevelInfo forumLevel,
        long chatroomId,
        ulong forumId, string text, IReadOnlyList<ChatroomMention>? mentions, int robotCode, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(account);
        ArgumentNullException.ThrowIfNull(selfInfo);
        ArgumentNullException.ThrowIfNull(forumLevel);

        if (string.IsNullOrWhiteSpace(account.SampleId))
            throw TiebaSessionAuthPolicy.CreateMissingSessionStateException(nameof(SendMessageAsync),
                nameof(Account.SampleId));

        using var tcpClient = new TcpClient(AddressFamily.InterNetwork);
        await tcpClient.ConnectAsync(BlcpHost, BlcpPort, cancellationToken);
        using var sslStream = new SslStream(tcpClient.GetStream(), false);
        await sslStream.AuthenticateAsClientAsync(BlcpHost, null,
            SslProtocols.Tls12 | SslProtocols.Tls13,
            true);

        var token = await GenerateLcmTokenAsync(account.CuidGalaxy2, cancellationToken);
        await PerformHandshakeAsync(sslStream, account, token, cancellationToken);
        var loginPayload = await PerformLoginAsync(sslStream, account, selfInfo, cancellationToken);
        return await SendChatroomPayloadAsync(sslStream, account, selfInfo, forumLevel, loginPayload, chatroomId,
            forumId,
            text, mentions, robotCode, cancellationToken);
    }

    private async Task<string> GenerateLcmTokenAsync(string cuidGalaxy2, CancellationToken cancellationToken)
    {
        using var client = new HttpClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, LcmTokenEndpoint);
        request.Headers.TryAddWithoutValidation("Accept-Encoding", "gzip");
        request.Headers.TryAddWithoutValidation("User-Agent", UserAgent);
        request.Headers.TryAddWithoutValidation("Host", LcmTokenHost);

        var ts = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var requestId = ts.ToString();
        var sign = ComputeMd5Hex($"{ChatAppId}{cuidGalaxy2}{AndroidPlatform}{ts}");
        var payload = new
        {
            app_id = ChatAppId.ToString(),
            app_version = ChatVersion,
            cuid = cuidGalaxy2,
            device_type = AndroidPlatform,
            manufacture = string.Empty,
            model_type = string.Empty,
            request_id = requestId,
            sdk_version = LcmSdkVersion,
            sign,
            ts,
            user_key = string.Empty
        };

        request.Content = new StringContent(JsonConvert.SerializeObject(payload), Utf8, "application/json");
        using var response = await client.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        var json = JObject.Parse(body);
        return json["token"]?.Value<string>() ?? string.Empty;
    }

    private async Task PerformHandshakeAsync(SslStream stream, Account account, string token,
        CancellationToken cancellationToken)
    {
        var correlationId = CreateCorrelationId();
        var rpcBody = BuildRpcBody(1, 1, correlationId, 1);
        var rpcData = new RpcData
        {
            LcmRequest = new LcmRequest
            {
                LogId = correlationId,
                Token = token,
                Common = new Common
                {
                    Cuid = account.CuidGalaxy2,
                    Device = AndroidPlatform,
                    AppId = ChatAppId.ToString(),
                    AppVersion = ChatVersion,
                    SdkVersion = LcmSdkVersion,
                    Network = "wifi"
                },
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                StartType = -1,
                ConnType = 1
            }
        };

        await WriteFrameAsync(stream, rpcBody, rpcData.ToByteArray(), cancellationToken);
        var (_, lcmBody) = await ReadFrameAsync(stream, cancellationToken);
        var rpcResponse = RpcData.Parser.ParseFrom(lcmBody);
        var lcmResponse = rpcResponse.LcmResponse;
        if (!string.Equals(lcmResponse.ErrorMsg, "success", StringComparison.OrdinalIgnoreCase) ||
            lcmResponse.ErrorCode != 0)
            throw new TiebaProtocolException("BLCP handshake failed.");
    }

    private async Task<LoginPayload> PerformLoginAsync(SslStream stream, Account account, UserInfo selfInfo,
        CancellationToken cancellationToken)
    {
        var firstLogin = new
        {
            @params = new
            {
                appname = "tieba",
                sid = account.SampleId,
                ua = "900_1600_android_12.68.1.0_240",
                uid = EnuidCodec.Encode(account.CuidGalaxy2),
                cfrom = LoginFrom,
                from = LoginFrom,
                network = "1_-1",
                p_sv = "32",
                mps = string.Empty,
                mpv = "1",
                c3_aid = account.C3Aid ?? string.Empty,
                type_id = "0"
            },
            filter = new { aps = new { cpu_abi = "armeabi-v7a" }, command = new { step = "0" } }
        };

        await SendJsonRpcAsync(stream, 4, 1, firstLogin, cancellationToken, "errno");

        var clientIdentifier = JsonConvert.SerializeObject(new { zid = string.Empty, version_code = string.Empty });
        var secondLogin = new JObject
        {
            ["method"] = 50,
            ["appid"] = ChatAppId,
            ["device_id"] = $"android_{account.CuidGalaxy2}",
            ["account_type"] = 1,
            ["token"] = account.Bduss,
            ["version"] = 4,
            ["sdk_version"] = ChatSdkVersion,
            ["app_version"] = ChatVersion,
            ["app_open_type"] = 0,
            ["client_identifier"] = clientIdentifier,
            ["tail"] = 0,
            ["timeout"] = 10,
            ["cookie"] = string.Empty,
            ["device_info"] = JObject.FromObject(new
            {
                app_version = ChatVersion,
                os_version = "32",
                platform = "android",
                appid = ChatAppId.ToString(),
                from = LoginFrom,
                cfrom = LoginFrom
            }),
            ["rpc"] = "{\"rpc_retry_time\":0}",
            ["user_type"] = 0,
            ["client_logid"] = CreateCorrelationId()
        };

        var secondResponse = await SendJsonRpcAsync(stream, 2, 50, secondLogin, cancellationToken,
            "err_code");

        var triggerToken = secondResponse["trigger_id"] as JArray;
        return new LoginPayload(
            triggerToken?.First?.Value<long>() ?? 0,
            secondResponse["uk"]?.Value<long>() ?? selfInfo.Uk);
    }

    [SuppressMessage("Major Code Smell", "S107:Methods should not have too many parameters",
        Justification =
            "The BLCP payload builder keeps required protocol fields explicit so chatroom send packing remains auditable against the upstream message shape.")]
    private async Task<bool> SendChatroomPayloadAsync(SslStream stream, Account account, UserInfo selfInfo,
        ForumLevelInfo forumLevel, LoginPayload loginPayload, long chatroomId, ulong forumId, string text,
        IReadOnlyList<ChatroomMention>? mentions, int robotCode, CancellationToken cancellationToken)
    {
        var requestData = BuildChatroomRequestData(account, selfInfo, forumLevel, loginPayload, chatroomId, forumId,
            text, mentions, robotCode);
        var response = await SendJsonRpcAsync(stream, 3, 185, requestData, cancellationToken, "err_code");
        return response["err_code"]?.Value<int>() == 0;
    }

    private static JObject BuildChatroomRequestData(Account account, UserInfo selfInfo, ForumLevelInfo forumLevel,
        LoginPayload loginPayload, long chatroomId, ulong forumId, string text, IReadOnlyList<ChatroomMention>? mentions,
        int robotCode)
    {
        var portraitWithTimestamp = $"{selfInfo.Portrait}?t={DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";
        var name = string.IsNullOrWhiteSpace(selfInfo.NickName) ? selfInfo.ShowName : selfInfo.NickName;
        var bduk = GetBdukFromUserId(selfInfo.UserId.ToString());

        var contentExt = new JObject
        {
            ["main_data"] = BuildMainData(forumLevel.UserLevel, selfInfo.IsVip, selfInfo.GLevel, forumId,
                selfInfo.Portrait,
                name),
            ["is_sys_msg"] = 0,
            ["version"] = string.Empty,
            ["portrait"] = portraitWithTimestamp,
            ["robot_role"] = 0,
            ["role"] = 0,
            ["send_status"] = 0,
            ["from"] = AndroidPlatform,
            ["session_id"] = chatroomId,
            ["type"] = 1,
            ["user_name"] = name,
            ["level"] = forumLevel.UserLevel,
            ["forum_id"] = forumId
        };

        contentExt["content"] = robotCode == -1
            ? JObject.FromObject(new { })
            : JObject.FromObject(new { robot_params = new { scene = "tieba_group_chat", type = robotCode } });

        var textPayload = new JObject
        {
            ["room_id"] = chatroomId.ToString(),
            ["type"] = "0",
            ["to_uid"] = "0",
            ["vip"] = selfInfo.IsVip ? "1" : "0",
            ["name"] = name,
            ["portrait"] = portraitWithTimestamp,
            ["content_type"] = "0",
            ["content_body"] = JsonConvert.SerializeObject(new { text }, Formatting.None),
            ["src"] = string.Empty,
            ["baidu_uk"] = bduk,
            ["ext"] = JsonConvert.SerializeObject(contentExt, Formatting.None)
        };

        if (mentions is { Count: > 0 })
            textPayload["at_data"] = BuildAtData(mentions);

        var content = new JObject { ["text"] = JsonConvert.SerializeObject(textPayload, Formatting.None) };

        return new JObject
        {
            ["method"] = 185,
            ["mcast_id"] = chatroomId,
            ["role"] = 3,
            ["token"] = account.Bduss,
            ["appid"] = ChatAppId,
            ["uk"] = loginPayload.Uk == 0 ? selfInfo.Uk : loginPayload.Uk,
            ["origin_id"] = loginPayload.TriggerId,
            ["type"] = 81,
            ["app_safe_ext"] =
                JsonConvert.SerializeObject(new { haotianjing = new { zid = account.ZId ?? string.Empty } },
                    Formatting.None),
            ["content"] = JsonConvert.SerializeObject(content, Formatting.None),
            ["msg_key"] = GetMsgKey(bduk),
            ["account_type"] = 1,
            ["sdk_version"] = ChatSdkVersion,
            ["event_list"] = JArray.FromObject(new object[]
            {
                new { @event = "CClickSendBegin", timestamp_ms = 0 },
                new { @event = "CSendBegin", timestamp_ms = 0 },
                new { @event = "CIMSendBegin", timestamp_ms = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() }
            })
        };
    }

    [SuppressMessage("Minor Code Smell",
        "S2325:Methods and properties that don't access instance data should be static",
        Justification =
            "This helper remains an instance method so existing transport tests can exercise it through the same reflection-based seam as the rest of the sender implementation.")]
    private async Task<JObject> SendJsonRpcAsync(SslStream stream, long serviceId, long methodId, object payload,
        CancellationToken cancellationToken, string expectLcmErrorField)
    {
        var correlationId = CreateCorrelationId();
        var rpcBody = BuildRpcBody(serviceId, methodId, correlationId, 1);
        var json = payload is JObject jsonObject ? jsonObject : JObject.FromObject(payload);
        json["client_logid"] ??= correlationId;
        json["rpc"] ??= "{\"rpc_retry_time\":0}";

        await WriteFrameAsync(stream, rpcBody, Utf8.GetBytes(json.ToString(Formatting.None)), cancellationToken);
        var (rpcMeta, lcmBody) = await ReadFrameAsync(stream, cancellationToken);
        if (!string.Equals(rpcMeta.Response?.ErrorText, "success", StringComparison.OrdinalIgnoreCase) ||
            rpcMeta.Response?.ErrorCode > 0)
            throw new TiebaProtocolException($"BLCP RPC {serviceId}/{methodId} failed: {rpcMeta.Response?.ErrorText}");

        var response = JObject.Parse(Utf8.GetString(lcmBody));
        var errorToken = response[expectLcmErrorField];
        if (errorToken is not null && errorToken.Type != JTokenType.Null && errorToken.Value<int>() != 0)
            throw new TiebaProtocolException(
                $"BLCP payload {serviceId}/{methodId} failed with {expectLcmErrorField}={errorToken.Value<int>()}.");

        return response;
    }

    private static async Task WriteFrameAsync(SslStream stream, byte[] rpcBody, byte[] lcmBody,
        CancellationToken cancellationToken)
    {
        var header = new byte[12];
        header[0] = (byte)'l';
        header[1] = (byte)'c';
        header[2] = (byte)'p';
        header[3] = 1;
        BinaryPrimitives.WriteInt32BigEndian(header.AsSpan(4, 4), rpcBody.Length + lcmBody.Length);
        BinaryPrimitives.WriteInt32BigEndian(header.AsSpan(8, 4), rpcBody.Length);
        await stream.WriteAsync(header, cancellationToken);
        await stream.WriteAsync(rpcBody, cancellationToken);
        await stream.WriteAsync(lcmBody, cancellationToken);
        await stream.FlushAsync(cancellationToken);
    }

    private static async Task<(RpcMeta RpcMeta, byte[] LcmBody)> ReadFrameAsync(SslStream stream,
        CancellationToken cancellationToken)
    {
        var prefix = new byte[4];
        await ReadExactAsync(stream, prefix, cancellationToken);
        if (prefix[0] != (byte)'l' || prefix[1] != (byte)'c' || prefix[2] != (byte)'p' || prefix[3] != 1)
            throw new TiebaProtocolException("Invalid BLCP response prefix.");

        var lengths = new byte[8];
        await ReadExactAsync(stream, lengths, cancellationToken);
        var totalLength = BinaryPrimitives.ReadInt32BigEndian(lengths.AsSpan(0, 4));
        var rpcLength = BinaryPrimitives.ReadInt32BigEndian(lengths.AsSpan(4, 4));
        if (totalLength < rpcLength || rpcLength <= 0)
            throw new TiebaProtocolException("Invalid BLCP response lengths.");

        var payload = new byte[totalLength];
        await ReadExactAsync(stream, payload, cancellationToken);
        var rpcBody = payload.AsSpan(0, rpcLength).ToArray();
        var lcmBody = payload.AsSpan(rpcLength).ToArray();
        var rpcMeta = RpcMeta.Parser.ParseFrom(rpcBody);

        if (rpcMeta.CompressType == 1)
            lcmBody = DecompressGzip(lcmBody);

        return (rpcMeta, lcmBody);
    }

    private static async Task ReadExactAsync(Stream stream, byte[] buffer, CancellationToken cancellationToken)
    {
        var read = 0;
        while (read < buffer.Length)
        {
            var length = await stream.ReadAsync(buffer.AsMemory(read, buffer.Length - read), cancellationToken);
            if (length == 0)
                throw new EndOfStreamException("BLCP stream closed unexpectedly.");
            read += length;
        }
    }

    private static byte[] BuildRpcBody(long serviceId, long methodId, long correlationId, int needCommon)
    {
        var requestMeta = new RpcRequestMeta
        {
            ServiceId = serviceId, MethodId = methodId, LogId = correlationId, NeedCommon = needCommon
        };
        requestMeta.EventList.Add(new EventTimestamp
        {
            Event = "CLCPReqBegin", TimestampMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        });

        var rpcMeta = new RpcMeta
        {
            Request = requestMeta, CorrelationId = correlationId, CompressType = 0, AcceptCompressType = 1
        };

        return rpcMeta.ToByteArray();
    }

    private static JArray BuildAtData(IReadOnlyList<ChatroomMention> mentions)
    {
        var array = new JArray();
        foreach (var mention in mentions)
            array.Add(JObject.FromObject(new
            {
                at_type = "user",
                at_baidu_uk = GetBdukFromUserId(mention.UserId.ToString()),
                at_name = mention.Name,
                at_portrait = mention.Portrait,
                position = mention.Position.ToString()
            }));

        return array;
    }

    private static JArray BuildMainData(int level, bool vip, int gLevel, ulong forumId, string portrait, string name)
    {
        var mainData = new JArray();
        if (vip)
            mainData.Add(JObject.FromObject(new
            {
                icon = new
                {
                    height = 75,
                    priority = 2,
                    schema =
                        "https://tieba.baidu.com/mo/q/hybrid-business-vip/tbvip?customfullscreen=1&nonavigationbar=1",
                    type = "1",
                    url = "https://tieba-ares.cdn.bcebos.com/mis/2023-7/1689061482682/13afea50121d.png",
                    width = 75
                },
                type = 2
            }));

        var nameData = new JObject
        {
            ["text"] = new JObject
            {
                ["short_enable"] = 1,
                ["short_length"] = 5,
                ["short_priority"] = 1,
                ["priority"] = 1,
                ["str"] = name,
                ["suffix"] = "...",
                ["type"] = "1"
            },
            ["type"] = 1
        };

        if (vip)
        {
            var textToken = (JObject)nameData["text"]!;
            textToken["text_color"] = JObject.FromObject(new { day = "CAM_X0301", night = "CAM_X0301", type = 2 });
        }

        mainData.Add(nameData);
        mainData.Add(JObject.FromObject(new
        {
            icon = new
            {
                height = 15,
                priority = 5,
                schema =
                    "https://tieba.baidu.com/mo/q/hybrid-main-user/taskCenter?customfullscreen=1&nonavigationbar=1",
                type = "3",
                url = $"local://icon/icon_mask_level_usergrouth_{gLevel}?type=webp",
                width = 24
            },
            type = 2
        }));
        mainData.Add(JObject.FromObject(new
        {
            icon = new
            {
                height = 12,
                priority = 2,
                schema =
                    $"https://tieba.baidu.com/mo/q/wise-bawu-core/forum-level?customfullscreen=1&forum_id={forumId}&nonavigationbar=1&obj_locate=5&portrait={portrait}?t={DateTimeOffset.UtcNow.ToUnixTimeSeconds()}",
                type = "4",
                url = $"local://icon/icon_level_{level:00}?type=webp",
                width = 16
            },
            type = 2
        }));

        return mainData;
    }

    private static string GetBdukFromUserId(string userId)
    {
        using var aes = Aes.Create();
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;
        aes.Key = Encoding.ASCII.GetBytes(Pkcs7Key);
        aes.IV = Encoding.ASCII.GetBytes(Pkcs7Iv);
        using var encryptor = aes.CreateEncryptor();
        var encrypted = encryptor.TransformFinalBlock(Encoding.UTF8.GetBytes(userId), 0, userId.Length);
        return Convert.ToBase64String(encrypted).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    private static string GetMsgKey(string bduk)
    {
        Span<byte> randomBytes = stackalloc byte[8];
        RandomNumberGenerator.Fill(randomBytes);
        var random = BitConverter.ToInt64(randomBytes);
        return $"{bduk}{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() * 1000}{random}";
    }

    private static long CreateCorrelationId()
    {
        return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() * 1000;
    }

    [SuppressMessage("Security Hotspot", "S4790:Using weak hashing algorithms is security-sensitive",
        Justification =
            "The BLCP token bootstrap endpoint requires an MD5 request signature for protocol compatibility with the upstream mobile client.")]
    private static string ComputeMd5Hex(string text)
    {
        var bytes = MD5.HashData(Utf8.GetBytes(text));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static byte[] DecompressGzip(byte[] payload)
    {
        using var source = new MemoryStream(payload);
        using var gzip = new GZipStream(source, CompressionMode.Decompress);
        using var target = new MemoryStream();
        gzip.CopyTo(target);
        return target.ToArray();
    }

    private sealed record LoginPayload(long TriggerId, long Uk);
}

internal sealed record ChatroomMention(long UserId, string Name, string Portrait, int Position);

internal static class EnuidCodec
{
    private static readonly Encoding Utf8 = new UTF8Encoding(false);

    private static readonly byte[] Alphabet =
        Utf8.GetBytes("qogjOuCRNkfil5p4SQ3LAmxGKZTdesvB6z_YPahMI9t80rJyHW1DEwFbc7nUVX2-");

    internal static string Encode(string cuidGalaxy2)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cuidGalaxy2);
        var source = Utf8.GetBytes(cuidGalaxy2);
        var working = new byte[(source.Length + 2) / 3 * 4 + 2];
        Buffer.BlockCopy(source, 0, working, 0, source.Length);
        var encodedLength = EncodeInPlace(working, source.Length, 0);
        return Utf8.GetString(working, 0, encodedLength - 1);
    }

    private static int EncodeInPlace(byte[] buffer, int inputLength, int mode)
    {
        if (inputLength == 0) return -1;

        var remainder = inputLength % 3;
        var blockCount = inputLength / 3;
        var prefixLength = 4 * blockCount;
        var table = BuildTable(mode, false);
        XorWords(table.State, buffer, inputLength);

        var temp = new byte[4];
        if (remainder > 0)
        {
            Buffer.BlockCopy(buffer, 3 * blockCount, temp, 0, remainder);
            EncodeTriplet(table.Map, temp, buffer, 3 * blockCount + blockCount);
        }

        var outputLength = remainder > 0 ? prefixLength + 4 : prefixLength;
        for (var i = 0; i < blockCount; i++)
        {
            var sourceIndex = 3 * (blockCount - 1 - i);
            var destinationIndex = prefixLength - 4 * (i + 1);
            EncodeTriplet(table.Map, buffer.AsSpan(sourceIndex, 3), buffer, destinationIndex);
        }

        buffer[outputLength] = (byte)(remainder + 65);
        buffer[outputLength + 1] = 0;
        return outputLength + 2;
    }

    private static (int[] State, byte[] Map) BuildTable(int mode, bool fillLookup)
    {
        var state = new int[49];
        var map = new byte[196];
        var offset = mode & 0x3F;
        var tailLength = 64 - offset;
        Buffer.BlockCopy(Alphabet, offset, map, 132, tailLength);
        Buffer.BlockCopy(Alphabet, 0, map, 132 + tailLength, offset);
        var rotated = (int)(((uint)mode << 27) | ((uint)mode >> 5));
        state[0] = unchecked(rotated ^ (758653732 << (rotated & 0xF)));

        if (fillLookup)
        {
            for (var i = 0; i < 128; i++)
                map[i + 4] = 64;

            for (var i = 0; i < 64; i++)
                map[map[i + 132] + 4] = (byte)i;
        }

        return (state, map);
    }

    private static void XorWords(int[] state, byte[] buffer, int length)
    {
        var seed = state[0];
        var wholeWords = length >> 2;
        for (var i = 0; i < wholeWords; i++)
        {
            var word = BitConverter.ToInt32(buffer, i * 4);
            word = unchecked(word ^ (int)(((uint)word >> 3) | ((uint)word << 29)));
            var bytes = BitConverter.GetBytes(word);
            Buffer.BlockCopy(bytes, 0, buffer, i * 4, 4);
        }

        if (wholeWords < (length + 3) >> 2)
        {
            var start = wholeWords * 4;
            var seedBytes = BitConverter.GetBytes(seed);
            for (var i = start; i < length; i++)
                buffer[i] ^= seedBytes[i - start];
        }
    }

    private static void EncodeTriplet(byte[] map, ReadOnlySpan<byte> input, byte[] destination, int destinationIndex)
    {
        var b0 = input[0];
        var b1 = input[1];
        var b2 = input[2];
        destination[destinationIndex] = map[132 + (b0 & 0x3F)];
        destination[destinationIndex + 1] = map[132 + (b1 & 0x3F)];
        destination[destinationIndex + 2] = map[132 + (b2 & 0x3F)];
        destination[destinationIndex + 3] = map[132 + (((b1 >> 6) * 4) | ((b0 >> 6) * 16) | (b2 >> 6))];
    }
}
