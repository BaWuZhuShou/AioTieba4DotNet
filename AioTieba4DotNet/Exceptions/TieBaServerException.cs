namespace AioTieba4DotNet.Exceptions;

public class TieBaServerException(int code, string msg) : Exception($"Code: {code}, Message: {msg}")
{
    public int Code { get; } = code;
}
