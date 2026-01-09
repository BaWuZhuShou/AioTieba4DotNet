using AioTieba4DotNet.Abstractions;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using AioTieba4DotNet.Core;
using Newtonsoft.Json.Linq;

namespace AioTieba4DotNet.Api.InitZId;

public class InitZId(ITiebaHttpCore httpCore) : BaseApiRequest<string>
{
    private const string AppKey = "200033"; // Get by p/5/aio
    private const string SecKey = "ea737e4f435b53786043369d2e5ace4f";


    public override async Task<string> RequestAsync()
    {
        var account = httpCore.Account!;
        var xyus = GetMd5Hash(account.AndroidId + account.Uuid);
        var xyusMd5Str = GetMd5Hash(xyus).ToLower();
        var currentTs = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        var reqBody = "{\"module_section\":[{\"zid\":\"" + xyus + "\"}]}";
        var reqBodyBytes = Encoding.UTF8.GetBytes(reqBody);
        var reqBodyCompressed = Compress(reqBodyBytes);
        var padding = Utils.ApplyPkcs7Padding(reqBodyCompressed, 32);
        var cryptoTransform = account.AesCbcCipher!.CreateEncryptor(account.AesCbcCipher.Key, account.AesCbcCipher.IV);
        var reqBodyAes = cryptoTransform.TransformFinalBlock(padding, 0, padding.Length);
        var reqBodyMd5 = MD5.HashData(reqBodyCompressed);
        var payload = new byte[reqBodyAes.Length + reqBodyMd5.Length];
        Buffer.BlockCopy(reqBodyAes, 0, payload, 0, reqBodyAes.Length);
        Buffer.BlockCopy(reqBodyMd5, 0, payload, reqBodyAes.Length, reqBodyMd5.Length);
        var pathCombine = AppKey + currentTs + SecKey;
        var pathCombineMd5 = GetMd5Hash(pathCombine);
        var rc4 = new Rc4(Encoding.UTF8.GetBytes(xyusMd5Str));
        var reqQuerySkey = Convert.ToBase64String(rc4.Crypt(account.AesCbcSecKey));
        var uri = new UriBuilder("https", Const.SofireHost, 443, $"/c/11/z/100/{AppKey}/{currentTs}/{pathCombineMd5}"
        ) { Query = $"skey={HttpUtility.UrlEncode(reqQuerySkey)}" }.Uri;
        var request = new HttpRequestMessage(HttpMethod.Post, uri);
        request.Headers.Add("x-device-id", xyusMd5Str);
        request.Headers.TryAddWithoutValidation("User-Agent", $"x6/{AppKey}/{Const.MainVersion}/4.4.1.3");
        request.Headers.Add("x-plu-ver", "x6/4.4.1.3");
        request.Content = new ByteArrayContent(payload);
        var httpResponseMessage = await httpCore.HttpClient.SendAsync(request);
        var result = await httpResponseMessage.Content.ReadAsStringAsync();
        var token = ParseBody(result);
        return token;
    }

    public override string ParseBody(string body)
    {
        var account = httpCore.Account!;
        var xyus = GetMd5Hash(account.AndroidId + account.Uuid);
        var xyusMd5Str = GetMd5Hash(xyus).ToLower();
        var resJson = JObject.Parse(body);
        var resQuerySkey = Convert.FromBase64String(resJson.GetValue("skey")!.Value<string>()!);
        var rc4 = new Rc4(Encoding.UTF8.GetBytes(xyusMd5Str));
        var resAesSecKey = rc4.Crypt(resQuerySkey);
        var resData = Convert.FromBase64String(resJson.GetValue("data")!.Value<string>()!);
        var aes = Aes.Create();
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.None;
        aes.Key = resAesSecKey;
        aes.IV = new byte[16];
        var cryptoTransform = aes.CreateDecryptor(aes.Key, aes.IV);
        var decryptedResData = cryptoTransform.TransformFinalBlock(resData, 0, resData.Length);
        decryptedResData = decryptedResData.Take(decryptedResData.Length - 16).ToArray();
        var removePkcs7Padding = Utils.RemovePkcs7Padding(decryptedResData, 32);
        var format = Encoding.UTF8.GetString(removePkcs7Padding);
        var formatJson = JObject.Parse(format);

        return formatJson.GetValue("token")!.Value<string>()!;
    }


    public static string GetMd5Hash(string input)
    {
        var data = MD5.HashData(Encoding.UTF8.GetBytes(input));

        return BitConverter.ToString(data).Replace("-", string.Empty);
    }

    public static byte[] Compress(byte[] data)
    {
        using var compressedStream = new MemoryStream();
        using var zipStream = new GZipStream(compressedStream, CompressionMode.Compress);
        zipStream.Write(data, 0, data.Length);
        zipStream.Close();
        var compress = compressedStream.ToArray();
        return compress;
    }
}

