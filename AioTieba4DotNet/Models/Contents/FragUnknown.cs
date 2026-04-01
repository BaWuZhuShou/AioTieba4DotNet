namespace AioTieba4DotNet.Models.Contents;

public sealed class FragUnknown : IFrag
{
    public string Type { get; init; } = string.Empty;

    public override string Text { get; init; } = string.Empty;

    public override string GetFragType() => nameof(FragUnknown);

    public override Dictionary<string, object> ToDict() => new()
    {
        ["type"] = Type,
        ["text"] = Text
    };

    public override string ToString() => $"{GetFragType()} {nameof(Type)}: {Type}, {nameof(Text)}: {Text}";
}
