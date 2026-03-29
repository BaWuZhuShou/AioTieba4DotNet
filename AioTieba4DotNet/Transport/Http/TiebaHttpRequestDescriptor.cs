namespace AioTieba4DotNet.Transport.Http;

internal sealed record TiebaHttpRequestDescriptor
{
    private TiebaHttpRequestDescriptor(TiebaHttpRequestKind kind, Uri uri,
        IReadOnlyList<KeyValuePair<string, string>>? formData = null,
        byte[]? protoPayload = null,
        bool allowRetry = false)
    {
        Kind = kind;
        Uri = uri;
        FormData = formData;
        ProtoPayload = protoPayload;
        AllowRetry = allowRetry;
    }

    internal TiebaHttpRequestKind Kind { get; }

    internal Uri Uri { get; }

    internal IReadOnlyList<KeyValuePair<string, string>>? FormData { get; }

    internal byte[]? ProtoPayload { get; }

    internal bool AllowRetry { get; }

    internal static TiebaHttpRequestDescriptor AppForm(Uri uri, IReadOnlyList<KeyValuePair<string, string>> formData) =>
        new(TiebaHttpRequestKind.AppForm, uri, formData.ToArray());

    internal static TiebaHttpRequestDescriptor AppProto(Uri uri, byte[] protoPayload) =>
        new(TiebaHttpRequestKind.AppProto, uri, protoPayload: protoPayload.ToArray());

    internal static TiebaHttpRequestDescriptor WebGet(Uri uri, IReadOnlyList<KeyValuePair<string, string>> formData,
        bool allowRetry) =>
        new(TiebaHttpRequestKind.WebGet, uri, formData.ToArray(), allowRetry: allowRetry);

    internal static TiebaHttpRequestDescriptor WebForm(Uri uri, IReadOnlyList<KeyValuePair<string, string>> formData) =>
        new(TiebaHttpRequestKind.WebForm, uri, formData.ToArray());
}
