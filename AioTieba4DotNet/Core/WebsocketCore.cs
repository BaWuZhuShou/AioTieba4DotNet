using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.IO.Compression;
using System.Net.WebSockets;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Channels;
using AioTieba4DotNet.Abstractions;
using Google.Protobuf;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AioTieba4DotNet.Core;

/// <summary>
/// WebSocket 核心实现类，负责维护与贴吧的实时双向长连接
/// </summary>
public class WebsocketCore : ITiebaWsCore, IDisposable
{
    private readonly ClientWebSocket _ws = new();

    /// <summary>
    /// 当前绑定的账户信息
    /// </summary>
    public Account? Account { get; private set; }

    private const string WsEndpoint = "ws://im.tieba.baidu.com:8000";

    /// <summary>
    /// 用于加密通信密钥的 RSA 公钥
    /// </summary>
    private const string RsaPublicKey =
        "MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEAwQpwBZxXJV/JVRF/uNfyMSdu7YWwRNLM8+2xbniGp2iIQHOikPpTYQjlQgMi1uvq1kZpJ32rHo3hkwjy2l0lFwr3u4Hk2Wk7vnsqYQjAlYlK0TCzjpmiI+OiPOUNVtbWHQiLiVqFtzvpvi4AU7C1iKGvc/4IS45WjHxeScHhnZZ7njS4S1UgNP/GflRIbzgbBhyZ9kEW5/OO5YfG1fy6r4KSlDJw4o/mw5XhftyIpL+5ZBVBC6E1EIiP/dd9AbK62VV1PByfPMHMixpxI3GM2qwcmFsXcCcgvUXJBa9k6zP8dDQ3csCM2QNT+CQAOxthjtp/TFWaD7MzOdsIYb3THwIDAQAB";

    private readonly SemaphoreSlim _connectLock = new(1, 1);
    private readonly SemaphoreSlim _sendLock = new(1, 1);
    private readonly CancellationTokenSource _cts = new();
    private Task? _heartbeatTask;
    private Task? _listenTask;
    private bool _disposed;

    private int _reqIdCounter = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    private readonly ConcurrentDictionary<int, TaskCompletionSource<WSRes>> _pendingRequests = new();
    private readonly Channel<WSRes> _messageChannel = Channel.CreateUnbounded<WSRes>();

    /// <summary>
    /// 初始化 WebsocketCore
    /// </summary>
    /// <param name="account">可选的账户信息</param>
    public WebsocketCore(Account? account = null)
    {
        Account = account;
    }

    /// <summary>
    /// 绑定账户信息
    /// </summary>
    public void SetAccount(Account newAccount)
    {
        Account = newAccount;
    }

    /// <summary>
    /// 建立 WebSocket 连接并执行初始化握手
    /// </summary>
    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        if (_ws.State == WebSocketState.Open) return;

        await _connectLock.WaitAsync(cancellationToken);
        try
        {
            if (_ws.State == WebSocketState.Open) return;

            _ws.Options.AddSubProtocol("chat");
            _ws.Options.SetRequestHeader("Sec-WebSocket-Extensions", "im_version=2.3");
            _ws.Options.SetRequestHeader("Accept-Encoding", "gzip");

            await _ws.ConnectAsync(new Uri(WsEndpoint), cancellationToken);

            // 启动接收循环
            _listenTask = Task.Run(() => ListenLoopAsync(_cts.Token), _cts.Token);

            // 如果有账号，发送 1001 握手包初始化身份
            if (Account != null)
            {
                var initData = PackUpdateClientInfo(Account);
                await SendAsync(1001, initData, false, cancellationToken);
            }

            // 启动心跳定时器
            _heartbeatTask = Task.Run(() => RunHeartbeatAsync(_cts.Token), _cts.Token);
        }
        finally
        {
            _connectLock.Release();
        }
    }

    /// <summary>
    /// 接收数据循环
    /// </summary>
    private async Task ListenLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
            try
            {
                var res = await ReceiveAsync(cancellationToken);
                if (res == null) break;

                // 根据 ReqId 将响应分发给等待的 Task，或者是推送 Channel
                if (res.ReqId != 0 && _pendingRequests.TryRemove(res.ReqId, out var tcs))
                    tcs.SetResult(res);
                else
                    await _messageChannel.Writer.WriteAsync(res, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch
            {
                // 异常暂不处理，等待重连机制（待完善）
            }
    }

    /// <summary>
    /// 心跳循环，维持连接活性
    /// </summary>
    private async Task RunHeartbeatAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
            try
            {
                await Task.Delay(30000, cancellationToken);
                await SendAsync(0, [], true, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch
            {
                // 忽略心跳异常
            }
    }

    /// <summary>
    /// 发送原始 WSReq 对象
    /// </summary>
    public async Task SendAsync(WSReq req, CancellationToken cancellationToken = default)
    {
        await ConnectAsync(cancellationToken);
        var data = req.Payload?.Data?.ToByteArray() ?? [];
        var buffer = PackWsBytes(data, req.Cmd, req.ReqId);
        await _sendLock.WaitAsync(cancellationToken);
        try
        {
            await _ws.SendAsync(buffer, WebSocketMessageType.Binary, true, cancellationToken);
        }
        finally
        {
            _sendLock.Release();
        }
    }

    /// <summary>
    /// 发送业务请求并异步等待对应的响应
    /// </summary>
    /// <param name="cmd">指令号</param>
    /// <param name="data">业务数据负载</param>
    /// <param name="encrypt">是否对负载加密</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>服务器返回的响应包</returns>
    public async Task<WSRes> SendAsync(int cmd, byte[] data, bool encrypt = true,
        CancellationToken cancellationToken = default)
    {
        if (cmd != 1001) await ConnectAsync(cancellationToken);

        var reqId = Interlocked.Increment(ref _reqIdCounter);
        var tcs = new TaskCompletionSource<WSRes>(TaskCreationOptions.RunContinuationsAsynchronously);
        _pendingRequests[reqId] = tcs;

        try
        {
            var buffer = PackWsBytes(data, cmd, reqId, encrypt);
            await _sendLock.WaitAsync(cancellationToken);
            try
            {
                await _ws.SendAsync(buffer, WebSocketMessageType.Binary, true, cancellationToken);
            }
            finally
            {
                _sendLock.Release();
            }

            await using var registration = cancellationToken.Register(() => tcs.TrySetCanceled());
            return await tcs.Task;
        }
        catch
        {
            _pendingRequests.TryRemove(reqId, out _);
            throw;
        }
    }

    /// <summary>
    /// 从底层 WebSocket 接收并拼接一个完整的消息包
    /// </summary>
    private async Task<WSRes?> ReceiveAsync(CancellationToken cancellationToken = default)
    {
        using var ms = new MemoryStream();
        WebSocketReceiveResult result;
        do
        {
            var buffer = new byte[4096];
            result = await _ws.ReceiveAsync(buffer, cancellationToken);
            if (result.MessageType == WebSocketMessageType.Close)
            {
                await _ws.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, cancellationToken);
                return null;
            }

            ms.Write(buffer, 0, result.Count);
        } while (!result.EndOfMessage);

        var rawData = ms.ToArray();
        var (payload, cmd, reqId) = ParseWsBytes(rawData);

        return new WSRes
        {
            Cmd = cmd, ReqId = reqId, Payload = new WSRes.Types.Payload { Data = ByteString.CopyFrom(payload) }
        };
    }

    /// <summary>
    /// 打包原始字节流为贴吧 WebSocket 协议格式
    /// </summary>
    internal byte[] PackWsBytes(byte[] data, int cmd, int reqId, bool encrypt = true)
    {
        byte flag = 0x08;
        var payload = data;
        if (encrypt && Account != null)
        {
            flag |= 0x80;
            payload = Account.AesEcbCipher.EncryptEcb(data, PaddingMode.PKCS7);
        }

        var result = new byte[9 + payload.Length];
        result[0] = flag;
        BinaryPrimitives.WriteInt32BigEndian(result.AsSpan(1, 4), cmd);
        BinaryPrimitives.WriteInt32BigEndian(result.AsSpan(5, 4), reqId);
        payload.CopyTo(result.AsSpan(9));
        return result;
    }

    /// <summary>
    /// 解析贴吧 WebSocket 协议包，处理解密与解压
    /// </summary>
    internal (byte[] data, int cmd, int reqId) ParseWsBytes(byte[] data)
    {
        var flag = data[0];
        var cmd = BinaryPrimitives.ReadInt32BigEndian(data.AsSpan(1, 4));
        var reqId = BinaryPrimitives.ReadInt32BigEndian(data.AsSpan(5, 4));

        var payload = data[9..];
        // 处理加密
        if ((flag & 0x80) != 0 && Account != null)
            payload = Account.AesEcbCipher.DecryptEcb(payload, PaddingMode.PKCS7);

        // 处理 GZip 压缩
        if ((flag & 0x40) != 0)
        {
            using var ms = new MemoryStream(payload);
            using var gzip = new GZipStream(ms, CompressionMode.Decompress);
            using var outMs = new MemoryStream();
            gzip.CopyTo(outMs);
            payload = outMs.ToArray();
        }

        return (payload, cmd, reqId);
    }

    /// <summary>
    /// 监听消息推送流
    /// </summary>
    public async IAsyncEnumerable<WSRes> ListenAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        while (await _messageChannel.Reader.WaitToReadAsync(cancellationToken))
        while (_messageChannel.Reader.TryRead(out var res))
            yield return res;
    }

    /// <summary>
    /// 优雅关闭连接
    /// </summary>
    public async Task CloseAsync(CancellationToken cancellationToken = default)
    {
        if (_ws.State == WebSocketState.Open)
            await _ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", cancellationToken);

        _cts.Cancel();
        if (_listenTask != null) await _listenTask;
        if (_heartbeatTask != null) await _heartbeatTask;
    }

    /// <summary>
    /// 打包 1001 客户端信息更新包（RSA 加密 AES 密钥）
    /// </summary>
    private byte[] PackUpdateClientInfo(Account account)
    {
        var device = new JObject
        {
            ["cuid"] = account.Cuid,
            ["_client_version"] = Const.MainVersion,
            ["_msg_status"] = "1",
            ["cuid_galaxy2"] = account.CuidGalaxy2,
            ["_client_type"] = "2",
            ["timestamp"] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString()
        };

        using var rsa = RSA.Create();
        rsa.ImportSubjectPublicKeyInfo(Convert.FromBase64String(RsaPublicKey), out _);
        var secretKey = rsa.Encrypt(account.AesEcbSecKey, RSAEncryptionPadding.Pkcs1);

        using var ms = new MemoryStream();
        using var cos = new CodedOutputStream(ms);

        using var innerMs = new MemoryStream();
        using var innerCos = new CodedOutputStream(innerMs);
        innerCos.WriteRawTag(0x0A);
        innerCos.WriteString(account.Bduss);
        innerCos.WriteRawTag(0x12);
        innerCos.WriteString(device.ToString(Formatting.None));
        innerCos.WriteRawTag(0x1A);
        innerCos.WriteBytes(ByteString.CopyFrom(secretKey));
        innerCos.WriteRawTag(0x62);
        innerCos.WriteString(account.Stoken);
        innerCos.Flush();
        var innerData = innerMs.ToArray();

        cos.WriteRawTag(0x0A);
        cos.WriteString($"{account.Cuid}|com.baidu.tieba{Const.MainVersion}");
        cos.WriteRawTag(0x12);
        cos.WriteBytes(ByteString.CopyFrom(innerData));
        cos.Flush();

        return ms.ToArray();
    }

    /// <summary>
    /// 销毁对象，关闭连接并释放资源
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;
        _cts.Cancel();
        _cts.Dispose();
        _ws.Dispose();
        _connectLock.Dispose();
        _sendLock.Dispose();
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
